# ============================================================
# OpenPC — Caddy custom (M5)
# Caddy 2.9 + módulo mholt/caddy-ratelimit (rate_limit NÃO está
# no core do Caddy — é plugin de terceiros, build via xcaddy).
# ============================================================

FROM caddy:2.9-builder AS builder
RUN xcaddy build --with github.com/mholt/caddy-ratelimit

FROM caddy:2.9-alpine
COPY --from=builder /usr/bin/caddy /usr/bin/caddy
