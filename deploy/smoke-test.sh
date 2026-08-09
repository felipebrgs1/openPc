#!/usr/bin/env bash
# ============================================================
# OpenPC — smoke test pós-deploy (M5)
# Valida que a stack está no ar e respondendo com dados reais.
#
# Uso:
#   deploy/smoke-test.sh [BASE_URL]
# Default: http://localhost (útil no CI pós-deploy: https://dominio)
#
# Verifica: health da API, categorias, produtos, health dos scrapers,
# e o front servindo a SPA.
# ============================================================
set -euo pipefail

BASE_URL="${1:-http://localhost}"

# Caddy usa cert interno (auto-assinado) para localhost — curl precisa de -k.
CURL_EXTRA=()
case "${BASE_URL}" in
    http://localhost*|https://localhost*) CURL_EXTRA=(-k) ;;
esac

echo "== smoke test: ${BASE_URL} =="

curl_ok() {
    local url="$1"
    local desc="$2"
    local status
    status=$(curl -sS "${CURL_EXTRA[@]}" -o /dev/null -w '%{http_code}' --max-time 15 "${BASE_URL}${url}")
    if [ "${status}" = "200" ]; then
        echo "ok   ${desc} (${status})"
    else
        echo "FAIL ${desc} (HTTP ${status})" >&2
        exit 1
    fi
}

# API: health (espera 200)
curl_ok "/api/v1/health" "GET /api/v1/health"

# API: categorias com dados (8 categorias do seed)
categories=$(curl -sS "${CURL_EXTRA[@]}" --max-time 15 "${BASE_URL}/api/v1/categories")
count=$(echo "${categories}" | grep -o '"slug"' | wc -l)
if [ "${count}" -ge 8 ]; then
    echo "ok   GET /api/v1/categories (${count} categorias)"
else
    echo "FAIL categorias insuficientes (${count})" >&2
    exit 1
fi

# API: produtos reais no catálogo
products=$(curl -sS "${CURL_EXTRA[@]}" --max-time 15 "${BASE_URL}/api/v1/products?limit=3")
echo "${products}" | grep -q '"items"' || { echo "FAIL GET /api/v1/products" >&2; exit 1; }
echo "ok   GET /api/v1/products (limit=3)"

# API: health dos scrapers
curl_ok "/api/v1/health/scrapers" "GET /api/v1/health/scrapers"

# Fotos: produto com imagem própria (/images/* → MinIO via Caddy)
# grep -m1 (sem head) evita SIGPIPE com pipefail
img=$(curl -sS "${CURL_EXTRA[@]}" --max-time 15 "${BASE_URL}/api/v1/products?limit=100" \
    | grep -om1 '"/images/[a-f0-9]\{40\}\.[a-z0-9]*"' | tr -d '"')
if [ -n "${img}" ]; then
    curl_ok "${img}" "GET ${img}"
    echo "ok   foto servida do MinIO (${img})"
else
    echo "FAIL nenhum produto com imagem própria (/images/...)" >&2
    exit 1
fi

# API: cria um build (fluxo do montador)
slug=$(curl -sS "${CURL_EXTRA[@]}" --max-time 15 -X POST -H 'Content-Type: application/json' \
    -d '{"name":"smoke-test"}' "${BASE_URL}/api/v1/builds" \
    | grep -o '"slug":"[^"]*"' | cut -d'"' -f4)
if [ -n "${slug}" ]; then
    curl_ok "/api/v1/builds/${slug}" "GET /api/v1/builds/${slug}"
    echo "ok   POST /api/v1/builds (slug=${slug})"
else
    echo "FAIL POST /api/v1/builds" >&2
    exit 1
fi

# Front: SPA servida
curl_ok "/" "GET / (SPA)"

echo "== smoke test OK =="
