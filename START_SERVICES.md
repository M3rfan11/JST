# How to Start Backend and Frontend

## ✅ Configuration Summary

- **Backend**: Port **8081** (configured in appsettings.json and Program.cs)
- **Frontend**: Port **3001** (configured in package.json)

## 🚀 Quick Start

### Option 1: Use Startup Scripts (Recommended)

**Start Backend:**
```bash
cd /Users/osz/Desktop/Whizz/jst/backend/Api
dotnet run
```
The backend will automatically use port **8081** from appsettings.json.

**Start Frontend:**
```bash
cd /Users/osz/Desktop/Whizz/jst
./start-frontend.sh
```
Or manually:
```bash
npm run dev
```

### Option 2: Manual Start

**Backend:**
```bash
cd backend/Api
dotnet run
```
✅ Backend will run on **http://localhost:8081**

**Frontend:**
```bash
npm run dev
```
✅ Frontend will run on **http://localhost:3001**

## 🔍 Verify Services Are Running

**Check Backend:**
```bash
curl http://localhost:8081/api/health
```

**Check Frontend:**
```bash
curl http://localhost:3001
```

**Check Ports:**
```bash
lsof -i:8081  # Backend
lsof -i:3001  # Frontend
```

## 📝 Important Notes

1. **Backend Port**: The backend is explicitly configured to use port **8081** in:
   - `appsettings.json` → `"Urls": "http://localhost:8081"`
   - `appsettings.Development.json` → `"Urls": "http://localhost:8081"`
   - `Program.cs` → Explicitly reads from configuration

2. **Frontend Port**: The frontend is configured in:
   - `package.json` → `"dev": "next dev -p 3001"`
   - `lib/api-client.ts` → Connects to backend on port 8081

3. **First Time Setup**: If you haven't installed frontend dependencies:
   ```bash
   npm install
   ```

## 🎯 Expected Output

**Backend:**
```
Now listening on: http://localhost:8081
Database seeding completed successfully!
```

**Frontend:**
```
Ready on http://localhost:3001
```

## 🔧 Troubleshooting

**Backend won't start:**
- Check if port 8081 is in use: `lsof -i:8081`
- Kill any existing processes: `lsof -ti:8081 | xargs kill -9`

**Frontend won't start:**
- Install dependencies: `npm install`
- Check if port 3001 is in use: `lsof -i:3001`
- Kill any existing processes: `lsof -ti:3001 | xargs kill -9`










