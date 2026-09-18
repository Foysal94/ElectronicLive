# Multi-stage build for ElectronicLive .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY backend/ElectronicLive.sln ./
COPY backend/Directory.Build.props ./
COPY backend/src/ElectronicLive.Api/ElectronicLive.Api.csproj src/ElectronicLive.Api/
COPY backend/tests/ElectronicLive.Api.UnitTests/ElectronicLive.Api.UnitTests.csproj tests/ElectronicLive.Api.UnitTests/

# Restore dependencies
RUN dotnet restore ElectronicLive.sln

# Copy backend source files
COPY backend/src/ src/
WORKDIR /src/src/ElectronicLive.Api

# Publish release build
RUN dotnet publish ElectronicLive.Api.csproj -c Release -o /app/publish --no-restore

# Final lightweight runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "ElectronicLive.Api.dll"]
