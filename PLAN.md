# OTEL POC — .NET 10 + OpenTelemetry Collector + Grafana (LGTM)

## Context

We need a local-only proof-of-concept that demonstrates an end-to-end OpenTelemetry
pipeline: a .NET service emits OTLP (traces, metrics, logs) → OTEL Collector →
(optionally Cribl) → Grafana observability backends (Loki/Tempo/Mimir). The primary
goal is to **prove trace and log correlation across two service instances** — one
service calls another, and we want to follow a single trace (and correlated logs)
across that hop in Grafana.

This is greenfield: the repo is empty (no commits). Everything is built from scratch.

### Environment facts
- Local runtime is **Podman 5.6.2 + podman-compose 1.5.0** (not Docker). Compose YAML
  must be written to work with both `podman-compose` and `docker compose`.
- Local SDKs are only .NET 8 and 9. **.NET 10 is GA (LTS, released 2025-11-11)** and
  will be provided *inside the devcontainer/images*, not relied upon from the host.

### Decisions locked with the user
- **Cribl**: design for it but **disabled by default** via a compose profile. Default
  path is `.NET → Collector → Grafana LGTM`. Cribl can be enabled later.
- **Grafana backends**: single **`grafana/otel-lgtm`** all-in-one image (Grafana +
  Loki + Tempo + Prometheus/Mimir, datasources pre-provisioned).
- **/send target**: service-a `/send` → service-b `/receive` (the *other* instance).
- **API style**: **controller-based** ASP.NET Core MVC (not Minimal API).
- **Test stack**: **xUnit + `WebApplicationFactory<T>`** in-memory integration tests +
  FluentAssertions. TDD — tests written first.
- **HTTP call**: `IHttpClientFactory` typed client, target URL from env var.
- **Signals**: all three — traces, metrics, logs — via `UseOtlpExporter()`.
- **Devcontainer**: dotnet-only (SDK for dev/test); full stack runs via separate compose.

## Architecture

```
                       ┌─────────────────────────────────────────┐
  service-a  ──OTLP──▶ │                                         │
             (gRPC/4317)│      otel-collector (contrib)          │
  service-b  ──OTLP──▶ │   receivers: otlp                       │
             (gRPC/4317)│   processors: batch, resource          │
                       │   exporters: otlphttp → Grafana LGTM    │
                       │              (+ optional → Cribl)       │
                       └──────────────────┬──────────────────────┘
                                          │ OTLP/HTTP
                                          ▼
                              grafana/otel-lgtm (all-in-one)
                              Grafana :3000 · OTLP in :4317/:4318
                              Loki · Tempo · Prometheus(Mimir)

  service-a /send  ──HTTP (traceparent propagated)──▶  service-b /receive
```

Two instances of the *same* image (`service-a`, `service-b`), differentiated purely by
environment variables (`OTEL_SERVICE_NAME`, `SEND_TARGET_URL`, instance id). This keeps
one codebase and proves correlation is env-driven.

## Repository layout

```
otel-poc/
├── .devcontainer/
│   └── devcontainer.json            # .NET 10 SDK image, dotnet-only
├── src/
│   └── OtelPoc.Api/
│       ├── OtelPoc.Api.csproj       # net10.0, OTEL packages
│       ├── Program.cs               # OTEL wiring via UseOtlpExporter()
│       ├── Controllers/
│       │   ├── HealthController.cs   # GET /health
│       │   ├── LogController.cs      # GET /log?message=
│       │   ├── SendController.cs     # POST/GET /send  -> calls SEND_TARGET_URL/receive
│       │   └── ReceiveController.cs  # POST/GET /receive
│       ├── Clients/
│       │   └── DownstreamClient.cs   # typed HttpClient (IHttpClientFactory)
│       └── Dockerfile                # multi-stage build on .NET 10 SDK/runtime
├── tests/
│   └── OtelPoc.Api.Tests/
│       ├── OtelPoc.Api.Tests.csproj  # xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing
│       ├── CustomWebApplicationFactory.cs
│       ├── HealthEndpointTests.cs
│       ├── LogEndpointTests.cs
│       ├── SendEndpointTests.cs      # mock downstream handler, assert propagation
│       └── ReceiveEndpointTests.cs
├── deploy/
│   ├── otel-collector-config.yaml
│   └── cribl/                        # placeholder config, profile-gated
├── docker-compose.yml
├── .env                             # correlation-friendly env defaults
├── OtelPoc.sln
├── PLAN.md                          # this file (durable reference)
├── MANIFEST.md                      # implementation progress tracker
└── README.md
```

## Implementation steps (TDD-first)

### 1. Solution + project scaffolding
- `OtelPoc.sln` with `src/OtelPoc.Api` (webapi, controllers) targeting `net10.0` and
  `tests/OtelPoc.Api.Tests`.
- Test csproj references: `xunit`, `xunit.runner.visualstudio`,
  `Microsoft.AspNetCore.Mvc.Testing`, `FluentAssertions`, project ref to the API.
- Make `Program` test-visible (`public partial class Program {}`) so
  `WebApplicationFactory<Program>` works.

### 2. Write tests FIRST (they should fail — no endpoints yet)
- **HealthEndpointTests** — `GET /health` → 200 with a body indicating healthy +
  service name (from env) so we can eyeball which instance answered.
- **LogEndpointTests** — `GET /log?message=hello` → 200 echoing the message; missing
  `message` → 400. (Behavior assertion; that it *emits* an ILogger record is verified
  manually in Grafana, but we assert the HTTP contract here.)
- **ReceiveEndpointTests** — `/receive` accepts a request, returns 200 with a payload
  including the responding service name and any correlation id passed in.
- **SendEndpointTests** — `/send` uses the typed `DownstreamClient`; inject a fake
  `HttpMessageHandler` via `WebApplicationFactory.WithWebHostBuilder` to capture the
  outbound request. Assert: it targets `{SEND_TARGET_URL}/receive`, forwards a
  correlation id, and returns the downstream response. This is where correlation is
  contract-tested without a second live instance.

### 3. Implement endpoints to make tests green
- Controllers as above; `DownstreamClient` registered via
  `builder.Services.AddHttpClient<DownstreamClient>(...)` reading `SEND_TARGET_URL`.
- Config strictly from environment variables (requirement): `OTEL_SERVICE_NAME`,
  `OTEL_EXPORTER_OTLP_ENDPOINT`, `SEND_TARGET_URL`, `SERVICE_INSTANCE_ID`.

### 4. OpenTelemetry wiring (`src/OtelPoc.Api/Program.cs`)
- Packages: `OpenTelemetry.Extensions.Hosting`,
  `OpenTelemetry.Exporter.OpenTelemetryProtocol`,
  `OpenTelemetry.Instrumentation.AspNetCore`,
  `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime`.
- Wire with the cross-cutting extension:
  ```csharp
  builder.Services.AddOpenTelemetry()
      .ConfigureResource(r => r.AddService(
          serviceName: Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME"),
          serviceInstanceId: Environment.GetEnvironmentVariable("SERVICE_INSTANCE_ID")))
      .WithTracing(t => t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation())
      .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation()
                          .AddRuntimeInstrumentation())
      .UseOtlpExporter();
  builder.Logging.AddOpenTelemetry(o => { o.IncludeScopes = true; o.IncludeFormattedMessage = true; });
  ```
- Endpoint/protocol come from `OTEL_EXPORTER_OTLP_ENDPOINT` (env) → keeps code
  instance-agnostic, satisfies the env-var correlation requirement.
- HttpClient auto-instrumentation propagates W3C `traceparent` on the /send → /receive
  hop automatically — this is the core correlation mechanism.

### 5. OTEL Collector config (`deploy/otel-collector-config.yaml`)
- `receivers.otlp` (grpc :4317, http :4318).
- `processors`: `batch`, `resource` (stamp a `deployment.environment` / pipeline tag).
- `exporters`: `otlphttp` → `grafana-lgtm:4318` for the default path. A second
  `otlphttp`/`otlp` exporter to Cribl kept in config but only referenced by pipelines
  when the Cribl profile is active (documented, commented block).
- Pipelines for traces, metrics, logs.

### 6. docker-compose.yml (Podman-compatible)
Services:
- `service-a` / `service-b` — same build context, differ only by env
  (`OTEL_SERVICE_NAME=service-a|service-b`, `SEND_TARGET_URL` pointing at the *other*
  service's URL, `SERVICE_INSTANCE_ID`). Both point `OTEL_EXPORTER_OTLP_ENDPOINT` at
  `http://otel-collector:4317`.
- `otel-collector` — `otel/opentelemetry-collector-contrib`, mounts the config.
- `grafana-lgtm` — `grafana/otel-lgtm`, expose Grafana `3000`.
- `cribl` — **profile `cribl`** (not started by default): `cribl/cribl` image with a
  placeholder mounted config; documented as opt-in via `--profile cribl`.
Notes for Podman: rely on compose service-name DNS for all inter-container URLs; no
Docker-socket mounts; explicit healthchecks; a shared user-defined network.

### 7. Devcontainer (`.devcontainer/devcontainer.json`)
- Base on `mcr.microsoft.com/devcontainers/dotnet` pinned to **.NET 10**, or the SDK
  image `mcr.microsoft.com/dotnet/sdk:10.0`.
- Purpose: `dotnet restore/build/test/run` for the API. The full stack is brought up
  separately with compose. Include the C# Dev Kit extension recommendation.

### 8. README.md
- How to open in devcontainer, run `dotnet test` (TDD loop), and
  `podman-compose up` / `docker compose up` the full stack.
- Correlation walkthrough (verification below).
- How to enable Cribl (`--profile cribl`).

## Verification (end-to-end)

1. **TDD unit/integration**: in the devcontainer, `dotnet test` — all endpoint tests
   pass (and they were red before implementation). This is the primary automated gate.
2. **Stack boot**: `podman-compose up --build` → collector, both services, and
   grafana-lgtm healthy. Hit `GET http://localhost:<a>/health` and `/health` on b.
3. **Log emission**: `GET /log?message=hello-from-a` on service-a → open Grafana
   (`localhost:3000`) → Loki/Explore, filter `service_name="service-a"`, see the log.
4. **Trace correlation (the point of the POC)**: `GET/POST /send` on service-a →
   in Grafana Tempo, find the trace; confirm it spans **both** `service-a` (client
   span) and `service-b` (server span for `/receive`) under one trace id.
5. **Log↔trace correlation**: confirm logs on both services carry the same `trace_id`
   and are linkable from Tempo → Loki in the LGTM datasource wiring.
6. **Metrics**: in Grafana Prometheus/Mimir, see HTTP + runtime metrics per service.
7. **(Optional) Cribl**: `podman-compose --profile cribl up` and confirm data flows
   Collector → Cribl → Grafana (feasibility-gated; documented if it needs manual
   license acceptance).

## Open risks / notes
- `podman-compose` 1.5.0 does not implement every `docker compose` feature identically
  (profiles and healthcheck ordering are the usual friction points). If a profile or
  `depends_on: condition: service_healthy` misbehaves under podman-compose, the README
  will note the `docker compose` fallback. Compose YAML stays within the common subset.
- `grafana/otel-lgtm` is a dev/demo image (not for prod) — appropriate for a POC.
- Cribl remains best-effort per the "if feasible" requirement; the default pipeline
  does not depend on it.

## Sources
- [Announcing .NET 10](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)
- [OTLP Exporter for OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md)
