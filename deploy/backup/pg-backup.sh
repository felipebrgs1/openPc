#!/bin/sh
# ============================================================
# OpenPC — backup diário do Postgres (M5)
# pg_dump custom (comprimido) → /backups + retenção local
# + cópia off-site via rclone (S3) se RCLONE_REMOTE configurado.
# Rodado pelo cron do container backup (03:30 UTC).
# ============================================================
set -eu

STAMP=$(date +%Y%m%d_%H%M%S)
DUMP="/backups/openpc_${STAMP}.dump"
KEEP="${BACKUP_KEEP_DAYS:-14}"

echo "[backup] iniciando pg_dump -> ${DUMP}"
pg_dump -Fc -f "${DUMP}"

# Retenção local: apaga dumps mais antigos que KEEP dias
find /backups -name 'openpc_*.dump' -mtime "+${KEEP}" -delete

# Off-site (opcional)
if [ -n "${RCLONE_REMOTE:-}" ]; then
    echo "[backup] copiando para rclone: ${RCLONE_REMOTE}"
    rclone copy "${DUMP}" "${RCLONE_REMOTE}" \
        --s3-provider AWS \
        --s3-access-key-id "${RCLONE_S3_ACCESS_KEY_ID:-}" \
        --s3-secret-access-key "${RCLONE_S3_SECRET_ACCESS_KEY:-}" \
        --s3-region "${RCLONE_S3_REGION:-sa-east-1}" \
        ${RCLONE_S3_ENDPOINT:+--s3-endpoint "${RCLONE_S3_ENDPOINT}"}
else
    echo "[backup] RCLONE_REMOTE vazio — sem cópia off-site"
fi

echo "[backup] ok: ${DUMP}"
