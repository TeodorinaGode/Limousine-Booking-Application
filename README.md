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

Open `http://localhost:5173`. Routes: `/` (public), `/booking` (public — anonymous multi-step booking form, ending in a "Pay Now" step), `/booking/payment/{bookingReference}?token=...` (public — check/retry payment status from a saved link), `/booking/payment/success` / `/booking/payment/cancelled` (public — Stripe/fake-checkout redirect targets), `/login`, `/driver` (Driver-only), `/driver/availability` (current status + schedule), `/admin` (Administrator-only — dashboard statistics, upcoming trips, and a notification summary), `/admin/routes`, `/admin/vehicles`, `/admin/drivers`, `/admin/drivers/{id}` (details + read-only schedule), `/admin/bookings` (search/filter/sort/paginate, including by payment status), `/admin/bookings/{id}` (detail, edit, assign/reassign, cancel, resend confirmation email, payment history + refund), `/admin/reports` (includes a Payments metrics section), `/unauthorized`.

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
| `GET /api/driver/availability` | `Driver` | `{ isCurrentlyAvailable, schedule[] }` for the *authenticated* driver. Query: `from`, `to` (date range, inclusive) |
| `PUT /api/driver/availability` | `Driver` | `{ isAvailable }` → sets the real-time flag. Independent of the schedule below |
| `POST /api/driver/availability` | `Driver` | Creates a schedule period. `400` if the driver is inactive; `409` on overlap with an existing period the same date |
| `PUT /api/driver/availability/{id}` | `Driver` | Updates one of the driver's own periods. `404` if it doesn't exist or belongs to another driver; `409` on overlap |
| `DELETE /api/driver/availability/{id}` | `Driver` | Removes one of the driver's own periods. Same ownership check as update |
| `GET /api/admin/drivers/{driverId}/availability` | `Administrator` | Read-only: same `{ isCurrentlyAvailable, schedule[] }` shape as the driver's own view. Query: `from`, `to` |
| `GET /api/public/routes` | none | Active routes only, minimal fields (no `isActive`/audit timestamps) — for the public booking form |
| `POST /api/public/bookings` | none | Anonymous booking creation. `404` if the route doesn't exist, `400` for an inactive route, over-capacity, past/insufficient-lead-time date, or invalid customer data. After creation, automatic driver/vehicle assignment is attempted server-side — the response's `status` is `Confirmed` on success or `Pending` if no eligible driver was found (never an error) |
| `GET /api/admin/bookings` | `Administrator` | List/search bookings. Query: `search` (reference/name/email/phone), `status` (comma-separated, e.g. `Pending,Confirmed`), `dateFrom`, `dateTo`, `driverId`, `vehicleId`, `routeId`, `assignmentFilter` (`all`\|`automatic`\|`manual`\|`requiresManual`), `sortBy` (`bookingDate`\|`pickupTime`\|`createdAt`\|`customerName`\|`status`), `sortDirection`, `page`, `pageSize` (max 100). Default sort: upcoming first (date, then pickup time, ascending) |
| `GET /api/admin/bookings/dashboard` | `Administrator` | Operational counters (total/today's/pending/requires-manual-assignment/confirmed/cancelled/upcoming) + the next 10 upcoming trips |
| `GET /api/admin/bookings/{id}` | `Administrator` | Full detail: customer PII, trip, price, assignment internals, and assignment history. `404` if not found |
| `PUT /api/admin/bookings/{id}` | `Administrator` | Edits trip/customer fields. Changing route/date/time/passenger count revalidates the assignment via `AutomaticAssignmentService`. `409` if the booking is `Cancelled`/`Completed` or the route is inactive; `404` if the route doesn't exist |
| `POST /api/admin/bookings/{id}/assign` | `Administrator` | `{ driverId, vehicleId }` → manual assignment or reassignment, fully revalidated server-side (active/available/schedule/conflict/capacity/compatibility). `409` with a specific reason on any failed check |
| `POST /api/admin/bookings/{id}/auto-assign` | `Administrator` | Re-runs `AutomaticAssignmentService` for this booking (e.g. after a driver frees up) |
| `POST /api/admin/bookings/{id}/cancel` | `Administrator` | `{ reason? }` → sets `Cancelled`, releases driver/vehicle, keeps the record/reference/price. `409` if already `Cancelled` or `Completed` |
| `POST /api/admin/bookings/{id}/notifications/confirmation/resend` | `Administrator` | Re-enqueues the confirmation email using the booking's current state. Never changes status/assignment, never creates a duplicate booking |
| `GET /api/admin/notifications/failed` | `Administrator` | Paginated list of notifications that exhausted all retries. Never exposes SMTP credentials or raw rendered email bodies |
| `POST /api/admin/notifications/{id}/retry` | `Administrator` | Resets retry state and puts the notification back into `Pending` for the background worker to pick up. Never sends the email itself. `404` if not found |
| `POST /api/public/bookings/{bookingReference}/payment` | none | `?token=` (the booking's `PublicAccessToken`, not the reference alone). Starts a payment attempt for the amount snapshotted on the booking — reuses a still-open Checkout Session instead of creating a duplicate (double-click/multi-tab protection). `404` on a wrong/missing token, `409` (`BOOKING_CANCELLED`/`BOOKING_NOT_PAYABLE`/`BOOKING_ALREADY_PAID`) if the booking can't be paid right now |
| `POST /api/public/bookings/{bookingReference}/payment/retry` | none | Same auth/validation as above, but always opens a brand-new attempt (never reuses an open session) — for after a `Failed`/`Cancelled` attempt. Every prior attempt is kept for audit |
| `GET /api/public/bookings/{bookingReference}/payment` | none | `?token=` → the most recent attempt's `{ status, amount, currency, paidAt }`. Only the webhook ever marks a payment `Paid`, so this always reflects the provider's actual confirmation, never client-side assumption. `404` if no attempt exists yet or the token doesn't match |
| `POST /api/payments/webhook` | none (provider-verified) | Stripe (or the fake provider's simulated) webhook delivery. Verifies `Stripe-Signature`, is idempotent on `ProviderEventId` (a unique DB index turns a duplicate delivery into a no-op), and is the *only* code path that ever marks a payment `Paid`/`Failed`/`Cancelled` |
| `POST /api/admin/bookings/{id}/refund` | `Administrator` | Refunds the booking's `Paid` payment via the active payment provider, then marks it `Refunded`. Never automatic — always an explicit admin action. `409` if there's no `Paid` payment to refund |
| `GET /api/admin/reports/payments` | `Administrator` | Same `dateFrom`/`dateTo` convention as the other reports. Payment-attempt counts by status (Pending/Processing merged as "in flight") + `paidRevenue` (currently-`Paid` attempts) and `refundedAmount` (currently-`Refunded` attempts) — kept as two always-separate figures, never netted against each other or against `ReportSummaryResponse`'s booking-price-based revenue |

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

## Testing Emails Locally (No Real SMTP Account Needed)

By default `EmailSettings:Enabled` is `false` in `appsettings.json`, which makes the API use `LoggingEmailService` instead of real SMTP — every "send" just logs `[DEV EMAIL] To: ... | Subject: ...` and always succeeds, so the full enqueue → background-worker → "send" pipeline is exercisable end-to-end without any email provider. To watch it happen:

1. Run the backend as usual (see above) and tail its console output.
2. Trigger any booking event (create a public booking, have an admin assign/cancel one, etc.).
3. Watch for `Notification ... enqueued for booking ...` followed a few seconds later (see `NotificationSettings:PollIntervalSeconds`, default 15s) by `[DEV EMAIL] To: ... | Subject: ...` and `Notification ... sent to ...` (the recipient is partially masked in this final log line — e.g. `n***@example.com` — full addresses are never written to logs at Information level).
4. Inspect `GET /api/admin/notifications/failed` and `POST /api/admin/notifications/{id}/retry` via Swagger to see the admin-facing side.

To test against a real inbox, set `EmailSettings:Enabled=true` plus `Host`/`Port`/`Username`/`Password`/`FromEmail` (e.g. via environment variables — never commit real credentials) and `SmtpEmailService` takes over automatically.

## Testing Payments Locally (No Real Stripe Account Needed)

By default `PaymentSettings:Enabled` is `false` in `appsettings.json`, which makes the API use `FakePaymentService` instead of the real Stripe SDK — `CreateCheckoutSessionAsync` returns a link to a dev-only page (`FakeCheckoutController`, `GET /api/payments/fake-checkout/{sessionId}`) with three buttons ("Simulate Successful Payment", "Simulate Failed Payment", "Simulate Session Expired"). Clicking one calls **the exact same `IPaymentWebhookService.HandleWebhookAsync` code path a real Stripe webhook delivery would hit** — only the transport (HTTP form POST vs. a signed Stripe request) is faked; no payment *processing* logic is bypassed. To exercise the full flow end-to-end:

1. Run the backend as usual and create a booking (`POST /api/public/bookings`) — note the response's `bookingReference` and `accessToken`.
2. `POST /api/public/bookings/{bookingReference}/payment?token={accessToken}` → returns `{ paymentId, checkoutUrl, expiresAt }`.
3. Open `checkoutUrl` in a browser (or `curl`/Swagger it) and click **Simulate Successful Payment** — you'll be redirected to `PaymentSettings:SuccessUrl` with `?ref=...&token=...` appended.
4. `GET /api/public/bookings/{bookingReference}/payment?token={accessToken}` now returns `"status": "Paid"`, and the `PaymentSucceeded` email was enqueued (see **Testing Emails Locally** above to watch it "send").
5. As an admin, `GET /api/admin/bookings/{id}` shows the `payment`/`paymentHistory` fields, and `POST /api/admin/bookings/{id}/refund` refunds it (via `FakePaymentService.RefundAsync`, which always succeeds) and marks it `Refunded`.

`FakeCheckoutController` 404s on every action whenever real Stripe is active (`PaymentSettings:Enabled=true`), so it's structurally inert outside local dev/tests — it can't be reached in a real deployment.

### Configuring Real Stripe (Test Mode)

Set these via environment variables or `dotnet user-secrets` — **never** commit them to `appsettings.json` (the file always ships with empty values, matching the `Jwt:SecretKey`/`EmailSettings:Password` convention):

```
PaymentSettings__Enabled=true
PaymentSettings__SecretKey=sk_test_...           # Stripe Dashboard → Developers → API keys (test mode)
PaymentSettings__WebhookSecret=whsec_...         # see below
PaymentSettings__SuccessUrl=http://localhost:5173/booking/payment/success
PaymentSettings__CancelUrl=http://localhost:5173/booking/payment/cancelled
```

If `Enabled=true` but `SecretKey`/`WebhookSecret` are blank, the API **fails fast at startup** (`InvalidOperationException`) rather than silently falling back to the fake provider — payments are never accidentally left unconfigured in what looks like a production setup.

**Local webhook delivery** (only needed when `Enabled=true` — the fake provider above needs none of this): install the [Stripe CLI](https://stripe.com/docs/stripe-cli), then:

```
stripe login
stripe listen --forward-to localhost:5099/api/payments/webhook
```

This prints a `whsec_...` value — set it as `PaymentSettings__WebhookSecret` for that session. Trigger test events with `stripe trigger checkout.session.completed`, or actually complete a Checkout Session using [Stripe's test card numbers](https://stripe.com/docs/testing) (e.g. `4242 4242 4242 4242`, any future expiry, any CVC).

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
- **A real local PostgreSQL is now used for manual verification** (see **Running Without Docker, Against a Real PostgreSQL** above) — installed via `winget` since Docker wasn't available in this environment. All Prompt 5/6/7 flows described in this README (login, route/vehicle/driver CRUD, duplicate detection, deactivation blocking login, password reset, availability scheduling) were exercised live against it, not just via mocked tests.
- **Current availability vs. scheduled availability stay fully independent** (Prompt 7): `Driver.IsAvailable` (the self-service toggle) and `DriverAvailability` records (the calendar) are never touched by each other's write paths — confirmed by a dedicated test (`CreatingSchedule_DoesNotChangeCurrentAvailability`). `GET /api/driver/availability` bundles both into one `{ isCurrentlyAvailable, schedule }` response purely because the availability page needs both on load and the spec doesn't define a separate "current status" endpoint — this is an API-shape choice, not a sign the two concepts are coupled server-side.
- **Overlap detection uses a half-open interval check** (`existing.Start < newEnd && newStart < existing.End`) against every existing record for that driver+date, regardless of whether it's marked available or unavailable — two records covering the same slot is ambiguous data either way. Touching-but-not-overlapping periods (e.g. 08:00–12:00 next to 12:00–17:00) are allowed by design, matching the spec's "gaps" example.
- **A driver's own availability endpoints never trust a driver id from the request** — `ICurrentUserService` gained a `DriverId` property (reads the `driverId` JWT claim already issued since Prompt 3); the controller resolves it from the token and passes it into the service explicitly. A record that exists but belongs to a different driver returns `404`, not `403` — this avoids confirming the record exists at all to a driver who isn't its owner.
- **`IAvailabilityEvaluationService.IsDriverAvailableAsync(driverId, date, start, end)`** is a separate reusable service (not wired into any endpoint yet) — deliberately isolated so a future automatic-assignment feature can layer a booking-conflict check on top without touching this logic. It checks active-status, then any overlapping *unavailable* period (which always wins), then whether an *available* period fully **contains** the requested window (partial overlap isn't enough to guarantee the driver is free for the whole trip).
- **A custom `TimeOnly` JSON converter was added** (`FlexibleTimeOnlyJsonConverter`): .NET's default converter only accepts the strict `"HH:mm:ss"` form, but the spec's own examples use `"08:00"`, and HTML `<input type="time">` also produces `"HH:mm"` natively. The converter accepts either on input and always emits `"HH:mm:ss"` in responses, so Swagger "Try it out" and the frontend both work without extra reformatting.
- **`/admin/drivers/{id}` is a new page**, not just an extension of an existing one — Prompt 6 satisfied its own "driver details" requirement with a modal instead of a dedicated route (a legitimate choice at the time, since Prompt 6 phrased it as "a details page *or* modal"), so Prompt 7's "extend the driver details page" needed this page created now to have somewhere to put the read-only Availability Schedule section.
- **`EmailFormat`/`PhoneFormat` were extracted into `Domain/Common`** (Prompt 8): `Booking` needed the same email/phone format rules `User` and `Driver` already enforced privately. Rather than a third copy of each regex, both were pulled out into shared static validators and `User`/`Driver` were refactored to call them — no behavior change, confirmed by the full existing test suite passing unchanged.
- **Booking reference format is `LM-{travelDate:yyyyMMdd}-{6-digit random}`** (e.g. `LM-20261225-218288`), generated by `IBookingReferenceGenerator` with a bounded retry loop (10 attempts) against a uniqueness check, rather than a sequential counter — a random suffix avoids needing a shared, lockable counter across concurrent anonymous submissions, and the retry loop handles the rare collision. The `Bookings.BookingReference` column already had a unique index from Prompt 2's schema.
- **Lead time and passenger-count limits are configuration, not domain invariants**: `BookingSettings` (`MinimumLeadTimeMinutes`, `MaximumPassengers`) is bound from `appsettings.json` via `IOptions<BookingSettings>` and enforced in `PublicBookingService`, not in the `Booking` constructor — these are business policy that could plausibly change per-deployment, unlike genuine data invariants (non-empty name, valid email) which stay in the domain entity.
- **Past-date/lead-time checking is Europe/Zurich-aware, not naive UTC comparison**: `SwissTimeZone` wraps a single `TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich")` lookup, and `IDateTimeProvider` (wrapping `DateTime.UtcNow`) makes "now" substitutable in tests. The customer's `bookingDate`/`pickupTime` are entered and validated as Zurich local time — converting the server's UTC clock to Zurich local (not comparing UTC directly against a local-looking `DateTime`) avoids an off-by-one-or-two-hours bug across the DST boundary.
- **`EstimatedEndTime` is not stored or returned**: the spec asked whether to compute a trip's end time from `Route.EstimatedDurationMinutes`. It's derivable at any time from data already on the response (`pickupTime + route.estimatedDurationMinutes`), so persisting or serializing it would just be redundant, staleness-prone data — the frontend (or a future consumer) can compute it if/when it's actually needed for display.
- **No public "get booking by reference" endpoint**: customers have no account to look bookings up through (per the spec's explicit no-login requirement), so `POST /api/public/bookings`'s `201` response returns the created booking directly in the body instead of a `Location` header pointing at a GET endpoint that doesn't exist.
- **Anti-spam prep is limited to `[RequestSizeLimit(16 * 1024)]`** on `POST /api/public/bookings` — per the spec's explicit instruction not to implement CAPTCHA or any external CAPTCHA provider, this prompt only adds the one preparatory guard it did ask for.
- **Client-supplied `price`/`currency`/`status`/`driverId`/`vehicleId`/`createdAt`/`bookingReference` are structurally impossible to submit**, not just ignored: `CreateBookingRequest` (the only shape the public endpoint binds from) has no such fields at all, so there's nothing for a malicious payload to override — verified live by POSTing a request with extra `price`/`status`/`driverId` fields and confirming the created booking used the route's snapshotted price and `Pending` status regardless.
- **Public routes and booking creation are unauthenticated by design** (`[AllowAnonymous]`, no `[Authorize]`), so there is no 401/403 authorization test for them (unlike every admin/driver endpoint) — their business-rule coverage lives in `PublicBookingServiceTests` (mocked repositories) instead, mirroring `RouteServiceTests`.
- **Automatic assignment is isolated in `AutomaticAssignmentService`** (Prompt 9), called once from `PublicBookingService.CreateBookingAsync` right after the booking is saved — not from any controller, and not inlined into `PublicBookingService` itself, so the eligibility/ranking rules live in exactly one reusable place (e.g. a future admin "retry assignment" action can call the same `AssignBookingAsync(bookingId)`).
- **`Booking.RequiresManualAssignment` is a separate flag from `Status`, on purpose**: a booking can be `Pending` because assignment hasn't run yet, or `Pending` because it ran and failed — the flag (plus `ManualAssignmentReason`, admin-only/never serialized to the public API) distinguishes the two so a future admin UI can query "which Pending bookings actually need my attention."
- **Driver eligibility is filtered mostly at the database level**: `IDriverRepository.GetAssignmentCandidatesAsync` expresses active/active-user/currently-available/has-an-active-vehicle-with-enough-seats as one LINQ query (translated to a single SQL query with joins), rather than loading every driver and filtering in memory — per the spec's explicit performance requirement. Only the two checks that need a *per-candidate* query (scheduled availability, via the Prompt 7 `IAvailabilityEvaluationService`; and booking conflicts, via one batched `GetConflictScanAsync` call covering all remaining candidates at once) happen after the DB-level narrowing, against what is by then a small candidate set.
- **Trip end time = `PickupTime + Route.EstimatedDurationMinutes`**, computed server-side from the route the booking already references — never trusted from the client (there was never a client-supplied duration to trust in the first place, since `CreateBookingRequest` has no such field).
- **Buffer is applied by padding the *existing* booking's interval by `DriverBufferMinutes` on both sides**, then doing a normal half-open `[Start, End)` overlap check against the new booking's raw interval. Padding both sides (not just "after") with one formula correctly protects the gap whether the new trip would come immediately before or immediately after the existing one. Default 15 minutes, configurable via `BookingSettings:DriverBufferMinutes`; verified live (10:05 start after a booking ending 10:00 conflicts, 10:15 does not).
- **Ranking order: smallest sufficient vehicle capacity → fewest upcoming non-cancelled bookings → driver ID ascending.** The workload count comes from one grouped `GetUpcomingBookingCountsAsync` query (not loaded-then-counted in memory); the final tie-breaker is a plain `Guid` comparison, which is deterministic (same inputs always produce the same ordering) even though it isn't creation-order or otherwise meaningful.
- **Concurrency is handled with a real PostgreSQL `Serializable` transaction, not application-level locking**: `AutomaticAssignmentService.AssignBookingAsync` runs its entire candidate-search-and-save sequence inside one transaction opened by a new `ITransactionRunner` abstraction (Application interface, Infrastructure implementation over `ApplicationDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable)`), retried up to 3 times on SQLSTATE `40001`/`40P01`. Verified live: two simultaneous `POST /api/public/bookings` requests for the same driver's only free slot resulted in exactly one `Confirmed` booking with that driver and one `Pending` booking — never both assigned.
- **`AssignmentHistory` was not introduced in Prompt 9** (documented then as unnecessary until reassignment existed) **but is introduced in Prompt 10**, once manual (re)assignment gave it a concrete purpose — see the Prompt 10 notes below. `Booking.AssignmentType`/`RequiresManualAssignment`/`ManualAssignmentReason` (added in Prompt 9) remain the "current state" fields; `AssignmentHistory` is the append-only "how did we get here" log layered on top.
- **`AdminBookingResponse` (Prompt 9's prepared, unconsumed DTO) was replaced in Prompt 10** by `AdminBookingDetailResponse` (fuller — adds estimated end time, cancellation/audit fields, assignment history) and `AdminBookingListItemResponse` (leaner, for the list table) once the Admin Booking Management UI gave the shape an actual consumer.
- **Conflict scanning is scoped to bookings on the same `TravelDate`** as the new booking (mirroring `DriverAvailability`, which is likewise single-day). A trip whose buffer padding pushes past midnight into the next calendar date is a known, undocumented-by-the-spec edge case this doesn't handle — consistent with the rest of the app not modeling multi-day/midnight-crossing trips.
- **New indexes (Prompt 9)**: `Bookings(VehicleId, TravelDate)` (mirrors the existing `Bookings(DriverId, TravelDate)` index, for the explicit vehicle-conflict check in section 14) and `Bookings(RequiresManualAssignment)` (so an admin "needs attention" list doesn't scan every `Pending` row). The old standalone `Bookings(VehicleId)` index was superseded by the new composite one.

### Prompt 10 — Administrator Booking Management

- **`AssignmentHistory` is introduced now** (Domain/Entities), insert-only, one row per *successful* assignment — written by both `AutomaticAssignmentService` (retrofitted this prompt; `AssignedByUserId = null`) and `AdminBookingService`'s manual-assign path (`AssignedByUserId` = the acting admin). Reassigning a booking never deletes or updates a prior row, so the full assignment lineage survives — verified live: reassigning a booking left both the original `Manual` entry and the new `Automatic` entry (from a subsequent trip-affecting edit) in its history. Cancellation audit (who/when/reason) lives directly on `Booking` instead (`CancellationReason`/`CancelledAt`/`CancelledByUserId`), since a booking is only ever cancelled once — a history table would be one row, always.
- **Editing a trip-affecting field never tries to decide "is the current driver still eligible" itself.** `UpdateAsync` resets the booking to unassigned (`Booking.UnassignForRevalidation()`, which also moves `Status` back to `Pending` — otherwise a booking with no driver could still read `Confirmed`) whenever route/date/time/passenger count changes, then calls the *same* `AutomaticAssignmentService.AssignBookingAsync` used at booking creation. If the previous driver is still the best eligible candidate, the deterministic ranking simply reselects them (verified live — same driver, `AssignmentType` flips back to `Automatic`); this trades a few extra queries for zero duplicated eligibility logic, matching the spec's explicit "do not duplicate the assignment logic" instruction more literally than a hand-rolled "is X still valid" check would.
- **Price is recalculated only when the route ID actually changes** on an edit — never as a side effect of an unrelated field changing, and never by silently re-pricing against the current route's price if the route stays the same (that would corrupt the "price is a snapshot" invariant from Prompt 8). The recalculation is logged (`ILogger`, old/new price + acting admin) rather than written to a second persisted price-history table — verified live: changing Basel→Zurich to Basel→Bern moved the price from 180.00 to 210.00 CHF exactly once, and re-saving without changing the route left it untouched.
- **Manual assignment reuses the automatic-assignment concurrency strategy exactly**: `AdminBookingService.AssignDriverAsync` runs its whole validate-then-assign sequence inside `ITransactionRunner.RunSerializableAsync` — the same Serializable-transaction-with-retry abstraction from Prompt 9 — so an admin manually assigning Driver A can never race a concurrent automatic assignment onto the same driver.
- **Driver↔vehicle "compatibility" means the requested vehicle IS the driver's current vehicle** (`Driver.CurrentVehicleId`) — this domain models one current vehicle per driver, not a many-to-many fleet, so no new relationship was invented for this check, per the spec's explicit instruction not to.
- **Reassignment is not a separate endpoint** — `POST /api/admin/bookings/{id}/assign` handles both "assign for the first time" and "change the current driver" identically: it validates the new driver/vehicle (excluding the booking's own prior conflict scan entry), overwrites `DriverId`/`VehicleId`/`AssignmentType`, and appends one new `AssignmentHistory` row. There is nothing to "remove" from the old assignment beyond the overwrite itself — the old assignment's history row is never touched.
- **Editing, manual assignment, and auto-assignment are all rejected with `409` on `Cancelled`/`Completed` bookings** — not explicitly demanded by every spec test case, but necessary: without this guard, editing a cancelled booking's trip details could have driven it back through the assignment pipeline into `Confirmed`, silently un-cancelling it.
- **Status filter defaults to "Active" (`Pending,Confirmed`) via a comma-separated `status` query parameter**, not a 6th named value — the spec lists exactly five status filter options (All/Pending/Confirmed/Cancelled/Completed) but also specifies a "Pending + Confirmed" default, which isn't expressible as one of those five. The frontend's status dropdown adds "Active (Pending + Confirmed)" as an explicit first/default option; the backend accepts any comma-separated combination of `BookingStatus` names.
- **New indexes (Prompt 10)**: `Bookings(CustomerEmail)` (search/filter path) and `AssignmentHistories(BookingId, AssignedAt)` (loading one booking's history, most recent first).

### Prompt 11 — Notifications & Email System

- **`Notification` (Prompt 2's previously-unused scaffold entity) was extended into this application's transactional outbox, instead of adding a new `OutboxMessage`/`NotificationLog` table.** Its shape (`BookingId`, `Recipient`, `NotificationType`, `Channel`, `Status`, `SentAt`, `ErrorMessage`) already matched almost exactly what an outbox row needs — only `Payload`, `RetryCount`, `NextAttemptAt`, and `ProcessingStartedAt` were missing. Per the spec's own "if the Outbox model already provides this information, do not create unnecessary duplicate tables" instruction, one table now serves as both the outbox and the delivery log — there was never a point where these needed to be two separate concepts.
- **Transactional outbox without an explicit `BeginTransaction`**: `NotificationService` enqueues a `Notification` row via the same `DbContext`-backed `INotificationRepository` the caller (`AutomaticAssignmentService`, `AdminBookingService`) already uses, and the caller's own `SaveChangesAsync()` call persists both the business mutation and the notification row together, atomically. This also makes the outbox insert safe under `ITransactionRunner`'s Serializable-retry (Prompt 9): if a transaction aborts and retries, the earlier attempt's `Notification` insert is rolled back along with everything else — no manual idempotency key needed for that specific risk.
- **Email is rendered once, at enqueue time, not at send time.** `NotificationService` calls `IEmailTemplateRenderer` immediately and stores the fully-rendered subject/HTML/plain-text in the `Payload` column (as a small `NotificationPayload` JSON blob). This matters concretely for reassignment: the *previous* driver's name is only available at the moment of reassignment — by the time the background worker runs, the booking's current driver is already the new one.
- **Templates are real `.html` files** (`Infrastructure/Email/Templates/`), one per event, each starting with a `<!--SUBJECT: ...-->` line the renderer parses out; placeholder substitution is a single `string.Replace` loop — no templating framework. Branding (header/footer) is centralized in one `_Layout.html` every template is wrapped in, rather than repeated per template, per the spec's explicit "centralize branding" instruction. Plain text is auto-derived from the rendered HTML by stripping tags, rather than maintaining a second parallel set of plain-text templates.
- **Event → recipient mapping lives in `NotificationService`, and *when* to call it lives in the two services that actually change assignment/status state** — `AutomaticAssignmentService` (confirmed+driver on success, pending+admin on failure — it already has the driver/reason in hand at exactly the right moment) and `AdminBookingService` (customer-assigned+driver on first manual assignment, all three reassignment notifications, customer-cancelled on cancellation). Nothing calls `IEmailService` directly from a controller, and nothing renders a template inline in business logic.
- **`BookingCompleted`/`RideCompleted` is prepared but has no caller** — there is still no "mark booking Completed" endpoint anywhere in the app (driver ride-status is a future prompt), so `NotifyCustomerCompletedAsync` and its template exist ready to be wired up, matching the same "build ahead of the endpoint" pattern as `IAvailabilityEvaluationService` (Prompt 7) and `AdminBookingResponse` (Prompt 9).
- **Editing a confirmed booking's trip details (Prompt 10's revalidation path) can re-fire a `BookingConfirmation` email** even though the customer already received one — `AutomaticAssignmentService`'s success path is uniform regardless of whether it was invoked from booking creation, an admin edit's revalidation, or the explicit auto-assign endpoint. This is treated as correct behavior (the customer should be told the outcome of an edit that touched their assignment), not a bug to special-case around.
- **Retry backoff is `[1, 5, 15, 30, 60]` minutes indexed by retry count, max 5 retries** (both configurable via `NotificationSettings`), after which a notification becomes permanently `Failed` and stays visible via `GET /api/admin/notifications/failed` for manual retry. A `Processing` row older than `StaleProcessingMinutes` (default 5) is re-claimed by the next poll — the only realistic crash-recovery case for a single-instance worker; true multi-instance claim-locking was judged out of scope for v1, consistent with the spec's own "the exact implementation can be simplified for version 1."
- **The background worker (`NotificationOutboxWorker`) is skipped entirely in the `Testing` environment** (`WebApplicationFactory`-based integration tests) — it would otherwise start polling a database the auth-focused tests never touch, adding noise without adding coverage. The actual send/retry/backoff logic is unit-tested directly against `NotificationOutboxProcessor` (the testable class the worker just loops around), with `IEmailService`/`INotificationRepository` mocked.
- **Booking creation is provably immune to email outages** — not just "designed to be," but structurally: the HTTP request path only ever inserts a `Notification` row (a plain `INSERT`), it never calls `IEmailService`. Only the background worker does that, entirely outside any request. Verified live: `LoggingEmailService` (dev mode) always "succeeds" so the pipeline is fully exercisable without SMTP; a simulated `Failed` notification was created directly and successfully retried via `POST /api/admin/notifications/{id}/retry` without touching the booking.
- **New indexes (Prompt 11)**: `Notifications(Status, NextAttemptAt)` (the worker's main due-message query), `Notifications(CreatedAt)`, `Notifications(SentAt)` (the dashboard's "sent today" count).

### Prompt 15 — Online Payments and Payment Management

- **`IPaymentService` is the one abstraction the Application layer depends on for the payment provider** — `StripePaymentService`/`FakePaymentService` (Infrastructure) are its only implementations, and Stripe SDK types (`Stripe.Checkout.Session`, `Stripe.Event`, ...) never appear outside `StripePaymentService`. This mirrors `IEmailService`'s Prompt 11 pattern exactly.
- **`PaymentStatus` is fully independent of `BookingStatus`** — a successful payment never touches `Booking.Status`. The existing automatic-assignment flow already confirms bookings independently of any payment concept, and the spec explicitly permitted integrating "without introducing conflicting states" rather than inventing a second state machine.
- **A new `Booking.PublicAccessToken` field (256 bits, `RandomNumberGenerator`-generated, base64url) was added** because the existing `BookingReference` (`LM-{date}-{6-digit random}`, ~1,000,000 possibilities/day) isn't cryptographically strong enough to gate payment-status disclosure per the spec's explicit warning against guessable references. This is purely additive — every existing endpoint/behavior is unchanged; only the new public payment endpoints require it (alongside `bookingReference`), compared in constant time (`CryptographicOperations.FixedTimeEquals`) to prevent timing attacks.
- **Payment amount is always `Booking.Price` at the moment a `Payment` row is created** — never re-read from `Route.Price` (which can change after the booking was made) and never accepted from the request body (`POST .../payment` takes only `bookingReference` + `token`, structurally no amount field to override).
- **A booking can have multiple `Payment` rows over time** (a failed/expired attempt followed by a successful retry) — at most one should ever reach `Paid`, enforced by `PublicPaymentService` (rejects starting a new attempt once a `Paid` one exists), not a database constraint, since failed/cancelled attempts are kept for audit. A repeat "start payment" call reuses a still-open, non-expired session instead of creating a duplicate (double-click/multi-tab protection) — retry always opens a fresh attempt instead.
- **Only the webhook (`PaymentWebhookService`) ever marks a payment `Paid`/`Failed`/`Cancelled`** — never the request/redirect path a browser takes back from Checkout. `PaymentWebhookEvent` (insert-only, unique index on `ProviderEventId`) makes duplicate delivery a no-op: a second delivery's `SaveChangesAsync()` fails the unique constraint, which `IPaymentWebhookEventRepository.IsDuplicateEventError(Exception)` recognizes (inspecting the exception chain for Postgres SQLSTATE `23505`, kept entirely in Infrastructure so Application never references `Npgsql` directly) and treated as "already processed." Applying the event is wrapped in the same `ITransactionRunner.RunSerializableAsync` used by automatic/manual assignment (Prompt 9/10), and defense-in-depth beyond the event-id dedup: a payment already in a terminal state (`Paid`/`Refunded`) never regresses however many times an event replays.
- **Cancelling a booking marks any open (`Pending`/`Processing`) payment `Cancelled`** but never touches a `Paid` one, and never triggers an automatic refund — refunds are always an explicit admin action (`POST /api/admin/bookings/{id}/refund`), prepared but never automatic per the spec.
- **Reports keep `paidRevenue` and `refundedAmount` as two always-separate figures** (`GET /api/admin/reports/payments`), never netted against each other or mixed into `ReportSummaryResponse`'s existing booking-price-based `grossRevenue`/`completedRevenue` — those measure a booking's price snapshot, this measures money the payment provider actually captured or returned.
- **`FakePaymentService`/`FakeCheckoutController`** (see **Testing Payments Locally** above) let the entire checkout→webhook→paid flow run without a Stripe account, in both automated tests and live manual verification — selected automatically whenever `PaymentSettings:Enabled=false` (the default), and structurally 404s on every action once real Stripe is active. Fixed one bug found during live verification: the fake checkout's "Simulate Successful Payment" button initially posted `outcome=success`, but `FakePaymentService.ParseWebhookEventAsync` only recognized `"completed"` — the mismatch made the webhook silently no-op (event recorded, but no `MarkPaid` call) instead of erroring, which is why this was caught by *live* end-to-end verification rather than the unit tests (which construct the payload directly and never hit this string mismatch).
- **`PaymentSettings.Enabled=true` without `SecretKey`/`WebhookSecret` throws at startup** rather than silently falling back to the fake provider — mirrors `EmailSettings`'s Prompt 11 fail-fast convention; a production deployment can never end up "configured for Stripe" but actually running the dev simulator.
- **New indexes**: unique on `Payments.ProviderCheckoutSessionId` and `Payments.ProviderPaymentId` (idempotency/lookup), plain on `Payments.BookingId`/`Status`/`CreatedAt`/`PaidAt` (admin filtering, reports), unique on `PaymentWebhookEvents.ProviderEventId` (the idempotency guarantee itself), and unique on `Bookings.PublicAccessToken`.
- **Migration backfill for pre-existing bookings**: the new `Bookings.PublicAccessToken` column is `NOT NULL` with a unique index, so the `AddPayments` migration backfills a unique value (`gen_random_uuid()` x2, concatenated) for every row that predates the column, before creating the index — otherwise applying the migration against a database with more than one existing booking would fail on the first unique-constraint violation (every pre-existing row would otherwise default to the same empty string).
- **"Do not implement" list honored**: no customer accounts, saved cards, recurring payments/subscriptions, crypto, buy-now-pay-later, multiple providers, corporate billing, promo codes/discounts, complex invoicing, automatic refunds, or SMS — exactly as scoped.

## Known Issues / Follow-ups

- **Docker was still not available in this environment** (`docker` is not installed), so `docker-compose.yml` and both Dockerfiles remain written-to-spec but unverified via an actual `docker compose up`. A real local PostgreSQL install (see above) covers the database side instead.
- SMS/WhatsApp/push notifications, notification preferences, customer accounts/saved cards/recurring payments, live tracking, reviews, and promo codes are all deliberately unimplemented — scoped for later steps (or explicitly out of scope), per each prompt's own "do not implement" list. The `NotificationChannel` enum already has a comment marking where `Sms` would be added without a breaking schema change.
- **Real Stripe was never exercised against this environment's sandbox** (no outbound internet access to Stripe's API) — `StripePaymentService` was written and compiled against the `Stripe.net` SDK's actual types, but only `FakePaymentService`/`FakeCheckoutController` were live-verified end-to-end here; a real Stripe test-mode account should be used to confirm `StripePaymentService` itself before production use.
