#!/bin/bash

echo "🔍 Verifying Backend and Frontend Configuration"
echo "================================================"
echo ""

# Check Backend Configuration (Port 8081)
echo "📦 BACKEND CONFIGURATION (Port 8081):"
echo "-----------------------------------"

if grep -q "8081" backend/Api/appsettings.json; then
    echo "✅ appsettings.json: Port 8081 configured"
else
    echo "❌ appsettings.json: Port 8081 NOT found"
fi

if grep -q "8081" backend/Api/appsettings.Development.json; then
    echo "✅ appsettings.Development.json: Port 8081 configured"
else
    echo "❌ appsettings.Development.json: Port 8081 NOT found"
fi

if grep -q "8081" backend/Api/Properties/launchSettings.json; then
    echo "✅ launchSettings.json: Port 8081 configured"
else
    echo "❌ launchSettings.json: Port 8081 NOT found"
fi

# Check Frontend Configuration (Port 3001)
echo ""
echo "🌐 FRONTEND CONFIGURATION (Port 3001):"
echo "-----------------------------------"

if grep -q "3001" package.json; then
    echo "✅ package.json: Port 3001 configured"
else
    echo "❌ package.json: Port 3001 NOT found"
fi

if grep -q "8081" lib/api-client.ts; then
    echo "✅ api-client.ts: Backend URL set to port 8081"
else
    echo "❌ api-client.ts: Backend URL NOT set to port 8081"
fi

# Check if services are running
echo ""
echo "🚀 SERVICE STATUS:"
echo "-----------------------------------"

if lsof -i:8081 > /dev/null 2>&1; then
    echo "✅ Backend is running on port 8081"
    lsof -i:8081 | head -2
else
    echo "❌ Backend is NOT running on port 8081"
fi

if lsof -i:3001 > /dev/null 2>&1; then
    echo "✅ Frontend is running on port 3001"
    lsof -i:3001 | head -2
else
    echo "❌ Frontend is NOT running on port 3001"
fi

# Test connectivity
echo ""
echo "🔌 CONNECTIVITY TESTS:"
echo "-----------------------------------"

if curl -s http://localhost:8081/api/health > /dev/null 2>&1; then
    echo "✅ Backend health check: SUCCESS"
    curl -s http://localhost:8081/api/health | head -3
else
    echo "❌ Backend health check: FAILED (backend may not be running)"
fi

if curl -s http://localhost:3001 > /dev/null 2>&1; then
    echo "✅ Frontend check: SUCCESS"
else
    echo "❌ Frontend check: FAILED (frontend may not be running)"
fi

echo ""
echo "================================================"
echo "✅ Verification Complete!"
echo ""
echo "To start backend: cd backend/Api && dotnet run"
echo "To start frontend: npm run dev"
echo ""






