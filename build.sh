#!/bin/bash
 # Usage: ./build.sh

# Aborts the Docker build if unit tests fail. 
echo "🏎️  Running All F1 Unit Tests..."
dotnet clean
# This runs every .csproj file found inside the tests folder
# 1. Collect coverage and print summary to console
# We use /p:CollectCoverage=true to trigger the Coverlet MSBuild integration
if dotnet test F1Competition.sln --nologo --verbosity minimal \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=cobertura #/
    #/p:Threshold=20; # Optional: Fails the build if coverage is < 80%
then
    echo "✅ All tests passed! Starting Docker Build..."
    docker compose up -d --build
else
    printf "\033[0;31m❌ One or more tests failed! Aborting build.\033[0m\n"
    printf "\a"
    exit 1
fi