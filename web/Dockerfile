# ============================================================
# OpenPC web — Dockerfile (dev)
# Servidor de desenvolvimento do Angular (HMR).
# A imagem de produção (nginx multi-stage) chega no M5.
# ============================================================

FROM node:24-alpine
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
EXPOSE 4200
CMD ["npm", "start", "--", "--host", "0.0.0.0", "--proxy-config", "proxy.conf.container.json"]
