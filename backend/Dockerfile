# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Stratix.sln ./
COPY src/Stratix.Domain/Stratix.Domain.csproj src/Stratix.Domain/
COPY src/Stratix.Application/Stratix.Application.csproj src/Stratix.Application/
COPY src/Stratix.Infrastructure/Stratix.Infrastructure.csproj src/Stratix.Infrastructure/
COPY src/Stratix.Api/Stratix.Api.csproj src/Stratix.Api/
RUN dotnet restore src/Stratix.Api/Stratix.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/Stratix.Api/Stratix.Api.csproj -c Release -o /app/publish

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && useradd -r -u 1001 stratix

COPY --from=build /app/publish .
RUN mkdir -p /app/data/org-logos /app/data/reports \
    && chown -R stratix:stratix /app
USER stratix

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV Stratix__Storage__Root=/app/data

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=120s --retries=5 \
  CMD curl -f http://localhost:8080/api/health/ready || exit 1

ENTRYPOINT ["dotnet", "Stratix.Api.dll"]
