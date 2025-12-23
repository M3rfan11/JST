#!/bin/bash

# Script to start the Next.js frontend

echo "🌐 Starting Frontend on port 3001..."
echo ""

cd "$(dirname "$0")" || exit 1

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
    echo "📦 Installing dependencies..."
    npm install
    echo ""
fi

# Kill any existing Next.js processes
pkill -f "next dev" 2>/dev/null
lsof -ti:3001 | xargs kill -9 2>/dev/null
sleep 1

echo "🚀 Starting Next.js development server..."
echo "Frontend will be available at: http://localhost:3001"
echo ""

npm run dev










