# ============================================================
# OpenPc.Scraper — Dockerfile multi-stage
# target `development`: dotnet watch com fonte montada (dev)
# target `final`: imagem de produção
# ============================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development
WORKDIR /src
COPY . .
ENTRYPOINT ["dotnet", "watch", "run", "--project", "src/OpenPc.Scraper/OpenPc.Scraper.csproj"]

FROM development AS build
RUN dotnet publish src/OpenPc.Scraper/OpenPc.Scraper.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenPc.Scraper.dll"]
