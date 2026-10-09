# Scheduling App

[![CI](https://github.com/Jhonatan1973/scheduling-app/actions/workflows/ci.yml/badge.svg)](https://github.com/Jhonatan1973/scheduling-app/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![Blazor](https://img.shields.io/badge/Blazor-Interactive%20Server-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)
![License: MIT](https://img.shields.io/badge/license-MIT-green)

A full-stack appointment booking system built **end-to-end with .NET**: clients book time slots with a professional
(barber shop, clinic, tutoring…), professionals publish their weekly hours, confirm or decline requests and follow their
day/week agenda. Every step sends an email.

**Live demo:** _add your Render URL here after deploying_ · **API docs:** `<api-url>/docs`

> Demo accounts (password `Demo@12345`): `client@demo.com` · `sofia@demo.com` (dermatologist) · `lucas@demo.com` (barber)

![Demo](docs/demo.gif)

## Features

- **Two profiles** – Client and Professional, with ASP.NET Core Identity + JWT and role-based policies.
- **Weekly availability** – several intervals per day (e.g. 09–12 and 13–18), session length and time zone per professional.
- **Free-slot listing** – computed from the weekly rules minus active appointments; past times and DST gaps are skipped.
- **Booking flow** – request → *Pending* → professional *Confirms* or *Declines*; clients and professionals can cancel with a reason.
- **No double booking** – checked in the use case **and** guaranteed by a partial unique index in PostgreSQL (tested with parallel requests).
- **Email notifications** – transactional outbox + background dispatcher with retries (Mailpit locally, Brevo HTTP API in production).
- **Professional dashboard** – day/week agenda with pending/confirmed/cancelled counters.
- **Responsive UI** – custom CSS, mobile navigation, no CSS framework.
- **Quality** – unit tests, integration tests with real PostgreSQL (Testcontainers), Playwright E2E tests, GitHub Actions CI, Docker.

## Tech stack

| Layer | Technology |
|---|---|
| Front-end | Blazor Web App (.NET 10, Interactive Server), custom responsive CSS |
| API | ASP.NET Core 10 Minimal APIs, OpenAPI + Swagger UI, ProblemDetails, rate limiting, health checks |
| Domain / use cases | Clean Architecture (Domain · Application · Infrastructure · Api · Web) |
| Data | EF Core 10 + PostgreSQL 16 (Npgsql) |
| Auth | ASP.NET Core Identity, JWT bearer |
| Email | Transactional outbox + `BackgroundService`; MailKit (SMTP) or Brevo (HTTP) |
| Tests | xUnit, EF Core InMemory, WebApplicationFactory + Testcontainers, Microsoft.Playwright |
| DevOps | Docker, Docker Compose, GitHub Actions, Render Blueprint |

## Architecture

```
src/
├── SchedulingApp.Domain          entities, business rules, SlotCalculator (no dependencies)
├── SchedulingApp.Contracts       HTTP DTOs shared by API and Web (no dependencies)
├── SchedulingApp.Application     use cases + ports (IAppDbContext, IEmailSender, IAuthService)
├── SchedulingApp.Infrastructure  EF Core/PostgreSQL, Identity, JWT, outbox dispatcher, email senders
├── SchedulingApp.Api             Minimal API endpoints, auth policies, error handling
└── SchedulingApp.Web             Blazor front-end (talks to the API over HTTP only)
tests/
├── SchedulingApp.UnitTests         domain rules + use cases (EF InMemory)
├── SchedulingApp.IntegrationTests  real API + real PostgreSQL in Docker
└── SchedulingApp.E2ETests          Playwright browser scenarios
```

Design decisions (time zones, double-booking protection, outbox, auth, error model), ER diagram and API table:
**[docs/architecture.md](docs/architecture.md)** · Screens, wireframes and user flows: **[docs/screens-and-flows.md](docs/screens-and-flows.md)**

## Running locally

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Docker](https://www.docker.com/).

### Option A – everything in Docker

```bash
cp .env.example .env          # then set JWT_SECRET (openssl rand -base64 48)
docker compose up --build
```

| | URL |
|---|---|
| Web app | http://localhost:8081 |
| API + Swagger | http://localhost:8080/docs |
| Mailpit (emails) | http://localhost:8025 |

### Option B – run the .NET projects (best for development)

```bash
docker compose up -d postgres mailpit       # database + fake SMTP inbox

dotnet run --project src/SchedulingApp.Api  # http://localhost:5251/docs
dotnet run --project src/SchedulingApp.Web  # http://localhost:5126
```

In `Development` the API uses a local-only JWT key, seeds the demo accounts and prints emails to the console
(set `Email__Provider=Smtp` to deliver them to Mailpit at http://localhost:8025).

### Option C – without Docker

Docker is optional. You only need a PostgreSQL database:

1. Install PostgreSQL for Windows (https://www.postgresql.org/download/windows/) **or** create a free database on
   [Neon](https://neon.tech) and copy its connection string.
2. Point the API to it (stored outside the repo with user-secrets):
   ```bash
   dotnet user-secrets --project src/SchedulingApp.Api set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=scheduling_app;Username=postgres;Password=<your password>"
   ```
   (Neon URLs like `postgresql://user:pass@host/db?sslmode=require` are accepted as-is.)
3. Run the API and the Web app as in option B. The database and demo accounts are created on start-up.

In `Development`, emails are written to the API console (`Email:Provider = Log`). To see them in an inbox, run
Mailpit and set `Email__Provider=Smtp`.

### Database migrations

The schema is created automatically on start-up. To use versioned EF Core migrations:

```bash
dotnet tool restore
dotnet ef migrations add InitialCreate -p src/SchedulingApp.Infrastructure -s src/SchedulingApp.Api -o Persistence/Migrations
```

Once a migration exists, the API applies migrations on start-up instead of `EnsureCreated`.

## Tests

```bash
dotnet test tests/SchedulingApp.UnitTests           # fast, no dependencies
dotnet test tests/SchedulingApp.IntegrationTests    # needs PostgreSQL: Docker (Testcontainers) or INTEGRATION_DB
```

Integration tests run against a real PostgreSQL. With Docker running, Testcontainers starts one automatically.
Without Docker, point them at an existing server (a dedicated database is created/used):

```powershell
$env:INTEGRATION_DB="Host=localhost;Port=5432;Database=scheduling_tests;Username=postgres;Password=<your password>"
dotnet test tests/SchedulingApp.IntegrationTests
```

If neither is available the tests are **skipped** (not failed); CI always runs them.

End-to-end tests run against a running app and are skipped unless `E2E_BASE_URL` is set:

```bash
dotnet build tests/SchedulingApp.E2ETests
pwsh tests/SchedulingApp.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium
# with API (seeded) and Web running:
E2E_BASE_URL=http://localhost:5126 dotnet test tests/SchedulingApp.E2ETests     # PowerShell: $env:E2E_BASE_URL="http://localhost:5126"
```

Set `HEADED=1` to watch the browser. Traces are saved to `playwright-traces/` (open with `pwsh playwright.ps1 show-trace <file>`).

CI (`.github/workflows/ci.yml`) runs build → unit → integration → E2E (Playwright against the real API + Web + PostgreSQL)
→ Docker image builds on every push and pull request.

## Deployment (free tier)

The repository contains a [Render Blueprint](render.yaml):

1. Push to GitHub → Render dashboard → **New + › Blueprint** → select the repo.
2. Render asks for `ConnectionStrings__Database`: paste a PostgreSQL URL (free [Neon](https://neon.tech) database,
   direct connection string). Then it creates `scheduling-app-api` and `scheduling-app-web` (Docker).
   If a service name is taken, rename it and update `Api__BaseUrl` / `Notifications__PublicWebUrl`.
3. Open the web URL and sign in with a demo account.

Notes: free Render services sleep after inactivity (the first request can take ~1 min). Any PostgreSQL works:
`ConnectionStrings__Database` accepts Npgsql strings and URLs (`postgresql://user:pass@host/db?sslmode=require`).

### Configuration

| Variable | Service | Description |
|---|---|---|
| `ConnectionStrings__Database` | API | Npgsql connection string **or** `postgresql://` URL |
| `Jwt__Secret` | API | Signing key, ≥ 32 chars (never commit it) |
| `Seed__DemoData` | API | `true` creates demo accounts on first start |
| `Email__Provider` | API | `Log` (default), `Smtp` or `Brevo` |
| `Email__FromEmail`, `Email__FromName` | API | Sender |
| `Email__Smtp__Host/Port/Username/Password` | API | SMTP settings |
| `Email__Brevo__ApiKey` | API | Brevo API key (HTTP – works where SMTP ports are blocked) |
| `Notifications__PublicWebUrl` | API | Web URL used in email links |
| `Api__BaseUrl` | Web | Public URL of the API |
| `Web__ShowDemoAccounts` | Web | Shows demo credentials on the login page |

## Roadmap

See [ROADMAP.md](ROADMAP.md) and the repository issues.

## License

[MIT](LICENSE)
