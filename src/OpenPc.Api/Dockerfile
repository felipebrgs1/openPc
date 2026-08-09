# ============================================================
# OpenPc.Api — Dockerfile multi-stage
# target `development`: dotnet watch com fonte montada (dev)
# target `final`: imagem de produção (M5 usa este target)
# ============================================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development
WORKDIR /src
# restore isolado: a camada só invalida quando csproj/props mudam —
# mudanças em código/outros arquivos reaproveitam o restore (cache gha).
COPY Directory.Build.props Directory.Packages.props ./
COPY src/OpenPc.Domain/OpenPc.Domain.csproj src/OpenPc.Domain/
COPY src/OpenPc.Infrastructure/OpenPc.Infrastructure.csproj src/OpenPc.Infrastructure/
COPY src/OpenPc.Api/OpenPc.Api.csproj src/OpenPc.Api/
RUN dotnet restore src/OpenPc.Api/OpenPc.Api.csproj
COPY . .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "watch", "run", "--project", "src/OpenPc.Api/OpenPc.Api.csproj"]

FROM development AS build
RUN dotnet publish src/OpenPc.Api/OpenPc.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/api/v1/health >/dev/null || exit 1
ENTRYPOINT ["dotnet", "OpenPc.Api.dll"]
