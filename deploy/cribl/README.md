# Cribl Configuration (Optional)

Cribl Stream is opt-in and fully pre-configured via mounted YAML files — no manual
UI setup required.

## Pipeline when Cribl is enabled

```
.NET services → OTEL Collector → Cribl Stream → Grafana LGTM
```

## Running with Cribl

```bash
# Using Podman
podman-compose -f docker-compose.yml -f docker-compose.cribl.yml --profile cribl up --build

# Using Docker
docker compose -f docker-compose.yml -f docker-compose.cribl.yml --profile cribl up --build
```

The compose override (`docker-compose.cribl.yml`) does two things:
- Swaps the collector config to route all signals through Cribl instead of directly to Grafana
- Mounts `deploy/cribl/local/` into the Cribl container with a pre-configured OTLP
  source (port 10080), passthrough pipeline, and OTLP/HTTP destination to grafana-lgtm

## Verifying the path

1. Open Grafana at http://localhost:3000 — confirm it is reachable
2. `curl http://localhost:5001/send` — trigger a cross-service trace
3. Grafana → Explore → Tempo — confirm the trace appears (Cribl forwarded it)
4. Grafana → Explore → Loki — confirm logs are visible
5. Cribl UI at http://localhost:9000 → Sources → OpenTelemetry → confirm events/sec > 0
6. Cribl UI → Destinations → OpenTelemetry → confirm outbound events/sec > 0

## Pre-configured files

| File | Purpose |
|------|---------|
| `local/cribl/inputs.yml` | OTLP/HTTP source on port 10080 |
| `local/cribl/outputs.yml` | OTLP/HTTP destination → grafana-lgtm:4318 |
| `local/cribl/pipelines/passthru.yml` | No-op passthrough pipeline |
| `local/cribl/routes.yml` | Route: all events → passthru → grafana |
