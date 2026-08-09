# Instruções para agentes

Regras permanentes de trabalho neste repositório.

## Git e GitHub (proteção da `main`)

- **`main` é protegida**: push direto é bloqueado para **todos** (inclusive
  admins). Mudanças só entram via **pull request** com a CI verde.
- Checks obrigatórios no PR: `.NET build + test` e `Angular build`
  (`.github/workflows/ci.yml`). PR com check vermelho **não** pode mergear.
- Fluxo padrão de mudança:
  1. Branch a partir de `main`: `git checkout -b <tema>`.
  2. Commits e `git push -u origin <branch>`.
  3. `gh pr create --fill` — a CI roda no PR.
  4. `gh pr merge --squash` — só depois de os checks passarem.
- **NUNCA** tentar burlar a proteção: sem force push, sem deletar branch,
  sem "empurrar" PR vermelho. Push direto na `main` é rejeitado pelo GitHub.
- **Tags `v*` e `workflow_dispatch` do Deploy disparam deploy de produção**
  (`.github/workflows/deploy.yml`): não criar/pushar tag nem disparar o
  workflow sem instrução explícita do usuário.

## Scraping

- **NUNCA rodar scraping por conta própria** — nem `run-once`, nem jobs
  agendados, nem coleta manual de loja/categoria, nem re-coleta para
  "refrescar"/"corrigir" dados.
- Scraping só é executado quando o usuário pedir **explicitamente**.
- O scraper **não roda no compose de produção** nem é buildado no CI
  (imagem construída localmente quando necessário). Coleta/sync de imagens
  (`sync-images`) só com autorização explícita.
- Em trabalho de desenvolvimento (ex: nova regra da engine, novo endpoint),
  usar os dados já existentes no banco; se faltar dado, sinalizar e esperar
  instrução — não inventar coleta.
