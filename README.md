# C# Technical Test — Todo REST API + Chat WebSocket

Two independent ASP.NET Core (.NET 10) apps, each deployed as its own Linux
Docker container:

- **TodoApi** — CRUD REST API for a Todo list, in-memory storage.
- **ChatWs** — broadcast chat over a raw WebSocket (`/ws`), no SignalR.

Design rationale and full spec: `docs/superpowers/specs/2026-08-15-csharp-todo-chat-design.md`.

## Requirements

Docker only. No .NET SDK needs to be installed on the host — every `dotnet`
command in this repo (including tests) runs inside the official SDK
container via `./dotnet-docker.sh`.

## Run both apps

```bash
docker compose up --build
```

- TodoApi:  http://localhost:5080
- ChatWs:   http://localhost:5081  (WebSocket at ws://localhost:5081/ws)

Stop with `docker compose down`.

## Run either app standalone (without compose)

```bash
docker build -f TodoApi/Dockerfile -t todo-api .
docker run --rm -p 5080:8080 todo-api

docker build -f ChatWs/Dockerfile -t chat-ws .
docker run --rm -p 5081:8080 chat-ws
```

## TodoApi usage

```bash
curl http://localhost:5080/health

curl -X POST http://localhost:5080/todos \
  -H "Content-Type: application/json" \
  -d '{"title":"Buy milk"}'

curl http://localhost:5080/todos

curl -X PUT http://localhost:5080/todos/<id> \
  -H "Content-Type: application/json" \
  -d '{"title":"Buy oat milk","isDone":true}'

curl -X DELETE http://localhost:5080/todos/<id>
```

## ChatWs usage

Any two WebSocket clients connected to `ws://localhost:5081/ws` will see
each other's messages (broadcast, not echoed back to the sender). Quick
test with Node.js:

```bash
node -e "
const ws1 = new WebSocket('ws://localhost:5081/ws');
const ws2 = new WebSocket('ws://localhost:5081/ws');
ws2.addEventListener('message', (e) => console.log('ws2 got:', e.data));
ws1.addEventListener('open', () => {
  ws2.addEventListener('open', () => setTimeout(() => ws1.send('hi'), 300));
});
"
```

## Running the tests

```bash
./dotnet-docker.sh test TodoApi.Tests/TodoApi.Tests.csproj
./dotnet-docker.sh test ChatWs.Tests/ChatWs.Tests.csproj
```

## Best practices demonstrated

- DI + interfaces at the seams that are actually tested (`ITodoRepository`, `ConnectionManager`).
- Nullable reference types enabled in every project.
- Structured logging via the built-in `ILogger<T>` (no extra logging dependency).
- Multi-stage Dockerfiles — small runtime image, non-root user by default.
- `/health` endpoint on both apps.
- Unit tests (xUnit) around the actual logic (repository, connection manager), not a coverage target.

## Explicitly out of scope

Persistence, auth, Swagger/OpenAPI UI, CI pipeline, SignalR — see the design
doc's "Explicitly deferred" section for the reasoning behind each.
