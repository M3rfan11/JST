# Cart Integration Manual Test Guide

This document provides a comprehensive guide for manually testing the cart integration with the backend API.

## Prerequisites

1. Backend API should be running on `http://localhost:8081` (or configured URL)
2. Frontend should be running on `http://localhost:5001`
3. Have test user credentials ready (Customer role)

## Test Scenarios

### 1. Unauthenticated User - localStorage Only

**Test Steps:**

1. Open the application in an incognito/private window (or clear localStorage)
2. Navigate to a product page
3. Select a size and add item to cart
4. Verify item appears in cart
5. Check browser DevTools > Application > Local Storage > cart key exists
6. Refresh the page - cart should persist

**Expected Results:**

- ✅ Items are stored in localStorage
- ✅ Cart persists across page refreshes
- ✅ No API calls are made to backend

### 2. Authenticated User - Backend Sync

**Test Steps:**

1. Log in with a Customer account
2. Navigate to a product page
3. Add an item to cart
4. Check browser DevTools > Network tab for API calls:
   - `POST /api/customerorder/cart/add` should be called
   - `GET /api/customerorder/cart` should be called after add
5. Verify item appears in cart
6. Refresh the page - cart should load from backend

**Expected Results:**

- ✅ API calls are made to backend
- ✅ Cart is synced with backend
- ✅ Cart persists after refresh (loaded from backend)

### 3. Add Item to Cart

**Test Steps:**

1. Log in as authenticated user
2. Navigate to product page
3. Select a size
4. Click "Add to Cart"
5. Check Network tab for:
   - `POST /api/customerorder/cart/add` with body: `{ productId: <number>, quantity: 1 }`
   - Response should return cart item details
   - `GET /api/customerorder/cart` should be called to refresh cart

**Expected Results:**

- ✅ Item is added to backend cart
- ✅ Cart UI updates immediately
- ✅ Success toast notification appears
- ✅ Item count in header updates

### 4. Update Item Quantity

**Test Steps:**

1. Go to cart page
2. Click "+" button to increase quantity
3. Check Network tab for:
   - `PUT /api/customerorder/cart/{cartItemId}` with body: `{ quantity: <newQuantity> }`
   - `GET /api/customerorder/cart` should be called after update
4. Click "-" button to decrease quantity
5. Verify quantity updates correctly

**Expected Results:**

- ✅ Quantity updates in backend
- ✅ Cart total recalculates correctly
- ✅ Update is reflected immediately

### 5. Remove Item from Cart

**Test Steps:**

1. Go to cart page
2. Click "X" button to remove an item
3. Check Network tab for:
   - `DELETE /api/customerorder/cart/{cartItemId}`
   - `GET /api/customerorder/cart` should be called after removal
4. Verify item is removed from cart

**Expected Results:**

- ✅ Item is removed from backend
- ✅ Cart UI updates immediately
- ✅ Item count decreases

### 6. Clear Entire Cart

**Test Steps:**

1. Add multiple items to cart
2. Go to cart page
3. Find and click "Clear Cart" button (if available) or remove all items
4. Check Network tab for:
   - `DELETE /api/customerorder/cart/clear`
5. Verify cart is empty

**Expected Results:**

- ✅ All items removed from backend
- ✅ Cart is empty
- ✅ localStorage cart is also cleared

### 7. Authentication State Change

**Test Steps:**

1. Start as unauthenticated user
2. Add items to cart (stored in localStorage)
3. Log in with Customer account
4. Check Network tab for:
   - `GET /api/customerorder/cart` should be called on login
5. Verify cart loads from backend
6. Log out
7. Verify cart falls back to localStorage

**Expected Results:**

- ✅ Cart syncs with backend on login
- ✅ Cart falls back to localStorage on logout
- ✅ No data loss during state transitions

### 8. Network Error Handling

**Test Steps:**

1. Log in as authenticated user
2. Open browser DevTools > Network tab
3. Set network throttling to "Offline"
4. Try to add item to cart
5. Verify error handling:
   - Toast notification about local storage fallback
   - Item is still added to localStorage
6. Set network back to "Online"
7. Refresh page - cart should sync with backend

**Expected Results:**

- ✅ Graceful error handling
- ✅ Fallback to localStorage
- ✅ User is notified of the issue
- ✅ Cart syncs when network is restored

### 9. Backend Error Handling

**Test Steps:**

1. Log in as authenticated user
2. Stop the backend server
3. Try to add item to cart
4. Verify:
   - Error is caught and handled
   - Item is added to localStorage as fallback
   - User sees appropriate notification
5. Start backend server
6. Refresh page - cart should sync

**Expected Results:**

- ✅ Errors are handled gracefully
- ✅ No application crashes
- ✅ User experience is maintained

### 10. Invalid Product ID

**Test Steps:**

1. Log in as authenticated user
2. Try to add item with invalid productId (e.g., 99999)
3. Check Network tab for error response
4. Verify error handling

**Expected Results:**

- ✅ Error is caught
- ✅ User sees error notification
- ✅ Cart state remains consistent

### 11. Concurrent Operations

**Test Steps:**

1. Log in as authenticated user
2. Rapidly click "Add to Cart" multiple times
3. Verify:
   - All requests are handled
   - Cart updates correctly
   - No duplicate items (if backend handles it)

**Expected Results:**

- ✅ Concurrent operations are handled
- ✅ Cart state is consistent
- ✅ No race conditions

### 12. Large Cart Data

**Test Steps:**

1. Add many items to cart (10+ items)
2. Verify:
   - Cart loads correctly
   - Performance is acceptable
   - localStorage doesn't exceed quota

**Expected Results:**

- ✅ Large carts are handled
- ✅ Performance remains good
- ✅ No localStorage quota errors

### 13. Image Optimization

**Test Steps:**

1. Add item with large base64 image (>133KB)
2. Verify:
   - Image is optimized in localStorage
   - Placeholder is used if too large
   - Cart still functions correctly

**Expected Results:**

- ✅ Large images are optimized
- ✅ localStorage quota is preserved
- ✅ Cart functionality is maintained

### 14. Cart Persistence

**Test Steps:**

1. Log in and add items to cart
2. Close browser
3. Reopen browser and navigate to site
4. Log in again
5. Verify cart is loaded from backend

**Expected Results:**

- ✅ Cart persists across sessions
- ✅ Cart loads from backend on login
- ✅ No data loss

## Browser Console Test Script

Run this in the browser console to test cart functionality:

```javascript
// Test Cart Integration
(async function testCartIntegration() {
  console.log("🧪 Starting Cart Integration Tests...\n");

  // Check if cart API is available
  const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8081";
  const token = localStorage.getItem("authToken");

  console.log("1. Checking authentication...");
  console.log("   Token exists:", !!token);
  console.log("   API URL:", API_URL);

  // Test GET cart
  console.log("\n2. Testing GET /api/customerorder/cart...");
  try {
    const response = await fetch(`${API_URL}/api/customerorder/cart`, {
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
    });
    const data = await response.json();
    console.log("   Status:", response.status);
    console.log("   Response:", data);
    console.log("   ✅ GET cart successful");
  } catch (error) {
    console.log("   ❌ GET cart failed:", error);
  }

  // Test ADD item (if you have a product ID)
  console.log("\n3. Testing POST /api/customerorder/cart/add...");
  const testProductId = 1; // Replace with actual product ID
  try {
    const response = await fetch(`${API_URL}/api/customerorder/cart/add`, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        productId: testProductId,
        quantity: 1,
      }),
    });
    const data = await response.json();
    console.log("   Status:", response.status);
    console.log("   Response:", data);
    if (response.ok) {
      console.log("   ✅ ADD item successful");
    } else {
      console.log("   ❌ ADD item failed:", data);
    }
  } catch (error) {
    console.log("   ❌ ADD item error:", error);
  }

  // Test localStorage
  console.log("\n4. Testing localStorage...");
  const localCart = localStorage.getItem("cart");
  console.log(
    "   Cart in localStorage:",
    localCart ? JSON.parse(localCart) : "None"
  );
  console.log("   ✅ localStorage check complete");

  console.log("\n✅ Cart Integration Tests Complete!");
})();
```

## Checklist

- [ ] Unauthenticated user can add items (localStorage)
- [ ] Authenticated user can add items (backend sync)
- [ ] Cart persists across page refreshes
- [ ] Cart syncs with backend on login
- [ ] Cart falls back to localStorage on logout
- [ ] Network errors are handled gracefully
- [ ] Backend errors are handled gracefully
- [ ] Quantity updates work correctly
- [ ] Item removal works correctly
- [ ] Cart clearing works correctly
- [ ] Large cart data is handled
- [ ] Image optimization works
- [ ] Concurrent operations are handled
- [ ] Cart calculations are correct

## Known Limitations

1. **Size/Variant Support**: The backend cart doesn't store variant/size information. The frontend uses size for UI purposes, but backend only stores ProductId and Quantity.

2. **Image Mapping**: When loading cart from backend, product images are fetched separately. If product fetch fails, a placeholder is used.

3. **Offline Support**: When offline, cart operations fall back to localStorage. When back online, manual refresh is needed to sync.

## Troubleshooting

### Cart not syncing with backend

- Check if user is authenticated (check localStorage for `authToken`)
- Check Network tab for API errors
- Verify backend is running and accessible
- Check CORS settings if API calls are blocked

### Items not persisting

- Check browser localStorage quota
- Check for console errors
- Verify API responses are successful

### Performance issues

- Check for large base64 images in cart
- Verify cart isn't too large (>4MB)
- Check network latency to backend
