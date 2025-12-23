# InstaPay Quick Start Guide

## ✅ Setup Complete!

The database migration has been applied successfully. You're ready to test!

## 🚀 Quick Test Steps

### 1. Start Services (if not already running)

**Backend:**
```bash
cd backend/Api
dotnet run
```
Backend should be on: `http://localhost:8081`

**Frontend:**
```bash
npm run dev
```
Frontend should be on: `http://localhost:5001`

### 2. Test the Flow

#### Step A: Create InstaPay Order
1. Go to: `http://localhost:5001/shop`
2. Add products to cart
3. Go to checkout
4. Fill in shipping details
5. **Select "InstaPay" as payment method**
6. Click "Complete Order"
7. ✅ Should redirect to: `/orders/{orderId}/payment`

#### Step B: Upload Payment Proof
1. On payment page, you'll see:
   - QR code placeholder
   - Reference number (your order number)
   - File upload field
2. Upload a test image (JPG/PNG) or PDF
3. Click "Submit Payment Proof"
4. ✅ Should show success message

#### Step C: Admin Review
1. Login as admin: `http://localhost:5001/login`
2. Go to: `http://localhost:5001/admin/instapay`
3. ✅ Should see your order in the list
4. Click "View Proof" to see uploaded file
5. Click "Accept" or "Reject" to process payment

## 📋 Test Checklist

Run through this checklist:

- [ ] Can create order with InstaPay payment method
- [ ] Redirects to payment page after order creation
- [ ] Payment page shows order details
- [ ] Can upload image file (JPG/PNG)
- [ ] Can upload PDF file
- [ ] File upload validation works (rejects invalid types)
- [ ] File upload validation works (rejects > 5MB)
- [ ] Proof submission updates order status
- [ ] Admin can see pending proofs
- [ ] Admin can view proof files
- [ ] Admin can accept payment
- [ ] Admin can reject payment
- [ ] Rejected orders can re-upload proof

## 🔍 Verify Database

Check that the migration was applied:

```bash
cd backend/Api
dotnet ef migrations list
```

You should see: `20251223124642_AddInstaPaySupport`

## 🐛 Troubleshooting

### Backend won't start
- Check if port 8081 is available
- Check database connection
- Look at backend logs

### File upload fails
- Check `backend/Api/wwwroot/uploads/payment-proofs` exists
- Check file permissions
- Check file size (< 5MB)

### Admin page shows no orders
- Verify order status is `PROOF_SUBMITTED`
- Verify payment method is `InstaPay`
- Check admin is logged in

### Proof file not accessible
- Check static files are configured in `Program.cs`
- Verify file exists in `wwwroot/uploads/payment-proofs`
- Check file URL in database

## 📞 Test Endpoints

Use the test script:
```bash
./test-instantpay.sh
```

Or test manually:
```bash
# Get order (replace {orderId} with actual ID)
curl http://localhost:8081/api/instapay/orders/1

# Upload proof (replace {orderId} and file path)
curl -X POST http://localhost:8081/api/instapay/orders/1/proof \
  -F "file=@/path/to/test-image.jpg"
```

## 📚 Full Documentation

See `INSTAPAY_TESTING_GUIDE.md` for comprehensive testing instructions.

## 🎯 Next Steps

1. **Replace QR Code**: Add actual InstaPay QR code image
2. **Update WhatsApp**: Change placeholder number to real support number
3. **Test End-to-End**: Complete full flow from order to payment confirmation
4. **Add Notifications**: (Optional) Email/SMS notifications for status changes

---

**Ready to test!** 🚀

