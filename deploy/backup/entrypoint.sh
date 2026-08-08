#!/bin/sh
# ============================================================
# OpenPC — entrypoint do container de backup
# Agenda o backup diário (03:30 UTC) e mantém o container vivo.
# Também roda um backup imediato no primeiro boot (idempotente),
# para validar o pipeline cedo.
# ============================================================
set -eu

# Cron diário 03:30 UTC
echo "30 3 * * * /backup/pg-backup.sh >> /var/log/backup.log 2>&1" | crontab -

# Primeira execução imediata (idempotente)
/backup/pg-backup.sh >> /var/log/backup.log 2>&1

echo "[backup] container pronto — cron diário 03:30 UTC agendado"
tail -f /var/log/backup.log
