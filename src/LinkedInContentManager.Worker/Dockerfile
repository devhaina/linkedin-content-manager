# syntax=docker/dockerfile:1
# Build context must be the repository root (see docker-compose.yml) — this Dockerfile
# references global.json/Directory.Build.props and the full src/ tree by relative path.
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src

COPY global.json .
COPY Directory.Build.props .
COPY src/LinkedInContentManager.Domain/LinkedInContentManager.Domain.csproj src/LinkedInContentManager.Domain/
COPY src/LinkedInContentManager.Application/LinkedInContentManager.Application.csproj src/LinkedInContentManager.Application/
COPY src/LinkedInContentManager.Infrastructure/LinkedInContentManager.Infrastructure.csproj src/LinkedInContentManager.Infrastructure/
COPY src/LinkedInContentManager.Contracts/LinkedInContentManager.Contracts.csproj src/LinkedInContentManager.Contracts/
COPY src/LinkedInContentManager.Worker/LinkedInContentManager.Worker.csproj src/LinkedInContentManager.Worker/
RUN dotnet restore src/LinkedInContentManager.Worker/LinkedInContentManager.Worker.csproj

COPY src/ src/
RUN dotnet publish src/LinkedInContentManager.Worker/LinkedInContentManager.Worker.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS runtime
WORKDIR /app
USER app

COPY --from=build /app .

ENTRYPOINT ["dotnet", "LinkedInContentManager.Worker.dll"]
