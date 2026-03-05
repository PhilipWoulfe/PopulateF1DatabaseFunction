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
dotnet clean
# find . -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} +
rm -rf tests/**/TestResults

# 3. Conditional Test Block
if [[ "$MODE" == "Debug" ]]; then
    echo "⏭️  Skipping Unit Tests for Debug Mode..."
else
    echo "🏎️  Running All F1 Unit Tests ($CONFIG Mode)..."
    if ! dotnet test F1Competition.sln -c "$CONFIG" --nologo --verbosity minimal \
        /p:CollectCoverage=true \
        /p:CoverletOutputFormat=cobertura \
        /p:Threshold=30; then
        printf "\033[0;31m❌ One or more tests failed! Aborting build.\033[0m\n"
        printf "\a"
        exit 1
    fi
    echo "✅ All tests passed!"
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