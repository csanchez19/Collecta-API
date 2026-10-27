# CLAUDE.md — Collecta.API

Backend for the Collecta mobile app (frontend lives in the sibling `Collecta/`
repo). ASP.NET Core 10, EF Core 10 + SQLite, JWT auth. The project lives in
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

## Branches

- `main` — releases.
- `develop` — integration branch; all work starts from here.
- `feature/<nombre-desarrollo>` — every new development (kebab-case).
- `bugfix/<nombre-del-bug>` — every bug fix.
- `db-migrations` — schema changes only (see rule 3).

Before starting a task, check the current branch and propose the right branch.

## Running locally

```bash
cd Collecta.API
dotnet run                 # http://localhost:5215, docs at /scalar
dotnet build
dotnet ef migrations add <Name>   # db-migrations branch only
```

`dotnet-ef` is a global tool in `~/.dotnet/tools` (must be on PATH).
`collecta.db` is local and git-ignored.

## Configuration

- `Jwt:Secret` lives in **user-secrets** (`dotnet user-secrets list`), never in
  `appsettings*.json`. `Jwt:Issuer` / `Jwt:Audience` are in `appsettings.json`.
- CORS origins come from `Cors:AllowedOrigins`; Development allows the Angular
  dev server (4200), `ionic serve` (8100) and Capacitor (`capacitor://localhost`,
  `http://localhost`).
- HTTPS redirection is only enabled outside Development.

## Data model

The catalog is shared; ownership is per user. Never store per-user values on
catalog rows.

- `Collection` — catalog theme published by admins (name, `CollectionKind`
  enum stored as text, kind label, accent hue).
- `CatalogItem` — piece in a collection; market value in cents (`long`)
  because SQLite cannot sort/aggregate `decimal`.
- `UserItem` — "user owns this item" (composite key user + item, quantity).
- `FavoriteCollection` — user favourites (composite key).
- `RefreshToken` — stores only the SHA-256 hash of the token.

`ownedCount`, `totalCount`, `mine` and `popularity` that the frontend shows are
computed in queries, not stored. IDs are `Guid` (`Guid.CreateVersion7()`).
Migrations are generated into `Collecta.API/Migrations/`.
