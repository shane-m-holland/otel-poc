# Implementation Manifest

Progress tracker for the OTEL POC. See [PLAN.md](PLAN.md) for the full design and
rationale. Update the status of each item as it is implemented and verified.

Status legend: ⬜ not started · 🟨 in progress · ✅ done · ⛔ blocked

## Milestone 1 — Solution scaffolding
| Status | Item | Artifact |
| :----: | ---- | -------- |
| ✅ | Solution file | `OtelPoc.sln` |
| ✅ | API project (net10.0, controllers) | `src/OtelPoc.Api/OtelPoc.Api.csproj` |
| ✅ | Test project (xUnit + Mvc.Testing + FluentAssertions) | `tests/OtelPoc.Api.Tests/OtelPoc.Api.Tests.csproj` |
| ✅ | `public partial class Program` for test host | `src/OtelPoc.Api/Program.cs` |
| ✅ | `global.json` requiring .NET 10 SDK | `global.json` |

## Milestone 2 — Tests first (red)
| Status | Item | Artifact |
| :----: | ---- | -------- |
| ✅ | Health tests | `tests/OtelPoc.Api.Tests/HealthEndpointTests.cs` |
| ✅ | Log tests (200 echo + 400 on missing message) | `tests/OtelPoc.Api.Tests/LogEndpointTests.cs` |
| ✅ | Receive tests | `tests/OtelPoc.Api.Tests/ReceiveEndpointTests.cs` |
| ✅ | Send tests (fake handler, propagation assert) | `tests/OtelPoc.Api.Tests/SendEndpointTests.cs` |
| ✅ | Test host factory | `tests/OtelPoc.Api.Tests/CustomWebApplicationFactory.cs` |

## Milestone 3 — Endpoints (green)
| Status | Item | Artifact |
| :----: | ---- | -------- |
| ✅ | Health controller | `src/OtelPoc.Api/Controllers/HealthController.cs` |
| ✅ | Log controller | `src/OtelPoc.Api/Controllers/LogController.cs` |
| ✅ | Receive controller | `src/OtelPoc.Api/Controllers/ReceiveController.cs` |
| ✅ | Send controller | `src/OtelPoc.Api/Controllers/SendController.cs` |
| ✅ | Typed downstream HttpClient | `src/OtelPoc.Api/Clients/DownstreamClient.cs` |
| ✅ | `dotnet test` all green | — (run in devcontainer) |

## Milestone 4 — OpenTelemetry wiring
| Status | Item | Artifact |
| :----: | ---- | -------- |
| ✅ | OTEL packages added | `src/OtelPoc.Api/OtelPoc.Api.csproj` |
| ✅ | Traces + metrics + logs via `UseOtlpExporter()` | `src/OtelPoc.Api/Program.cs` |
| ✅ | Env-driven config (`OTEL_*`, `SEND_TARGET_URL`, `SERVICE_INSTANCE_ID`) | `src/OtelPoc.Api/Program.cs` |

## Milestone 5 — Containerization & deployment
| Status | Item | Artifact |
| :----: | ---- | -------- |
| ✅ | Multi-stage Dockerfile (.NET 10) | `src/OtelPoc.Api/Dockerfile` |
| ✅ | `.dockerignore` | `.dockerignore` |
| ✅ | Collector config | `deploy/otel-collector-config.yaml` |
| ✅ | Compose: service-a, service-b, collector, grafana-lgtm | `docker-compose.yml` |
| ✅ | Cribl profile (opt-in) + placeholder config | `docker-compose.yml`, `deploy/cribl/` |
| ✅ | Env defaults | `.env` |

## Milestone 6 — Devcontainer & docs
| Status | Item | Artifact |
| :----: | ---- | -------- |
| ✅ | Devcontainer (dotnet-only, .NET 10) | `.devcontainer/devcontainer.json` |
| ✅ | README (run/test/correlate/cribl) | `README.md` |

## Milestone 7 — End-to-end verification
| Status | Item |
| :----: | ---- |
| ✅ | `dotnet test` passes in devcontainer |
| ✅ | Stack boots under podman-compose; both `/health` respond |
| ✅ | `/log` message visible in Grafana Loki |
| ✅ | `/send` produces one trace spanning service-a + service-b in Tempo |
| ✅ | Logs carry matching `trace_id`; Tempo↔Loki linking works |
| ✅ | Metrics visible in Grafana Prometheus/Mimir |
| ✅ | (Optional) Cribl path verified |

## Notes
- `.NET 10 SDK` is required to build/test. The host only has SDK 8/9; use the devcontainer
  or install SDK 10 locally from https://aka.ms/dotnet/download
- Container runtime is Podman 5.6.2 + podman-compose 1.5.0. Docker compose also works.
- Cribl is opt-in via `--profile cribl`. Default pipeline skips Cribl.
