# AI Calendar Manager

A B2B scheduling platform that lets businesses offer self-service appointment booking to their clients. Built with .NET 8, Angular 21, PostgreSQL, Google Calendar API, and Claude AI.

**What it does:** Business owners connect their Google Calendar, define their services and availability, then share a booking link. Clients visit the link, pick a service, choose an available time slot, and book — no calls or emails needed. An AI chat assistant helps both sides manage scheduling through natural language.

## Architecture

```
                          ┌──────────────────┐
                          │  Google Calendar  │
                          │       API         │
                          └────────▲─────────┘
                                   │
┌─────────────────┐    ┌───────────┴────────┐    ┌──────────────┐
│   Angular 21    │◄──►│  .NET 8 Web API    │◄──►│  PostgreSQL  │
│   Frontend      │    │     Backend        │    │   Database   │
└─────────────────┘    └───────────▲────────┘    └──────────────┘
                                   │
                        ┌──────────┴──────────┐
                        │  Claude AI    SMTP  │
                        │  (Anthropic)  Email │
                        └─────────────────────┘
```

**Two user-facing sides:**
- **Admin dashboard** (`/admin/*`) — Business owner configures services, availability, and manages bookings
- **Public booking page** (`/book/{slug}`) — Clients self-schedule appointments, no account needed

## Features

### For Business Owners
- Connect Google Calendar via OAuth
- Set up business profile with shareable booking link
- Define services (name, duration, price, color)
- Configure weekly availability, breaks, and date overrides (holidays, days off)
- View and manage all bookings (filter by date/status, cancel, mark complete)
- AI chat assistant to manage calendar and bookings via natural language
- Email notifications when clients book or cancel

### For Clients
- Browse available services on the business's booking page
- Pick a date and see real-time available time slots
- Book in seconds — name, email, phone, done
- Receive confirmation email with booking details
- Cancel or view booking via a unique management link (no login required)
- Mobile-friendly booking experience

### AI Chat (Claude)
- Natural language calendar management ("Schedule lunch tomorrow at 1pm")
- Check booking availability for services
- View upcoming bookings and appointments
- Cancel bookings through conversation
- Smart tool calling with Google Calendar integration

## Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- PostgreSQL (or Docker)
- Google Cloud Console project (Calendar API + OAuth credentials)
- Claude API key (Anthropic)

### 1. Clone and Configure

```bash
git clone <repository-url>
cd ai-calendar
cp .env.example .env
# Edit .env with your API keys and database credentials
```

### 2. Database

```bash
# Docker (recommended)
docker-compose up postgres -d

# Or use a local PostgreSQL instance on localhost:5432
```

### 3. Backend

```bash
cd CalendarManager.API
dotnet restore
dotnet run
# Database migrations run automatically on startup
```

### 4. Frontend

```bash
cd calendar-manager-ui
npm install
ng serve
```

### 5. Google OAuth Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Enable Google Calendar API
3. Create OAuth 2.0 credentials
4. Add redirect URI: `http://localhost:5047/api/auth/callback`

### 6. Email Setup (Optional)

Configure SMTP in `appsettings.json` or environment variables to enable booking confirmation and notification emails. When disabled, emails are logged but not sent.

### 7. Access

- **Frontend**: http://localhost:4200
- **API**: http://localhost:5047
- **Swagger**: http://localhost:5047/swagger

## Project Structure

```
ai-calendar/
├── CalendarManager.API/              # .NET 8 Web API Backend
│   ├── Controllers/
│   │   ├── AuthController.cs         # Google OAuth (PKCE)
│   │   ├── ChatController.cs         # Claude AI chat
│   │   ├── CalendarController.cs     # Calendar CRUD
│   │   ├── AdminController.cs        # Business admin (profile, services, availability, bookings)
│   │   ├── PublicBookingController.cs # Client booking (public, no auth)
│   │   └── TestController.cs         # Health checks
│   ├── Services/
│   │   ├── Implementations/
│   │   │   ├── ClaudeService.cs      # AI with tool calling (calendar + booking tools)
│   │   │   ├── GoogleCalendarService.cs
│   │   │   ├── OAuthService.cs       # OAuth 2.0 + PKCE
│   │   │   ├── TokenEncryptionService.cs # AES-256-GCM
│   │   │   ├── AvailabilityService.cs # Slot calculation engine
│   │   │   ├── BookingService.cs     # Booking lifecycle management
│   │   │   └── EmailService.cs       # SMTP notifications (MailKit)
│   │   └── Interfaces/              # Service contracts
│   ├── Data/
│   │   ├── AppDbContext.cs           # EF Core context
│   │   └── Entities/                 # 10 entity classes
│   │       ├── User.cs
│   │       ├── OAuthToken.cs
│   │       ├── UserPreferences.cs
│   │       ├── Conversation.cs
│   │       ├── Message.cs
│   │       ├── BusinessProfile.cs
│   │       ├── Service.cs
│   │       ├── AvailabilityRule.cs
│   │       ├── Client.cs
│   │       └── Booking.cs
│   ├── Models/DTOs/                  # Data transfer objects
│   └── Migrations/                   # EF Core migrations
├── calendar-manager-ui/              # Angular 21 Frontend
│   └── src/app/
│       ├── components/
│       │   ├── login/                # Google OAuth login
│       │   ├── dashboard/            # AI chat + calendar view
│       │   │   ├── calendar/         # Monthly calendar grid
│       │   │   └── chat/             # Chat panel
│       │   ├── admin/                # Business management
│       │   │   ├── admin-dashboard   # Overview + stats
│       │   │   ├── business-setup    # Profile wizard
│       │   │   ├── service-manager   # Service CRUD
│       │   │   ├── availability-manager # Weekly schedule + overrides
│       │   │   └── booking-list      # Booking table + filters
│       │   └── booking/              # Public client booking
│       │       ├── booking-page      # 5-step booking flow
│       │       └── booking-manage    # View/cancel via token
│       ├── services/
│       │   ├── auth.ts               # Auth state management
│       │   ├── api.ts                # HTTP client
│       │   ├── ai-chat.service.ts    # Chat interface
│       │   ├── calendar.service.ts   # Calendar operations
│       │   ├── admin-api.service.ts  # Admin API client
│       │   └── public-booking-api.service.ts # Public booking client
│       ├── models/                   # TypeScript interfaces
│       └── guards/                   # Route protection
├── plans/                            # Implementation plans & TODO
├── docker-compose.yml
└── .env.example
```

## API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/auth/google-login` | Initiate OAuth flow |
| GET | `/api/auth/callback` | OAuth callback |
| GET | `/api/auth/status` | Check auth status |
| POST | `/api/auth/logout` | Revoke tokens |

### AI Chat
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/chat/message` | Process natural language command |
| GET | `/api/chat/health` | Service health check |

### Calendar
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/calendar/events` | List events (date range) |
| POST | `/api/calendar/events` | Create event |
| PUT | `/api/calendar/events/{id}` | Update event |
| DELETE | `/api/calendar/events/{id}` | Delete event |

### Admin (Auth Required)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET/POST/PUT | `/api/admin/profile` | Business profile CRUD |
| GET/POST/PUT/DELETE | `/api/admin/services` | Service management |
| GET | `/api/admin/availability` | Get availability rules |
| POST | `/api/admin/availability/weekly` | Set weekly schedule |
| POST | `/api/admin/availability/override` | Add date override |
| POST | `/api/admin/availability/break` | Add break |
| DELETE | `/api/admin/availability/{id}` | Remove rule |
| GET | `/api/admin/bookings` | List bookings (filterable) |
| GET | `/api/admin/bookings/today` | Today's bookings |
| GET | `/api/admin/bookings/upcoming` | Upcoming bookings |
| PUT | `/api/admin/bookings/{id}/cancel` | Cancel booking |
| PUT | `/api/admin/bookings/{id}/complete` | Mark complete |

### Public Booking (No Auth)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/book/{slug}` | Business info + services |
| GET | `/api/book/{slug}/slots` | Available time slots for a date |
| POST | `/api/book/{slug}` | Create booking |
| GET | `/api/book/manage/{token}` | View booking details |
| POST | `/api/book/manage/{token}/cancel` | Cancel booking |

## How It Works

### Availability Calculation
When a client requests time slots, the system:
1. Loads the business's availability rules (weekly hours, breaks, date overrides)
2. Queries Google Calendar for the owner's actual busy times
3. Checks existing confirmed bookings in the database
4. Subtracts all busy periods from available windows
5. Slices remaining time into slots based on the service duration + buffer

This ensures no double-booking, even if the business owner has personal events on their calendar.

### Booking Flow
1. Client visits `/book/{business-slug}`
2. Picks a service from the list
3. Selects a date (next 30 days)
4. Chooses an available time slot
5. Enters contact info and confirms
6. System creates a Google Calendar event on the business owner's calendar
7. Confirmation email sent to client, notification email sent to business owner
8. Client receives a management link to view or cancel

## AI Chat Examples

```
"Schedule lunch with Sarah tomorrow at 1pm"
→ Creates a Google Calendar event

"What's on my calendar today?"
→ Lists all events for the current day

"Show me my bookings for this week"
→ Displays upcoming client bookings

"Check availability for consultations on Friday"
→ Returns open time slots for the service

"Cancel booking abc123"
→ Cancels the booking and removes the calendar event
```

## Current Status

> **This project is under active development.** Core features are built but some areas are still being stabilized.

### Working
- Admin dashboard — business profile setup, service CRUD, availability rules, booking management
- Public booking flow — 5-step client booking with real-time slot calculation
- Availability engine — calculates open slots from weekly rules, breaks, overrides, Google Calendar busy times, and existing bookings
- Booking lifecycle — create, cancel, with race condition protection at the DB level
- Claude AI chat — tool calling for calendar events, booking queries, and cancellations (falls back to pattern matching if API is unavailable)
- Email notifications — booking confirmation, cancellation, and owner alerts via SMTP (optional, logs when disabled)
- Database — EF Core with auto-migrations, 10 entities, PostgreSQL
- Unit tests — XUnit + Moq covering services and controllers

### In Progress
- **Google OAuth integration** — flow is implemented (PKCE, token refresh, encryption at rest) but has integration bugs being worked through
- **Admin authentication** — currently uses a demo mode bypass; real session/JWT auth not yet wired up

### Not Yet Implemented
- **Conversation history** — chat works per-request but message persistence is stubbed out (DB entities exist, endpoint returns empty)
- **Multi-instance session storage** — PKCE verifier uses in-memory storage (needs Redis or similar for scaled deployments)

## Security

- OAuth 2.0 with PKCE for Google authentication
- AES-256-GCM encryption for stored tokens at rest
- Rate limiting on public booking endpoints (30 req/min, 10 POST/5min)
- CORS configured for specific origins
- Automatic token refresh (5 minutes before expiry)
- Cancellation tokens for client self-service (no login required)

## Environment Variables

```env
# PostgreSQL
POSTGRES_DB=ai_calendar_manager
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_secure_password
DATABASE_URL=postgresql://postgres:password@localhost:5432/ai_calendar_manager

# Google OAuth
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
GOOGLE_REDIRECT_URI=http://localhost:5047/api/auth/callback

# Claude AI
CLAUDE_API_KEY=your-claude-api-key

# Security
ENCRYPTION_KEY=your-32-byte-base64-encryption-key

# App
FRONTEND_URL=http://localhost:4200

# Email (optional)
EMAIL_ENABLED=true
EMAIL_SMTP_HOST=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_SMTP_USERNAME=your-email@gmail.com
EMAIL_SMTP_PASSWORD=your-app-password
EMAIL_FROM_ADDRESS=noreply@yourdomain.com
EMAIL_FROM_NAME=AI Calendar
```

## Development

```bash
# Backend with hot reload
cd CalendarManager.API && dotnet watch run

# Frontend with hot reload
cd calendar-manager-ui && ng serve

# Database
docker-compose up postgres -d
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 21, TypeScript 5.9, SCSS |
| Backend | .NET 8, C# 12, Entity Framework Core 8 |
| Database | PostgreSQL |
| AI | Claude (Anthropic SDK) with tool calling |
| Calendar | Google Calendar API v3 |
| Auth | Google OAuth 2.0 + PKCE |
| Email | MailKit (SMTP) |

---

Built by Andrew Rivera
