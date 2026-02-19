# 🗓️ AI Calendar Manager

A production-ready AI-powered calendar assistant that uses Claude AI and natural language processing to intelligently manage your Google Calendar. Built with .NET 8 Web API, Angular 17+, and Claude 3.5 Sonnet for sophisticated calendar operations.

## ✨ Features

- 🤖 **Claude AI Integration** - Advanced natural language understanding for complex scheduling requests
- 🔐 **Secure Google OAuth** - Industry-standard authentication with encrypted token storage
- 💬 **Conversational Interface** - Multi-turn conversations with context awareness
- 📅 **Smart Calendar Operations** - Create, view, and manage events through natural language
- ⚡ **Real-time Processing** - Instant responses with proper error handling
- 🔒 **Enterprise Security** - PKCE OAuth flow, encrypted tokens, input validation
- 🎯 **Intelligent Parsing** - Understands complex time expressions and scheduling preferences

## 🏗️ Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Angular 17+   │◄──►│  .NET 8 Web API  │◄──►│  Google Calendar│
│   Frontend      │    │     Backend      │    │      API        │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                       │                       
         │                       ▼                       
         │              ┌──────────────────┐             
         │              │   Claude 3.5     │             
         └──────────────│   Sonnet API     │             
                        └──────────────────┘             
                                 │                        
                                 ▼                        
                        ┌──────────────────┐             
                        │   PostgreSQL     │             
                        │    Database      │             
                        └──────────────────┘             
```

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- PostgreSQL (or Docker)
- Google Cloud Console account
- Claude API key (Anthropic)

### 1. Clone and Setup Environment

```bash
git clone <repository-url>
cd ai-calendar

# Setup environment variables
cp .env.example .env
# Edit .env with your API keys and database credentials
```

### 2. Database Setup

```bash
# Option A: Docker (Recommended)
docker-compose up postgres -d

# Option B: Local PostgreSQL
# Make sure PostgreSQL is running on localhost:5432
```

### 3. Backend Setup

```bash
cd CalendarManager.API

# Restore dependencies
dotnet restore

# Set environment variables (replace with your keys)
export GOOGLE_CLIENT_ID="your-google-client-id"
export GOOGLE_CLIENT_SECRET="your-google-client-secret"
export CLAUDE_API_KEY="your-claude-api-key"
export ENCRYPTION_KEY="your-base64-encryption-key"

# Run migrations and start API
dotnet run
```

### 4. Frontend Setup

```bash
cd calendar-manager-ui

# Install dependencies
npm install

# Start development server
ng serve
```

### 5. Google OAuth Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create a new project or select existing
3. Enable Google Calendar API
4. Create OAuth 2.0 credentials
5. Add redirect URIs:
   - `http://localhost:5047/api/auth/callback` (for development)
   - `http://localhost:4200/?auth=success` (for frontend redirect)

### 6. Access Your Application

- **Frontend**: http://localhost:4200
- **API**: http://localhost:5047
- **Swagger**: http://localhost:5047/swagger

## 🎯 Usage Examples

Once authenticated, try these natural language commands:

```
"Schedule lunch with Sarah tomorrow at 1pm"
→ Creates calendar event for tomorrow 1:00-2:00 PM

"What's on my calendar today?"
→ Lists all events for current day

"Find me 2 hours next week to work on the project"
→ Suggests available time slots

"Move my 3pm meeting to 4pm"
→ Updates existing event time

"Cancel all meetings on Friday"
→ Removes Friday events (with confirmation)
```

## 📁 Project Structure

```
ai-calendar/
├── CalendarManager.API/           # .NET 8 Web API Backend
│   ├── Controllers/               # API endpoints
│   │   ├── AuthController.cs      # OAuth authentication
│   │   ├── ChatController.cs      # Claude AI chat interface
│   │   └── TestController.cs      # Health checks
│   ├── Services/                  # Business logic
│   │   ├── Interfaces/            # Service contracts
│   │   └── Implementations/       # Service implementations
│   │       ├── ClaudeService.cs   # Claude AI integration
│   │       ├── GoogleCalendarService.cs
│   │       ├── OAuthService.cs    # OAuth flow management
│   │       └── TokenEncryptionService.cs
│   ├── Data/                      # Database context & entities
│   ├── Models/DTOs/               # Data transfer objects
│   └── Migrations/                # EF Core migrations
├── calendar-manager-ui/           # Angular 17+ Frontend
│   └── src/app/
│       ├── components/            # UI components
│       │   ├── dashboard/         # Main dashboard
│       │   └── login/             # Authentication UI
│       ├── services/              # Angular services
│       │   ├── api.ts             # HTTP API client
│       │   ├── ai-chat.service.ts # Chat interface
│       │   ├── auth.ts            # Authentication state
│       │   └── calendar.service.ts # Calendar operations
│       └── models/                # TypeScript interfaces
├── KeyGenerator/                  # Utility for encryption keys
├── docker-compose.yml             # Container orchestration
├── .env.example                   # Environment template
└── README.md                      # This file
```

## 🔧 API Endpoints

### Authentication
- `GET /api/auth/google-login` - Initiate OAuth flow
- `GET /api/auth/callback` - Handle OAuth callback  
- `GET /api/auth/status` - Check authentication status
- `POST /api/auth/logout` - Revoke tokens
- `POST /api/auth/test-token` - Validate access token

### AI Chat Interface  
- `POST /api/chat/message` - Process natural language commands
- `GET /api/chat/conversation/{id}` - Get conversation history
- `GET /api/chat/health` - Chat service health check

### Calendar Operations (Future)
- `GET /api/calendar/events` - List calendar events
- `POST /api/calendar/events` - Create new event  
- `PUT /api/calendar/events/{id}` - Update event
- `DELETE /api/calendar/events/{id}` - Delete event
- `GET /api/calendar/freebusy` - Check availability

## 🔒 Security Features

- ✅ **OAuth 2.0 with PKCE** - Secure authorization flow
- ✅ **Token Encryption** - AES-256 encryption for stored tokens
- ✅ **Input Validation** - Comprehensive request validation
- ✅ **CORS Protection** - Configured for specific origins
- ✅ **HTTPS Enforcement** - SSL/TLS in production
- ✅ **Rate Limiting** - API abuse prevention
- ✅ **Error Handling** - Secure error responses

## 🌍 Environment Variables

Create a `.env` file with these variables:

```env
# PostgreSQL Configuration
POSTGRES_DB=ai_calendar_manager
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_secure_password
POSTGRES_PORT=5432

# Database Connection
DATABASE_URL=postgresql://postgres:your_secure_password@localhost:5432/ai_calendar_manager

# Google OAuth Configuration  
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
GOOGLE_REDIRECT_URI=http://localhost:5047/api/auth/callback

# Claude AI Configuration
CLAUDE_API_KEY=your-claude-api-key

# Security
ENCRYPTION_KEY=your-32-byte-base64-encryption-key

# Application URLs
FRONTEND_URL=http://localhost:4200
```

## 🧪 Development & Testing

### Run Tests
```bash
# Backend tests
cd CalendarManager.API
dotnet test

# Frontend tests  
cd calendar-manager-ui
npm test
```

### Development Mode
```bash
# Start with hot reload
# Terminal 1: Backend
cd CalendarManager.API && dotnet watch run

# Terminal 2: Frontend  
cd calendar-manager-ui && ng serve

# Terminal 3: Database
docker-compose up postgres -d
```

### Production Build
```bash
# Build backend
cd CalendarManager.API
dotnet publish -c Release

# Build frontend
cd calendar-manager-ui  
ng build --configuration production
```

## 🚀 Deployment

### Docker Deployment
```bash
# Build and run all services
docker-compose up --build

# Production deployment
docker-compose -f docker-compose.prod.yml up -d
```

### Manual Deployment
1. Configure production database
2. Set production environment variables
3. Build applications for production
4. Deploy to your hosting platform
5. Configure HTTPS and domain

## 🤖 Claude AI Integration

The system uses Claude 3.5 Sonnet with function calling for:

- **Natural Language Processing** - Understanding complex scheduling requests
- **Intent Recognition** - Determining user goals from conversational input
- **Context Management** - Maintaining conversation history and context
- **Smart Responses** - Generating helpful, contextual replies
- **Tool Orchestration** - Calling appropriate calendar functions

### Function Calling Tools
- `create_calendar_event` - Creates new calendar events
- `list_calendar_events` - Retrieves calendar events
- `find_free_time` - Identifies available time slots
- `update_calendar_event` - Modifies existing events
- `delete_calendar_event` - Removes events

## 📊 Performance & Monitoring

- **Response Times**: < 2s for AI processing
- **Uptime Monitoring**: Health check endpoints
- **Error Tracking**: Comprehensive logging
- **Rate Limiting**: API abuse prevention
- **Caching**: Smart token and response caching

## 🔮 Roadmap

- [ ] **Enhanced AI Features**
  - Multi-language support
  - Voice input/output
  - Meeting summarization
  - Smart conflict resolution

- [ ] **Advanced Calendar Features**
  - Recurring event patterns
  - Meeting room booking
  - Attendee management
  - Calendar analytics

- [ ] **Enterprise Features**
  - Multi-tenant support
  - Admin dashboard
  - Usage analytics
  - API rate limiting per user

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is part of Andrew Rivera's portfolio demonstrating production-ready AI integrations and modern web development practices.

## 📞 Support

For questions or support:
- Create an issue on GitHub
- Email: [your-email@domain.com]
- LinkedIn: [Your LinkedIn Profile]

---

**Status**: ✅ **Production Ready** - Claude AI integration complete, OAuth flow working, full-stack application deployed

**Latest Update**: Implemented Claude 3.5 Sonnet integration with function calling architecture for intelligent calendar management