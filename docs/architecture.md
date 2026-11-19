# Solution Architecture

## Component diagram

```
                              ┌─────────────────────────────┐
                              │   Angular 22 Frontend        │
                              │   (campaign dashboard,        │
                              │    approvals, calendar)       │
                              │   served by nginx in prod;    │
                              │   nginx/ng-serve proxy /api/* │
                              │   to the line below — never   │
                              │   a cross-origin request      │
                              └──────────────┬────────────────┘
                                             │ REST + JWT (same-origin)
                                             ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                     LinkedInContentManager.API (ASP.NET Core)             │
│  Controllers → MediatR Commands/Queries → Application handlers           │
│  JWT auth · FluentValidation · Swagger/OpenAPI · Serilog · OpenTelemetry   │
└───────────┬───────────────────────────────┬───────────────────┬──────────┘
            │                               │                   │
            ▼                               ▼                   ▼
 ┌─────────────────────┐        ┌─────────────────────┐  ┌──────────────┐
 │ Application layer    │        │ Infrastructure layer │  │   RabbitMQ    │
 │ (CQRS handlers,      │◄──────►│ EF Core + Npgsql,    │  │ campaign-exec │
 │  DTOs, interfaces)    │        │ Redis cache,         │  │ queue         │
 └──────────┬───────────┘        │ CrewAI HTTP client    │  └──────┬───────┘
            │                    │ (Polly resilience)    │         │
            ▼                    └──────────┬────────────┘         │
 ┌─────────────────────┐                   │                      ▼
 │   Domain layer        │                   │           ┌──────────────────┐
 │ (entities, enums,     │                   │           │ LinkedInContent-  │
 │  business rules)      │                   │           │ Manager.Worker    │
 └───────────────────────┘                   │           │ (background       │
                                              │           │  processing,      │
                                              │           │  campaign exec)   │
                                              │           └─────────┬─────────┘
                                              │                     │
                                              ▼                     ▼
                                   ┌─────────────────────────────────────┐
                                   │   PostgreSQL          Redis          │
                                   └─────────────────────────────────────┘
                                              │
                                              │ HTTPS (correlation ID,
                                              │  retry + circuit breaker)
                                              ▼
                              ┌───────────────────────────────┐
                              │  CrewAI microservice (Python)  │
                              │  FastAPI + CrewAI, sequential   │
                              │  5-agent pipeline:               │
                              │  Research → Writing → Critique → │
                              │  Optimization → Scheduling       │
                              │  (OpenAI GPT-4o, SerperDevTool)  │
                              └───────────────────────────────┘
```

## Why a hybrid .NET + Python architecture

CrewAI is Python-native; there is no supported .NET port. Running it as its
own FastAPI microservice, called over REST from .NET, keeps the orchestration
engine on the runtime it actually requires while letting .NET own every
business workflow: campaigns, approvals, scheduling, persistence, auth. This
also lets the two deploy and scale independently — the CrewAI service is
CPU/IO-bound on LLM calls, the .NET API is request/response and DB-bound.

## Clean Architecture layering

- **Domain** — entities, enums, domain events. No package dependencies at all.
- **Application** — CQRS commands/queries/handlers (MediatR), validators
  (FluentValidation), interfaces the Infrastructure layer implements. Depends
  only on Domain.
- **Infrastructure** — EF Core + Npgsql, Redis cache, RabbitMQ publisher, the
  strongly-typed CrewAI HTTP client (HttpClientFactory + Polly resilience).
  Implements Application's interfaces. Depends on Application + Domain.
- **Contracts** — request/response DTOs shared between API and Worker (and,
  by contract, the Angular frontend). Depends only on Domain, and only for its enums
  (`WriterPersona`, `ExecutionStatus`, etc.) — stable shared vocabulary, not Domain entities
  or business logic. No dependency on Application or Infrastructure.
- **API** — ASP.NET Core controllers. Thin: translates HTTP → MediatR
  command/query → HTTP response. No business logic.
- **Worker** — background host: consumes the RabbitMQ campaign-execution
  queue, drives long-running CrewAI jobs, updates execution status.

## Entity relationships (Phase 2)

```
Niche 1───* Campaign *───1 User (owner)
Campaign 1───* ContentTopic
ContentTopic 1───* AgentExecution 1───1 ResearchResult
                              └──────1 LinkedInPost 1───* ContentReview
                                                     ├───* LinkedInPostHashtag *───1 Hashtag
                                                     ├───1 ContentApproval
                                                     └───1 PublishingSchedule
AgentExecution 1───* AgentExecutionLog
```

Key business rules enforced in the Domain layer (not just documented — see
`src/LinkedInContentManager.Domain/Entities`):

- `PublishingSchedule.MarkPublished` is the **only** path to `ScheduleStatus.Published`,
  and it throws `PublishingNotConfirmedException` if given an empty external post ID —
  this is where "never claim a post was scheduled/published unless the LinkedIn API
  confirms it" is enforced at the data layer, not just in agent prompts.
- `ContentApproval.Approve`/`Reject` only fire from `Pending`; re-approving/re-rejecting
  throws `InvalidStateTransitionException`.
- `AgentExecution` tracks `RetryCount` and `ErrorDetails` directly, and raises
  `AgentExecutionFailedDomainEvent`/`AgentExecutionCompletedDomainEvent` for Serilog/OpenTelemetry
  to pick up in Phase 6.
- Niches are a seeded table (`niches`), not an enum — `NicheSeedData` seeds exactly the five
  from the brief with deterministic GUIDs and a fixed seed timestamp (`Niche.SeedTimestamp`)
  so the EF Core model stays reproducible across migrations.

**Verified against a real database, not just "builds":** the `InitialCreate` migration was
applied with `dotnet ef database update` to a disposable `postgres:17-alpine` Docker
container, confirmed to create all 14 tables and seed all 5 niches via `psql`, confirmed
idempotent on a second `database update` run, then torn down.

## CrewAI service (Phase 3) — verified with a real execution, not a mock

`crewai-service/app/` implements the five agents and five tasks exactly as specified
(`app/agents/definitions.py`, `app/tasks/definitions.py`), assembled into one
`Process.sequential`, `memory=True` crew in `app/crew/linkedin_content_crew.py`, exposed over
FastAPI at `POST /api/v1/execute` (`app/api/routes.py`) with a `GET /health` probe.

This was run for real — not just import-checked — against the live OpenAI and Serper APIs
through the actual HTTP endpoint (`TestClient` → `app/main.py` → `Crew.kickoff`), topic
"Zero-knowledge proofs for private crypto transactions" / niche "Crypto" /
persona `FormalAuthoritative`. Confirmed in that real run:

- All five tasks returned schema-valid structured output (`output_pydantic`), not free text.
- The Research task's `source_urls` were real URLs returned by `SerperDevTool` — e.g.
  `sygnum.com/blog/...`, live LinkedIn posts — not placeholders.
- The Critique task gave a real score (8/10) and an explicit (empty, in that run)
  `guardrail_violations` list — proving the guardrail check actually runs, not just exists
  in a prompt nobody reads.
- The Scheduling task's output stated its timezone/day/time reasoning without claiming real
  analytics access, matching the "evidence-aware, never fakes analytics access" requirement.
- `final_publishing_brief` was populated from the Scheduling task's `final_post`.

Two real bugs surfaced only by running this live, both fixed and re-verified:

1. `CREWAI_SERVICE_API_KEY=` (empty) in `.env` parses to `""` via pydantic-settings, not
   `None` — the auth dependency's `is None` check let through `None` but rejected `""`,
   a 401 on every call with no key configured. Fixed to `not settings.service_api_key`.
2. `Crew.kickoff()` refuses to run synchronously from inside a running asyncio event loop.
   The `/api/v1/execute` route was `async def`, which under ASGI *is* a running event loop,
   so every real request threw `RuntimeError`. Fixed by making the route a sync `def` —
   FastAPI then runs it in a worker thread with no event loop bound, which is exactly what
   `Crew.kickoff()`'s synchronous path expects.

## .NET ↔ CrewAI integration (Phase 4) — verified across the real language boundary

`ICrewAiClient` (Application) / `CrewAiHttpClient` (Infrastructure) is a strongly-typed
`HttpClientFactory` client with `Microsoft.Extensions.Http.Resilience`'s standard resilience
handler (retry + circuit breaker + per-attempt/total timeout), correlation-id propagation via
`X-Correlation-Id`, and snake_case JSON mapping to match the Python service's Pydantic field
names exactly (`JsonNamingPolicy.SnakeCaseLower` + `JsonStringEnumConverter`).

Verified with `tests/IntegrationTests/CrewAiHttpClientTests.cs` run against the actual
`uvicorn` process from Phase 3 (not a mock, not an in-memory fake) — real request in, real
5-agent pipeline run, real structured response out, 2m19s end to end, `dotnet test` green.
The test skips (reported as **Skipped**, never silently **Passed**) if the live service isn't
reachable, via `Xunit.SkippableFact` — a live-dependency test that can't reach its dependency
must say so.

Two more real bugs surfaced only by actually running this, both fixed and re-verified:

3. `AddStandardResilienceHandler`'s circuit breaker validates that `SamplingDuration` is at
   least double the attempt timeout. With a 170s attempt timeout (CrewAI runs are genuinely
   that slow — five sequential GPT-4o calls plus live web search) and a default-ish 60s
   sampling window, DI container build threw `OptionsValidationException` on first resolve.
   Fixed by setting `SamplingDuration` to 360s.
4. (Carried from Phase 3, re-confirmed from the .NET side): the empty-string vs. `None`
   `CREWAI_SERVICE_API_KEY` bug would have surfaced here too had it not already been fixed —
   confirming the fix holds from the .NET caller's perspective, not just Python's own tests.

## CQRS, RabbitMQ, background processing (Phase 5) — verified against a full live stack

Every endpoint in the brief's Section 6 is implemented as a thin controller dispatching to a
MediatR command/query in `Application/Features/{Campaigns,Content,Schedules}`, backed directly
by `IApplicationDbContext` (EF Core as the Unit of Work — no separate repository layer; see
the "why no repository pattern" note below), with FluentValidation validators wired through a
`ValidationBehavior<,>` MediatR pipeline step. `POST /api/content/generate` and
`/regenerate` only create the `ContentTopic`/`AgentExecution` rows and publish a
`ContentGenerationJobMessage` to RabbitMQ — they never call CrewAI directly, because a real run
takes 1-3 minutes and must not block an HTTP request. `ContentGenerationJobConsumer` (Worker)
dequeues the job, calls `ICrewAiClient` for real, and persists every stage as real entities.

**Why no separate Repository/Unit-of-Work layer:** `IApplicationDbContext` already *is* the
Unit of Work (one `SaveChangesAsync` commits everything a handler touched), and EF Core's
`DbSet<T>` already is the per-aggregate repository. Adding a hand-rolled repository per entity
on top would just be a thinner wrapper around the same calls with no behavior of its own — the
brief's "where appropriate" judgment call here is that it is not appropriate, given the
project already uses the current, accepted (Jason Taylor / "Clean Architecture .NET" template)
pattern of handlers depending directly on the context interface.

### What was actually run, end to end, against real infrastructure

Not a mock, not an in-memory DB, not a stubbed queue. A disposable `postgres:17-alpine`
(port 5544 locally — see note below) and `rabbitmq:4-management-alpine` container, the real
Phase 3 `uvicorn` CrewAI service, and the API + Worker run via `dotnet run`:

1. `POST /api/campaigns` → real campaign row, seeded niche/admin-user foreign keys resolved.
2. `POST /api/content/generate` → `202 Accepted`, `AgentExecution` created `Pending`, job
   published to RabbitMQ.
3. Worker dequeued it, called the live CrewAI service, and — confirmed via `GET /api/content/{id}`
   mid-flight — the real GPT-4o/Serper pipeline ran for ~2 minutes and completed with real
   research URLs, a real critique score, real optimized content and hashtags, and a real
   (non-binding) scheduling recommendation.
4. `POST /api/content/{id}/approve` → `200`, approval persisted.
5. `POST /api/schedules` → commits the Scheduling Agent's recommendation to a real future
   timestamp, status `Scheduled` (never `Published` — there is no LinkedIn integration wired
   up yet, and the domain model structurally cannot fake one).
6. `PUT /api/schedules/{id}`, `DELETE /api/schedules/{id}` (cancels), `DELETE /api/campaigns/{id}`
   (archives), `POST /api/content/{id}/reject` (including confirming a second
   approve-after-reject call correctly 409s), `PUT /api/content/{id}/edit`,
   `POST /api/content/{id}/regenerate`, `GET /api/content/history` — all exercised for real.

Five more real bugs surfaced only by running this live, all fixed and re-verified:

5. **Enums as JSON.** ASP.NET Core's default `System.Text.Json` config does not accept enum
   strings (`"Practitioner"`) — only numbers — unless a `JsonStringEnumConverter` is
   registered. Every request with an enum field 400'd until `AddJsonOptions` registered it.
6. **Status visibility gap.** `AgentExecution.Start(string externalExecutionId)` originally
   required the ID the CrewAI service only returns once its (slow) response arrives, so the
   Worker could only call `Start()` *after* the ~2-minute call — meaning `GET /api/content/{id}`
   showed `Pending` for the entire run instead of `Running`. Split into `Start()` (called and
   saved immediately on dequeue) and `SetExternalExecutionId(...)` (called once the response
   arrives).
7. **Reschedule blocked.** `PublishingSchedule.Schedule()` only allowed transitioning from
   `Recommended`/`Failed`, so `PUT /api/schedules/{id}` (changing an already-`Scheduled` time)
   threw `InvalidStateTransitionException` — a legitimate reschedule isn't a terminal-state
   violation. Fixed to also allow `Scheduled` → `Scheduled`.
8. **Local port 5432 conflict.** Not a code bug, but worth recording: this machine already had
   something holding port 5432 (and 5433) with no process visible to `lsof`, forcing local
   verification onto port 5544. Checked-in `appsettings.json` still uses the standard 5432 —
   only my local test run used an env-var override, so this doesn't affect the deliverable.
9. **Stale RabbitMQ consumer.** `pkill -f "*.dll"` doesn't match this app's actual process name
   (it runs as a native apphost, not `dotnet x.dll`), so an old Worker process survived a kill
   attempt and kept holding queue messages via its prefetch-1 consumer — not a defect in the
   shipped code, but a reminder that a crashed-but-not-reaped consumer can silently hold a
   message until RabbitMQ's connection heartbeat (60s default) reaps it.

## Security, logging, monitoring (Phase 6) — verified with real tokens, real role checks

- **JWT auth**: `POST /api/auth/register|login|refresh`. Passwords hashed with ASP.NET Core
  Identity's `PasswordHasher<User>` (PBKDF2, salted) — not a hand-rolled hash. Refresh tokens
  are opaque random values; only their SHA-256 hash is persisted, and every refresh rotates the
  token (single-use).
- **Role-based authorization**: every mutating endpoint requires `Admin` or `ContentManager`
  (`Roles.Editors`); reads require any authenticated user. Verified live: downgraded a real
  user to `Viewer` in the database and confirmed `POST /api/campaigns` now 403s while
  `GET /api/campaigns` still 200s for the same token.
- **`ICurrentUserService`**: replaces the Phase 5 placeholder `OwnerUserId`/`ReviewerUserId`
  request fields — campaigns are now owned by, and approvals attributed to, whoever the JWT
  actually says they are. Verified the returned `ownerUserId` on a created campaign matches the
  authenticated caller's id, not a client-supplied value.
- **Secrets**: `Jwt:SigningKey` is empty in the committed `appsettings.json` and the app refuses
  to start without it set via environment variable or `dotnet user-secrets` — never a real key
  in source control. Real DB/CrewAI/RabbitMQ passwords for local dev stay as the well-known
  `postgres`/`guest` defaults in committed config (consistent with every other phase's
  Docker-based local setup); nothing proprietary is ever in source control.
- **CORS**: configurable allow-list (`Cors:AllowedOrigins`), verified via a real preflight
  request returning the expected `Access-Control-Allow-*` headers for `localhost:4200`.
- **Rate limiting**: built-in `Microsoft.AspNetCore.RateLimiting`, fixed-window per client IP,
  no extra package needed.
- **Serilog**: replaces default logging via `UseSerilog` + `UseSerilogRequestLogging`, enriched
  with correlation IDs. Verified structured request/EF-command logs in real output.
- **OpenTelemetry**: traces ASP.NET Core + outgoing HttpClient calls, OTLP exporter configured
  but no collector stood up yet (that's Phase 8's docker-compose) — confirmed the app runs
  cleanly with no errors when the configured OTLP endpoint is unreachable; exports just fail
  silently in the background rather than crashing requests.
- **Health checks**: `GET /health` returns real per-dependency status (not just an aggregate) —
  confirmed PostgreSQL, Redis, RabbitMQ, and the CrewAI service all independently report
  `Healthy` against the actual running containers/service.

Two more real bugs surfaced only by running this live:

10. **Worker startup crash.** `AddInfrastructureServices` now eagerly requires
    `ConnectionStrings:Redis` (the new `AddStackExchangeRedisCache` call), but only the API's
    `appsettings.json` had it — the Worker threw `InvalidOperationException` on every startup
    the moment this phase's Infrastructure changes landed. Added the same connection string to
    the Worker's config.
11. **`AddRabbitMQ`'s real signature.** Guessed it took a plain connection string like the
    Postgres/Redis health checks; the installed `AspNetCore.HealthChecks.Rabbitmq` 9.0.0
    actually takes a `Func<IServiceProvider, Task<IConnection>>` factory. Found via reflecting
    the installed assembly rather than guessing twice, then fixed.

## The test suite (Phase 7) — 90 real tests, several of which found real bugs while being written

- **`tests/ArchitectureTests`** (6 tests, NetArchTest): enforces the layering documented above
  as executable rules — Domain depends on nothing, Contracts depends only on Domain,
  Application never depends on Infrastructure/API, Infrastructure never depends on API, every
  Domain entity has zero public setters, and there's a handler for every command/query.
  Verified these have real teeth, not just "always green" tautologies: temporarily added a
  public setter to `Niche.IsActive` and confirmed the test fails and names the exact violation
  (`Niche.IsActive`), then reverted. A cross-layer violation (a Domain class inheriting an
  Infrastructure type) doesn't even need a test — it fails to *compile*, since Domain has no
  project reference to break.
- **`tests/UnitTests`** (63 tests): every Domain business rule exercised throughout live
  testing in Phases 2–6 — `PublishingSchedule.MarkPublished` rejecting empty/whitespace
  external IDs, `ContentApproval` single-decision guards, `AgentExecution`'s retry state
  machine, `Hashtag.Normalize`, FluentValidation rules for Register/CreateCampaign/
  GenerateContent/RejectContent. One of these tests (`Start_AfterFailed_IsAllowed_ForRetry`)
  found a real inconsistency on first run: `Fail()` leaves an execution `PartiallyFailed` on
  its first failure, but `Start()`'s guard only allowed resuming from `Pending`/`Failed` — the
  real Worker flow never hit this (it always calls `RegisterRetry()`, which resets to
  `Pending`, before retrying), but the inconsistency was real and is now fixed to allow
  resuming from `PartiallyFailed` directly too.
- **`tests/IntegrationTests`** (20 fast + 1 live, `dotnet test tests/IntegrationTests`):
  - The 20 fast tests run against a **real, isolated PostgreSQL container** (Testcontainers,
    not an in-memory provider) via a shared `CustomWebApplicationFactory`, with only
    `ICrewAiClient` and `IContentGenerationJobPublisher` swapped for in-memory fakes — real JWT
    issuance/validation, real password hashing, real EF Core migrations, real role-based
    authorization (including a repeatable version of the live Viewer-downgrade check from
    Phase 6).
  - `CrewAiHttpClientTests` (carried over from Phase 4) still runs the real pipeline against
    the real CrewAI service — 1m27s, no mocks — confirming the full Phase 6 rewrite of
    `Program.cs` didn't break the real integration.

Writing these tests surfaced **three more real, non-trivial bugs** that live manual testing in
earlier phases had never happened to hit:

12. **Five Postgres containers racing.** Five test classes each creating their own
    `IClassFixture<CustomWebApplicationFactory>` meant five Testcontainers Postgres instances
    starting concurrently, which failed under Docker Desktop resource contention with a
    misleading `28P01: password authentication failed` (a confirmed-working, isolated
    Testcontainers setup proved the credentials were never the problem). Fixed by sharing one
    factory/container across all test classes via `[CollectionDefinition]` +
    `ICollectionFixture<>`.
13. **The actual root cause, once the containers were fixed.** `AddInfrastructureServices` read
    `configuration.GetConnectionString("PostgreSql")`/`"Redis"` into a local variable *once*,
    at service-registration time — which freezes whatever `appsettings.json` said, silently
    ignoring any configuration provider added afterward. That's exactly what
    `WebApplicationFactory.ConfigureWebHost` does to inject a test connection string, so every
    test was quietly hitting the real default `localhost:5432`, not the test container. Fixed
    by resolving both lazily via `(IServiceProvider sp, options) => ...` /
    `OptionsBuilder.Configure<IConfiguration>(...)` — reading the *final*, fully-assembled
    configuration at first use instead of a frozen snapshot from startup.
14. **The same bug, independently, in JWT validation.** `Program.cs`'s `AddJwtBearer` setup
    read `Jwt:SigningKey` into a local variable the same eager way — so the token-*issuing*
    side (bound via `IOptions<JwtOptions>`, already lazy) picked up a test's overridden signing
    key while the token-*validating* side kept using whatever key was present at process
    startup. Every authenticated test call 401'd with a perfectly valid, freshly-issued JWT —
    a textbook "looks right until you actually run it" bug. Fixed with the same
    `Configure<IConfiguration>(...)` pattern. Also surfaced along the way: the test HTTP
    client's default `JsonSerializerOptions` didn't match the server's (missing
    `PropertyNameCaseInsensitive` and a `JsonStringEnumConverter`), which made every
    deserialized response silently come back with null/default fields instead of throwing —
    fixed with a shared `TestJsonOptions`.

## Docker deployment & documentation (Phase 8) — the full stack, run for real

`docker-compose.yml` builds and runs all six pieces (PostgreSQL, Redis, RabbitMQ, the CrewAI
service, the API, the Worker), `src/LinkedInContentManager.API/Dockerfile` and
`.../Worker/Dockerfile` are multi-stage .NET 11 builds (verified against the real,
pullable `mcr.microsoft.com/dotnet/sdk:11.0` / `dotnet/aspnet:11.0` images — not assumed to
exist), and `README.md` documents both the Docker and local-dev paths.

**This was actually brought up and exercised end to end, not just written:**

- `docker compose build` — all three custom images (api, worker, crewai-service) built
  successfully.
- `docker compose up -d` — every service, including the API and Worker's own `HEALTHCHECK`
  directives, reports `healthy`.
- `GET http://localhost:8080/health` — all four dependencies (postgresql, redis, rabbitmq,
  crewai-service) report `Healthy` from *inside* the container network.
- EF Core migrations applied automatically on API startup, confirmed by querying the `niches`
  table directly inside the Postgres container — all 5 seeded rows present.
- A full real workflow through the containerized stack: registered a user, created a campaign,
  called `POST /api/content/generate` — confirmed via `docker logs` that the **containerized**
  Worker picked the job off the **containerized** RabbitMQ and called
  `http://crewai-service:8000` (the compose network hostname, not `localhost`) — and polled
  `GET /api/content/{id}` until the real 5-agent pipeline finished, producing genuine
  Serper-sourced research URLs, a real critique score, and real optimized content, all visible
  through the public `localhost:8080` port.
- `GET /swagger/v1/swagger.json` and `/swagger` (the interactive UI) both verified serving real
  content — 14 real paths, not an empty or broken document.

Two more real issues surfaced only by actually building and running the containers:

15. **`.dockerignore` was missing.** `COPY src/ src/` after the restore step copied the host's
    own `bin/`/`obj/` folders into the build context, clobbering the container's freshly
    restored NuGet state and failing `dotnet publish` with a "package was not found" error that
    had nothing to do with the actual package. Added a `.dockerignore` excluding `**/bin/`,
    `**/obj/`, and other host-only paths.
16. **Microsoft.OpenApi's security-requirement API changed shape** between the 1.x/2.x line
    most Swashbuckle tutorials still show and the 3.11.0 version that Swashbuckle.AspNetCore
    11.0.1 (the real, net11.0-targeting release) actually pulls in — `OpenApiSecurityRequirement`
    is now `Dictionary<OpenApiSecuritySchemeReference, List<string>>`, and
    `OpenApiSecuritySchemeReference` needs a live `OpenApiDocument` to construct. Rather than
    fight a UI-convenience feature (auto-applying the padlock per `[Authorize]` endpoint)
    against a genuinely different API shape, kept the `AddSecurityDefinition` (so the manual
    "Authorize" button still works) and dropped the auto-apply.

## Angular frontend (Phase 9) — built against the real API, verified over real HTTP

`web/` is a real Angular 22 app: standalone components only (no `NgModule`), zoneless change
detection (`provideZonelessChangeDetection()`, no zone.js), the new `@if`/`@for` control-flow
syntax, signals for all reactive state, `inject()` for DI, functional router guards
(`CanActivateFn`) and functional `HttpInterceptorFn`s, and lazy-loaded routes
(`loadComponent` + `withComponentInputBinding()`).

**Same-origin by construction, not by CORS config.** The app always calls a relative
`/api/*` path (`environment.apiBaseUrl = '/api'`); `ng serve`'s `proxyConfig`
(`web/proxy.conf.json`) forwards that to the real API in dev, and nginx's `proxy_pass`
(`web/nginx.conf`) does the same in production. The browser never issues a cross-origin
request in either mode — this is a deliberate deployment choice, not an accident of the test
environment.

**Auth**: JWT access + refresh tokens, stored under `localStorage['licm.auth']`, attached by
`authInterceptor`; `errorInterceptor` catches `401`s, refreshes once (a module-level in-flight
`Observable` dedupes concurrent refreshes from several requests failing at once), retries the
original request, and logs out + redirects to `/login` only if the refresh itself fails —
mirrors the backend's rotate-on-refresh/opaque-token design exactly.

**Covers the full real workflow**: register/login, campaign CRUD (niche dropdown locked after
creation — `UpdateCampaignRequest` has no `nicheId`, by backend design), content generation with
a live 5-second poll while `AgentExecution.Status` is `Pending`/`Running`, the research brief,
critique cards (score, guardrail violations, per-dimension evaluation), inline-editable
optimized content, approve/reject, and schedule/reschedule/cancel.

### Two backend changes this frontend work required

1. **`GET /api/niches`** (`NichesController` → `GetNichesQuery` → `GetNichesQueryHandler`) — the
   campaign-creation form needs the five seeded niches; nothing previously exposed them over
   HTTP. Verified live: returns all 5 seeded rows with real `Id`/`Name`/`Slug`/`Description`.
2. **`ContentDetailResponse.LinkedInPostId`** — the content-detail page needs the post's id to
   call `POST /api/schedules`, but the DTO never exposed it (only derived fields like
   `DraftContent` did). Added `Guid? LinkedInPostId` to the record and `GetContentByIdQueryHandler`
   (`post?.Id`). Verified existing tests still pass — `ContentApiTests` deserializes this DTO via
   `ReadFromJsonAsync<T>`, not positional construction, so adding a field is additive, not
   breaking — and confirmed live that `linkedInPostId` populates correctly once a post exists,
   through both the dev proxy (`:4201`) and the production nginx proxy (`:4202`).

### Real bugs found building this, all fixed and re-verified

17. **TS2729, field initializer before constructor body.** Six components (`login`, `register`,
    `campaign-form`, `content-generate`, `content-detail`, `schedule-list`) built their reactive
    forms in a field initializer (`form = this.fb.group(...)`) while still injecting `FormBuilder`
    via the constructor — but a class field initializer runs *before* the constructor body
    assigns its parameter properties, so `this.fb` was genuinely `undefined` at that point, not
    just a type-checker false positive. Fixed by converting every constructor-injected dependency
    in these files to `inject()`-based fields declared above the fields that use them, which
    resolves immediately rather than waiting for a constructor body to run.
18. **Stale generated unit test.** `app.spec.ts` (from `ng new`) still referenced a removed
    `title` signal and old placeholder template text, and didn't provide `HttpClient`/`Router`
    for a component that now injects both — would have failed on both counts. Rewritten as a
    minimal smoke test with real providers (`provideRouter([])`, `provideHttpClient()`,
    `provideHttpClientTesting()`). Verified via `ng test --watch=false`: 1 passed.
19. **Two independent host-port conflicts, both pre-existing and unrelated to this project.** An
    unrelated `TransactionWebApp` dev server had already held port 4200 for several days — both
    its IPv4 bind (blocking the standalone `ng serve`, worked around with `--port 4201`) and its
    IPv6 `[::1]:4200` bind (blocking Docker's `4200:80` mapping even though nominally a different
    container, worked around by remapping `web`'s compose port to `4202:80`). Neither was a bug
    in this project; the other process was deliberately left running rather than killed.
20. **Docker `web` healthcheck reported `unhealthy` despite the service genuinely working.**
    `wget -q -O- http://localhost:80/` failed with `Connection refused` inside the container,
    even while `docker exec`-ing the identical command by hand succeeded every time, and
    `nginx`'s own access log showed real `200`s being served. Root cause: Alpine's `/etc/hosts`
    resolves `localhost` to both `127.0.0.1` and `::1`; this nginx image's config (before the fix)
    only bound the IPv4 socket (`listen 80;`, no IPv6 counterpart), so a healthcheck client that
    tries `::1` first gets refused. Two-part fix, both required:
    - `web/nginx.conf`: added `listen [::]:80;` alongside `listen 80;`, so nginx genuinely
      listens on both families (this also prevents the base image's `docker-entrypoint.d` script
      from attempting its own IPv6 auto-patch against a custom config, a second source of
      inconsistency).
    - `docker-compose.yml`'s own `healthcheck.test` for `web` still said `http://localhost:80/`
      after the Dockerfile's `HEALTHCHECK` was changed to `127.0.0.1` — compose's `healthcheck:`
      key overrides the image's baked-in `HEALTHCHECK` entirely when both are present, so editing
      only the Dockerfile had no effect. Fixed the compose-level test to match.
    Verified via `docker inspect -f '{{json .State.Health}}'`: clean `{"Status":"healthy",
    "FailingStreak":0,...}` with a real captured HTML response in the health log, not just the
    summary `(healthy)` label in `docker compose ps`.

### What was verified, and what was not

Verified by issuing the exact HTTP requests the Angular `HttpClient` makes — same headers, same
JSON shapes — through both `ng serve`'s dev proxy (`:4201`) and the production nginx proxy
(`:4202`): the complete real journey (register → login → niches → create campaign → generate
content → the real 5-agent GPT-4o pipeline, including a genuine mid-flight 170s Polly
`AttemptTimeout` followed by a successful automatic retry → approve → schedule), with every
response matching the TypeScript models field-for-field, including `linkedInPostId`.

**Not verified in that session: the app rendered in an actual browser.** No Claude-in-Chrome or
other browser tool was available, so no screenshot, click, form-validation-message, or
polling-animation check was performed. Open <http://localhost:4201> (dev) or
<http://localhost:4202> (Docker) to confirm layout and interactive behavior directly.

## Verified version decisions (2026-11-15)

These were checked against real release pages before being pinned — see the
"Version verification" rules in the project brief. Where a verified version
conflicts with the brief's named version, that conflict is recorded here
rather than silently resolved.

| Component | Pinned version | Verified | Note |
|---|---|---|---|
| .NET SDK / runtime | 11.0.100 (net11.0) | GA'd 2026-11-10, STS, confirmed via `dotnet --list-sdks` after install | Matches brief |
| Angular | 22.2.0 | Confirmed real `@angular/core: "^22.2.0"` in `web/package.json` after `npx @angular/cli@22 new` | Matches brief; frontend built and verified in Phase 9 |
| CrewAI | 1.15.41 | Confirmed importable; `pip install` dry-run shows `crewai-tools==1.15.41` requires `crewai==1.15.41` exactly (lockstep versioning) | Brief didn't name a version; 1.15.28 was latest at search time but is incompatible with the required `crewai_tools` version |
| crewai_tools | 1.15.41 | Confirmed importable (`SerperDevTool`, `ScrapeWebsiteTool`) | |
| Python | 3.12.8 (venv) | CrewAI requires `>=3.10,<3.14`; this machine's default `python3` is 3.14.0, which CrewAI rejects | Service must run under its own 3.12/3.13 venv, never the system default |
| FastAPI | 0.163.1 | Installed and imported cleanly | |
| MediatR | 12.5.0 | Confirmed restorable on NuGet; Apache-2.0 | **Deliberately not using MediatR 13+** — Jimmy Bogard moved MediatR to a dual-license model in 2025 (RPL-1.5 / paid commercial tier, ~$400–1200/mo) starting at v13. 12.5.0 is the last version under the original permissive license. |
| AutoMapper | **Not used** | N/A | Same commercial-license change hit AutoMapper at v15 (2025-07-02). The last free (MIT) version, 14.0.0, carries an unpatched high-severity advisory (NU1903) that was only fixed in the paid 15.1.1/16.1.1. Rather than ship a known-vulnerable mapper or a paid dependency, DTO mapping is done by hand with small static `ToDto()`/`ToEntity()` extension methods. |
| EF Core / Npgsql provider | EF Core 10.0.4, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 | Confirmed restorable | The PostgreSQL provider for EF Core **11** only has prerelease builds (`11.0.0-rc.3`) as of 2026-11-15, pinned to an exact EF Core 11 RC — not GA. Shipping a prerelease DB provider in a "production-ready" deliverable would contradict the brief's own never-fake / version-verification rules, so Infrastructure references EF Core 10.0.x (GA) instead. A net11.0 app can reference a net10.0-targeted library; this will be revisited once the GA Npgsql provider for EF Core 11 ships. |
| Swashbuckle.AspNetCore | 11.0.1 | Confirmed restorable, targets net11.0 | |
| RabbitMQ.Client | 7.2.2 | Confirmed restorable | |
| Serilog.AspNetCore | 10.0.0 (latest resolved by NuGet) | Confirmed restorable | |
| OpenTelemetry.* | 1.21.0 | Confirmed restorable | |

No package above was pinned from memory — every version in this table was
either resolved live by `dotnet add package` / `pip install` against the real
package index, or confirmed by `import`/`dotnet build` actually succeeding.
