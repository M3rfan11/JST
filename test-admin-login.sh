#!/bin/bash

echo "🔍 Testing Admin Dashboard Authentication..."
echo ""

# Check if backend is running
echo "1. Checking if backend is running on port 8080..."
if lsof -ti:8080 > /dev/null 2>&1; then
    echo "   ✅ Backend is running on port 8080"
else
    echo "   ❌ Backend is NOT running on port 8080"
    echo "   Please start the backend first: cd backend/Api && dotnet run"
    exit 1
fi

echo ""
echo "2. Testing login endpoint..."
LOGIN_RESPONSE=$(curl -s -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@jst.com","password":"Admin123!"}')

if [ -z "$LOGIN_RESPONSE" ]; then
    echo "   ❌ No response from login endpoint"
    exit 1
fi

echo "$LOGIN_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$LOGIN_RESPONSE"

# Extract token
TOKEN=$(echo "$LOGIN_RESPONSE" | python3 -c "import sys, json; data = json.load(sys.stdin); print(data.get('accessToken', ''))" 2>/dev/null)

if [ -z "$TOKEN" ]; then
    echo ""
    echo "   ❌ Failed to get access token"
    echo "   Response: $LOGIN_RESPONSE"
    exit 1
fi

echo ""
echo "   ✅ Login successful! Token received"
echo "   Token (first 50 chars): ${TOKEN:0:50}..."

echo ""
echo "3. Testing dashboard stats endpoint..."
STATS_RESPONSE=$(curl -s -X GET http://localhost:8080/api/dashboard/stats \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

if echo "$STATS_RESPONSE" | grep -q "totalProducts\|totalUsers\|totalRevenue" 2>/dev/null; then
    echo "   ✅ Dashboard stats endpoint working!"
    echo "$STATS_RESPONSE" | python3 -m json.tool 2>/dev/null | head -20 || echo "$STATS_RESPONSE" | head -20
else
    echo "   ⚠️  Dashboard stats response:"
    echo "$STATS_RESPONSE" | head -10
fi

echo ""
echo "4. Testing recent activity endpoint..."
ACTIVITY_RESPONSE=$(curl -s -X GET http://localhost:8080/api/dashboard/recent-activity \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

if echo "$ACTIVITY_RESPONSE" | grep -q "\[\]\|id\|type" 2>/dev/null; then
    echo "   ✅ Recent activity endpoint working!"
    echo "$ACTIVITY_RESPONSE" | python3 -m json.tool 2>/dev/null | head -15 || echo "$ACTIVITY_RESPONSE" | head -15
else
    echo "   ⚠️  Recent activity response:"
    echo "$ACTIVITY_RESPONSE" | head -10
fi

echo ""
echo "5. Testing low stock items endpoint..."
LOW_STOCK_RESPONSE=$(curl -s -X GET http://localhost:8080/api/dashboard/low-stock-items \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

if echo "$LOW_STOCK_RESPONSE" | grep -q "\[\]\|id\|productName" 2>/dev/null; then
    echo "   ✅ Low stock items endpoint working!"
    echo "$LOW_STOCK_RESPONSE" | python3 -m json.tool 2>/dev/null | head -15 || echo "$LOW_STOCK_RESPONSE" | head -15
else
    echo "   ⚠️  Low stock items response:"
    echo "$LOW_STOCK_RESPONSE" | head -10
fi

echo ""
echo "6. Testing products endpoint..."
PRODUCTS_RESPONSE=$(curl -s -X GET http://localhost:8080/api/products \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

if echo "$PRODUCTS_RESPONSE" | grep -q "\[\]\|id\|name" 2>/dev/null; then
    echo "   ✅ Products endpoint working!"
    PRODUCT_COUNT=$(echo "$PRODUCTS_RESPONSE" | python3 -c "import sys, json; data = json.load(sys.stdin); print(len(data) if isinstance(data, list) else 'N/A')" 2>/dev/null)
    echo "   Found $PRODUCT_COUNT products"
else
    echo "   ⚠️  Products response:"
    echo "$PRODUCTS_RESPONSE" | head -10
fi

echo ""
echo "✅ All tests completed!"
echo ""
echo "📝 Summary:"
echo "   - Backend is running ✅"
echo "   - Login successful ✅"
echo "   - Token received ✅"
echo "   - Dashboard endpoints tested ✅"
echo ""
echo "🎉 You can now access the admin dashboard at: http://localhost:3001/admin"
echo "   Login with: admin@jst.com / Admin123!"










