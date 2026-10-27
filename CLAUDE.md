# CLAUDE.md — Collecta.API Backend

## Project Info

- **Tech Stack**: ASP.NET Core 10 + Entity Framework Core + SQLite
- **Architecture**: Clean Architecture (Domain → Application → Infrastructure → API)
- **Database**: SQLite (`collecta.db`)

---

## ⚠️ CRITICAL RULES — Git & Database Work

### Git Commits — CARLES ONLY
**Claude NEVER commits code.** Only Carles makes commits.
- Claude writes/edits files
- Carles reviews and commits
- All git commands require explicit approval from Carles

### Domain Models & Migrations — db-migrations Branch ONLY
**All work on entities and migrations happens ONLY on `db-migrations` branch.**

Development workflow:
1. ✅ **ON db-migrations**: Write entities (Domain/), create DbContext
2. ✅ **ON db-migrations**: `dotnet ef migrations add <MigrationName>`
3. ❌ **FORBIDDEN**: `dotnet ef database update` — Claude NEVER runs this
4. ❌ **NO entities in main** — main stays clean, only features go there
5. 🔄 Carles reviews and handles PR/merge to main after migrations work

When adding new entities:
- Create in `Domain/`
- Update `AppDbContext.cs` with DbContext config
- Generate migration with `dotnet ef migrations add <Name>`
- Leave `dotnet ef database update` for Carles (CI/CD or manual)

---

## Project Structure

```
Collecta.API/
├── Domain/              (Business entities, no dependencies)
├── Application/         (Services, DTOs, business logic)
├── Infrastructure/      (EF Core, DbContext, repositories)
├── API/                 (Controllers, middleware)
├── appsettings.json     (JWT secret, DB connection)
└── Program.cs           (DI configuration, CORS, auth)
```

---

## Getting Started

```bash
cd Collecta-API/Collecta.API
dotnet run                    # Start API at https://localhost:5001
dotnet build                  # Build project
dotnet ef migrations add Initial   # Create migration (db-migrations branch ONLY)
```

---

## Authentication

- **Type**: JWT Bearer tokens
- **Secret**: Configured in `appsettings.json` (`Jwt:Secret`)
- **Tokens**: 15-minute access token + refresh token
- **Password**: Hashed with BCrypt

---

## Endpoints (Phase 1)

```
POST   /api/auth/register
POST   /api/auth/login
GET    /api/collections
GET    /api/collections/{id}
GET    /api/collections/{collectionId}/items
GET    /api/items/{id}
POST   /api/favorites/{collectionId}
DELETE /api/favorites/{collectionId}
```
