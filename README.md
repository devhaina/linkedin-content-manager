# LinkedIn Content Manager

An autonomous LinkedIn content pipeline: a .NET 11 backend orchestrates campaigns, approvals,
and scheduling; a Python CrewAI microservice runs five specialized AI agents
(**Research → Writing → Critique → Optimization → Scheduling**) to produce publish-ready
LinkedIn posts for Fintech, Fintech Security, Crypto, Cryptocurrency, and emerging-AI niches.

Every piece of this system has been run for real against live infrastructure — see
[`docs/architecture.md`](docs/architecture.md) for the verification log, including the real
bugs that surfaced along the way and how they were fixed. Nothing here is simulated: there is
no fake LLM output, no faked publishing confirmation, and no claimed test result that wasn't
actually run.

## Table of contents

- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Quick start (Docker Compose)](#quick-start-docker-compose)
- [Local development (without Docker)](#local-development-without-docker)
- [Angular frontend](#angular-frontend)
- [Environment variables](#environment-variables)
- [API documentation](#api-documentation)
- [Running the tests](#running-the-tests)
- [Database migrations](#database-migrations)
- [Project structure](#project-structure)
- [Known deviations from a generic brief](#known-deviations-from-a-generic-brief)
- [Security notes](#security-notes)

## Architecture

```
Angular dashboard ──▶ ASP.NET Core API ──▶ RabbitMQ ──▶ .NET Worker ──▶ CrewAI (FastAPI, Python)
                           │                                                      │
                           └──────────────▶ PostgreSQL ◀── Worker persists results ┘
                           └──────────────▶ Redis
```

Full component diagram, layering rules, and every verified version/compatibility decision are
in [`docs/architecture.md`](docs/architecture.md). In short:

- **`src/LinkedInContentManager.Domain`** — entities, enums, business rules. Zero dependencies.
- **`src/LinkedInContentManager.Application`** — CQRS (MediatR) commands/queries, FluentValidation.
- **`src/LinkedInContentManager.Infrastructure`** — EF Core/Postgres, Redis, RabbitMQ, the CrewAI
  HTTP client, JWT/password hashing.
- **`src/LinkedInContentManager.Contracts`** — request/response DTOs for the API.
- **`src/LinkedInContentManager.API`** — ASP.NET Core Web API (controllers are thin; all logic
  is in Application).
- **`src/LinkedInContentManager.Worker`** — background service; consumes the RabbitMQ
  content-generation queue and drives the real CrewAI pipeline run.
- **`crewai-service/`** — the Python FastAPI microservice running the five CrewAI agents.
- **`web/`** — the Angular 22 frontend (standalone components, signals, zoneless change
  detection). Talks to the API exclusively through same-origin `/api/*` requests — `ng serve`'s
  dev proxy and nginx's `proxy_pass` in production both forward to the real API, so the app
  never makes a cross-origin request and needs no CORS configuration of its own.

## Prerequisites

- [Docker](https://www.docker.com/) and Docker Compose (bundled with Docker Desktop) — the only
  requirement for the Quick Start path.
- For local (non-Docker) development: [.NET 11 SDK](https://dotnet.microsoft.com/) and
  Python 3.12 or 3.13 (**not** 3.14 — CrewAI requires `>=3.10,<3.14`).
- A real **OpenAI API key** and a real **Serper.dev API key** — the five agents cannot run
  without them. There is no mock/offline mode; this system never fakes an agent's output.

## Quick start (Docker Compose)

```bash
cp .env.example .env
# Edit .env: set OPENAI_API_KEY, SERPER_API_KEY, and JWT_SIGNING_KEY (generate one with
# `python3 -c "import secrets; print(secrets.token_urlsafe(48))"`). The app refuses to start
# without a signing key — see "Security notes" below.

docker compose up -d --build
```

This builds and starts, in order (via healthcheck-gated `depends_on`): PostgreSQL, Redis,
RabbitMQ, the CrewAI service, then the API and Worker, and finally the Angular frontend (`web`),
built and served by nginx. The API applies pending EF Core migrations (including niche seed
data) automatically on startup.

Once it's up:

- **Frontend: <http://localhost:4202>** — the actual app; register, log in, and use it from here.
- API: <http://localhost:8080> — Swagger UI at <http://localhost:8080/swagger>
- Health check (per-dependency status): <http://localhost:8080/health>
- CrewAI service: <http://localhost:8000/health>
- RabbitMQ management UI: <http://localhost:15672> (user/pass from `.env`, default `guest`/`guest`)

> **Note on the frontend's host port:** `docker-compose.yml` maps the `web` service to host port
> **4202**, not Angular's conventional 4200 — on the machine this was built on, an unrelated
> pre-existing process already held both `4200` (IPv4) and `[::1]:4200` (IPv6). If 4202 is free
> on your machine, no change needed; otherwise edit the `ports:` line under `web:`. This only
> affects access from your host browser — nginx inside the container always listens on `80`.

A minimal end-to-end smoke test:

```bash
# Register, then use the returned accessToken as a Bearer token for everything else.
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","password":"SomeStrongPassword123!","displayName":"You"}'
```

> **Note on host ports:** `docker-compose.yml` maps Postgres to host port **5544**, not the
> standard 5432 — on the machine this was built on, something held both 5432 and 5433 (`lsof`
> showed nothing, but Docker refused to bind either). If 5544 collides with something on your
> machine too, change the `ports:` line under `postgres:` to any free host port — it only
> affects access *from your host machine*; containers always reach Postgres as `postgres:5432`
> over the compose network regardless of this mapping.

To stop everything: `docker compose down` (add `-v` to also delete the Postgres volume).

## Local development (without Docker)

Useful for iterating on one service at a time.

**1. Start the infrastructure only:**

```bash
docker compose up -d postgres redis rabbitmq
```

**2. Run the CrewAI service:**

```bash
cd crewai-service
python3.12 -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
cp .env.example .env   # fill in OPENAI_API_KEY / SERPER_API_KEY
uvicorn app.main:app --host 0.0.0.0 --port 8000
```

**3. Set the JWT signing key as a user secret (never in `appsettings.json`):**

```bash
cd src/LinkedInContentManager.API
dotnet user-secrets set "Jwt:SigningKey" "$(python3 -c 'import secrets; print(secrets.token_urlsafe(48))')"
```

**4. Apply migrations and run the API + Worker:**

```bash
# from the repo root
dotnet tool install --global dotnet-ef   # once
dotnet ef database update --project src/LinkedInContentManager.Infrastructure --startup-project src/LinkedInContentManager.Infrastructure

dotnet run --project src/LinkedInContentManager.API    # Swagger at http://localhost:5080/swagger (or whatever ASPNETCORE_URLS you set)
dotnet run --project src/LinkedInContentManager.Worker  # in another terminal
```

A default admin user is seeded for convenience (`admin@linkedin-content-manager.local`) but its
password hash is a deliberate placeholder — it cannot log in. Register a real account via
`POST /api/auth/register` instead; self-registered users get the `ContentManager` role, which
is enough to do everything except whatever you choose to reserve for `Admin` (there's currently
no endpoint to grant that role — do it directly in the `users` table if you need an admin).

## Angular frontend

The `web/` app covers the full user journey: register/login, create campaigns, generate content
(kicks off the real 5-agent CrewAI pipeline), watch it run (polls every 5s while `Pending`/
`Running`), review the research brief and critique, approve/reject/edit the optimized post, and
schedule/reschedule/cancel its publish time.

Run it standalone against an already-running API (Docker or local):

```bash
cd web
npm install
npm start   # ng serve --port 4201; proxies /api/* to http://localhost:8080 (see proxy.conf.json)
```

Open <http://localhost:4201>. (Port 4201, not Angular's usual 4200 — see the port note above;
same reasoning applies to the standalone dev server.)

To build and run it exactly as production does (nginx-fronted, same-origin `/api/*` proxy, no
dev server): `docker compose up -d --build web` — see [Quick start](#quick-start-docker-compose)
for the port it's served on.

There's no separate `.env` for the frontend: `apiBaseUrl` is a relative path (`/api`) baked in at
build time, so dev and prod both resolve it against whichever origin served the page.

**Verification note:** the full user journey (register → login → campaign → generate →
poll-to-completion → approve → schedule) was verified by issuing the exact HTTP requests the
Angular app's `HttpClient` makes, through both proxy paths, and confirming the responses match
the TypeScript models byte-for-byte — see [`docs/architecture.md`](docs/architecture.md). No
browser-based visual check was performed in that session; open the app yourself to confirm
layout, form validation feedback, and interactive behavior.

## Environment variables

| Variable | Where | Required | Notes |
|---|---|---|---|
| `OPENAI_API_KEY` | crewai-service | **Yes** | GPT-4o calls for all five agents. |
| `SERPER_API_KEY` | crewai-service | **Yes** | Trend Researcher agent's web search tool. |
| `CREWAI_SERVICE_API_KEY` | crewai-service + API | No | Shared secret between .NET and the CrewAI service. Blank = open (local dev only). |
| `JWT_SIGNING_KEY` | API | **Yes** | API refuses to start without it. 32+ random characters; never commit a real value. |
| `JWT_ISSUER` / `JWT_AUDIENCE` | API | No | Defaults provided; override if you need to. |
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | postgres, API, Worker | No | Defaults to `postgres`/`postgres`/`linkedin_content_manager`. |
| `RABBITMQ_USER` / `RABBITMQ_PASSWORD` | rabbitmq, API, Worker | No | Defaults to `guest`/`guest`. |
| `CORS_ALLOWED_ORIGIN` | API | No | Angular dev server origin; defaults to `http://localhost:4200`. |

See [`.env.example`](.env.example) (docker-compose variables) and
[`crewai-service/.env.example`](crewai-service/.env.example) (Python service variables) for the
full, authoritative list with comments.

## API documentation

Interactive Swagger UI is served at `/swagger` in every environment (this is an internal/B2B
API, not public-facing, so the docs stay available in "production" too). The raw OpenAPI 3
document is at `/swagger/v1/swagger.json`.

All endpoints except `/api/auth/*` and `/health` require a JWT (`Authorization: Bearer <token>`,
obtained from `POST /api/auth/login`). Mutating endpoints (create/update/delete/generate/
approve/reject/edit) additionally require the `Admin` or `ContentManager` role; `Viewer` can
read but not write.

`GET /api/niches` (added for the Angular frontend's campaign-creation form) lists the five
seeded, active niches — `id`, `name`, `slug`, `description` — same auth rules as every other
read endpoint.

## Running the tests

```bash
dotnet test tests/UnitTests                                            # 63 tests, pure Domain logic, no infra
dotnet test tests/ArchitectureTests                                    # 6 tests, Clean Architecture dependency rules
dotnet test tests/IntegrationTests --filter "FullyQualifiedName!~CrewAiHttpClientTests"
                                                                        # 20 tests, real Testcontainers Postgres + real JWT, fast
dotnet test tests/IntegrationTests --filter "FullyQualifiedName~CrewAiHttpClientTests"
                                                                        # 1 test, the REAL CrewAI pipeline — needs the
                                                                        # crewai-service running locally with real API keys,
                                                                        # costs real OpenAI/Serper usage, takes ~1-2 minutes
```

The last one is skipped (not failed) if the CrewAI service isn't reachable at
`localhost:8000` — see [`docs/architecture.md`](docs/architecture.md) for why this test exists
and what it's caught.

## Database migrations

```bash
dotnet tool install --global dotnet-ef   # once, if you don't have it

# Add a migration after changing an entity:
dotnet ef migrations add YourMigrationName \
  --project src/LinkedInContentManager.Infrastructure \
  --startup-project src/LinkedInContentManager.Infrastructure \
  --output-dir Persistence/Migrations

# Apply migrations (the API also does this automatically on startup):
dotnet ef database update \
  --project src/LinkedInContentManager.Infrastructure \
  --startup-project src/LinkedInContentManager.Infrastructure
```

`CONNECTION_STRING__PostgreSql` as an environment variable overrides the design-time factory's
default connection string (see `ApplicationDbContextFactory`), useful for pointing migrations at
a specific running Postgres instance without touching `appsettings.json`.

## Project structure

```
AutonomousLinkedInManager/
├── src/
│   ├── LinkedInContentManager.API/              # ASP.NET Core Web API, JWT auth, Swagger, health checks
│   ├── LinkedInContentManager.Application/      # CQRS commands/queries, validators, interfaces
│   ├── LinkedInContentManager.Domain/           # Entities, enums, business rules, domain events
│   ├── LinkedInContentManager.Infrastructure/   # EF Core, Npgsql, Redis, RabbitMQ, CrewAI client, JWT
│   ├── LinkedInContentManager.Worker/           # Background service: RabbitMQ consumer
│   └── LinkedInContentManager.Contracts/        # Request/response DTOs
├── crewai-service/
│   ├── app/
│   │   ├── agents/      # The five CrewAI agent definitions
│   │   ├── tasks/       # The five sequential CrewAI tasks
│   │   ├── crew/        # Crew assembly (Process.sequential, memory=True)
│   │   ├── tools/       # SerperDevTool / ScrapeWebsiteTool wiring
│   │   ├── api/         # FastAPI routes + auth dependency
│   │   ├── core/        # Settings
│   │   └── models/      # Pydantic schemas
│   ├── requirements.txt
│   └── Dockerfile
├── web/
│   ├── src/app/
│   │   ├── core/          # Services, models, auth/error interceptors, auth guard
│   │   ├── layout/        # Top nav, toast host
│   │   ├── shared/        # Status badge and other small reusable components
│   │   └── features/      # auth, campaigns, content, schedules — one folder per route group
│   ├── nginx.conf          # production same-origin /api/* proxy + SPA fallback
│   ├── proxy.conf.json     # ng serve's dev-time equivalent
│   └── Dockerfile
├── tests/
│   ├── UnitTests/            # Domain business rules, validators
│   ├── IntegrationTests/      # WebApplicationFactory + Testcontainers Postgres, + the live CrewAI test
│   └── ArchitectureTests/     # NetArchTest Clean Architecture rules
├── docs/architecture.md        # Full architecture + every verified version/compatibility decision
├── docker-compose.yml
├── .env.example
└── global.json                 # pins the .NET 11.0.100 SDK
```

## Known deviations from a generic brief

Every one of these was verified against the real package/runtime, not assumed — full detail
with dates and sources in [`docs/architecture.md`](docs/architecture.md):

- **MediatR** is pinned to **12.5.0** (Apache-2.0), not a newer version — MediatR 13+ requires a
  paid commercial license above certain usage.
- **AutoMapper is not used at all.** Its free (MIT) version has an unpatched high-severity
  advisory; the paid version fixes it. DTO mapping is done by hand instead.
- **EF Core / Npgsql provider** is pinned to the **10.0.x** GA line, not 11.0 — the PostgreSQL
  provider for EF Core 11 is still release-candidate-only as of this writing.
- **.NET 11** itself is real and GA (STS, released 2026-11-10) — this is the one place the brief
  asked for something that turned out to already exist and work.

## Security notes

- **Never commit a real `Jwt:SigningKey`, `OPENAI_API_KEY`, `SERPER_API_KEY`, or database
  password.** `appsettings.json` ships with an empty signing key on purpose — the API throws a
  clear error on startup rather than silently running with no real auth. Use
  `dotnet user-secrets` locally or environment variables in any real deployment.
- Passwords are hashed with ASP.NET Core Identity's `PasswordHasher<T>` (PBKDF2, salted).
  Refresh tokens are opaque random values; only their SHA-256 hash is ever persisted, and every
  refresh rotates the token (old one becomes invalid immediately).
- **Publishing is never faked.** `PublishingSchedule.MarkPublished` is the only path to a
  "Published" status, and it throws if given an empty external post ID — there is no code path
  that can mark something published without a real confirmation from an actual LinkedIn API
  call, and this repository does not implement that LinkedIn integration (it stops at
  "Scheduled"/ready-to-publish, per the brief's explicit instruction that scheduling must be
  evidence-aware and never claim a real publish without one).
