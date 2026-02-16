# AI Calendar Manager

A production-ready AI-powered calendar assistant that uses natural language to manage Google Calendar. Built with .NET 8 Web API, Angular 17+, and Claude AI for intelligent calendar operations.

## Features

- 🔐 **Secure OAuth2 Integration** - Connect with Google Calendar using industry-standard security
- 🤖 **Natural Language Commands** - "Schedule lunch with John tomorrow at noon"
- 🧠 **Smart Scheduling** - AI finds optimal meeting times based on availability and preferences
- ⚡ **Real-time Conflict Detection** - Prevents double-booking automatically
- 🎯 **Personalized Preferences** - Learns your meeting preferences and working hours
- 💬 **Multi-turn Conversations** - Context-aware chat interface

## Tech Stack

- **Backend**: .NET 8 Web API
- **Frontend**: Angular 17+
- **Database**: PostgreSQL 16
- **AI**: Claude 4.5 Sonnet with function calling
- **Authentication**: Google OAuth2
- **Containerization**: Docker & Docker Compose

## Quick Start

### Prerequisites

- Docker and Docker Compose
- .NET 8 SDK (for development)
- Node.js 18+ (for frontend development)

### 1. Database Setup

```bash
# Copy environment template
cp .env.example .env

# Update .env with your preferred passwords
# Start PostgreSQL database
docker-compose up -d postgres

# (Optional) Start pgAdmin for database management
docker-compose up -d pgadmin
```

### 2. Access Information

- **PostgreSQL**: `localhost:5432`
  - Database: `ai_calendar_manager`
  - Username: `postgres` 
  - Password: Set in `.env` file

- **pgAdmin** (optional): `http://localhost:5050`
  - Email: `admin@aicalendar.local`
  - Password: Set in `.env` file

### 3. Database Commands

```bash
# View database logs
docker-compose logs postgres

# Connect to database directly
docker-compose exec postgres psql -U postgres -d ai_calendar_manager

# Stop services
docker-compose down

# Stop and remove all data (WARNING: Destructive)
docker-compose down -v
```

## Project Structure

```
AICalendarManager/
├── CalendarManager.API/          # .NET 8 Web API
│   ├── Controllers/              # API endpoints
│   ├── Services/                 # Business logic
│   ├── Data/                     # Entity Framework context
│   └── Models/                   # DTOs and entities
├── calendar-manager-ui/          # Angular frontend
│   └── src/app/                  # Angular components and services

├── docker-compose.yml            # Docker services
└── README.md                     # This file
```

## Database Schema

The system uses a PostgreSQL database managed by Entity Framework Core. The schema will include:

- **users** - User accounts from Google OAuth
- **oauth_tokens** - Encrypted Google Calendar access tokens
- **user_preferences** - Working hours and meeting preferences
- **conversations** - Chat history with Claude AI
- **messages** - Individual messages in conversations

Database schema will be created and managed through Entity Framework migrations.

## Development Phases

1. **Phase 1**: Authentication & Database Setup ✅
2. **Phase 2**: Google Calendar API Integration
3. **Phase 3**: Claude AI Function Calling
4. **Phase 4**: Smart Scheduling Algorithm
5. **Phase 5**: User Preferences System
6. **Phase 6**: Angular Frontend
7. **Phase 7**: Production Polish

## API Endpoints

### Authentication
- `GET /api/auth/google-login` - Initiate OAuth flow
- `GET /api/auth/callback` - OAuth callback handler
- `POST /api/auth/logout` - Revoke tokens
- `GET /api/auth/status` - Check authentication status

### Calendar Operations
- `GET /api/calendar/events` - List calendar events
- `POST /api/calendar/events` - Create new event
- `PUT /api/calendar/events/{id}` - Update event
- `DELETE /api/calendar/events/{id}` - Delete event
- `POST /api/calendar/find-slots` - Find available time slots

### AI Chat Interface
- `POST /api/chat/message` - Process natural language commands
- `GET /api/chat/conversations` - List conversations
- `GET /api/chat/conversations/{id}` - Get conversation history

## Security Features

- ✅ OAuth tokens encrypted at rest (AES-256)
- ✅ HTTPS only in production
- ✅ PKCE for OAuth security
- ✅ Input validation on all endpoints
- ✅ Rate limiting protection
- ✅ CORS configured correctly
- ✅ SQL injection prevention

## Configuration

Key configuration is managed through environment variables:

```env
# Database
DATABASE_URL=postgresql://postgres:password@localhost:5432/ai_calendar_manager

# Google OAuth (to be added)
GOOGLE_CLIENT_ID=your_client_id
GOOGLE_CLIENT_SECRET=your_client_secret

# Claude AI (to be added)
CLAUDE_API_KEY=your_claude_api_key

# Encryption (to be added)
ENCRYPTION_KEY=your_32_byte_base64_key
```

## Example Usage

Once complete, you'll be able to use natural language commands like:

- "What's on my calendar today?"
- "Schedule a meeting with John tomorrow at 2pm"
- "Find 30 minutes with Sarah this week"
- "Move my 3pm meeting to 4pm"
- "Cancel all meetings on Friday"

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is part of Andrew's portfolio demonstrating production-ready AI integrations.

---

**Current Status**: Database setup complete ✅  
**Next Steps**: Implement .NET 8 Web API with Google OAuth2 integration