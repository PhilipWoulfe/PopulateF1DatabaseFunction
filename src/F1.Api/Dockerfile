# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# 1. Declare the argument (Defaults to Release)
ARG CONFIGURATION=Release
WORKDIR /app

# Copy project files first for better caching
COPY ["src/F1.Api/F1.Api.csproj", "src/F1.Api/"]
COPY ["src/F1.Core/F1.Core.csproj", "src/F1.Core/"]
COPY ["src/F1.Services/F1.Services.csproj", "src/F1.Services/"]
COPY ["src/F1.Infrastructure/F1.Infrastructure.csproj", "src/F1.Infrastructure/"]

RUN dotnet restore "src/F1.Api/F1.Api.csproj"

COPY . .
WORKDIR "/app/src/F1.Api"

# 2. Use the variable in the publish command
RUN dotnet publish -c $CONFIGURATION -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "F1.Api.dll"]