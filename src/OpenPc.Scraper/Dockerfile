# ============================================================
# OpenPc.Scraper — Dockerfile multi-stage
# target `development`: dotnet watch com fonte montada (dev)
# target `final`: produção com Chromium completo + deps (M5)
#
# O final usa a imagem oficial do Playwright (Ubuntu noble) que já
# traz Chromium completo + libs do sistema + PLAYWRIGHT_BROWSERS_PATH
# em /ms-playwright. Chromium completo (não headless-shell) é
# obrigatório — o Cloudflare de Pichau/Terabyte detecta o shell (M1).
# O runtime .NET 10 é instalado por cima via dotnet-install.sh.
# ============================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development
WORKDIR /src
# restore isolado: a camada só invalida quando csproj/props mudam —
# mudanças em código/outros arquivos reaproveitam o restore (cache gha).
COPY Directory.Build.props Directory.Packages.props ./
COPY src/OpenPc.Domain/OpenPc.Domain.csproj src/OpenPc.Domain/
COPY src/OpenPc.Infrastructure/OpenPc.Infrastructure.csproj src/OpenPc.Infrastructure/
COPY src/OpenPc.Scraper/OpenPc.Scraper.csproj src/OpenPc.Scraper/
RUN dotnet restore src/OpenPc.Scraper/OpenPc.Scraper.csproj
COPY . .
ENTRYPOINT ["dotnet", "watch", "run", "--project", "src/OpenPc.Scraper/OpenPc.Scraper.csproj"]

FROM development AS build
RUN dotnet publish src/OpenPc.Scraper/OpenPc.Scraper.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/playwright:v1.61.0-noble AS final
# .NET runtime 10 (o playwright image não tem dotnet)
RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin \
        --channel 10.0 --runtime dotnet --install-dir /usr/share/dotnet \
    && ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet

WORKDIR /app
COPY --from=build /app/publish .
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
ENTRYPOINT ["dotnet", "OpenPc.Scraper.dll"]
