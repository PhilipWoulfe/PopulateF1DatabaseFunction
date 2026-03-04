#!/bin/bash
# Usage: ./build.sh
# Aborts the Docker build if unit tests fail.

echo "🏎️  Running F1 Unit Tests..."

if dotnet test tests/F1.Tests.Unit --nologo --verbosity minimal; then
    echo "✅ Tests passed! Starting Docker Build..."
    docker-compose up -d --build
else
    printf "\033[0;31m❌ Tests failed! Aborting build.\033[0m\n"
    printf "\a" # Sound alert
    exit 1
fi