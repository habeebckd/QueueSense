# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY src/backend/JobFlow.sln ./src/backend/
COPY src/backend/JobFlow.Core/JobFlow.Core.csproj ./src/backend/JobFlow.Core/
COPY src/backend/JobFlow.Infrastructure/JobFlow.Infrastructure.csproj ./src/backend/JobFlow.Infrastructure/
COPY src/backend/JobFlow.Worker/JobFlow.Worker.csproj ./src/backend/JobFlow.Worker/
COPY src/backend/JobFlow.Api/JobFlow.Api.csproj ./src/backend/JobFlow.Api/
COPY src/backend/JobFlow.Tests/JobFlow.Tests.csproj ./src/backend/JobFlow.Tests/

RUN dotnet restore ./src/backend/JobFlow.sln

# Copy the rest of the source code
COPY src/backend/ ./src/backend/

# Publish Api
WORKDIR /src/backend/JobFlow.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Run as non-root user for security
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "JobFlow.Api.dll"]
