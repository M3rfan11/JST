#!/bin/bash

# Script to start the .NET backend, handling port conflicts

echo "🔧 Cleaning up existing processes..."

# Kill all dotnet processes related to the Api
pkill -9 -f "dotnet.*Api" 2>/dev/null
pkill -9 -f "dotnet run" 2>/dev/null

# Kill any process on port 8080
lsof -ti:8080 | xargs kill -9 2>/dev/null

# Wait a moment for ports to be released
sleep 2

# Check if port is free
if lsof -ti:8080 > /dev/null 2>&1; then
    echo "❌ Port 8080 is still in use. Trying to kill again..."
    lsof -ti:8080 | xargs kill -9 2>/dev/null
    sleep 2
fi

# Verify port is free
if lsof -ti:8080 > /dev/null 2>&1; then
    echo "❌ ERROR: Port 8080 is still occupied. Please manually kill the process:"
    lsof -i:8080
    exit 1
fi

echo "✅ Port 8080 is now free"
echo ""
echo "🚀 Starting backend..."
echo ""

# Navigate to backend directory and run
cd "$(dirname "$0")/backend/Api" || exit 1

# Run dotnet
dotnet run






