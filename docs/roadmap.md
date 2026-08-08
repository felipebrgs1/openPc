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
| M5 | Deploy VPS + observabilidade | 1 semana |
| M6 | Histórico, ofertas e alertas | 2 semanas |
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

## M1 — Spike de scraping (validação de risco)

**Objetivo:** provar que dá para coletar dados úteis das 3 lojas da v1
**antes** de construir o pipeline inteiro. Kabum primeiro — é a loja piloto.

- [ ] Protótipo descartável (console app) por loja, na ordem:
  - [ ] **Kabum**: listar 1 categoria (processadores), extrair
        nome/preço/URL/sku/EAN
  - [ ] Terabyte: idem
  - [ ] Pichau: idem
- [ ] Comparar para cada loja: JSON-LD vs API interna vs Playwright
  (custo, estabilidade, taxa de sucesso em 100 requisições)
- [ ] Documentar por loja: estratégia escolhida, rate limit seguro, specs
  extraíveis da página de produto (socket, TDP, dimensões...)
- [ ] ~~Amazon~~ — adiada para o backlog pós-M7 (decisão 2026-08-07)

**Critério de aceite:** relatório curto em `docs/scraping-findings.md` com
taxa de sucesso por loja e estratégia definida. **Gate:** se menos de 2 lojas
forem viáveis, repensar escopo antes de seguir.

---

## M2 — Catálogo: ingestão + API de leitura

**Objetivo:** banco populado com produtos reais e API servindo o catálogo.

- [ ] Schema completo: `products`, `product_attributes`, `listings`,
      `price_history`, `scrape_jobs`, `scrape_runs`
- [ ] `IStoreCollector` + collectors de produção (Kabum, Terabyte, Pichau)
- [ ] Normalizer: parse de specs por categoria → `product_attributes`
- [ ] Dedup níveis 1 e 2 (EAN + tokens) + fila de revisão manual
- [ ] Scheduler (Quartz.NET): catálogo diário, preços 4×/dia em GPU/CPU
- [ ] Endpoints: `GET /categories`, `GET /products` (busca, filtros por
      atributo, ordenação), `GET /products/{id}`, `GET /stores`,
      `GET /health/scrapers`
- [ ] Cache Redis nas listagens
- [ ] Testes: normalizer com fixtures HTML versionadas por loja

**Critério de aceite:** catálogo com ≥3 lojas e ≥1.500 produtos; busca por
"7600" retorna o mesmo produto canônico com ofertas de lojas diferentes;
`scrape_runs` mostra runs diários saudáveis.

---

## M3 — Engine de compatibilidade

**Objetivo:** coração do produto, com cobertura de testes alta.

- [ ] `BuildSnapshot` + `ICompatibilityRule` + executor (§4.1)
- [ ] Todas as regras **Error** da tabela §4.2
- [ ] Regras **Warning** prioritárias: `PSU_WATTAGE_LOW`, `NO_GPU_NO_IGPU`,
      `BIOS_UPDATE_NEEDED`, `RAM_SPEED_CAPPED`
- [ ] Seed curado `compatibility.json` (matriz socket/chipset/BIOS,
      gerações AM4/AM5/LGA1700/LGA1851)
- [ ] Endpoints: `POST /builds`, `PUT/DELETE .../items`,
      `GET .../compatibility`, filtro `compatibleWith` em `GET /products`
- [ ] Estimador de wattage (TDP CPU+GPU+overhead) com margem recomendada
- [ ] Testes de unidade: cada regra com casos positivo/negativo/borda
      (ex: GPU com comprimento exatamente igual ao limite do gabinete)

**Critério de aceite:** build com Ryzen AM5 + placa AM4 → erro
`CPU_SOCKET_MISMATCH`; seletor de placa-mãe filtrado por socket; wattage
estimado bate com calculadoras de referência (±10%).

---

## M4 — Frontend: catálogo + montador

**Objetivo:** produto utilizável de ponta a ponta (anônimo).

- [ ] Design system mínimo com Tailwind (decidido: sem Material) + tokens de
      cor/espaçamento
- [ ] `/` home, `/pecas/:category`, `/pecas/:category/:id` (detalhe + ofertas
      por loja + gráfico de histórico)
- [ ] **`/montar`**: 8 slots, seletor filtrado por compatibilidade, toggle
      "mostrar incompatíveis" com motivo inline
- [ ] Painel do build: preço total (menor preço × por loja), barra de wattage,
      lista de errors/warnings acionáveis
- [ ] `/build/:slug` compartilhável (anônimo, slug na URL)
- [ ] Estado do build com signals + persistência em `localStorage`
- [ ] Responsivo (mobile-first no montador)
- [ ] Formatação BRL, datas pt-BR, SEO básico (meta tags por rota)

**Critério de aceite:** usuário anônimo monta um PC completo, vê
incompatibilidades bloqueadas, preço total por loja e compartilha o link.
Teste manual de fluxo completo no browser.

---

## M5 — Deploy VPS + observabilidade

**Objetivo:** produção estável e operável.

- [ ] Dockerfiles multi-stage (api, scraper c/ Playwright, web/nginx)
- [ ] `docker-compose.yml` de produção + Caddy (TLS automático)
- [ ] Pipeline CI/CD: build → GHCR → deploy SSH na VPS
- [ ] Migrações EF no startup com lock
- [ ] Backup diário `pg_dump` → off-site (rclone/S3) + teste de restore
- [ ] Logs estruturados (Serilog JSON) + alerta simples de scraper quebrado
      (run failed → webhook/email)
- [ ] Rate limiting no Caddy, CORS restrito, headers de segurança
- [ ] Smoke test pós-deploy automatizado

**Critério de aceite:** deploy de uma tag nova com um comando/push; restore de
backup testado de verdade; 7 dias de uptime sem intervenção manual.

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
