# M1 — Findings do spike de scraping

> Validado em 2026-08-08. Fonte: `spikes/ScrapeSpike` (console app descartável
> que servirá de referência para os collectors do M2).
> **Veredito do gate: 3/3 lojas viáveis (exigência mínima: 2). Kabum, Pichau e
> Terabyte entram no M2.**

## 1. Resumo por loja

| Loja | Anti-bot | Estratégia vencedora | Taxa de sucesso | Throughput | Preço à vista |
|---|---|---|---|---|---|
| **Kabum** | Nenhum (HTTP 200 puro) | **HTTP + `__NEXT_DATA__`** (SSR) | **180/180 (100%)** | 15,6 prod/s | `price` + `maxInstallment` |
| **Pichau** | Cloudflare (bloqueia tudo, até API VTEX) | **Playwright** (Chromium completo) | **45/45 (100%)** | 4,3 cards/s | texto do card (`de R$ X por R$ Y`) |
| **Terabyte** | Cloudflare (idem) | **Playwright** (Chromium completo) | **149/149 (100%)** | 16,4 cards/s | texto do card (`De: R$ X por: R$ Y`) |

Condições do teste: 3 páginas (Kabum) com delay de 1,5–2,5 s entre requests;
sessão única de browser com scroll progressivo (Pichau/Terabyte). Nenhum
bloqueio, captcha ou rate-limit observado nesses volumes.

## 2. Descobertas críticas

### 2.1 Cloudflare: 2 de 3 lojas bloqueiam curl, mas Chromium real passa
- Pichau e Terabyte respondem `403 "Just a moment..."` para `curl` **em qualquer
  rota** — inclusive na API de catálogo VTEX da Pichau no mesmo domínio.
- O **Chromium headless-shell** (modo headless antigo) também é detectado;
  com **Chromium completo** (`Channel = "chromium"` + `--disable-blink-features=AutomationControlled`)
  o challenge resolve em ~2–5 s e a página renderiza normal.
- Kabum: **sem anti-bot nenhum** — `HttpClient` puro com User-Agent honesto.

### 2.2 Estrutura dos dados (o que o M2 vai parsear)

**Kabum — listagem** (`GET /hardware/processadores?page_number=N`):
`__NEXT_DATA__` → `props.pageProps.data` (string JSON duplamente codificada) →
`catalogServer.data[]`:
`code` (SKU), `name`, `price`, `oldPrice`, `priceWithDiscount`,
`maxInstallment` ("10x de R$ 128,99"), `thumbnail`, `available`, `quantity`,
`rating`, `manufacturer.name`, `category`, `friendlyName`, `warranty`.
60 produtos/página; paginação confirmada (`?page_number=2` → current=2, 10 págs
em processadores).

**Kabum — produto** (`GET /produto/{code}/{friendlyName}`):
`pageProps.product` com `technicalInformation.text` — ficha técnica em HTML
("Arquitetura: Zen 3", "Clock", "TDP", "Socket"...) parseável linha a linha.
Sem JSON-LD em nenhuma das duas páginas.

**Pichau — card** (DOM renderizado): nome completo, `de R$ X por R$ Y`
(oldPrice→price PIX), "15% de desconto no PIX", "Em até 12x de R$ Z", badge de
estoque ("60 UNID"), frete grátis por região. URL: `/processador-{slug}` com
part number do fabricante no final (ex: `100-100001721WOF`). ~45 cards por
carga de scroll; catálogo declara 364 processadores → **paginação a mapear no
M2** (padrão VTEX: `?page=2`/`/p2`).

**Terabyte — card** (DOM renderizado): nome, `De: R$ X por: R$ Y`, "à vista no
Pix", "12x de R$ Z sem juros no cartão", badges ("Frete grátis", "2º Mais
vendido"). URL: `/produto/{id}/{slug}` (mesmo padrão da Kabum). 149 cards
coletados (catálogo declara 148 — cobertura completa da categoria).

### 2.3 Dedup: nenhuma loja expõe EAN no front
- Kabum: campo `ean` **inexistente** na listagem e na página de produto.
- Terabyte/Pichau: não observado no DOM da listagem (validar na página de
  produto no M2).
- **Âncora de matching vira o part number do fabricante**, presente em todas:
  Kabum no fim do `name`/`friendlyName` (`100-100000926WOF`), Pichau no fim do
  slug, Terabyte no fim do slug. Nível 2 do dedup (tokens marca+modelo) assume
  papel central; EAN fica como reforço quando existir.

### 2.4 Preço: listagem ≠ página de produto (Kabum)
Ryzen 7 5700X: R$ 2.163,95 na listagem vs R$ 1.289,99 na página de produto.
A listagem expõe `priceWithDiscount` — o M2 deve validar com amostra qual
campo corresponde ao preço PIX exibido ao usuário, e preferir a página de
produto (ou `priceWithDiscount` da listagem) para o preço canônico.

## 3. Estratégia por loja (decisão M1 → M2)

| Loja | Transporte | Coleta | Rate limit seguro | Risco |
|---|---|---|---|---|
| Kabum | `HttpClient` | listagem paginada; página de produto só quando o preço da listagem divergir (validar no M2) | 1 req / 2 s (jitter); catálogo diário + preços 4×/dia em GPU/CPU | baixo — sem anti-bot hoje; mudanças de layout do `__NEXT_DATA__` |
| Terabyte | Playwright | listagem por scroll; página de produto para specs se necessário | 1 sessão de browser por coleta; delay 400–800 ms entre scrolls; 1×/dia | médio — Cloudflare pode endurecer; preço é texto, parse por regex |
| Pichau | Playwright | listagem + paginação VTEX (mapear no M2) | idem Terabyte | médio — idem + página mais pesada (lazy-load agressivo) |

**Arquitetura do collector M2:**
- `IStoreCollector` com transporte plugável: `HttpCollector` (Kabum) e
  `BrowserCollector` (Pichau/Terabyte) compartilhando o pipeline de
  normalização.
- **Pool único de 1–2 browsers Chromium** reutilizado entre coletas (iniciar
  com a API, não por job) — o custo dominante é o boot do browser (~1 s) e o
  Cloudflare só é resolvido uma vez por sessão.
- Scraper roda com **mais RAM que o planejado**: Chromium completo + worker
  .NET ≈ 1,5 GB sob carga. VPS de 4 GB fica apertada com API+DB+redis — revisar
  requisito para 8 GB (ou 2 browsers só em jobs de catálogo completo).
- Página de produto da Kabum (specs) só para produtos sem specs na listagem —
  `technicalInformation` HTML é a fonte de specs; parse por regex linha a linha
  (o normalizer M2 define o contrato por categoria).

## 4. Riscos residuais

1. **Cloudflare endurecer** (Pichau/Terabyte): mitigação = volume baixo,
   frequência diária, browser real. Se bloquearem, a loja degrada para
   "manual/sem scraping" sem quebrar o resto (isolamento por collector).
2. **Preço como texto** (Pichau/Terabyte): regex sensível a mudança de layout;
   os testes com fixtures HTML (M2) cobrem a regressão.
3. **Kabum sem EAN**: matching por part number + tokens; fila de revisão manual
   continua sendo o mecanismo de segurança.

## 5. Spike preservado

`spikes/ScrapeSpike/Program.cs` (Kabum HTTP) e `PlaywrightProbe.cs`
(Pichau/Terabyte) ficam como referência executável para o M2:
```bash
cd spikes/ScrapeSpike
dotnet run            # Kabum HTTP (3 páginas, 180 produtos)
dotnet run -- playwright   # Pichau + Terabyte via Chromium
```
Requer `playwright install chromium` (já feito nesta máquina).
