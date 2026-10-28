namespace Collecta.Importer;

/// <summary>Command line of the importer. Kept dependency-free on purpose: a handful of flags.</summary>
internal sealed record ImportOptions(
    string Source,
    string Language,
    IReadOnlySet<string> SetKeys,
    bool PricesOnly,
    bool SkipComplete,
    int Concurrency,
    string? ConnectionString)
{
    public const string Usage = """
        Uso: dotnet run --project Collecta.Importer -- tcgdex [opciones]

          --lang <código>        Idioma del catálogo (en, es, fr, de, it, pt, ja…). Por defecto: en.
                                 Cada idioma es una colección distinta.
          --set <id>             Importa solo ese set (id de TCGdex, p. ej. sv03.5). Se puede repetir.
          --prices-only          Solo actualiza los precios de lo ya importado.
          --skip-complete        Salta los sets que ya tienen todas sus piezas (para retomar).
          --concurrency <n>      Peticiones a TCGdex en paralelo. Por defecto: 6.
          --connection <cadena>  Base de datos destino. Por defecto: ConnectionStrings:DefaultConnection
                                 de user-secrets o de la variable ConnectionStrings__DefaultConnection.
        """;

    /// <returns>The options, or null when the command line is not valid.</returns>
    public static ImportOptions? Parse(string[] args)
    {
        if (args.Length == 0 || args[0] != "tcgdex")
        {
            return null;
        }

        var language = "en";
        var sets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pricesOnly = false;
        var skipComplete = false;
        var concurrency = 6;
        string? connection = null;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--lang" when i + 1 < args.Length:
                    language = args[++i].Trim().ToLowerInvariant();
                    break;
                case "--set" when i + 1 < args.Length:
                    sets.Add(args[++i]);
                    break;
                case "--prices-only":
                    pricesOnly = true;
                    break;
                case "--skip-complete":
                    skipComplete = true;
                    break;
                case "--concurrency" when i + 1 < args.Length && int.TryParse(args[i + 1], out var n) && n is >= 1 and <= 16:
                    concurrency = n;
                    i++;
                    break;
                case "--connection" when i + 1 < args.Length:
                    connection = args[++i];
                    break;
                default:
                    return null;
            }
        }

        return language.Length is >= 2 and <= 8
            ? new ImportOptions(args[0], language, sets, pricesOnly, skipComplete, concurrency, connection)
            : null;
    }
}
