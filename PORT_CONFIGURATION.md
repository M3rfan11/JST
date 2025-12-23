# Port Configuration Verification

## ✅ Configuration Status

### Backend (Port 8081) - CONFIGURED ✅

1. **appsettings.json** ✅
   - Line 27: `"Urls": "http://localhost:8081"`

2. **appsettings.Development.json** ✅
   - Line 11: `"Urls": "http://localhost:8081"`

3. **Properties/launchSettings.json** ✅
   - Line 8: `"applicationUrl": "http://localhost:8081"`
   - Line 17: `"applicationUrl": "https://localhost:7261;http://localhost:8081"`

### Frontend (Port 3001) - CONFIGURED ✅

1. **package.json** ✅
   - Line 6: `"dev": "next dev -p 3001"`

2. **lib/api-client.ts** ✅
   - Line 8: `const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8081'`

## 🚀 How to Start Services

### Start Backend:
```bash
cd backend/Api
dotnet run
```
**Expected output:** `Now listening on: http://localhost:8081`

### Start Frontend:
```bash
npm run dev
```
**Expected output:** `Ready on http://localhost:3001`

## 🔍 Verification Commands

### Check if Backend is Running:
```bash
curl http://localhost:8081/api/health
```

### Check if Frontend is Running:
```bash
curl http://localhost:3001
```

### Check Port Usage:
```bash
lsof -i:8081  # Backend
lsof -i:3001  # Frontend
```

## 📝 Summary

- ✅ Backend configured to run on **port 8081**
- ✅ Frontend configured to run on **port 3001**
- ✅ Frontend API client configured to connect to backend on **port 8081**

All configuration files are correctly set up!










