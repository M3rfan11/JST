# .NET Backend Integration Guide

## Step 1: Copy Your .NET Backend

Run this command to copy your .NET backend folder to this project:

```bash
# Replace /path/to/your/dotnet/backend with your actual backend path
cp -r /path/to/your/dotnet/backend /Users/osz/Desktop/Whizz/jst/backend
```

Or if you prefer to keep it in a sibling directory:

```bash
# Copy to a sibling directory
cp -r /path/to/your/dotnet/backend /Users/osz/Desktop/Whizz/backend
```

## Step 2: Configure .NET Backend CORS

Your .NET backend needs to allow requests from your Next.js frontend. Update your `Program.cs` or `Startup.cs`:

### For .NET 6+ (Program.cs):

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJS", policy =>
    {
        policy.WithOrigins("http://localhost:3001") // Your Next.js dev server
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseCors("AllowNextJS");
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### For .NET Core 3.1/5 (Startup.cs):

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();
    services.AddCors(options =>
    {
        options.AddPolicy("AllowNextJS", policy =>
        {
            policy.WithOrigins("http://localhost:3001")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseCors("AllowNextJS");
    app.UseRouting();
    app.UseAuthorization();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

### For Production:

```csharp
// Allow your production domain
policy.WithOrigins("https://yourdomain.com", "http://localhost:3001")
```

## Step 3: Configure .NET Backend Port

Update your `appsettings.json` or `launchSettings.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

Or in `launchSettings.json`:

```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5000"
    }
  }
}
```

## Step 4: Set Up Frontend Environment Variables

1. **Create `.env.local` file** in the project root:

```bash
cp .env.local.example .env.local
```

2. **Update `.env.local`** with your .NET backend URL:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000
```

If your .NET API has a base path (e.g., `/api`), include it:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

## Step 5: Update Frontend Code to Use API

The API client is already set up in `lib/api-client.ts`. You can now replace localStorage calls with API calls.

### Example: Update Checkout Form

**Before (localStorage):**
```typescript
const existingOrders = JSON.parse(localStorage.getItem("orders") || "[]")
localStorage.setItem("orders", JSON.stringify([order, ...existingOrders]))
```

**After (API):**
```typescript
import { api } from '@/lib/api-client'

const order = await api.orders.create({
  total,
  status: "processing",
  shipping: formData,
  userId: user?.id || null,
})
```

### Example: Fetch Products

**Before:**
```typescript
// Products hardcoded or from localStorage
```

**After:**
```typescript
import { api } from '@/lib/api-client'

const products = await api.products.getAll()
```

## Step 6: Run Both Servers

### Option A: Run Separately

**Terminal 1 - .NET Backend:**
```bash
cd backend
dotnet run
```

**Terminal 2 - Next.js Frontend:**
```bash
npm run dev
```

### Option B: Use a Process Manager (Optional)

Install `concurrently`:
```bash
npm install --save-dev concurrently
```

Update `package.json`:
```json
{
  "scripts": {
    "dev": "next dev -p 3001",
    "dev:backend": "cd backend && dotnet run",
    "dev:all": "concurrently \"npm run dev\" \"npm run dev:backend\""
  }
}
```

Then run:
```bash
npm run dev:all
```

## Step 7: Test the Integration

1. Start your .NET backend
2. Start your Next.js frontend
3. Open browser console and check for CORS errors
4. Test API calls from the frontend

## Common Issues & Solutions

### CORS Errors

**Problem:** `Access-Control-Allow-Origin` error

**Solution:** 
- Ensure CORS is configured in your .NET backend
- Check that the origin URL matches exactly (including port)
- Verify `AllowCredentials()` is set if using cookies/auth

### Connection Refused

**Problem:** Can't connect to backend

**Solution:**
- Verify .NET backend is running on the correct port
- Check `.env.local` has the correct `NEXT_PUBLIC_API_URL`
- Ensure no firewall is blocking the connection

### 404 Not Found

**Problem:** API endpoint not found

**Solution:**
- Check your .NET API route paths match what's in `api-client.ts`
- Verify the base path in `.env.local` (e.g., `/api` if your routes are `/api/products`)
- Check .NET routing configuration

## API Client Usage Examples

```typescript
import { api } from '@/lib/api-client'

// Get all products
const products = await api.products.getAll()

// Get single product
const product = await api.products.getById('123')

// Create order
const order = await api.orders.create({
  items: cartItems,
  total: 199.99,
  shipping: address
})

// Track order
const tracking = await api.orders.track('order-123')

// Login
const response = await api.auth.login('user@example.com', 'password')
localStorage.setItem('authToken', response.token)

// Get user profile
const profile = await api.users.getProfile()
```

## Next Steps

1. Replace all `localStorage` calls with API calls
2. Update authentication to use JWT tokens from .NET backend
3. Implement proper error handling
4. Add loading states for API calls
5. Set up API response types/interfaces

## Need Help?

If you encounter issues:
1. Check browser console for errors
2. Check .NET backend logs
3. Verify CORS configuration
4. Test API endpoints directly (using Postman or curl)











