using System.Diagnostics;
using Collecta.API.Infrastructure.Data;
using Collecta.Importer;
using Collecta.Importer.Import;
using Collecta.Importer.Tcgdex;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

// Imports an external catalog into Collecta's database. Run by hand (never at
// API startup); safe to repeat: rows are upserted, never deleted.

var options = ImportOptions.Parse(args);
if (options is null)
{
    Console.Error.WriteLine(ImportOptions.Usage);
    return 1;
}

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<ImportOptions>(optional: true)
    .AddEnvironmentVariables()
    .Build();
var connectionString = options.ConnectionString ?? configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Falta la cadena de conexión: usa --connection o configura ConnectionStrings:DefaultConnection (ver CLAUDE.md).");
    return 1;
}

var target = new NpgsqlConnectionStringBuilder(connectionString);
Console.WriteLine($"Destino: {target.Host}:{target.Port}/{target.Database}");

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    // First Ctrl+C stops after the current request; finished sets are already saved.
    e.Cancel = true;
    cancellation.Cancel();
    Console.WriteLine("Cancelando… los sets ya terminados quedan guardados.");
};

using var http = new HttpClient { BaseAddress = TcgdexClient.BaseAddress, Timeout = TimeSpan.FromSeconds(30) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("Collecta-Importer/1.0");

var source = new TcgdexCatalogSource(new TcgdexClient(http), options.Language, options.Concurrency);
var dbOptions = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options;
var importer = new CatalogImporter(() => new AppDbContext(dbOptions), Console.Out);
var run = new ImportRun(options.SetKeys, options.PricesOnly, options.SkipComplete);

var clock = Stopwatch.StartNew();
try
{
    var summary = await importer.RunAsync(source, run, cancellation.Token);
    Console.WriteLine(
        $"Hecho en {clock.Elapsed:hh\\:mm\\:ss}: {summary.Sets} sets, {summary.Added} piezas nuevas, " +
        $"{summary.Updated} actualizadas, {summary.Skipped} sets saltados.");
    return 0;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Importación cancelada. Vuelve a lanzarla con --skip-complete para continuar.");
    return 130;
}
