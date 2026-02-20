#!/bin/bash

# Load environment variables from .env file
set -a
source .env
set +a

# Navigate to API directory and run
cd CalendarManager.API
echo "🚀 Starting backend with environment variables..."
echo "🔗 Database URL: ${DATABASE_URL}"
echo "🔐 Claude API Key: ${CLAUDE_API_KEY:0:20}..."
echo "📱 Frontend URL: ${FRONTEND_URL}"

dotnet run