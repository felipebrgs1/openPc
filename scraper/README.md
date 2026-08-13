# OpenPC Scraper (Python)

Port completo do scraper C# (`src/OpenPc.Scraper`) — mesma lógica, mesmos
comandos, mesmo schema de banco (Postgres compartilhado com a API).

## Status do port (2026-08-11)

| Módulo C# | Módulo Python | Status |
|---|---|---|
| `Normalization/*` (PriceParser, MatchKey, PartNumber, CpuGeneration, GpuSeries, SpecExtractor, CategoryNoiseFilter) | `normalize/` | ✅ portado, 235+ casos de teste espelhados |
| `Collectors/KabumCollector` + `KabumPageParser` | `collect/kabum.py` | ✅ portado, validado ao vivo |
| `Collectors/BrowserCollectorBase` + `BrowserPool` + `PichauCollector` + `TerabyteCollector` + `CardListingBuilder` | `collect/browser.py`, `pichau.py`, `terabyte.py`, `card.py` | ✅ portado (Playwright) |
| `Ingest/IngestionService` | `ingest/service.py` | ✅ portado, validado contra produção |
| `Ingest/ImageSyncService` + `ImageKeys` | `ingest/image_sync.py` | ✅ portado (MinIO) |
| `Jobs/CollectionService` | `jobs/collection.py` | ✅ portado |
| `Jobs/ScrapeScheduler` (Quartz) | `jobs/scheduler.py` (APScheduler) | ✅ portado |
| `Jobs/PriceAggregationService` | `jobs/price_aggregation.py` | ✅ portado |
| `Jobs/PriceAlertService` + `ScrapeAlertService` | `jobs/alerts.py` | ✅ portado |
| `Email/SmtpEmailSender` | `email.py` | ✅ portado (smtplib) |
| `Program.cs` (CLI + seed) | `__main__.py` | ✅ portado |

**Validação contra o banco de produção** (2026-08-11):
`run-once kabum cpu` → 256 itens ingeridos, **0 produtos novos** (dedup
perfeito por listing SKU/part number/match key), 467 ruídos descartados
(CPUs antigas, mesmo filtro do C#).

## Comandos

```bash
uv run python -m openpc_scraper run-once [store] [category] [--concurrency N]   # coleta imediata (jobs em paralelo)
uv run python -m openpc_scraper cleanup-noise [category] [--dry-run]
uv run python -m openpc_scraper aggregate-prices [days]
uv run python -m openpc_scraper sync-images
uv run python -m openpc_scraper alerts-check <productId>
uv run python -m openpc_scraper backfill-attributes
uv run python -m openpc_scraper scheduler                     # agendamento
```

O `run-once` coleta em paralelo (default 4 jobs simultâneos, máx. 2 por
loja); a ingestão continua serializada para preservar o dedup e as
constraints. Use `--concurrency 1` para o comportamento sequencial.
O Kabum busca páginas em ondas concorrentes de 3 (vs. fetch sequencial).

Configuração via env:

| Variável | Uso |
|---|---|
| `DATABASE_URL` ou `ConnectionStrings__Default` | Postgres (aceita formato ADO.NET `Host=...`) |
| `SCRAPE_CONCURRENCY` | nº de jobs coletados em paralelo no `run-once` (default 4; 1 = sequencial) |
| `ALERTS_WEBHOOK_URL` | webhook de run falho |
| `SMTP_HOST`/`SMTP_PORT`/`SMTP_USERNAME`/`SMTP_PASSWORD`/`SMTP_FROM` | e-mail de alertas (sem host = dry-run) |
| `MINIO_ENDPOINT`/`MINIO_ACCESS_KEY`/`MINIO_SECRET_KEY`/`MINIO_BUCKET`/`MINIO_PUBLIC_PATH` | sync de imagens (sem endpoint = no-op) |
| `SITE_URL` | base das URLs de produto nos e-mails |

## Testes

```bash
uv run pytest        # 294 testes (port dos testes do C#)
```

## Docker

```bash
docker build -t openpc-scraper .     # imagem Playwright oficial (Chromium completo)
docker run --rm -e DATABASE_URL=... openpc-scraper run-once kabum cpu
```
