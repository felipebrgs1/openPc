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

# Fotos do catálogo (bucket MinIO) → mesmo destino off-site. rclone fala S3
# com o MinIO; copy é idempotente (pula objetos idênticos) e não apaga nada.
if [ -n "${RCLONE_REMOTE:-}" ] && [ -n "${MINIO_ACCESS_KEY:-}" ]; then
    echo "[backup] espelhando bucket MinIO -> ${RCLONE_REMOTE}/openpc-images"
    rclone copy "minio:openpc-images" "${RCLONE_REMOTE}/openpc-images" \
        --s3-provider Minio \
        --s3-access-key-id "${MINIO_ACCESS_KEY}" \
        --s3-secret-access-key "${MINIO_SECRET_KEY}" \
        --s3-endpoint "http://${MINIO_HOST:-minio}:9000" \
        --s3-no-check-bucket
else
    echo "[backup] sem MinIO ou RCLONE_REMOTE — fotos não espelhadas"
fi

echo "[backup] ok: ${DUMP}"
