# ============================================================
# OpenPC web — Dockerfile multi-stage (produção, M5)
# build: Angular 22 (node 24) → nginx:alpine servindo a SPA.
# O proxy /api NÃO fica aqui: em produção o Caddy roteia
# /api/* direto para o serviço api (same-origin para o browser).
# ============================================================

FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/web/browser /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -qO- http://127.0.0.1/ >/dev/null || exit 1
CMD ["nginx", "-g", "daemon off;"]
