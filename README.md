# OTEL POC

A local proof-of-concept demonstrating end-to-end OpenTelemetry observability across two .NET service instances.

**Pipeline:** `.NET 10 services → OTEL Collector → Grafana LGTM (Loki + Tempo + Mimir)`
**Optional:** `→ Cribl Stream → Grafana LGTM` (see below)

## Architecture

```
  service-a (port 5001)  ──OTLP/gRPC──▶  otel-collector  ──OTLP/HTTP──▶  grafana-lgtm (Grafana :3000)
  service-b (port 5002)  ──OTLP/gRPC──▶

  service-a /send  ──HTTP (W3C traceparent)──▶  service-b /receive
```

Two instances of the same image, differentiated only by environment variables.

## Prerequisites

- .NET 10 SDK (for running tests locally — or use the devcontainer)
- Podman + podman-compose **or** Docker + docker compose

## Running tests (TDD)

The devcontainer provides the .NET 10 SDK. Open in VS Code, choose "Reopen in Container", then:

```bash
dotnet test
```

Or if you have .NET 10 installed locally:

```bash
dotnet test
```

## Running the full stack

```bash
# Using Podman
podman-compose up --build

# Using Docker
docker compose up --build
```

Services start in dependency order: Grafana LGTM → Collector → service-a + service-b.

**Port map:**

| Service | Host port | Endpoint |
|---------|-----------|----------|
| Grafana UI | 3000 | http://localhost:3000 |
| service-a | 5001 | http://localhost:5001 |
| service-b | 5002 | http://localhost:5002 |
| Collector gRPC | 4319 | (internal use) |

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Returns service name, instance ID, status |
| `GET` | `/log?message=<text>` | Logs the message via ILogger (visible in Loki) |
| `GET/POST` | `/send` | Calls `/receive` on the other service instance |
| `GET/POST` | `/receive` | Accepts a call, echoes correlation info |

## Correlation walkthrough

### 1. Emit a log

```bash
curl "http://localhost:5001/log?message=hello-from-a"
```

Open Grafana → Explore → Loki datasource → filter `{service_name="service-a"}` → see the log.

### 2. Trigger a cross-service trace

```bash
curl http://localhost:5001/send
```

The response will include the downstream (service-b) reply.

Open Grafana → Explore → Tempo datasource → find the trace → confirm it spans **both**
`service-a` (client span for `/send`) and `service-b` (server span for `/receive`)
under a single trace ID.

### 3. Log ↔ trace correlation

In Loki, filter by the `trace_id` value from the Tempo span. Both services' logs
for the same request share the same trace ID because the W3C `traceparent` header
is propagated automatically by the HttpClient instrumentation.

### 4. Metrics

Open Grafana → Explore → Prometheus datasource → query `http_server_duration_milliseconds_bucket`
or `dotnet_gc_collections_total` to see HTTP and runtime metrics per service.

## Enabling Cribl (optional)

```bash
# Using Podman
podman-compose --profile cribl up

# Using Docker
docker compose --profile cribl up
```

See [deploy/cribl/README.md](deploy/cribl/README.md) for Cribl setup steps and how to
wire the Collector → Cribl → Grafana pipeline.

## Configuration via environment variables

All behaviour is driven by environment variables — no code changes needed to differentiate instances:

| Variable | Purpose |
|----------|---------|
| `OTEL_SERVICE_NAME` | Service name reported in all telemetry |
| `SERVICE_INSTANCE_ID` | Unique instance identifier |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP collector endpoint |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` or `http/protobuf` |
| `SEND_TARGET_URL` | Base URL of the service that `/send` calls |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment name |

## Podman vs Docker notes

The compose file targets the common subset of `podman-compose` and `docker compose`.

Known limitations under `podman-compose` 1.5.0:
- `depends_on: condition: service_healthy` may not be fully respected. If services
  start before the collector is ready, they will retry OTLP export automatically.
- Profile support (`--profile cribl`) should work but test with your version.

If you encounter networking issues under rootless Podman, try:
```bash
podman network create otel-poc
```
before running `podman-compose up`.
