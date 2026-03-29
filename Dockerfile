# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY RCParkWeatherForcaster.sln ./
COPY RCParkWeatherForcaster/RCParkWeatherForcaster.csproj RCParkWeatherForcaster/
COPY RCParkWeatherForcaster.Tests/RCParkWeatherForcaster.Tests.csproj RCParkWeatherForcaster.Tests/
RUN dotnet restore

# Copy all source
COPY . .

# Run tests
RUN dotnet test RCParkWeatherForcaster.Tests/RCParkWeatherForcaster.Tests.csproj \
    --configuration Release \
    --no-restore \
    --logger "console;verbosity=normal"

# Publish the app
RUN dotnet publish RCParkWeatherForcaster/RCParkWeatherForcaster.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# ASP.NET Core listens on port 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Override any appsettings.json value via env vars, e.g.:
#   -e Location__Name="My Field"
#   -e Location__Latitude=38.8977
#   -e Location__Longitude=-77.0365
#   -e PlaneSize=Large
#   -e ForecastHours=24

ENTRYPOINT ["dotnet", "RCParkWeatherForcaster.dll"]
