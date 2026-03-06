#!/bin/bash
# Usage: 
#   ./build.sh          <- Normal build (Release + Tests)
#   ./build.sh --debug  <- Debug build (No Tests + Debug Config + Web Debug)

# 1. Initialize variables
COMPOSE_FILES="-f docker-compose.yml"
MODE="Normal"
CONFIG="Release"

# 2. Check for the --debug flag
if [[ "$1" == "--debug" ]]; then
    # Ensure we include the debug override file
    COMPOSE_FILES="-f docker-compose.yml -f docker-compose.debug.yml"
    MODE="Debug"
    CONFIG="Debug"
fi

echo "🧹 Cleaning stale artifacts and bin/obj folders..."
# Clean local folders to prevent volume-mount pollution in Docker
# dotnet clean
find . -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} +
rm -rf tests/**/TestResults

# 3. Conditional Test Block
if [[ "$MODE" == "Debug" ]]; then
    echo "⏭️  Skipping Unit Tests for Debug Mode..."
else
    echo "🏎️  Running API/Backend Unit Tests with Coverage ($CONFIG Mode)..."
    if ! dotnet test tests/F1.Api.Tests/F1.Api.Tests.csproj -c "$CONFIG" --nologo --verbosity minimal \
        /p:CollectCoverage=true \
        /p:CoverletOutputFormat=cobertura \
        /p:CoverletOutput=./TestResults/api-coverage/ \
        /p:Include="[F1.Api]*%2c[F1.Core]*%2c[F1.Services]*%2c[F1.Infrastructure]*" \
        /p:Threshold=30; then
        printf "\033[0;31m❌ API/Backend tests failed! Aborting build.\033[0m\n"
        printf "\a"
        exit 1
    fi

    echo "🏎️  Running Web Unit Tests with Web-Only Coverage ($CONFIG Mode)..."
    if ! dotnet test tests/F1.Web.Tests/F1.Web.Tests.csproj -c "$CONFIG" --nologo --verbosity minimal \
        /p:CollectCoverage=true \
        /p:CoverletOutputFormat=cobertura \
        /p:CoverletOutput=./TestResults/web-coverage/ \
        /p:Include="[F1.Web]*" \
        /p:Threshold=30; then
        printf "\033[0;31m❌ Web tests failed! Aborting build.\033[0m\n"
        printf "\a"
        exit 1
    fi

    echo "✅ API and Web tests passed!"
fi

# 4. Fire up the containers
echo "🏗️  Starting Docker Build ($MODE Mode with $CONFIG configuration)..."

# We export CONFIG so docker-compose can pick it up, 
# or we pass it directly via --build-arg
export CONFIGURATION=$CONFIG

docker compose $COMPOSE_FILES up -d --build

echo "🚀 Containers are up! Mode: $MODE"
if [[ "$MODE" == "Debug" ]]; then
    echo "💡 DEBUGGER READY:"
    echo "   - API: Attach to 'f1-local' (Port 15000)"
    echo "   - Web: Launch 'Debug Blazor Web (Docker)' in VS Code"
fi