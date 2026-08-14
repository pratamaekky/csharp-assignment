# C# REST API + WebSocket Assignment — Design

**Date**: 2026-08-15
**Context**: Second part of a technical test (Tech Lead role application). Requirement from the test email:
- Deploy a simple REST API application using C# on Linux
- Deploy a simple WebSocket application using C# on Linux
- Candidate can include any best practices they have
- Will be reviewed live; candidate demos on a Unix-based OS (macOS host here)

## Goals

- Two independently deployable C# apps that satisfy the two bullet points literally (not one combined app).
- Both actually run on Linux, verifiable on this macOS host, without installing the .NET SDK natively.
- Demonstrate a defensible set of best practices at "minimal-complete" depth — enough to discuss in a review, not a showcase of every possible pattern.

## Non-goals

- No real database / persistence (not requested; in-memory is sufficient and simpler).
- No auth/authorization (not requested).
- No SignalR, no message broker, no horizontal scaling concerns — single-instance, single-process apps.
- No CI pipeline, no Swagger UI — deferred, see "Explicitly deferred" below.

## Architecture

Two independent ASP.NET Core (.NET 10 LTS) Minimal API projects, each with its own `Dockerfile`, orchestrated together by one `docker-compose.yml` for convenience but each runnable standalone via plain `docker run`.

Rationale for two separate apps instead of one combined process: the assignment lists "Deploy a simple REST API application" and "Deploy a simple Websocket application" as two distinct bullets — treating them as two deployables is the literal, defensible reading. A merged app would save a little setup but weakens the "I deployed two things" story during review.

Rationale for Docker over native Linux install: no .NET SDK is installed on this Mac; Docker is already available (v29.6.2). Building official `mcr.microsoft.com/dotnet/sdk:10.0` / `aspnet:10.0` Linux images satisfies "on Linux" unambiguously and needs zero host toolchain setup. The official ASP.NET runtime images provide a non-root `app` user (uid 1654) and listen on port 8080 by default, but the non-root user must be activated explicitly with `USER $APP_UID` — it's not the default process owner.

Rationale for Minimal API over MVC Controllers: less boilerplate for two small apps, current idiomatic default for new ASP.NET Core services, still supports the same DI/testability patterns.

Rationale for native `System.Net.WebSockets` over SignalR: SignalR is a higher-level abstraction (auto-reconnect, transport fallback, hubs) aimed at production real-time apps — appropriate complexity for a production chat feature, not for a "simple websocket" demo. Using the raw WebSocket API demonstrates understanding of the protocol itself (handshake, frames, close codes), which is more relevant to what's being assessed here.

## Component 1 — `TodoApi` (REST)

### Endpoints

| Method | Route | Body | Response |
|---|---|---|---|
| GET | `/todos` | — | 200, array of Todo |
| GET | `/todos/{id}` | — | 200 + Todo, or 404 |
| POST | `/todos` | `{ title }` | 201 + Todo, or 400 if title empty |
| PUT | `/todos/{id}` | `{ title, isDone }` | 200 + updated Todo, or 404 |
| DELETE | `/todos/{id}` | — | 204, or 404 |
| GET | `/health` | — | 200 `{ status: "ok" }` |

### Model

```csharp
record Todo(Guid Id, string Title, bool IsDone, DateTimeOffset CreatedAt);
```

`Id` is server-generated (`Guid.NewGuid()`), never accepted from the client on create.

### Storage

`ITodoRepository` interface with one implementation, `InMemoryTodoRepository`, backed by `ConcurrentDictionary<Guid, Todo>` for thread safety under concurrent requests. Registered as a Singleton in DI (state must survive across requests within the process lifetime; there is intentionally no persistence across restarts — that's an accepted trade-off of "simple", not an oversight).

```csharp
interface ITodoRepository
{
    IEnumerable<Todo> GetAll();
    Todo? GetById(Guid id);
    Todo Add(string title);
    Todo? Update(Guid id, string title, bool isDone); // null if id not found
    bool Delete(Guid id); // false if id not found
}
```

### Validation

- `POST`: `Title` required, non-empty after trim, max length 200. Violations → `400` with a plain `{ error: "..." }` body.
- `PUT`/`DELETE`/`GET by id`: unknown `id` → `404`.
- No other business rules (no dedup, no ownership, no pagination — out of scope for "simple").

### Error handling

- Validation errors → `400` via manual checks in the endpoint handler (no FluentValidation dependency — the ruleset is small enough that a library would be the over-engineered choice here).
- Unhandled exceptions → ASP.NET Core's built-in `UseExceptionHandler` in production mode returns a generic `500`; full exception detail only in the Development environment (standard ASP.NET Core behavior, not custom code).

## Component 2 — `ChatWs` (WebSocket)

### Endpoints

| Route | Protocol | Behavior |
|---|---|---|
| `/ws` | WebSocket upgrade | Accepts connection, adds to connection registry, then loops: receive text frame → broadcast to every *other* connected client. |
| `/health` | HTTP GET | 200 `{ status: "ok" }` |

### Connection management

`ConnectionManager` (singleton, thread-safe via `ConcurrentDictionary<Guid, WebSocket>`):
- `Add(WebSocket) -> Guid` — registers a new connection.
- `Remove(Guid)` — deregisters.
- `BroadcastAsync(Guid senderId, string message)` — sends the message to every registered socket except the sender, skipping any socket not in `WebSocketState.Open` (best-effort; a failed send to one client does not throw for the others — each send is wrapped individually so one client's dead connection can't abort the loop).

### Message format

Plain UTF-8 text frames, no envelope/JSON wrapper — keeps the demo trivial to test with any raw WebSocket client (browser devtools, `websocat`, a one-line JS snippet) without needing a matching client-side parser.

### Lifecycle / graceful close

- On receiving a `WebSocketMessageType.Close` frame, the handler calls `CloseAsync` with `WebSocketCloseStatus.NormalClosure` and removes the connection from the manager — this is the specific "best practice" being demonstrated: not just dropping the TCP connection, but completing the WebSocket close handshake per RFC 6455.
- On any read exception (client disconnects uncleanly), the connection is removed from the manager in a `finally` block so the registry never accumulates dead entries.

## Cross-cutting best practices (both apps)

- **DI + interfaces** where there's a real seam worth testing (`ITodoRepository`, `ConnectionManager` used through its public surface) — no interfaces created purely for ceremony.
- **Structured logging** via the built-in `ILogger<T>` (already structured/scoped by ASP.NET Core hosting) — no Serilog/NLog; the built-in provider is sufficient for console output in a container.
- **Config**: `appsettings.json` + `appsettings.Development.json`, overridable by environment variables (`ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`) — standard ASP.NET Core convention, nothing custom.
- **Nullable reference types** (`<Nullable>enable</Nullable>`) in both `.csproj`s — compile-time null safety at zero runtime cost.
- **Dockerfile**: multi-stage (`sdk` build stage → `aspnet` runtime stage), final image only contains the published app, activates the image's built-in non-root `app` user via `USER $APP_UID`, listens on `8080`.
- **Health check**: plain `/health` minimal endpoint (not the full `Microsoft.Extensions.Diagnostics.HealthChecks` package with dependency probes — there are no external dependencies to probe, so the package would add ceremony without adding a real check).

## Testing

xUnit, one test project per app, targeting the units that have actual logic worth verifying — not integration tests, not a coverage target.

**`TodoApi.Tests`** against `InMemoryTodoRepository` directly (no HTTP layer involved):
- Add then GetById returns the same item
- GetById with an unknown id returns null
- Update on an unknown id returns null; update on a known id returns the updated item and GetById reflects the change
- Delete on a known id returns true and the item is gone from GetAll; a second Delete on the same id returns false

**`ChatWs.Tests`** against `ConnectionManager` directly (fake/mock `WebSocket` where needed, no real sockets):
- Add registers a connection and returns a usable id
- Remove deregisters it (subsequent broadcast doesn't target it)
- Broadcast sends to all connections except the sender

## Folder structure

```
csharp-assignment/
├── CSharpAssignment.sln
├── docker-compose.yml
├── README.md
├── TodoApi/
│   ├── TodoApi.csproj
│   ├── Program.cs
│   ├── Endpoints/TodoEndpoints.cs
│   ├── Models/Todo.cs
│   ├── Models/TodoDtos.cs
│   ├── Repositories/ITodoRepository.cs
│   ├── Repositories/InMemoryTodoRepository.cs
│   ├── appsettings.json
│   └── Dockerfile
├── TodoApi.Tests/
│   ├── TodoApi.Tests.csproj
│   └── InMemoryTodoRepositoryTests.cs
├── ChatWs/
│   ├── ChatWs.csproj
│   ├── Program.cs
│   ├── WebSockets/ConnectionManager.cs
│   ├── WebSockets/ChatWebSocketHandler.cs
│   ├── appsettings.json
│   └── Dockerfile
└── ChatWs.Tests/
    ├── ChatWs.Tests.csproj
    └── ConnectionManagerTests.cs
```

## Deployment / demo flow

```bash
cd csharp-assignment
docker compose up --build
# TodoApi   -> http://localhost:5080  (container port 8080)
# ChatWs    -> http://localhost:5081  (container port 8080), ws://localhost:5081/ws
```

`README.md` documents: build/run via Docker (compose and standalone `docker run` for each), a handful of `curl` examples for `TodoApi`, and a way to test `ChatWs` (browser devtools WebSocket snippet or `websocat`) — kept in the README rather than a separate doc so there's one place to look before the review call.

## Explicitly deferred (not building now, noted for the review discussion if asked)

- Persistence (SQLite/EF Core) — swap-in point already exists via `ITodoRepository`.
- Swagger/OpenAPI UI — not required to demo the API works; `curl` examples suffice.
- CI (GitHub Actions) — no git remote/CI target exists yet for this project.
- Auth — not requested by the assignment.
- SignalR / reconnect logic on the WebSocket side — deliberate scope choice, see Architecture rationale above.

## Open questions

None — all decisions above were confirmed with the candidate before writing this spec.
