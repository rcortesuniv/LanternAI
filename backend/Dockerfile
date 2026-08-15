FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (layer-cacheable) before copying the rest of the source.
COPY LanternAI.sln .
COPY src/LanternAI.Api/LanternAI.Api.csproj src/LanternAI.Api/
COPY tests/LanternAI.Api.Tests/LanternAI.Api.Tests.csproj tests/LanternAI.Api.Tests/
RUN dotnet restore LanternAI.sln

COPY . .
RUN dotnet publish src/LanternAI.Api/LanternAI.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# curl is needed for the Docker healthcheck; the aspnet base image doesn't include it.
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LanternAI.Api.dll"]
