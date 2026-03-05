#!/bin/bash
# Usage: 
#   ./build.sh          <- Normal build
#   ./build.sh --debug  <- Debug build with watch & vsdbg

# 1. Initialize variables
COMPOSE_FILES="-f docker-compose.yml"
MODE="Normal"

# 2. Check for the --debug flag
if [ "$1" == "--debug" ]; then
    COMPOSE_FILES="-f docker-compose.yml -f docker-compose.debug.yml"
    MODE="Debug"
fi

echo "🧹 Cleaning stale artifacts and bin/obj folders..."
# Nuclear clean to prevent "ghost" DLLs
find . -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} +
rm -rf tests/**/TestResults

echo "🏎️  Running All F1 Unit Tests ($MODE Mode)..."

# 3. Run Tests
if dotnet test F1Competition.sln --nologo --verbosity minimal \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=cobertura \
    /p:Threshold=40; # Optional: Fails the build if coverage is < value
then
    echo "✅ All tests passed! Starting Docker Build ($MODE Mode)..."
    
    # 4. Fire up the containers using the selected files
    docker compose $COMPOSE_FILES up -d --build
    
    echo "🚀 Containers are up! Mode: $MODE"
    if [ "$MODE" == "Debug" ]; then
        echo "💡 Don't forget to attach your VS Code debugger (F5)!"
    fi
else
    printf "\033[0;31m❌ One or more tests failed! Aborting build.\033[0m\n"
    printf "\a"
    exit 1
fi