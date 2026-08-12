# Spike: scraper Python (Kabum)

Objetivo: **testar Python como base para o scraper** antes de qualquer
reescrita. Escopo: coletor da Kabum (categoria CPU) com HTTP puro, parser
do `__NEXT_DATA__`, normalização de preço BR e specs de título — tudo
espelhando o scraper C# (`src/OpenPc.Scraper`).

## Como rodar

```bash
uv run pytest                 # 12 testes de normalização/parser
uv run python -m openpc_spike kabum cpu --pages 3 --out /tmp/kabum_cpu.json --summary
```

## Estrutura

| Arquivo | Espelho do C# |
|---|---|
| `src/openpc_spike/fetch.py` | `Collectors/KabumCollector.cs` (rotas, paginação, rate limit) |
| `src/openpc_spike/parse.py` | `Collectors/KabumPageParser.cs` (`__NEXT_DATA__`) |
| `src/openpc_spike/normalize.py` | `Normalization/PriceParser.cs` + `SpecExtractor.cs` (CPU) |
| `src/openpc_spike/pipeline.py` | `Collectors/CardListingBuilder.cs` |
| `src/openpc_spike/models.py` | records `KabumListItem` / `RawListing` |
| `tests/` | casos espelhados dos testes do scraper |

## Resultados (coleta real, 2026-08-11)

**Coleta**: 180 itens / 3 páginas em ~9s (delay de cortesia 1,5–2,5s entre
páginas; fetch puro ~0,7s/página). **Kabum responde sem Playwright e sem
anti-bot** — o `__NEXT_DATA__` (JSON SSR) entrega tudo.

**Validação cruzada contra o banco de produção (API)**:
- 76 match keys casadas entre spike e catálogo (dedup C# por modelo) ✅
- Preços divergem da API quando a Kabum não é a loja mais barata
  (a API guarda o **menor** entre Kabum/Terabyte/Pichau) — comportamento
  correto do agregador, não erro do spike ✅
- 41 itens do spike sem par na API: **todos** são CPUs antigas
  (2ª–11ª geração, LGA 1150/1151/1155) que o C# **filtra de propósito**
  (`CpuGeneration` — matriz de compatibilidade da engine). O spike precisa
  desse filtro para paridade. ✅ (diferença esperada, não bug)
- Specs do título: socket 126/180, threads 87, cores 64, iGPU 61 — mesmas
  limitações do C# ("6-Core" em inglês não casa, só "núcleos")

## Veredito

| Critério | Resultado |
|---|---|
| Kabum via HTTP puro | ✅ funciona (httpx, sem JS) |
| Parser `__NEXT_DATA__` | ✅ idêntico ao C# |
| Preço BR / specs de título | ✅ regras traduzem 1:1 |
| Tipo/segurança | ✅ dataclasses + testes (12) |
| Vantagem sobre o C# | ⚠️ nenhuma decisiva: o C# **já** coleta a Kabum via HTTP puro |

**Conclusão**: Python é totalmente viável para o scraper, mas o spike não
encontrou vantagem que justifique a reescrita. Pichau/Terabyte exigem
Chromium (Cloudflare) em qualquer linguagem, e o C# já tem o pipeline
completo (dedup, ingestão, filtro de ruído, jobs de preço/alerta, 332
testes). Se o objetivo for evoluir a base, o caminho de menor risco é
manter o C#; se a decisão for Python mesmo assim, o próximo passo seria
portar `CpuGeneration` + `CategoryNoiseFilter` + dedup e rodar os dois em
paralelo por uma semana comparando saída.
