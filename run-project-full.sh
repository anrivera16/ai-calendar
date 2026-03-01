#!/bin/bash

set -a
source .env
set +a

PROJECT_ROOT=$(pwd)
BACKEND_PID=""
FRONTEND_PID=""

cleanup() {
    echo ""
    echo "🛑 Stopping services..."
    [ -n "$BACKEND_PID" ] && kill $BACKEND_PID 2>/dev/null
    [ -n "$FRONTEND_PID" ] && kill $FRONTEND_PID 2>/dev/null
    echo "✅ All services stopped"
    exit 0
}

trap cleanup SIGINT SIGTERM

echo "🚀 Starting AI Calendar Manager..."
echo "─────────────────────────────────"
echo "🔗 Database URL: ${DATABASE_URL}"
echo "🔐 Claude API Key: ${CLAUDE_API_KEY:0:20}..."
echo "📱 Frontend URL: ${FRONTEND_URL}"
echo "─────────────────────────────────"

echo "🔧 Starting backend..."
cd "$PROJECT_ROOT/CalendarManager.API"
dotnet run &
BACKEND_PID=$!
cd "$PROJECT_ROOT"

echo "🎨 Starting frontend..."
cd "$PROJECT_ROOT/calendar-manager-ui"
ng serve &
FRONTEND_PID=$!
cd "$PROJECT_ROOT"

echo ""
echo "✅ All services starting..."
echo "   Backend:  http://localhost:5047"
echo "   Frontend: http://localhost:4200"
echo "   Swagger:  http://localhost:5047/swagger"
echo ""
echo "Press Ctrl+C to stop all services"

wait
