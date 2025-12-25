# Backend Restart Instructions

The verify-user endpoint needs the backend to be restarted to be available.

## Steps to Restart Backend:

1. **Stop the current backend** (if running):
   ```bash
   pkill -f "dotnet.*Api"
   ```

2. **Navigate to backend directory**:
   ```bash
   cd backend/Api
   ```

3. **Build the project** (to check for errors):
   ```bash
   dotnet build
   ```

4. **Run the backend**:
   ```bash
   dotnet run
   ```

5. **Wait for startup messages** - You should see:
   - "Running database migrations..."
   - "Checking seed data..."
   - "Database seeding completed successfully!"
   - "Now listening on: http://localhost:8080"

## Test the Endpoints:

1. **Health Check**:
   - Open: http://localhost:8080/api/health
   - Should return: `{"status":"healthy",...}`

2. **Verify User** (NEW):
   - Open: http://localhost:8080/api/health/verify-user
   - This will check if admin@jst.com exists and create it if missing
   - Also tests password verification

3. **Swagger UI** (to see all endpoints):
   - Open: http://localhost:8080/swagger
   - You should see the verify-user endpoint under HealthController

4. **Test Login**:
   - Use Swagger UI or:
   ```bash
   curl -X POST http://localhost:8080/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@jst.com","password":"Admin123!"}'
   ```

## If verify-user endpoint still doesn't work:

1. Check Swagger UI to see if the endpoint is listed
2. Check backend console for any errors
3. Make sure you restarted the backend after adding the endpoint
4. Check the browser console for CORS errors











