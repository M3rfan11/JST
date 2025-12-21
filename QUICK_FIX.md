# Quick Fix for Login Issue

## The Problem:
The `verify-user` endpoint needs the backend to be restarted to be available. But we can test login directly.

## Quick Solution:

### Option 1: Test Login Directly (Easiest)

1. **Make sure backend is running**:
   ```bash
   cd backend/Api
   dotnet run
   ```

2. **Test login in Swagger UI**:
   - Go to: http://localhost:8080/swagger
   - Find: `POST /api/auth/login`
   - Click "Try it out"
   - Enter:
     ```json
     {
       "email": "admin@jst.com",
       "password": "Admin123!"
     }
     ```
   - Click "Execute"

3. **If login fails**, the user doesn't exist. We need to:
   - Delete the database file to force re-seeding
   - Or manually create the user

### Option 2: Force Re-seed (If login fails)

1. **Stop the backend** (Ctrl+C)

2. **Delete the database**:
   ```bash
   cd backend/Api
   rm jst.db jst_dev.db 2>/dev/null
   ```

3. **Restart the backend**:
   ```bash
   dotnet run
   ```

4. **Watch the console** for:
   - "Running database migrations..."
   - "Checking seed data..."
   - "Database seeding completed successfully!"

5. **Test login again** in Swagger UI

### Option 3: Check if User Exists (After restart)

Once backend is restarted with the new endpoint:

1. **Open**: http://localhost:8080/api/health/verify-user
2. This will show if user exists and create it if missing

## Expected Result:

After successful login, you should get:
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "...",
  "user": {
    "id": 1,
    "fullName": "Super Administrator",
    "email": "admin@jst.com",
    "roles": ["SuperAdmin"]
  }
}
```

Then you can use the `accessToken` to access the admin dashboard!






