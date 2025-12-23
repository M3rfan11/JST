#!/bin/bash

# InstaPay Integration Test Script
# This script helps test the InstaPay endpoints

API_URL="${NEXT_PUBLIC_API_URL:-http://localhost:8081}"
BASE_URL="${API_URL}/api/instapay"

echo "🧪 InstaPay Integration Test Script"
echo "===================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if backend is running
echo "📡 Checking backend connection..."
if curl -s "${API_URL}/api/health" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Backend is running${NC}"
else
    echo -e "${RED}❌ Backend is not running. Please start the backend first.${NC}"
    echo "   Run: cd backend/Api && dotnet run"
    exit 1
fi

echo ""
echo "📋 Available Test Commands:"
echo "=========================="
echo ""
echo "1. Get Order Details (Anonymous):"
echo "   curl ${BASE_URL}/orders/{orderId}"
echo ""
echo "2. Upload Payment Proof (Anonymous):"
echo "   curl -X POST ${BASE_URL}/orders/{orderId}/proof -F 'file=@/path/to/proof.jpg'"
echo ""
echo "3. Get Pending Proofs (Requires Auth):"
echo "   curl ${BASE_URL}/admin/pending-proofs -H 'Authorization: Bearer {token}'"
echo ""
echo "4. Accept Payment (Requires Auth):"
echo "   curl -X POST ${BASE_URL}/admin/orders/{orderId}/accept \\"
echo "     -H 'Authorization: Bearer {token}' \\"
echo "     -H 'Content-Type: application/json' \\"
echo "     -d '{\"adminNote\": \"Payment verified\"}'"
echo ""
echo "5. Reject Payment (Requires Auth):"
echo "   curl -X POST ${BASE_URL}/admin/orders/{orderId}/reject \\"
echo "     -H 'Authorization: Bearer {token}' \\"
echo "     -H 'Content-Type: application/json' \\"
echo "     -d '{\"rejectionReason\": \"Wrong amount\", \"adminNote\": \"Please check\"}'"
echo ""
echo "🌐 Frontend URLs:"
echo "=================="
echo "• Shop: http://localhost:5001/shop"
echo "• Checkout: http://localhost:5001/checkout"
echo "• Payment Page: http://localhost:5001/orders/{orderId}/payment"
echo "• Admin Review: http://localhost:5001/admin/instapay"
echo "• Track Order: http://localhost:5001/track"
echo ""
echo "📝 Testing Steps:"
echo "================="
echo "1. Add products to cart"
echo "2. Go to checkout and select InstaPay"
echo "3. Place order (should redirect to payment page)"
echo "4. Upload payment proof"
echo "5. Login as admin and review at /admin/instapay"
echo "6. Accept or reject payment"
echo ""
echo "For detailed testing guide, see: INSTAPAY_TESTING_GUIDE.md"

