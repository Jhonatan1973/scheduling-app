# Build context: repository root  ->  docker build -f src/SchedulingApp.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Restore first (cached layer)
COPY global.json nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/SchedulingApp.Domain/SchedulingApp.Domain.csproj src/SchedulingApp.Domain/
COPY src/SchedulingApp.Contracts/SchedulingApp.Contracts.csproj src/SchedulingApp.Contracts/
COPY src/SchedulingApp.Application/SchedulingApp.Application.csproj src/SchedulingApp.Application/
COPY src/SchedulingApp.Infrastructure/SchedulingApp.Infrastructure.csproj src/SchedulingApp.Infrastructure/
COPY src/SchedulingApp.Api/SchedulingApp.Api.csproj src/SchedulingApp.Api/
RUN dotnet restore src/SchedulingApp.Api/SchedulingApp.Api.csproj

COPY src/ src/
RUN dotnet publish src/SchedulingApp.Api/SchedulingApp.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# IANA time-zone database for professional time zones
RUN apt-get update && apt-get install -y --no-install-recommends tzdata && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "SchedulingApp.Api.dll"]
