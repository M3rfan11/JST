# InstaPay QR Code Management Guide

## Overview
Admins can now upload and manage the InstaPay QR code that appears on the payment page. The QR code is stored on the server and displayed to customers during checkout.

## How to Upload QR Code

### Step 1: Access Admin Settings
1. Login as admin: `http://localhost:5001/login`
2. Navigate to: `http://localhost:5001/admin/settings`
3. Scroll down to "InstaPay QR Code" section

### Step 2: Upload QR Code
1. Click "Choose File" button
2. Select your InstaPay QR code image (JPG, PNG, GIF, or WEBP)
3. File must be under 2MB
4. Click "Upload QR Code" button
5. ✅ QR code will be saved and displayed on payment pages

### Step 3: Replace QR Code
- To replace existing QR code, simply upload a new one
- The old QR code will be automatically deleted

### Step 4: Delete QR Code
- Click "Delete" button next to current QR code
- Confirm deletion
- Payment pages will show placeholder until new QR code is uploaded

## Where QR Code Appears

The QR code is displayed on:
- **Payment Pending Page**: `/orders/{orderId}/payment`
- Shown alongside the reference number (order number)
- Customers scan this to complete InstaPay payment

## File Storage

- **Location**: `backend/Api/wwwroot/uploads/instapay-qr/`
- **URL Format**: `/uploads/instapay-qr/instapay-qr-{timestamp}.{ext}`
- **Database**: File path stored in Settings table with key `InstaPayQrCode`

## API Endpoints

### Get QR Code (Public)
```
GET /api/settings/instapay-qr
```
Returns: `{ qrCodeUrl: string | null }`

### Upload QR Code (Admin Only)
```
POST /api/settings/instapay-qr
Content-Type: multipart/form-data
Body: file (image file)
```
Requires: Admin authentication

### Delete QR Code (Admin Only)
```
DELETE /api/settings/instapay-qr
```
Requires: Admin authentication

## Technical Details

- **File Types**: JPG, PNG, GIF, WEBP
- **Max Size**: 2MB
- **Storage**: Files stored in `wwwroot/uploads/instapay-qr/`
- **Static Serving**: Files served via static file middleware
- **Database**: File URL stored in Settings table

## Troubleshooting

### QR Code not showing on payment page
- Check if QR code is uploaded in admin settings
- Verify file exists in `wwwroot/uploads/instapay-qr/`
- Check browser console for errors
- Verify static file serving is configured

### Upload fails
- Check file size (must be < 2MB)
- Check file type (must be image)
- Verify admin is logged in
- Check backend logs for errors

### QR Code not updating
- Clear browser cache
- Verify new file was uploaded
- Check file path in database Settings table

## Next Steps

1. **Upload Your QR Code**: Go to admin settings and upload your InstaPay QR code
2. **Test Payment Flow**: Create a test order with InstaPay and verify QR code displays
3. **Verify Scanning**: Test that the QR code can be scanned by InstaPay app

---

**Note**: The QR code is optional. If no QR code is uploaded, a placeholder will be shown on the payment page.

