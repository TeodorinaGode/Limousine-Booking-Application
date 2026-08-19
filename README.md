# Limousine Booking Application

A limousine booking platform. Customers submit bookings as guests (no account required); drivers and administrators authenticate to manage the business.

This is the initial foundation: project structure, tooling, and a working "hello world" slice through the whole stack. Business functionality (bookings, authentication flows, driver/admin features) will be added in later steps.

## Tech Stack

**Backend:** ASP.NET Core Web API (.NET 8), Entity Framework Core, PostgreSQL, JWT authentication, Swagger/OpenAPI
**Frontend:** React, TypeScript, Vite, React Router
**Infrastructure:** Docker, Docker Compose

## Project Structure

```
limousine-booking/
├── backend/
│   ├── src/
│   │   ├── LimousineBooking.Api/             # Controllers, DI/auth/Swagger config
│   │   ├── LimousineBooking.Application/     # Use cases, DTOs, interfaces, validators
│   │   ├── LimousineBooking.Domain/          # Entities, enums, value objects, core rules
│   │   ├── LimousineBooking.Infrastructure/  # EF Core, PostgreSQL, repositories, auth
│   │   └── LimousineBooking.Tests/           # xUnit test project
│   ├── LimousineBooking.sln
│   └── Dockerfile
├── frontend/
│   ├── src/
│   │   ├── components/, layouts/, hooks/, services/, types/, utils/, routes/
│   │   └── pages/{public,auth,driver,admin}/
│   └── Dockerfile
├── docker-compose.yml
└── README.md
```

Clean Architecture is used on the backend: `Domain` has no dependencies, `Application` depends only on `Domain`, `Infrastructure` implements `Application`'s interfaces (EF Core / PostgreSQL specifics live here), and `Api` wires everything together.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) and npm
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the containerized workflow)

## Configuration

Copy the example env files and fill in real values (never commit the real `.env` files):

```
cp .env.example .env
cp backend/.env.example backend/.env
cp frontend/.env.example frontend/.env
```

- Root `.env` – used by `docker-compose.yml` (Postgres credentials, JWT settings).
- `backend/.env` – used when running the API directly with `dotnet run` (not read automatically by ASP.NET Core; export the values as environment variables, or use `dotnet user-secrets`, before running).
- `frontend/.env` – used by Vite (`VITE_API_BASE_URL`).

## Running Everything with Docker Compose

From the repository root, with a `.env` file in place:

```
docker compose up --build
```

This starts:
- PostgreSQL on `localhost:5432` (data persisted in the `postgres_data` volume)
- Backend API on `http://localhost:5000` (Swagger UI at `http://localhost:5000/swagger`)
- Frontend dev server on `http://localhost:5173`

## Running the Backend Locally (without Docker)

```
cd backend
dotnet restore
dotnet build

# Set required configuration (PowerShell example):
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=limousine_booking;Username=postgres;Password=change-me"
$env:Jwt__Issuer = "LimousineBooking"
$env:Jwt__Audience = "LimousineBooking"
$env:Jwt__SecretKey = "replace-with-a-long-random-secret"

dotnet run --project src/LimousineBooking.Api
```

The API listens on the port shown in the console (see `src/LimousineBooking.Api/Properties/launchSettings.json`). Swagger UI is available at `/swagger` in the Development environment. `GET /api/health` returns `{ "status": "ok" }`.

You'll need a PostgreSQL instance reachable at the connection string above — either run `docker compose up postgres` or point at a local install. In Development, on startup the API also seeds two dev-only login accounts (Administrator/Driver — see **Development Credentials** below) if PostgreSQL is reachable; if it isn't, seeding is skipped with a logged warning rather than crashing the app.

### Applying EF Core Migrations

An initial (empty) migration is already included to validate the migration infrastructure. EF Core tooling is installed as a local tool (`backend/.config/dotnet-tools.json`):

```
cd backend
dotnet tool restore
dotnet ef database update --project src/LimousineBooking.Infrastructure --startup-project src/LimousineBooking.Api
```

## Running the Frontend Locally (without Docker)

```
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`. Routes: `/` (public), `/login`, `/driver` (Driver-only), `/admin` (Administrator-only), `/admin/routes` (route management), `/admin/vehicles` (vehicle management), `/admin/drivers` (driver management), `/unauthorized`.

## Running Without Docker, Against a Real PostgreSQL

If Docker isn't available, PostgreSQL 16 can be installed directly (Windows example):

```powershell
winget install --id PostgreSQL.PostgreSQL.16 --accept-package-agreements --accept-source-agreements --silent
```

This installs and starts `postgresql-x64-16` as a Windows service (default superuser `postgres` / `postgres`) — it will keep running in the background afterward, same as on any machine with Postgres installed. Then:

```powershell
$env:PGPASSWORD = "postgres"
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -p 5432 -U postgres -c "CREATE DATABASE limousine_booking;"
```

...and run the backend as described above (`ConnectionStrings__DefaultConnection` pointing at `Password=postgres`) followed by `dotnet ef database update`. This is exactly how Prompts 5 and 6 were verified end-to-end in the environment this was built in.

## API Endpoints

| Endpoint | Auth | Description |
|---|---|---|
| `GET /api/health` | none | Health check |
| `POST /api/auth/login` | none | `{ email, password }` → `{ accessToken, expiresAt, user }` |
| `GET /api/test/authenticated` | any authenticated user | Dev-only: verifies a token is accepted |
| `GET /api/test/admin` | `Administrator` | Dev-only: verifies role-based authorization |
| `GET /api/test/driver` | `Driver` | Dev-only: verifies role-based authorization |
| `GET /api/admin/routes` | `Administrator` | List routes. Query: `search`, `isActive`, `sortBy` (`departure`\|`destination`\|`duration`\|`price`\|`status`\|`createdAt`), `sortDirection` (`asc`\|`desc`), `page`, `pageSize` (max 100) |
| `GET /api/admin/routes/{id}` | `Administrator` | Get one route, or `404` |
| `POST /api/admin/routes` | `Administrator` | Create a route (active by default). `409` on duplicate active departure+destination |
| `PUT /api/admin/routes/{id}` | `Administrator` | Full update, including `isActive` |
| `PUT /api/admin/routes/{id}/activate` | `Administrator` | Convenience toggle; also blocked by `409` if it would recreate a duplicate |
| `PUT /api/admin/routes/{id}/deactivate` | `Administrator` | Convenience toggle. Never deletes the route |
| `GET /api/admin/vehicles` | `Administrator` | List vehicles. Query: `search`, `isActive`, `minCapacity`, `sortBy` (`registrationNumber`\|`make`\|`model`\|`passengerCapacity`\|`createdAt`), `sortDirection`, `page`, `pageSize` (max 100) |
| `GET /api/admin/vehicles/{id}` | `Administrator` | Get one vehicle, or `404` |
| `POST /api/admin/vehicles` | `Administrator` | Create a vehicle (active by default). `409` on duplicate registration number (global, not just active vehicles) |
| `PUT /api/admin/vehicles/{id}` | `Administrator` | Full update, including `isActive` |
| `PUT /api/admin/vehicles/{id}/activate` | `Administrator` | Convenience toggle |
| `PUT /api/admin/vehicles/{id}/deactivate` | `Administrator` | Convenience toggle. Never deletes the vehicle |
| `GET /api/admin/drivers` | `Administrator` | List drivers. Query: `search`, `isActive`, `isAvailable`, `hasVehicle`, `sortBy` (`firstName`\|`lastName`\|`email`\|`createdAt`), `sortDirection`, `page`, `pageSize` (max 100) |
| `GET /api/admin/drivers/{id}` | `Administrator` | Get one driver (+ user info + current vehicle), or `404` |
| `POST /api/admin/drivers` | `Administrator` | Creates the linked User (Role=Driver, password hashed) and Driver profile together. `409` on duplicate email or a vehicle already assigned elsewhere |
| `PUT /api/admin/drivers/{id}` | `Administrator` | Full update (name, email, phone, active status, vehicle). Role can never be changed here |
| `PUT /api/admin/drivers/{id}/activate` | `Administrator` | Convenience toggle — also reactivates the linked User's login |
| `PUT /api/admin/drivers/{id}/deactivate` | `Administrator` | Convenience toggle — also deactivates the linked User's login. Never deletes either record |
| `PUT /api/admin/drivers/{id}/password` | `Administrator` | `{ newPassword }` → resets the linked User's password (hashed, never returned) |

`TestController` exists purely to verify authentication/authorization end-to-end (including from Swagger); it isn't part of the product API surface and can be removed once real protected endpoints exist.

### Testing auth via Swagger

1. `POST /api/auth/login` with one of the dev credentials below.
2. Copy the `accessToken` from the response.
3. Click **Authorize** in Swagger UI and enter `Bearer <token>`.
4. Call `/api/test/authenticated`, `/api/test/admin`, or `/api/test/driver` to confirm role enforcement.

## Development Credentials

Seeded automatically on API startup **only** when `ASPNETCORE_ENVIRONMENT=Development` and PostgreSQL is reachable. Never used for production accounts, never reused as real credentials.

| Role | Email | Password |
|---|---|---|
| Administrator | `admin@example.com` | `Dev#Passw0rd!` |
| Driver | `driver@example.com` | `Dev#Passw0rd!` |

## Running Tests

```
cd backend
dotnet test
```

```
cd frontend
npm run test
```

Frontend tests use Vitest + React Testing Library.

## Assumptions and Decisions

- **Controllers over minimal APIs**: the Api project uses `--use-controllers` for a conventional, discoverable structure as the API surface grows.
- **DbContext has no entities yet** *(Prompt 1 only — superseded)*: as of Prompt 2, `ApplicationDbContext` exposes the full domain model; see the entity list under **Database changes** in the Prompt 3 notes below.
- **Frontend pinned to Vite 6 / React Router 7 / React 18** instead of the very latest majors: `npm create vite@latest` currently scaffolds Vite 8, which ships an experimental Rolldown-based bundler requiring native platform bindings that aren't available for this Node version (20.15) on Windows — `npm run build` failed outright. Started on Vite 5.4 (Prompt 1), then bumped to Vite 6.4 in Prompt 4 (alongside Vitest 3, needed for a testing setup) since Vite 6 patches known path-traversal/`fs.deny`-bypass advisories in the dev server that 5.x doesn't, while still working on this Node version — Vite 7+/8 require Node ≥20.19 and 8 has the broken bundler. React Router was still bumped to v7 (from the v6 the template would otherwise pull in) because v6 has two unpatched CVEs (open redirect, SSR deserialization); v7 does not have the Vite 8 problem.
- **Frontend test tooling**: Vitest 3 + React Testing Library + jsdom, added in Prompt 4 since no test runner existed before. `npm audit` is clean (0 vulnerabilities) after the Vite 6 bump.
- **Frontend dev container runs `vite --host` directly** rather than a multi-stage Nginx build, since this is a development Compose setup, not a production deployment config.
- **No customer-facing auth artifacts were created** (no login/register endpoints, no customer JWT) — consistent with customers remaining unauthenticated guests.
- **JWT config property names**: `Jwt:SecretKey` / `Jwt:AccessTokenExpirationMinutes` (not `Jwt:Key/ExpiryMinutes`), matching the Prompt 3 spec. Note the spec's illustrative env var casing (`JWT__SECRET_KEY`) would **not** actually bind to a `SecretKey` property under ASP.NET Core's config rules — the env var must be `Jwt__SecretKey` (double underscore as the section separator, then the exact — case-insensitive — property name with no extra underscores). `.env.example` files use the correct form.
- **JWT claims use short names** (`sub`, `email`, `role`, `name`, `driverId`) rather than the long `ClaimTypes.*` URIs, per the spec. This requires `JwtBearerOptions.MapInboundClaims = false` plus explicit `RoleClaimType = "role"` — otherwise ASP.NET Core's default inbound claim remapping breaks `[Authorize(Roles = ...)]` silently. `NameClaimType` is set to `"email"`.
- **Password hashing**: ASP.NET Core Identity's `PasswordHasher<User>` (PBKDF2), wrapped behind an `IPasswordService` interface — no custom cryptography, no plaintext password ever persisted or logged.
- **Dev user seeding runs at API startup** (Development environment only, idempotent, wrapped in try/catch so an unreachable database doesn't crash the app) rather than via an EF Core migration `HasData` — `IPasswordHasher` output is intentionally randomized per call, so a reproducible hash can't be baked into a migration the way the seed *routes* were in Prompt 2.
- **No refresh tokens / no logout endpoint**, per the spec: access-token-only, frontend just discards the token locally on logout.
- **`TestController` (`/api/test/*`) is a temporary, clearly-marked dev/QA surface** for verifying the auth pipeline — not a real product endpoint. Fine to delete once real protected endpoints exist.
- **Frontend route guards are UX-only**: `ProtectedRoute` redirects unauthenticated/wrong-role users client-side, but the backend's `[Authorize(Roles=...)]` is the actual security boundary, per the spec's explicit requirement.
- **Duplicate-route uniqueness applies only to active routes** (trimmed, case-insensitive on departure+destination), not globally — a deactivated route can coexist with a new active route covering the same city pair, which is what lets a route's price/duration be revised without losing the old record's history.
- **`Route.Update(...)` is a new domain method** (Prompt 4): Prompt 2's `Route` entity only exposed `UpdatePrice()` plus `Activate()`/`Deactivate()`; full-field admin editing needed a general update method. It reuses the same validation as the constructor (extracted into a shared private `Validate` helper) — no new columns, no migration.
- **Both `PUT /{id}` and dedicated `/activate`, `/deactivate` endpoints exist**: the full `PUT` can flip `isActive` as part of an edit, while the dedicated endpoints let the frontend toggle status with one click without resending the whole form.
- **Sorting is an explicit allow-list** (`departure`, `destination`, `duration`, `price`, `status`, `createdAt`) mapped to real columns in the repository — an unrecognized `sortBy` silently falls back to `departure` rather than erroring, since this is a list endpoint where leniency is friendlier than a 400.
- **Global exception-handling middleware was added** (`Program.cs`, non-Development only) since none existed before this prompt and the spec requires unhandled errors to return a generic `500` without stack traces in production. In Development, ASP.NET Core's built-in developer exception page (auto-enabled) still shows full details.
- **Deactivation confirmation uses the native `window.confirm()`** rather than a custom modal — satisfies "show a confirmation dialog" without extra UI code; success/error feedback uses a simple inline message with a 3s auto-dismiss rather than a toast library.
- **Vehicle registration uniqueness is global** (active AND inactive vehicles), unlike Route's active-only duplicate rule — a deliberate difference: Prompt 2 already put an unconditional unique index on `Vehicle.RegistrationNumber`, and a real license plate identifies one physical vehicle permanently (unlike a route, which is just a reusable city pair). Registration numbers are also normalized (trimmed, internal whitespace collapsed, uppercased) *before storage*, not just for comparison — the DB's plain unique index is sufficient without needing `citext` or a functional index.
- **Vehicle type stays free text with `<datalist>` suggestions** (Sedan/SUV/Van/Limousine/Minivan) rather than an enum, matching the spec's "keep it extensible" note and Prompt 2's original string-typed column.
- **Swagger XML doc comments span two assemblies**: DTOs live in `LimousineBooking.Application`, not `LimousineBooking.Api`, so both projects now set `GenerateDocumentationFile`, and `Program.cs` calls `IncludeXmlComments` for both — Swashbuckle only picks up comments from assemblies you explicitly point it at, regardless of which project's types appear in a controller signature. (`CS1591` is suppressed in both projects since most existing public members still lack doc comments.)
- **Driver creation is atomic without an explicit transaction**: `DriverService.CreateAsync` adds the new `User` and `Driver` to the same scoped `DbContext` and calls `SaveChangesAsync()` exactly once — EF Core already wraps a single `SaveChanges` call covering multiple pending inserts in one database transaction, so a manual `BeginTransaction`/`Commit` would be redundant. This works because both entities use client-generated GUIDs (assigned at construction, not by the database), so `Driver.UserId` can reference the new `User.Id` before either has been persisted.
- **Vehicle assignment is "prevent, not reassign"**: attempting to assign a vehicle that already has another driver returns `409` with a clear message, rather than silently stealing it from the other driver — the spec explicitly asked for the safer of the two options.
- **Vehicle registration uniqueness stays global; new domain methods added for Prompt 6**: `User.UpdateProfile(...)`, `User.SetPasswordHash(...)`, and `Driver.UpdatePhone(...)` — none existed before since Prompt 3 only needed `Activate`/`Deactivate`. Both `User` and `Driver` also gained lightweight format validation (a loose email regex, a loose international-friendly phone regex) in their shared `Validate` helpers, so "invalid email"/"invalid phone" is enforced at the domain layer, not just via `[EmailAddress]`/`[Required]` on the DTOs.
- **Deactivating a driver also deactivates their `User` (blocks login), and activating symmetrically re-enables it** — the spec only stated the deactivation half explicitly, but leaving an "active" driver locked out of login (or a "deactivated" driver still able to log in) would be an inconsistent state the spec doesn't intend. `User.IsActive` and `Driver.IsActive` remain two separate columns on two separate aggregates (auth concern vs. business-profile concern) — the driver-management endpoints just choose to always keep them in sync.
- **`DriverResponse` is used for both the list and single-driver endpoints** — the spec's field lists for "get all" and "get by id" are identical, so a separate `DriverDetailsResponse` type would just duplicate it. The spec's "future bookings/schedule" placeholder section for the driver-details page was intentionally left out of the DTO, per the instruction not to implement it yet.
- **Password reset is a dedicated endpoint** (`PUT /api/admin/drivers/{id}/password`) reusing `IPasswordService` from Prompt 3 — no second hashing mechanism, no password ever included in `DriverResponse`.
- **A real local PostgreSQL is now used for manual verification** (see **Running Without Docker, Against a Real PostgreSQL** above) — installed via `winget` since Docker wasn't available in this environment. All Prompt 5/6 flows described in this README (login, route/vehicle/driver CRUD, duplicate detection, deactivation blocking login, password reset) were exercised live against it, not just via mocked tests.

## Known Issues / Follow-ups

- **Docker was still not available in this environment** (`docker` is not installed), so `docker-compose.yml` and both Dockerfiles remain written-to-spec but unverified via an actual `docker compose up`. A real local PostgreSQL install (see above) covers the database side instead.
- Customer booking APIs, automatic driver assignment, notifications, and the admin/driver dashboards are all deliberately unimplemented — scoped for later steps. The future `GET /api/public/routes` endpoint mentioned in Prompt 4 was **not** added, per its own instruction not to implement it yet. Driver availability *scheduling* (Prompt 7) is also not implemented — Prompt 6 only exposes the current `IsAvailable` flag as read-only in the driver list/details.
