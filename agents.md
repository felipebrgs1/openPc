# Instruções para agentes

Regras permanentes de trabalho neste repositório.

## Scraping

- **NUNCA rodar scraping por conta própria** — nem `run-once`, nem jobs
  agendados, nem coleta manual de loja/categoria, nem re-coleta para
  "refrescar"/"corrigir" dados.
- Scraping só é executado quando o usuário pedir **explicitamente**.
- Em trabalho de desenvolvimento (ex: nova regra da engine, novo endpoint),
  usar os dados já existentes no banco; se faltar dado, sinalizar e esperar
  instrução — não inventar coleta.
