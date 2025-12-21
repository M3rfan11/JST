#!/bin/bash

echo "🔍 Testing Backend Connection..."
echo ""

echo "1. Checking if backend is running..."
if curl -s http://localhost:8080/api/health > /dev/null 2>&1; then
    echo "   ✅ Backend is responding"
    
    echo ""
    echo "2. Testing health endpoint..."
    curl -s http://localhost:8080/api/health | python3 -m json.tool 2>/dev/null || curl -s http://localhost:8080/api/health
    
    echo ""
    echo ""
    echo "3. Testing verify-user endpoint..."
    RESPONSE=$(curl -s http://localhost:8080/api/health/verify-user)
    if [ -z "$RESPONSE" ]; then
        echo "   ❌ No response from verify-user endpoint"
        echo "   The endpoint might not be registered or backend needs restart"
    else
        echo "$RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$RESPONSE"
    fi
    
    echo ""
    echo "4. Testing login endpoint..."
    LOGIN_RESPONSE=$(curl -s -X POST http://localhost:8080/api/auth/login \
      -H "Content-Type: application/json" \
      -d '{"email":"admin@jst.com","password":"Admin123!"}')
    
    echo "$LOGIN_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$LOGIN_RESPONSE"
    
else
    echo "   ❌ Backend is NOT responding on port 8080"
    echo ""
    echo "   Please start the backend:"
    echo "   cd backend/Api && dotnet run"
fi






