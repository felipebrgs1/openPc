# OpenPC — Roadmap

> Ordem pensada para chegar cedo a um produto utilizável e validar as partes
> arriscadas (scraping e dedup) antes de investir em polish.
> Cada fase termina com algo **demonstrável e deployável**.

| Fase | Tema | Duração alvo |
|---|---|---|
| M0 | Fundação | 1 semana |
| M1 | Spike de scraping (validação de risco) | 1 semana |
| M2 | Catálogo: pipeline de ingestão + API de leitura | 2–3 semanas |
| M3 | Engine de compatibilidade | 2 semanas |
| M4 | Frontend: catálogo + montador de PC | 3–4 semanas |
| M5 | Deploy VPS + observabilidade 🔶 (infra pronta; deploy real pendente de VPS) | 1 semana |
| M6 | Histórico, ofertas e alertas ✅ | 2 semanas |
| M7 | Auth, builds salvos, compartilhamento social | 2 semanas |

---

## M0 — Fundação

**Objetivo:** esqueleto completo rodando local com Docker.

- [ ] Monorepo: `src/OpenPc.{Domain,Infrastructure,Api,Scraper}`, `web/`, `deploy/`
- [ ] Solution .NET 10, `Directory.Build.props`, editorconfig, nullable on
- [ ] Angular 22 app (zoneless, standalone, routing base)
- [ ] `docker-compose.dev.yml`: db (PG18) + api + web + redis
- [ ] EF Core 10: DbContext inicial, primeira migration (stores, categories)
- [ ] CI: GitHub Actions build + test das duas stacks
- [ ] Healthchecks e `GET /api/v1/health` respondendo

**Critério de aceite:** `docker compose up` sobe tudo; front chama a API e
exibe categorias vindas do banco.

---

## M1 — Spike de scraping ✅ (validado 2026-08-08)

**Objetivo:** provar que dá para coletar dados úteis das 3 lojas da v1
**antes** de construir o pipeline inteiro. Kabum primeiro — é a loja piloto.

**Resultado: 3/3 lojas viáveis, 100% de sucesso — gate aprovado.**
Detalhes e decisões em `docs/scraping-findings.md`.

- [x] Protótipo descartável (console app) por loja, na ordem:
  - [x] **Kabum**: extração via `__NEXT_DATA__` (SSR) — 180/180 produtos, sem anti-bot
  - [x] Terabyte: Playwright (Chromium completo) — 149/149 cards
  - [x] Pichau: Playwright (Chromium completo) — 45/45 cards
- [x] Comparar para cada loja: JSON-LD vs API interna vs Playwright
  (custo, estabilidade, taxa de sucesso em 100 requisições)
- [x] Documentar por loja: estratégia escolhida, rate limit seguro, specs
  extraíveis da página de produto (socket, TDP, dimensões...)
- [x] ~~Amazon~~ — adiada para o backlog pós-M7 (decisão 2026-08-07)

**Critério de aceite:** relatório curto em `docs/scraping-findings.md` com
taxa de sucesso por loja e estratégia definida. **Gate:** se menos de 2 lojas
forem viáveis, repensar escopo antes de seguir.

---

## M2 — Catálogo: ingestão + API de leitura ✅ (validado 2026-08-08)

**Objetivo:** banco populado com produtos reais e API servindo o catálogo.

- [x] Schema completo: `products`, `product_attributes`, `listings`,
      `price_history`, `scrape_jobs`, `scrape_runs` + `product_match_candidates`
      + extensão `pg_trgm` (índice GIN no nome)
- [x] `IStoreCollector` + collectors de produção: Kabum (HTTP/`__NEXT_DATA__`),
      Pichau/Terabyte (Playwright via `BrowserCollectorBase`)
- [x] Normalizer: `SpecExtractor` (CPU: socket/cores/threads/iGPU/TDP/DDR;
      GPU: memória/TDP/dimensões/conectores) + `MatchKey` + `PartNumber`
- [x] Dedup: part number (AMD/Intel) + match key marca+modelo + fila de
      revisão (`no_anchor` em CPU/GPU sem âncora)
- [x] Scheduler (Quartz.NET): um job por linha de `scrape_jobs`, cron na row
      (catálogo 04:30 diário, CPU/GPU a cada 6h); `run-once [loja] [categoria]`
- [x] Endpoints: `GET /categories`, `GET /stores`, `GET /products` (q, brand,
      min/maxPrice, `attrs[socket]=am5`, sort, paginação), `GET /products/{id}`,
      `GET /health/scrapers`
- [x] Cache Redis (5 min) nas listagens
- [x] Testes: 33/33 passando — normalizer, parsers (fixtures Kabum real +
      cards Pichau/Terabyte reais do M1), price parser BR

**Critério de aceite:** ✅ catálogo com 3 lojas e ~6.200 produtos; busca
"7600" retorna o canônico `amd 7600x` com ofertas de **Kabum, Pichau e
Terabyte**; `scrape_runs` saudáveis por loja.

**Achados operacionais (registrados no código/docs):**
- Rotas reais da Kabum via sitemap: `placas-mae`, `placa-de-video-vga`,
  `fontes`, `coolers`, `ssd-2-5` — gabinete sem rota pública (TODO aberto).
- Terabyte **trunca part numbers** nos slugs (limite de URL) — o match key
  cobre o caso; part number truncado não casa.
- Preço de card por regex tolera separador `|` (`por | R$ 1.599,99`).
- Bugs corrigidos no caminho: `JsonDocument` disposto com `JsonElement` vivo,
  `Normalize` removendo espaços (quebrava regexes), chave de dedup com marca
  duplicada (`intel intel 265f`), ancestral de card subindo até o grid
  (misturava nome/preço de cards diferentes — agora descarta >2 anchors).

---

## M3 — Engine de compatibilidade ✅ (validado 2026-08-08)

**Objetivo:** coração do produto, com cobertura de testes alta.

**Resultado: 16 regras (12 erro + 4 warning) cobertas por 148 testes (106
domain + 42 scraper); aceite end-to-end validado na API com dados reais
(smoke): Ryzen AM5 + placa AM4 → `CPU_SOCKET_MISMATCH`; `compatibleWith`
excluiu 100% das 163 placas AM4 do seletor.**

- [x] `BuildSnapshot` + `ICompatibilityRule` + executor (§4.1)
- [x] Todas as regras **Error** da tabela §4.2
- [x] Regras **Warning** prioritárias: `PSU_WATTAGE_LOW`, `NO_GPU_NO_IGPU`,
      `BIOS_UPDATE_NEEDED`, `RAM_SPEED_CAPPED`
- [x] Seed curado `compatibility.json` (matriz socket/chipset/BIOS,
      gerações AM4/AM5/LGA1700/LGA1851) — 22 chipsets
- [x] Endpoints: `POST /builds`, `GET /builds/{slug}`, `PUT/DELETE .../items`,
      `GET .../compatibility`, filtro `compatibleWith` em `GET /products`
- [x] Estimador de wattage (TDP CPU+GPU+overhead ×1.4) com margem recomendada
- [x] Testes de unidade: cada regra com casos positivo/negativo/borda
      (ex: GPU com comprimento exatamente igual ao limite do gabinete)

**Critério de aceite:** ✅ build com Ryzen AM5 + placa AM4 → erro
`CPU_SOCKET_MISMATCH`; seletor de placa-mãe filtrado por socket (0 AM4 entre
os compatíveis); wattage estimado = fórmula documentada (base = TDP CPU+GPU
+ 100 W, recomendado ×1.4 — alinhado com calculadoras ±10%).

**Achados operacionais (registrados no código/docs):**
- Scraper passou a extrair specs de placa-mãe do título (`ExtractMotherboard`:
  socket, chipset, form factor, DDR) — necessário para o seletor filtrado por
  socket ter dados reais. Ficha técnica completa (página de produto) segue
  como job de enrichment futuro (verde no backlog).
- Bug corrigido: `PUT /builds/{slug}/items/{category}` com item existente não
  persistia — build carregado com `AsNoTracking`, mutação em entidade detached
  era silenciosamente descartada. Item agora é carregado tracked.
- Normalização de socket: "LGA 1700" (com espaço) vira `lga1700` — a engine
  compara o valor bruto e espaços divergentes gerariam erro falso.
- Placa sem socket/chipset na base passa no filtro (spec desconhecida ≠
  incompatível) — comportamento conservador por design (§4.4).
- Regras com dados insuficientes retornam nulo (nunca erro falso); a matriz
  BIOS é editorial aproximada — revisar a cada geração nova.

---

## M4 — Frontend: catálogo + montador ✅ (validado 2026-08-08)

**Objetivo:** produto utilizável de ponta a ponta (anônimo).

**Resultado: fluxo completo validado no browser com dados reais — usuário
monta um PC (7600X + placa AM4 → erro `CPU_SOCKET_MISMATCH` no painel; toggle
"mostrar incompatíveis" mostra o motivo inline; troca por placa AM5 limpa os
erros), vê total por loja (Kabum 2/2 peças) e compartilha o link; clone do
build compartilhado volta para o montador.**

- [x] Design system mínimo com Tailwind v4 + tokens de cor (`@theme` brand/acento)
- [x] `/` home, `/pecas/:category`, `/pecas/:category/:id` (detalhe + ofertas
      por loja + gráfico de histórico com fallback)
- [x] **`/montar`**: 8 slots, seletor filtrado por compatibilidade, toggle
      "mostrar incompatíveis" com motivo inline (busca no seletor incluída)
- [x] Painel do build: preço total (menor preço × por loja), barra de wattage,
      lista de errors/warnings acionáveis (com nomes das peças)
- [x] `/build/:slug` compartilhável (anônimo, slug na URL) + "clonar e editar"
- [x] Estado do build com signals + persistência do slug em `localStorage`
- [x] Responsivo (mobile-first: grids `sm:`/`lg:`, modal bottom-sheet no mobile)
- [x] Formatação BRL (`Intl`), datas pt-BR, SEO básico (title + meta por rota)

**Critério de aceite:** ✅ usuário anônimo monta um PC completo, vê
incompatibilidades bloqueadas (com motivo inline), preço total por loja e
compartilha o link. Teste manual de fluxo completo no browser (headless
Chromium, dados reais).

**Achados operacionais (registrados no código/docs):**
- Endpoints novos na API para o front: `GET /products/{id}/prices` (série
  diária p/ sparkline), `GET /builds/{slug}/price-comparison` (total por loja
  + menor preço individual — specs.md §6) e `showIncompatible=true` +
  `blockedBy` em `GET /products` (motivo inline do toggle).
- Tailwind v4: o builder do Angular **só lê `postcss.config.json`** — o
  `postcss.config.js` é ignorado em silêncio (tema era emitido sem as
  utilities). Resolvido com JSON + `@source` explícito em `styles.css`.
- Seletor de peças ganhou busca (o filtro da engine escondia peças relevantes
  além da página de 100 mais baratas).
- Sparkline: histórico ainda esparso (scrapes todos do mesmo dia → 1 ponto) —
  componente renderiza fallback "sem histórico"; gráfico real chega com a
  agregação do M6.
- ~435 placas-mãe de Pichau/Terabyte ingeridas antes do `ExtractMotherboard`
  seguem sem specs e passam no filtro como compatíveis (conservador por
  design) — corrigido por re-scrape futuro (regra agents.md: sem scraping
  sem pedido).
- **Limpeza de ruído de catálogo (feedback 2026-08-08)**: `CategoryNoiseFilter`
  na ingestão + comando `cleanup-noise` no scraper — palavras-chave por
  categoria (contact frame em cpu, suporte/riser/cabo/soundbar em gpu, fonte
  de notebook em psu, pasta/massa/cabo em cooler, monitor em storage) e
  marcadores de **outra** categoria com borda de palavra (cross-listing das
  rotas Kabum: CPU/GPU/RAM em psu, placa-mãe/GPU/RAM em gpu e memory).
  Banco limpo: **2.219 produtos removidos** (psu 188→38, gpu 562→236,
  cooler 4.092→3.128, memory 2.850→2.648, mobo 2.488→2.089, cpu 541→519,
  storage 347→221). Falsos positivos evitados: GDDR5, 80 Plus Titanium,
  Cooler Master, "Socket AM5" em CPU, "Gabinete com fonte", "ventoinha" em
  gabinete. Montador em **coluna única** (preço mantido no topo-direita).
- **Segunda rodada de limpeza (2026-08-08)**: memória SODIMM/para notebook
  (665 → 0 em memory), placas-mãe de notebook/sucata (mobo), pendrives
  (storage) e **CPUs antigas** — política: só CPUs que a engine consegue
  avaliar (Intel ≥ 12th, AMD Ryzen/Athlon AM4+, Ultra 2xx; fora: Intel ≤ 11th,
  Xeon, A-series/FX, mobile). Classificação pelo título cru (o `MatchKey`
  junta "i5-12400F" em "i512400f" e quebra o regex; o match key de
  "Ryzen 5 Pro 5650G" vira GUID pelo "Pro"). **+1.081 removidos**
  (memory 2.648→1.977, cpu 519→130, mobo 2.089→2.070, storage 221→219).
  Total acumulado: **~3.300 produtos removidos** sem re-scraping;
  `cleanup-noise --dry-run` lista contagem/amostra antes de deletar.

---

## M5 — Deploy VPS + observabilidade 🔶 (infra pronta e validada local; deploy real pendente de VPS)

**Objetivo:** produção estável e operável.

**Resultado: toda a infraestrutura implementada e validada localmente —
imagens buildam, stack prod completa sobe (Caddy TLS + API + web/nginx +
scraper/Chromium + db + redis + backup), smoke test verde, rate limit 429
no 61º req/min, restore de backup testado com dados reais (7.760 produtos).
O deploy real na VPS e o critério de 7 dias de uptime dependem de o usuário
fornecer o host (secrets `VPS_HOST/VPS_USER/VPS_SSH_KEY` + `VPS_DOMAIN`).**

- [x] Dockerfiles multi-stage (api, scraper c/ Playwright, web/nginx)
- [x] `docker-compose.yml` de produção + Caddy (TLS automático)
- [x] Pipeline CI/CD: build → GHCR → deploy SSH na VPS
- [x] Migrações EF no startup com lock (advisory lock do Postgres)
- [x] Backup diário `pg_dump` → off-site (rclone/S3) + teste de restore
- [x] Logs estruturados (Serilog JSON) + alerta simples de scraper quebrado
      (run failed → webhook/email)
- [x] Rate limiting por IP na API (ASP.NET RateLimiter, 60 req/min em /api/*),
      CORS restrito, headers de segurança
- [x] Smoke test pós-deploy automatizado

**Critério de aceite:** deploy de uma tag nova com um comando/push
(workflow `deploy.yml` em tag `v*` — pronto, aguardando VPS); restore de
backup testado de verdade ✅ (7.760 produtos restaurados localmente); 7 dias
de uptime sem intervenção manual (depende do deploy real).

**Como deployar (documentado):**
1. `cp deploy/.env.example deploy/.env` na VPS e preencher `DOMAIN`,
   `POSTGRES_PASSWORD`, `ALERTS_WEBHOOK_URL`, `RCLONE_*`.
2. No GitHub: secrets `VPS_HOST`, `VPS_USER`, `VPS_SSH_KEY`, `VPS_DOMAIN`,
   `VPS_APP_DIR` (default `/opt/openpc`), `GHCR_PAT` (pull de imagens privadas).
3. Push de tag `vX.Y.Z` → pipeline build → GHCR → SSH → compose pull/up →
   smoke test.

**Achados operacionais (registrados no código/docs):**
- **`rate_limit` não é core do Caddy** — exigia o módulo `mholt/caddy-ratelimit`
  e imagem custom via xcaddy; **movido para a API** (ASP.NET RateLimiter,
  60 req/min por IP em `/api/*`, 429 + Retry-After) e o Caddy voltou à imagem
  padrão `caddy:2-alpine` (sem build custom).
- **Bug latente de seed corrigido (M5)**: `DbSeeder` adicionava
  categorias/lojas com `AddRange` e lia os IDs do banco **antes** do
  `SaveChanges` — na primeira subida a API criava 0 jobs de scraping e o
  scraper os criava depois por acidente de ordem (se o scraper atrasasse, os
  jobs nunca existiriam). Agora persiste categorias/lojas antes de buildar os
  jobs: API cria 8 categorias + 3 lojas + 24 jobs sozinha.
- Scraper em produção usa a imagem oficial `mcr.microsoft.com/playwright`
  (Ubuntu noble) com runtime .NET 10 por cima — o `runtime:10.0-alpine` não
  suporta as libs do Chromium, e `runtime:10.0` (noble) não tem SDK para
  `dotnet tool install` do CLI Playwright.
- Alerta de scraper: webhook genérico (`Alerts:WebhookUrl`) com payload
  JSON (`event`, `store`, `category`, `status`, `error`, timestamps);
  fire-and-forget com timeout de 5 s — nunca derruba o job. Cobre Slack/
  Discord/ntfy/gateway de e-mail; sem URL configurada, apenas loga.
- `Logging__Format=json` no compose prod → Serilog JSON no stdout (docker
  logs); texto em dev.
- CORS: `Cors__AllowedOrigins` via env (vazio em prod — front é same-origin
  via Caddy; origens de dev mantidas por default).

---

## M6 — Histórico, ofertas e alertas

**Objetivo:** valor recorrente — motivo para o usuário voltar.

- [ ] Agregação `price_daily` + retenção (raw 90 dias)
- [ ] `/ofertas`: maiores quedas 24 h/7 dias, badge "menor preço em X dias"
- [ ] Gráfico de histórico completo no detalhe do produto
- [ ] Alerta de preço por e-mail (definir alvo no produto; disparo no re-scrape)
      — requer auth mínima ou magic link
- [ ] Detecção de anomalia simples: queda >15% vs mediana 30 dias

**Critério de aceite:** página de ofertas com dados reais; alerta dispara
e-mail em queda real (testado com preço simulado em staging).

---

## M6 — Histórico, ofertas e alertas ✅ (validado 2026-08-08)

**Objetivo:** valor recorrente — motivo para o usuário voltar.

**Resultado: fluxo completo validado de ponta a ponta — agregação `price_daily`
populada com dados reais (3.546 linhas), `/ofertas` com quedas calculadas
(10,7% em 7d, badge "menor preço em 8 dias", toggle 24h/7d no browser), alerta
de preço disparando e-mail (dry-run) com cooldown de 24 h e evento auditado,
gráfico de 90 dias com labels no detalhe, UI de alerta no produto.**

- [x] Agregação `price_daily` + retenção (raw 90 dias, daily 24 meses)
- [x] `/ofertas`: maiores quedas 24 h/7 dias, badge "menor preço em X dias"
- [x] Gráfico de histórico completo no detalhe do produto
- [x] Alerta de preço por e-mail (definir alvo no produto; disparo no re-scrape)
      — magic link de confirmação/cancelamento (auth mínima, sem conta)
- [x] Detecção de anomalia simples: queda >15% vs mediana 30 dias

**Critério de aceite:** ✅ página de ofertas com dados reais (validada no
browser); alerta dispara e-mail em queda real (testado com preço simulado em
staging — dry-run de SMTP + evento registrado).

**Achados operacionais (registrados no código/docs):**
- **Queda de janela = preço no INÍCIO da janela** (primeiro ponto com data ≥
  corte), não o ponto mais recente — "preço de 7 dias atrás" tem semântica
  estável mesmo com séries esparsas. Testes capturaram e fixaram a regra.
- **Badge "menor preço em X dias" = 0 quando o preço atual não é o menor**
  (ontem foi menor) — o front esconde o badge nesse caso; 1..N indica há
  quantos dias o atual é o mínimo.
- Alerta: cooldown de 24 h entre disparos do mesmo alerta (CPU/GPU são
  coletadas 4×/dia); `price_alert_events` é append-only (auditoria);
  cancelamento exige o token do magic link (401 sem ele).
- **Scraper não usa `AddInfrastructure`** (registra DbContext próprio) — o
  `PriceAggregationService` foi registrado manualmente no Program.cs dele.
- **Design-time**: criada `AppDbContextFactory` (IDesignTimeDbContextFactory)
  para `dotnet ef` sem o pacote Design na API — `dotnet ef migrations
  add/update --startup-project OpenPc.Infrastructure`.
- E-mail: `Smtp:Host/Port/Username/Password/From` via env; sem host, dry-run
  no log (modo staging/dev). O e-mail de confirmação do alerta fica a cargo do
  deploy (o link de confirmação é GET `/api/v1/alerts/confirm?token=...`).
- Comandos novos no scraper: `aggregate-prices [dias]` (roda a agregação
  manualmente, não coleta nada) e `alerts-check <productId>` (dispara alertas
  de um produto — validação em staging).

---

## M7 — Auth e builds salvos

**Objetivo:** contas e retenção.

- [ ] .NET Identity + JWT (ou magic link por e-mail — mais leve)
- [ ] Builds nomeados, múltiplos builds por usuário, clone de build público
- [ ] Perfil: meus builds, meus alertas
- [ ] Builds públicos com página de descoberta (ordenados por recência/clones)

**Critério de aceite:** usuário logado salva 2 builds, volta em outro
dispositivo e os recupera; build anônimo é "claimable" após login.

---

## Backlog pós-M7 (sem compromisso de data)

- **Amazon** (adiada na v1): scraping dedicado ou PA-API; arquitetura
  (`IStoreCollector` + dedup) já acomoda uma loja nova sem mudança estrutural
- Links de afiliado (monetização) — Kabum tem programa
- Compatibilidade avançada: watercooler custom, fan headers, USB interno,
  espessura de GPU vs slots PCIe bloqueados
- Mais lojas (AliExpress com frete/imposto calculado)
- Comparação de builds ("o que muda entre build A e B")
- PWA offline do montador
- Recomendação de build por orçamento ("PC de R$ 5.000 para jogos")
- API pública read-only para terceiros

---

## Riscos e mitigações

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Loja bloqueia scraping (Cloudflare/captcha) | Alta | Alto | Spike M1 antes de tudo; fallback Playwright; baixo volume; Amazon já adiada (loja mais hostil fora da v1) |
| Dedup automático une produtos errados | Média | Alto | Conservador no auto-match; fila de revisão; EAN como âncora |
| Specs faltando/incorretas quebram a engine | Média | Alto | Seed editorial p/ socket/BIOS; engine trata spec ausente como "desconhecido" (warning, não erro); UI de correção manual |
| Mudança de layout quebra seletores | Alta | Médio | Preferir JSON-LD/APIs internas; validação de schema no parse + alerta; fixtures em teste detectam regressão |
| Escopo estourar (8 categorias × 4 lojas) | Média | Médio | Fases cortáveis: M6/M7 são independentes; catálogo pode lançar com 5 categorias (CPU, GPU, mobo, RAM, PSU) |
