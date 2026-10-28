using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Collecta.Importer.Tcgdex;

/// <summary>
/// Thin client for TCGdex's REST API. Retries rate limits (429), server errors
/// and timeouts with backoff, so a long import survives hiccups.
/// </summary>
internal sealed class TcgdexClient(HttpClient http)
{
    public static readonly Uri BaseAddress = new("https://api.tcgdex.net/v2/");

    private const int MaxAttempts = 5;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public async Task<IReadOnlyList<TcgdexSetBrief>> GetSetsAsync(string language, CancellationToken ct) =>
        await GetAsync<List<TcgdexSetBrief>>($"{Segment(language)}/sets", ct) ?? [];

    public Task<TcgdexSet?> GetSetAsync(string language, string setId, CancellationToken ct) =>
        GetAsync<TcgdexSet>($"{Segment(language)}/sets/{Segment(setId)}", ct);

    public Task<TcgdexCard?> GetCardAsync(string language, string cardId, CancellationToken ct) =>
        GetAsync<TcgdexCard>($"{Segment(language)}/cards/{Segment(cardId)}", ct);

    /// <summary>
    /// True when an asset URL actually serves a file. TCGdex sometimes lists
    /// assets its CDN does not have (set symbols, at the time of writing).
    /// </summary>
    public async Task<bool> AssetExistsAsync(string url, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await http.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    /// <returns>The parsed body, or null when TCGdex answers 404.</returns>
    private async Task<T?> GetAsync<T>(string path, CancellationToken ct) where T : class
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var response = await http.GetAsync(path, ct);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                if (IsTransient(response.StatusCode) && attempt < MaxAttempts)
                {
                    await Task.Delay(RetryDelay(response, attempt), ct);
                    continue;
                }
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>(Json, ct);
            }
            catch (Exception ex) when (attempt < MaxAttempts && !ct.IsCancellationRequested
                                       && ex is HttpRequestException or TaskCanceledException)
            {
                await Task.Delay(Backoff(attempt), ct);
            }
        }
    }

    private static bool IsTransient(HttpStatusCode status) =>
        status == HttpStatusCode.TooManyRequests || (int)status >= 500;

    private static TimeSpan RetryDelay(HttpResponseMessage response, int attempt) =>
        response.Headers.RetryAfter?.Delta ?? Backoff(attempt);

    private static TimeSpan Backoff(int attempt) => TimeSpan.FromSeconds(Math.Pow(2, attempt));

    private static string Segment(string value) => Uri.EscapeDataString(value);
}
