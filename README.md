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
$env:Jwt__Key = "replace-with-a-long-random-secret"

dotnet run --project src/LimousineBooking.Api
```

The API listens on the port shown in the console (see `src/LimousineBooking.Api/Properties/launchSettings.json`). Swagger UI is available at `/swagger` in the Development environment. `GET /api/health` returns `{ "status": "ok" }`.

You'll need a PostgreSQL instance reachable at the connection string above — either run `docker compose up postgres` or point at a local install.

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

Open `http://localhost:5173`. Placeholder routes are available at `/`, `/login`, `/driver`, and `/admin`.

## Running Tests

```
cd backend
dotnet test
```

## Assumptions and Decisions

- **Controllers over minimal APIs**: the Api project uses `--use-controllers` for a conventional, discoverable structure as the API surface grows.
- **JWT middleware is wired up but unused**: `Program.cs` configures the JWT bearer scheme and reads `Jwt:*` settings, but no endpoints require authentication yet — this exists only to prepare the architecture for driver/admin login, per the requirement not to implement auth flows in this step.
- **DbContext has no entities yet**: `ApplicationDbContext` is intentionally empty. The included `InitialCreate` migration is empty too — it exists solely to prove the migration pipeline works end-to-end.
- **Frontend pinned to Vite 5 / React Router 7 / React 18** instead of the very latest majors: `npm create vite@latest` currently scaffolds Vite 8, which ships an experimental Rolldown-based bundler requiring native platform bindings that aren't available for this Node version (20.15) on Windows — `npm run build` failed outright. Vite 5.4 is stable and builds/runs cleanly here. React Router was still bumped to v7 (from the v6 the template would otherwise pull in) because v6 has two unpatched CVEs (open redirect, SSR deserialization); v7 does not have the Vite 8 problem.
- **Frontend dev container runs `vite --host` directly** rather than a multi-stage Nginx build, since this is a development Compose setup, not a production deployment config.
- **No customer-facing auth artifacts were created** (no login/register endpoints, no customer JWT) — consistent with customers remaining unauthenticated guests.

## Known Issues / Follow-ups

- **Docker was not available in the environment this was built in** (`docker` is not installed), so `docker-compose.yml` and both Dockerfiles are written to spec but not verified end-to-end with an actual `docker compose up`. Please verify on a machine with Docker Desktop installed.
- `npm audit` reports one moderate advisory in `esbuild` (bundled transitively via Vite 5) that only affects the local dev server accepting cross-origin requests — not exploitable in production builds. Resolving it requires the still-broken Vite 8, so it's left as-is for now.
- Domain entities, DTOs, validators, repositories, and the actual JWT-issuing login flow are all deliberately unimplemented — they are scoped for later steps.
