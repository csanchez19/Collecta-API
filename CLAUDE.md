# CLAUDE.md — Collecta.API

Backend for the Collecta mobile app (frontend lives in the sibling `Collecta/`
repo). ASP.NET Core 10, EF Core 10 + PostgreSQL (Npgsql), JWT auth. The project lives in
`Collecta.API/`; run every `dotnet` command from there.

## Hard rules

1. **Claude never commits, merges, pushes, resets or rebases.** Carles does all
   git history changes. Claude may create/switch branches on a clean tree and
   edit files; anything else in git needs Carles's explicit OK.
2. **Claude never runs `dotnet ef database update`** (nor `migrations remove
   --force`, nor `Database.Migrate()` at startup — same effect). Carles applies
   migrations himself.
3. **Schema work only on `db-migrations`.** Entities (`Domain/`),
   `AppDbContext` and `dotnet ef migrations add <Name>` happen only on that
   branch. Claude may run `migrations add`; Carles commits and applies it.
4. **Claude never runs the importer against `collecta`** (nor seeds data any
   other way). Carles loads data. Claude tests imports only on a throwaway
   database (`--connection` pointing at it) and drops it afterwards.

## Branches

- `main` — releases.
- `develop` — integration branch; all work starts from here.
- `feature/<nombre-desarrollo>` — every new development (kebab-case).
- `bugfix/<nombre-del-bug>` — every bug fix.
- `db-migrations` — schema changes only (see rule 3).

Before starting a task, check the current branch and propose the right branch.

## Running locally

```bash
docker compose up -d       # repo root: PostgreSQL 18 on localhost:5432 (container collecta-db)
cd Collecta.API
dotnet run                 # http://localhost:5215, docs at /scalar
dotnet build
dotnet ef migrations add <Name>   # db-migrations branch only
```

`dotnet-ef` is a global tool in `~/.dotnet/tools` (must be on PATH).

First-time setup: copy `.env.example` to `.env` (git-ignored) and choose
`POSTGRES_PASSWORD`; then store the matching connection string as a user-secret:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=collecta;Username=collecta;Password=<same as .env>"
```

Claude tests migrations only against a throwaway database in the same container
(`createdb` + the script from `dotnet ef migrations script`), never `collecta`.

## Catalog importer (`Collecta.Importer/`)

Console app that loads external catalogs into the database. Run by hand from
the repo root; it reuses the API's `AppDbContext` and reads the same
`ConnectionStrings:DefaultConnection` user-secret (or `--connection`).

```bash
dotnet run --project Collecta.Importer -- tcgdex --lang en                 # whole catalog
dotnet run --project Collecta.Importer -- tcgdex --lang en --set sv03.5    # one set (repeatable)
dotnet run --project Collecta.Importer -- tcgdex --lang en --skip-complete # resume a cut-short run
dotnet run --project Collecta.Importer -- tcgdex --lang en --prices-only   # refresh market values
```

- **Each language is its own catalog.** `--lang en` fills the collection
  "Pokémon TCG (EN)" (`ExternalId` `pokemon:en`); sets and items get
  language-prefixed ids (`en:sv03.5`, `en:sv03.5-006`). An English and a
  Spanish card are different items; owning one says nothing about the other.
  Adding a language is only a new `--lang`.
- **Layers:** `Import/ICatalogSource` (a source lists sets and loads one set
  as source-neutral `Imported*` records) → `Import/CatalogImporter` (generic
  upsert by `Source` + `ExternalId`, one transaction per set, never deletes).
  `Tcgdex/` is the only source today; a new game or API is a new source class.
- Collection name/label/hue are only set on creation (admins may edit them).
- TCGdex: Cardmarket `trend` price in EUR (falls back to `avg`); images stored
  as URLs (`…/high.webp`, `…/low.webp`); set logo/symbol stored only if the
  CDN actually serves them. TCGdex returns slightly different prices between
  calls, so re-runs report some "actualizadas" even within minutes.
- Every run (including `--prices-only`) records today's price of each item in
  `PriceSnapshots` (one row per item and day); run it periodically and the
  price chart fills up.

## API

Features live in `Collecta.API/Features/<Feature>/` (controller + service +
contracts). Try every endpoint from `Collecta.API.http`.

- **Public catalog** (`[AllowAnonymous]`): `GET /api/collections[/{id}]`,
  `/api/collections/{id}/sets`, `/api/sets/{id}`, `/api/sets/{id}/items` (whole
  set in album order — `ItemQueries.InAlbumOrder`), `/api/items/{id}`,
  `/api/items/{id}/prices?range=7d|30d|1y`. A signed-in caller also gets their
  own numbers (owned copies, wishlist, favourites); services take
  `User.FindUserId()` and use `Guid.Empty` (matches no rows) for anonymous.
- **`/api/me/*`** (`[Authorize]`): `PUT items/{id}` `{copies}` (0 removes;
  owning drops the wish), `GET items/recent`, `GET|PUT|DELETE wishlist[/{id}]`
  (409 if already owned), `PUT|DELETE favorites/{collectionId}`,
  `GET progression`.
- `RejectInvalidTokenFilter` (global): a request that *sends* a token must send
  a valid one, even on public endpoints — an expired token gets 401 (so the app
  refreshes) instead of silently being treated as anonymous.
- Every item list shares one projection (`ItemQueries.ForUser` →
  `ItemResponse`); add fields there, not per endpoint. `RarityTier` (0–3)
  collapses source rarity names for clients.
- Progression is derived, never stored: `ProgressionService.XpPerItem` (40 per
  distinct owned item, keep in sync with the frontend's `XP_PER_NEW_ITEM`),
  levels of 250 XP, achievements computed from ownership (pages are 9 items in
  album order).

## Configuration

- `Jwt:Secret` and `ConnectionStrings:DefaultConnection` live in
  **user-secrets** (`dotnet user-secrets list`), never in `appsettings*.json`;
  outside Development they come from environment variables
  (`ConnectionStrings__DefaultConnection`). `Jwt:Issuer` / `Jwt:Audience` are in
  `appsettings.json`. The app refuses to start without a connection string.
- CORS origins come from `Cors:AllowedOrigins`; Development allows the Angular
  dev server (4200), `ionic serve` (8100) and Capacitor (`capacitor://localhost`,
  `http://localhost`).
- HTTPS redirection is only enabled outside Development.

## Data model

The catalog is shared; ownership is per user. Never store per-user values on
catalog rows.

- `Collection` — catalog theme published by admins (name, `CollectionKind`
  enum stored as text, kind label, accent hue).
- `CollectionSet` — groups the items of a collection (a Pokémon expansion, a
  Hot Wheels series…): series, release date, logo/symbol URLs.
- `CatalogItem` — piece in a set; market value in whole cents (`long`);
  kind-specific details in `Attributes` (`jsonb`).
- Catalog rows carry `Source` + `ExternalId` (unique together) so importers
  upsert instead of duplicating; hand-made rows leave both null.
- Languages are not a column: a catalog in another language is another
  collection with its own sets and items (see the importer section).
- `UserItem` — "user owns this item" (composite key user + item, quantity).
- `FavoriteCollection` — user favourites (composite key).
- `WishlistItem` — "user is looking for this item" (composite key user + item).
- `PriceSnapshot` — an item's market value on one day (key item + date) for
  the price chart; importers upsert one per item and day. The current price
  stays on `CatalogItem.MarketValueCents`.
- `RefreshToken` — stores only the SHA-256 hash of the token.

`ownedCount`, `totalCount`, `mine` and `popularity` that the frontend shows are
computed in queries, not stored. IDs are `Guid` (`Guid.CreateVersion7()`).
Migrations are generated into `Collecta.API/Migrations/`.
