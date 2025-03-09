# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Base image for running Azure Functions in an isolated process with .NET 8
FROM mcr.microsoft.com/azure-functions/dotnet:4-dotnet8 AS base
WORKDIR /home/site/wwwroot
EXPOSE 8080

# Build stage using .NET SDK 8.0
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["PopulateF1Database/PopulateF1Database.csproj", "PopulateF1Database/"]
RUN dotnet restore "./PopulateF1Database/PopulateF1Database.csproj"

# Copy the entire source code and build the project
COPY . .
WORKDIR "/src/PopulateF1Database"
RUN dotnet build "./PopulateF1Database.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage to publish the service project
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PopulateF1Database.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage for production or regular mode
FROM base AS final
WORKDIR /home/site/wwwroot

# Copy the published output from the publish stage
COPY --from=publish /app/publish .

# Set environment variables for Azure Functions runtime
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

# Optionally, add a health check (uncomment if needed)
# HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
#     CMD curl -f http://localhost:8080/ || exit 1