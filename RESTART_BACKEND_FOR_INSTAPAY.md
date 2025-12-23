# ⚠️ Backend Restart Required for InstaPay

## Issue
The InstaPay endpoints are returning 404 because the backend was started before the new `InstaPayController` was created.

## Solution
**Restart the backend server** to load the new controller.

### Steps to Restart Backend:

1. **Stop the current backend process:**
   ```bash
   # Find the process
   ps aux | grep "dotnet run"
   
   # Kill it (replace PID with actual process ID)
   kill <PID>
   
   # Or use the script
   pkill -f "dotnet.*Api"
   ```

2. **Restart the backend:**
   ```bash
   cd backend/Api
   dotnet run
   ```
   
   Or use the start script:
   ```bash
   ./start-backend.sh
   ```

3. **Verify the endpoint is available:**
   ```bash
   curl http://localhost:8081/api/instapay/orders/1
   ```
   
   Should return order data or "Order not found" (not 404).

## Temporary Workaround

I've added a fallback in the payment page that will try to use the customer order endpoint if the InstaPay endpoint isn't available. However, **you still need to restart the backend** for full functionality (especially for proof upload and admin endpoints).

## After Restart

Once the backend is restarted:
- ✅ InstaPay endpoints will be available
- ✅ Payment proof upload will work
- ✅ Admin review page will work
- ✅ All InstaPay features will function correctly

---

**Note:** The frontend has a temporary fallback, but please restart the backend for complete functionality.

