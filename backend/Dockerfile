# ── Stage 1: Build ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj files first for layer caching
COPY HoleriteSign.Core/HoleriteSign.Core.csproj HoleriteSign.Core/
COPY HoleriteSign.Infrastructure/HoleriteSign.Infrastructure.csproj HoleriteSign.Infrastructure/
COPY HoleriteSign.Api/HoleriteSign.Api.csproj HoleriteSign.Api/
RUN dotnet restore HoleriteSign.Api/HoleriteSign.Api.csproj

# Copy everything and publish (ARG busts cache when code changes)
ARG CACHEBUST=1
COPY . .
RUN dotnet publish HoleriteSign.Api/HoleriteSign.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ───────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Security: run as non-root
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "HoleriteSign.Api.dll"]
