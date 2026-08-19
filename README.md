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

Open `http://localhost:5173`. Routes: `/` (public), `/login`, `/driver` (Driver-only), `/admin` (Administrator-only), `/unauthorized`.

## API Endpoints

| Endpoint | Auth | Description |
|---|---|---|
| `GET /api/health` | none | Health check |
| `POST /api/auth/login` | none | `{ email, password }` → `{ accessToken, expiresAt, user }` |
| `GET /api/test/authenticated` | any authenticated user | Dev-only: verifies a token is accepted |
| `GET /api/test/admin` | `Administrator` | Dev-only: verifies role-based authorization |
| `GET /api/test/driver` | `Driver` | Dev-only: verifies role-based authorization |

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

## Assumptions and Decisions

- **Controllers over minimal APIs**: the Api project uses `--use-controllers` for a conventional, discoverable structure as the API surface grows.
- **DbContext has no entities yet** *(Prompt 1 only — superseded)*: as of Prompt 2, `ApplicationDbContext` exposes the full domain model; see the entity list under **Database changes** in the Prompt 3 notes below.
- **Frontend pinned to Vite 5 / React Router 7 / React 18** instead of the very latest majors: `npm create vite@latest` currently scaffolds Vite 8, which ships an experimental Rolldown-based bundler requiring native platform bindings that aren't available for this Node version (20.15) on Windows — `npm run build` failed outright. Vite 5.4 is stable and builds/runs cleanly here. React Router was still bumped to v7 (from the v6 the template would otherwise pull in) because v6 has two unpatched CVEs (open redirect, SSR deserialization); v7 does not have the Vite 8 problem.
- **Frontend dev container runs `vite --host` directly** rather than a multi-stage Nginx build, since this is a development Compose setup, not a production deployment config.
- **No customer-facing auth artifacts were created** (no login/register endpoints, no customer JWT) — consistent with customers remaining unauthenticated guests.
- **JWT config property names**: `Jwt:SecretKey` / `Jwt:AccessTokenExpirationMinutes` (not `Jwt:Key/ExpiryMinutes`), matching the Prompt 3 spec. Note the spec's illustrative env var casing (`JWT__SECRET_KEY`) would **not** actually bind to a `SecretKey` property under ASP.NET Core's config rules — the env var must be `Jwt__SecretKey` (double underscore as the section separator, then the exact — case-insensitive — property name with no extra underscores). `.env.example` files use the correct form.
- **JWT claims use short names** (`sub`, `email`, `role`, `name`, `driverId`) rather than the long `ClaimTypes.*` URIs, per the spec. This requires `JwtBearerOptions.MapInboundClaims = false` plus explicit `RoleClaimType = "role"` — otherwise ASP.NET Core's default inbound claim remapping breaks `[Authorize(Roles = ...)]` silently. `NameClaimType` is set to `"email"`.
- **Password hashing**: ASP.NET Core Identity's `PasswordHasher<User>` (PBKDF2), wrapped behind an `IPasswordService` interface — no custom cryptography, no plaintext password ever persisted or logged.
- **Dev user seeding runs at API startup** (Development environment only, idempotent, wrapped in try/catch so an unreachable database doesn't crash the app) rather than via an EF Core migration `HasData` — `IPasswordHasher` output is intentionally randomized per call, so a reproducible hash can't be baked into a migration the way the seed *routes* were in Prompt 2.
- **No refresh tokens / no logout endpoint**, per the spec: access-token-only, frontend just discards the token locally on logout.
- **`TestController` (`/api/test/*`) is a temporary, clearly-marked dev/QA surface** for verifying the auth pipeline — not a real product endpoint. Fine to delete once real protected endpoints exist.
- **Frontend route guards are UX-only**: `ProtectedRoute` redirects unauthenticated/wrong-role users client-side, but the backend's `[Authorize(Roles=...)]` is the actual security boundary, per the spec's explicit requirement.

## Known Issues / Follow-ups

- **Docker was not available in the environment this was built in** (`docker` is not installed), so `docker-compose.yml` and both Dockerfiles are written to spec but not verified end-to-end with an actual `docker compose up`. Likewise, no local PostgreSQL was available to run `dotnet ef database update` or exercise `/api/auth/login` against a real database — this was substituted with (a) inspecting the generated migration SQL and (b) mocked/`WebApplicationFactory`-based tests that don't require a live database. Please run a real login through Swagger once Postgres is available, to confirm end-to-end.
- `npm audit` reports one moderate advisory in `esbuild` (bundled transitively via Vite 5) that only affects the local dev server accepting cross-origin requests — not exploitable in production builds. Resolving it requires the still-broken Vite 8, so it's left as-is for now.
- Customer booking APIs, automatic driver assignment, notifications, and the admin/driver dashboards are all deliberately unimplemented — scoped for later steps.
