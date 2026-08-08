# ============================================================
# OpenPC web — Dockerfile multi-stage
# target `development`: Angular dev server com HMR (compose dev)
# target `final`: produção (M5) — SPA estática via nginx.
# O proxy /api NÃO fica na imagem de produção: em prod o Caddy roteia
# /api/* direto para o serviço api (same-origin para o browser).
# ============================================================

FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .

FROM build AS development
EXPOSE 4200
CMD ["npm", "start", "--", "--host", "0.0.0.0", "--proxy-config", "proxy.conf.container.json"]

FROM build AS prod-build
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=prod-build /app/dist/web/browser /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -qO- http://127.0.0.1/ >/dev/null || exit 1
CMD ["nginx", "-g", "daemon off;"]
