# InstaPay Integration Testing Guide

## Prerequisites

1. ✅ Database migration completed
2. ✅ Backend server running on port 8080/8081
3. ✅ Frontend server running on port 5001
4. ✅ Admin user account available

## Database Migration Status

The migration `AddInstaPaySupport` has been created. The database will be automatically updated when you restart the backend server.

## Testing Steps

### Step 1: Test Order Creation with InstaPay

1. **Navigate to Shop**
   - Go to `http://localhost:5001/shop`
   - Add some products to cart

2. **Go to Checkout**
   - Click on cart icon
   - Click "Checkout" button
   - Fill in shipping information:
     - Email
     - First Name, Last Name
     - Address, City, State, Country, ZIP
     - Phone number

3. **Select InstaPay Payment Method**
   - In "Payment Information" section
   - Select "InstaPay" from dropdown
   - Verify that credit card fields are NOT shown (only for Credit Card)

4. **Place Order**
   - Click "Complete Order" button
   - **Expected Result**: 
     - Order should be created
     - You should be redirected to `/orders/{orderId}/payment` page
     - Order status should be `PENDING_PAYMENT`

### Step 2: Test Payment Pending Page

1. **Verify Page Elements**
   - ✅ QR code placeholder is displayed
   - ✅ Reference number (Order Number) is shown
   - ✅ Payment instructions are visible
   - ✅ Upload file input is available
   - ✅ "Contact us on WhatsApp" button is present

2. **Test File Upload**
   - Click "Choose File" or drag a file
   - Try uploading:
     - ✅ Valid image (JPG, PNG) - should work
     - ✅ Valid PDF - should work
     - ❌ Invalid file type (e.g., .txt) - should show error
     - ❌ File > 5MB - should show error
   
3. **Submit Payment Proof**
   - Select a valid image or PDF file
   - Click "Submit Payment Proof"
   - **Expected Result**:
     - Success message: "Proof submitted successfully!"
     - Page shows "Proof Submitted Successfully" section
     - Order status changes to `PROOF_SUBMITTED`

### Step 3: Test Admin Review Interface

1. **Login as Admin**
   - Go to `http://localhost:5001/login`
   - Login with admin credentials

2. **Navigate to InstaPay Review**
   - Go to `http://localhost:5001/admin/instapay`
   - **Expected Result**:
     - List of orders with `PROOF_SUBMITTED` or `UNDER_REVIEW` status
     - Each order shows:
       - Order number
       - Customer name, phone, email
       - Total amount
       - Status
       - "View Proof" button
       - "Accept" and "Reject" buttons

3. **View Payment Proof**
   - Click "View Proof" button on an order
   - **Expected Result**:
     - Proof file opens in new tab/window
     - File should be accessible from backend

4. **Accept Payment**
   - Click "Accept" button
   - Optionally add admin note
   - Click "Accept Payment"
   - **Expected Result**:
     - Success message
     - Order status changes to `ACCEPTED`
     - Payment status changes to `Paid`
     - Order disappears from pending list

5. **Reject Payment** (Test with another order)
   - Click "Reject" button
   - Enter rejection reason (required)
   - Optionally add admin note
   - Click "Reject Payment"
   - **Expected Result**:
     - Success message
     - Order status changes to `REJECTED`
     - Order disappears from pending list
     - Customer can re-upload proof

### Step 4: Test Re-upload After Rejection

1. **Customer Re-uploads Proof**
   - Go back to payment page: `/orders/{orderId}/payment`
   - Upload a new proof file
   - **Expected Result**:
     - New proof can be uploaded
     - Status changes back to `PROOF_SUBMITTED`
     - Order appears in admin review list again

### Step 5: Test Order Tracking

1. **Track Order**
   - Go to `http://localhost:5001/track`
   - Enter order number and email
   - **Expected Result**:
     - Order details displayed
     - Status shows current InstaPay status
     - Tracking history shows status changes

## API Endpoint Testing

### Test InstaPay Endpoints Directly

1. **Get Order Details** (Anonymous)
   ```bash
   curl http://localhost:8081/api/instapay/orders/{orderId}
   ```

2. **Upload Proof** (Anonymous)
   ```bash
   curl -X POST http://localhost:8081/api/instapay/orders/{orderId}/proof \
     -F "file=@/path/to/proof.jpg"
   ```

3. **Get Pending Proofs** (Requires Auth)
   ```bash
   curl http://localhost:8081/api/instapay/admin/pending-proofs \
     -H "Authorization: Bearer {token}"
   ```

4. **Accept Payment** (Requires Auth)
   ```bash
   curl -X POST http://localhost:8081/api/instapay/admin/orders/{orderId}/accept \
     -H "Authorization: Bearer {token}" \
     -H "Content-Type: application/json" \
     -d '{"adminNote": "Payment verified"}'
   ```

5. **Reject Payment** (Requires Auth)
   ```bash
   curl -X POST http://localhost:8081/api/instapay/admin/orders/{orderId}/reject \
     -H "Authorization: Bearer {token}" \
     -H "Content-Type: application/json" \
     -d '{"rejectionReason": "Wrong amount", "adminNote": "Please check amount"}'
   ```

## Status Flow Verification

Verify the order status transitions:

1. **Order Created** → `PENDING_PAYMENT`
2. **Proof Uploaded** → `PROOF_SUBMITTED`
3. **Admin Accepts** → `ACCEPTED` (PaymentStatus: `Paid`)
4. **Admin Rejects** → `REJECTED`
5. **Re-upload After Rejection** → `PROOF_SUBMITTED` (again)

## Common Issues & Solutions

### Issue: File upload fails
- **Check**: Backend `wwwroot/uploads/payment-proofs` directory exists
- **Check**: File size is under 5MB
- **Check**: File type is image (JPG/PNG) or PDF

### Issue: Proof file not accessible
- **Check**: Static files are configured in `Program.cs`
- **Check**: File is saved in `wwwroot/uploads/payment-proofs`
- **Check**: Backend is serving static files correctly

### Issue: Order not showing in admin panel
- **Check**: Order status is `PROOF_SUBMITTED` or `UNDER_REVIEW`
- **Check**: Payment method is `InstaPay`
- **Check**: Admin is logged in

### Issue: Cannot access payment page
- **Check**: Order ID is correct
- **Check**: Order payment method is InstaPay
- **Check**: Order exists in database

## Test Checklist

- [ ] Order creation with InstaPay redirects to payment page
- [ ] Payment page displays QR code and reference number
- [ ] File upload accepts valid image files
- [ ] File upload accepts valid PDF files
- [ ] File upload rejects invalid file types
- [ ] File upload rejects files > 5MB
- [ ] Proof submission updates order status to PROOF_SUBMITTED
- [ ] Admin can see pending proofs in review page
- [ ] Admin can view proof files
- [ ] Admin can accept payment
- [ ] Admin can reject payment with reason
- [ ] Rejected orders can have proof re-uploaded
- [ ] Order tracking shows correct status
- [ ] WhatsApp contact button works (opens WhatsApp)

## Next Steps After Testing

1. **Replace QR Code Placeholder**
   - Add actual InstaPay QR code image
   - Update payment pending page to display it

2. **Update WhatsApp Number**
   - Change placeholder number in payment pending page
   - Update to actual support WhatsApp number

3. **Configure InstaPay Details**
   - Add actual InstaPay account details
   - Configure QR code generation if needed

4. **Add Email Notifications** (Optional)
   - Notify customer when proof is accepted/rejected
   - Notify admin when new proof is submitted

## Notes

- The QR code is currently a placeholder - replace with actual InstaPay QR code
- WhatsApp number is placeholder - update to actual support number
- File uploads are stored in `backend/Api/wwwroot/uploads/payment-proofs/`
- Make sure `wwwroot` directory exists in backend project

