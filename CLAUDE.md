# CLAUDE.md — Collecta.API Backend

## Project Info

- **Tech Stack**: ASP.NET Core 10 + Entity Framework Core + SQLite
- **Architecture**: Clean Architecture (Domain → Application → Infrastructure → API)
- **Database**: SQLite (`collecta.db`)

---

## ⚠️ CRITICAL RULES — Database Migrations

### Migration Workflow

**Migrations are ONLY done on the `db-migrations` branch.**

When working with migrations:
1. ✅ **ALLOWED**: `dotnet ef migrations add <MigrationName>`
   - Creates a migration file in `Infrastructure/Migrations/`
   - Commits to `db-migrations` branch
   - Creates a PR to `main` for review

2. ❌ **FORBIDDEN**: `dotnet ef database update`
   - NEVER run this command
   - Database updates happen only after PR approval on `main`
   - This is enforced to prevent accidental DB changes during development

3. 🔄 **Process**:
   - Create migration on `db-migrations`
   - Create PR to `main` (migrations only, no other code)
   - Wait for approval
   - Merge to `main` (CI/CD runs `dotnet ef database update`)

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
