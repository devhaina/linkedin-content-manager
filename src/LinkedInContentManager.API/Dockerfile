# syntax=docker/dockerfile:1
# Build context must be the repository root (see docker-compose.yml) — this Dockerfile
# references global.json/Directory.Build.props and the full src/ tree by relative path.
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src

# Copy only the project files first so NuGet restore is cached across rebuilds
# that only change source, not dependencies.
COPY global.json .
COPY Directory.Build.props .
COPY src/LinkedInContentManager.Domain/LinkedInContentManager.Domain.csproj src/LinkedInContentManager.Domain/
COPY src/LinkedInContentManager.Application/LinkedInContentManager.Application.csproj src/LinkedInContentManager.Application/
COPY src/LinkedInContentManager.Infrastructure/LinkedInContentManager.Infrastructure.csproj src/LinkedInContentManager.Infrastructure/
COPY src/LinkedInContentManager.Contracts/LinkedInContentManager.Contracts.csproj src/LinkedInContentManager.Contracts/
COPY src/LinkedInContentManager.API/LinkedInContentManager.API.csproj src/LinkedInContentManager.API/
RUN dotnet restore src/LinkedInContentManager.API/LinkedInContentManager.API.csproj

COPY src/ src/
RUN dotnet publish src/LinkedInContentManager.API/LinkedInContentManager.API.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS runtime
WORKDIR /app

USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
# Runs as a non-root user — the image ships one named "app" (UID 64198) by default.
USER app

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "LinkedInContentManager.API.dll"]
