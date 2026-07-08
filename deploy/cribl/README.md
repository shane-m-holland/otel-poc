# Cribl Configuration (Optional)

Cribl Stream is opt-in. To enable it, start compose with the `cribl` profile:

```bash
podman-compose --profile cribl up
# or
docker compose --profile cribl up
```

## Setup steps (first run)

1. Open the Cribl UI at http://localhost:9000
2. Accept the license agreement
3. Configure an OTLP source on port 10080
4. Configure an OTLP destination pointing at grafana-lgtm:4318
5. In `deploy/otel-collector-config.yaml`, uncomment the `otlphttp/cribl` exporter
   and swap it into the pipeline exporters lists, then restart the collector.

## Pipeline when Cribl is enabled

```
.NET services → OTEL Collector → Cribl Stream → Grafana LGTM
```
