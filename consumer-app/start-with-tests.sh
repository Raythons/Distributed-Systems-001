#!/bin/bash
set -e

# This script runs tests before starting the application
# It assumes the workspace root is mounted at /workspace

echo "=========================================="
echo "Building consumer-app..."
echo "=========================================="

cd /workspace/consumer-app
dotnet build

echo "=========================================="
echo "Running Tests for consumer-app"
echo "=========================================="

cd /workspace/consumer-app.Tests
echo "Building and running unit tests..."
dotnet build
dotnet test

TEST_RESULT=$?

if [ $TEST_RESULT -eq 0 ]; then
    echo "=========================================="
    echo "✓ All tests passed! Starting application..."
    echo "=========================================="
    cd /workspace/consumer-app
    dotnet run
else
    echo "=========================================="
    echo "✗ Tests failed! Application will not start."
    echo "=========================================="
    exit $TEST_RESULT
fi

