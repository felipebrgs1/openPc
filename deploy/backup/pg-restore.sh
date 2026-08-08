#!/bin/sh
# ============================================================
# OpenPC — teste de restore (M5)
# Restaura o dump mais recente em um banco efêmero e valida a
# contagem de produtos — prova que o backup serve para recovery.
#
# Uso:
#   docker compose -f deploy/docker-compose.yml exec backup \
#     /backup/pg-restore.sh [arquivo.dump]
# Sem argumento, usa o dump mais recente de /backups.
# ============================================================
set -eu

DUMP="${1:-$(ls -t /backups/openpc_*.dump 2>/dev/null | head -1)}"
TEST_DB="openpc_restore_test"

if [ -z "${DUMP}" ] || [ ! -f "${DUMP}" ]; then
    echo "[restore] erro: nenhum dump encontrado em /backups" >&2
    exit 1
fi

echo "[restore] dump: ${DUMP}"

# Banco de teste descartável (mesmo cluster, nome separado)
psql -d postgres -c "DROP DATABASE IF EXISTS ${TEST_DB};"
createdb "${TEST_DB}"

echo "[restore] restaurando..."
pg_restore --no-owner --no-privileges -d "${TEST_DB}" "${DUMP}"

PRODUCTS=$(psql -d "${TEST_DB}" -tAc "SELECT count(*) FROM products;")
LISTINGS=$(psql -d "${TEST_DB}" -tAc "SELECT count(*) FROM listings;")

echo "[restore] ok — produtos: ${PRODUCTS}, listagens: ${LISTINGS}"

# Limpeza do banco de teste
psql -d postgres -c "DROP DATABASE IF EXISTS ${TEST_DB};"
echo "[restore] banco de teste removido"
