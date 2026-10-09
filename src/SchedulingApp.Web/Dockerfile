# Build context: repository root  ->  docker build -f src/SchedulingApp.Web/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY global.json nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/SchedulingApp.Contracts/SchedulingApp.Contracts.csproj src/SchedulingApp.Contracts/
COPY src/SchedulingApp.Web/SchedulingApp.Web.csproj src/SchedulingApp.Web/
RUN dotnet restore src/SchedulingApp.Web/SchedulingApp.Web.csproj

COPY src/SchedulingApp.Contracts/ src/SchedulingApp.Contracts/
COPY src/SchedulingApp.Web/ src/SchedulingApp.Web/
RUN dotnet publish src/SchedulingApp.Web/SchedulingApp.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends tzdata && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "SchedulingApp.Web.dll"]
