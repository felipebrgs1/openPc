# ============================================================
# OpenPC scraper Python — imagem oficial do Playwright (noble)
# já traz Python + Chromium completo + libs do sistema + deps.
# Uso: docker run --env DATABASE_URL=... openpc-scraper run-once kabum cpu
# ============================================================

FROM mcr.microsoft.com/playwright/python:v1.61.0-noble

WORKDIR /app

COPY pyproject.toml README.md ./
COPY src ./src

RUN pip install --no-cache-dir . \
    && rm -rf /root/.cache/pip

ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
ENTRYPOINT ["python", "-m", "openpc_scraper"]
