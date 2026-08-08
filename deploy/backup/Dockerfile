# ============================================================
# OpenPC — container de backup (M5)
# postgres client (pg_dump/pg_restore) + rclone + cron diário.
# ============================================================

FROM postgres:18-alpine

RUN apk add --no-cache rclone curl tzdata

COPY pg-backup.sh /backup/pg-backup.sh
COPY pg-restore.sh /backup/pg-restore.sh
COPY entrypoint.sh /backup/entrypoint.sh
RUN chmod +x /backup/*.sh

ENTRYPOINT ["/backup/entrypoint.sh"]
