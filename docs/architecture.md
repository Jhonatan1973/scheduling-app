# Architecture

Scheduling App is a full-stack .NET 10 application organised with **Clean Architecture**: business rules in the
centre, frameworks at the edges. Every arrow below points *inwards* (towards fewer dependencies).

```mermaid
flowchart LR
    subgraph Browser
        UI[Blazor UI<br/>Interactive Server]
    end

    subgraph WebHost["SchedulingApp.Web (ASP.NET Core)"]
        Pages[Razor components] --> ApiClient[Typed ApiClient<br/>JWT bearer]
    end

    subgraph ApiHost["SchedulingApp.Api (ASP.NET Core)"]
        Endpoints[Minimal API endpoints<br/>ProblemDetails · OpenAPI · Rate limiting]
        Dispatcher[[OutboxEmailDispatcher<br/>BackgroundService]]
    end

    subgraph Core
        App[SchedulingApp.Application<br/>use cases]
        Domain[SchedulingApp.Domain<br/>entities · rules · SlotCalculator]
        Contracts[SchedulingApp.Contracts<br/>DTOs]
    end

    Infra[SchedulingApp.Infrastructure<br/>EF Core · Identity · JWT · Email]
    DB[(PostgreSQL)]
    Mail[(SMTP / Brevo)]

    UI <-- SignalR --> Pages
    ApiClient -- HTTPS/JSON --> Endpoints
    Endpoints --> App
    App --> Domain
    App --> Contracts
    Pages --> Contracts
    Infra --> App
    Endpoints --> Infra
    Infra --> DB
    Dispatcher --> DB
    Dispatcher --> Mail
```

| Project | Responsibility | Depends on |
|---|---|---|
| `SchedulingApp.Domain` | Entities, state transitions (`Appointment.Confirm/Cancel`), `SlotCalculator`, business errors | nothing |
| `SchedulingApp.Contracts` | HTTP request/response records shared by API and front-end | nothing |
| `SchedulingApp.Application` | Use cases (`AppointmentService`, `AvailabilityService`, …), ports (`IAppDbContext`, `IEmailSender`, `IAuthService`), email composition | Domain, Contracts, EF Core abstractions |
| `SchedulingApp.Infrastructure` | EF Core + PostgreSQL, ASP.NET Core Identity, JWT, outbox dispatcher, SMTP/Brevo senders, seeding | Application |
| `SchedulingApp.Api` | Minimal API endpoints, auth policies, ProblemDetails, OpenAPI, health checks | Infrastructure |
| `SchedulingApp.Web` | Blazor Web App (Interactive Server). Talks to the API **only over HTTP** | Contracts |

## Key decisions

### Time zones done right
Professionals define weekly hours in **their own local time** (`Professional.TimeZoneId`, IANA id).
`SlotCalculator` converts local slots to UTC, skips non-existent local times on DST changes and
the database only stores UTC (`timestamptz`). The API returns `DateTimeOffset` values in the
professional's offset, so the UI always shows the professional's clock with a time-zone label.

### No double booking
1. The use case recomputes the slots and only accepts a start time that is offered and free.
2. A **partial unique index** (`ProfessionalId, StartUtc WHERE Status <> Cancelled`) makes the database reject
   concurrent requests that passed step 1 at the same time. The violation is mapped to `409 Conflict`.
   An integration test fires 6 parallel bookings and asserts exactly one succeeds.

### Transactional outbox for emails
Booking, confirmation and cancellation write rows to `OutboxEmails` **in the same `SaveChanges`** as the
appointment change. `OutboxEmailDispatcher` (a `BackgroundService`) delivers them with exponential backoff
(30 s, 1 min, 2 min, …, max 5 attempts).
This replaces the RabbitMQ + worker design: same reliability guarantees (no lost email, no email for a rolled-back
change), zero extra infrastructure, so the app runs on free hosting tiers.

```mermaid
sequenceDiagram
    actor Client
    participant Web as Blazor Web
    participant API
    participant DB as PostgreSQL
    participant Worker as OutboxEmailDispatcher
    participant Mail as Email provider

    Client->>Web: picks 10:00 slot
    Web->>API: POST /api/appointments
    API->>DB: recompute slots for that day
    API->>DB: INSERT appointment (Pending) + 2 OutboxEmails (one transaction)
    API-->>Web: 201 Created
    loop every 5 s
        Worker->>DB: pending emails due
        Worker->>Mail: send
        Worker->>DB: mark Sent / schedule retry
    end
```

### Authentication
ASP.NET Core Identity (password hashing, lockout after 5 failures) + stateless **JWT** with `role` and
`professional_id` claims. Authorization policies `Client` and `Professional` protect the endpoints, and ownership is
checked inside the use cases (another professional gets `404`, not `403`, so ids are not leaked).
The Blazor app keeps the token in `ProtectedLocalStorage` (encrypted with Data Protection).

### Errors
Application exceptions → RFC 7807 `ProblemDetails` with a stable `code` (e.g. `slot.taken`) via `IExceptionHandler`:

| Exception | HTTP |
|---|---|
| `ValidationException` | 400 (with `errors` dictionary) |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `DomainException` | 422 |

## Data model

```mermaid
erDiagram
    AspNetUsers ||--o| Professionals : "has profile (role Professional)"
    AspNetUsers ||--o{ Appointments : "books (role Client)"
    Professionals ||--o{ AvailabilityRules : "weekly hours"
    Professionals ||--o{ Appointments : receives

    AspNetUsers {
        string Id PK
        string Email
        string FullName
        string PasswordHash
    }
    Professionals {
        uuid Id PK
        string UserId FK
        string DisplayName
        string Email
        string Specialty
        string Bio
        string TimeZoneId "IANA, e.g. America/Sao_Paulo"
        int SlotDurationMinutes
    }
    AvailabilityRules {
        uuid Id PK
        uuid ProfessionalId FK
        int DayOfWeek
        time StartTime "local time"
        time EndTime "local time"
    }
    Appointments {
        uuid Id PK
        uuid ProfessionalId FK
        string ClientId FK
        timestamptz StartUtc "unique per professional while active"
        timestamptz EndUtc
        int Status "Pending, Confirmed, Cancelled"
        string Notes
        int CancelledBy
        string CancellationReason
    }
    OutboxEmails {
        uuid Id PK
        string ToEmail
        string Subject
        int Status "Pending, Sent, Failed"
        int Attempts
        timestamptz NextAttemptAtUtc
    }
```

### Appointment lifecycle

```mermaid
stateDiagram-v2
    [*] --> Pending: client requests a slot
    Pending --> Confirmed: professional confirms
    Pending --> Cancelled: client cancels / professional declines
    Confirmed --> Cancelled: client or professional cancels (before start)
    Cancelled --> [*]
```

## API overview

| Method | Route | Who |
|---|---|---|
| POST | `/api/auth/register` · `/api/auth/login` | anyone |
| GET | `/api/auth/me` | signed in |
| GET | `/api/professionals?search=` · `/{id}` · `/{id}/availability` · `/{id}/slots?date=` | anyone |
| GET/PUT | `/api/professionals/me` | Professional |
| GET/POST/DELETE | `/api/professionals/me/availability[/{ruleId}]` | Professional |
| POST | `/api/appointments` | Client |
| GET | `/api/appointments/mine` | Client |
| GET | `/api/appointments/schedule?from=&to=` | Professional |
| POST | `/api/appointments/{id}/confirm` | Professional |
| POST | `/api/appointments/{id}/cancel` | owner (Client or Professional) |

Interactive docs: `/docs` (Swagger UI over the built-in `/openapi/v1.json`). Health: `/health`.
