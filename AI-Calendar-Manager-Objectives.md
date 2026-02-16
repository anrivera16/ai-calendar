# AI Calendar Manager - Project Knowledge

## Project Overview

This is a production-ready AI-powered calendar assistant that uses natural language to manage Google Calendar. Users can schedule meetings, check availability, find optimal meeting times, and query their calendar using conversational commands.

**Business Purpose**: Andrew's second portfolio piece demonstrating AI automation for productivity tools. Shows integration with external APIs (Google Calendar), OAuth2 security, and practical AI tool use.

**Tech Stack**:
- Backend: .NET 8 Web API
- Frontend: Angular 17+
- Database: PostgreSQL
- AI: Claude API (Sonnet 4.5) with function calling
- Integration: Google Calendar API
- Developer: Andrew (full-stack, 5+ years .NET/Angular/EF/PostgreSQL)

## Architecture

### High-Level Flow
```
Authentication Flow:
User logs in → OAuth2 redirect to Google → Exchange code for tokens → Store encrypted tokens → Ready

Calendar Operation Flow:
User: "Schedule lunch with John tomorrow at noon"
↓
1. Claude analyzes intent (create_event)
2. Extract details: title="Lunch with John", date=tomorrow, time=12:00pm
3. Check Google Calendar for conflicts
4. Create event via Google Calendar API
5. Confirm to user: "Scheduled lunch with John for Tuesday, Feb 10 at 12:00 PM"

Smart Scheduling Flow:
User: "Find 30 minutes with Sarah this week"
↓
1. Get user's calendar (all events this week)
2. Get Sarah's free/busy info
3. Apply user preferences (working hours, buffer times)
4. Claude finds optimal slot
5. Propose time to user
6. Create event on confirmation
```

### Why This Project?

**Skills Demonstrated**:
- OAuth2 implementation (real-world auth)
- Claude function calling (AI tool use)
- External API integration (Google Calendar)
- Natural language understanding (intent extraction)
- Secure token management (encryption at rest)
- Complex business logic (availability algorithms)

**Business Applications**:
- Executive assistants automation
- Meeting scheduler for sales teams
- Personal productivity enhancement
- Integration template for other Google services

## Project Structure
```
AICalendarManager/
├── CalendarManager.API/
│   ├── Controllers/
│   │   ├── AuthController.cs          # OAuth2 flow
│   │   ├── CalendarController.cs      # Calendar operations
│   │   └── ChatController.cs          # Natural language interface
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── Entities/
│   │       ├── User.cs
│   │       ├── OAuthToken.cs
│   │       ├── UserPreferences.cs
│   │       ├── Conversation.cs
│   │       └── Message.cs
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IGoogleCalendarService.cs
│   │   │   ├── IClaudeService.cs
│   │   │   ├── IOAuthService.cs
│   │   │   ├── ITokenEncryptionService.cs
│   │   │   └── IAvailabilityService.cs
│   │   └── Implementations/
│   │       ├── GoogleCalendarService.cs
│   │       ├── ClaudeService.cs
│   │       ├── OAuthService.cs
│   │       ├── TokenEncryptionService.cs
│   │       └── AvailabilityService.cs
│   ├── Models/
│   │   ├── DTOs/
│   │   │   ├── ChatRequest.cs
│   │   │   ├── ChatResponse.cs
│   │   │   ├── CalendarEvent.cs
│   │   │   ├── AvailabilitySlot.cs
│   │   │   └── UserPreferencesDto.cs
│   │   └── Claude/
│   │       ├── ClaudeTool.cs
│   │       └── ToolDefinitions.cs
│   └── Program.cs
│
└── calendar-manager-ui/
    └── src/app/
        ├── components/
        │   ├── chat-interface/
        │   ├── calendar-view/
        │   ├── preferences-settings/
        │   └── login/
        └── services/
            ├── calendar.service.ts
            ├── chat.service.ts
            └── auth.service.ts
```

## Database Schema
```sql
-- Users
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    google_user_id VARCHAR(255) UNIQUE,
    display_name VARCHAR(255),
    created_at TIMESTAMP DEFAULT NOW(),
    last_login TIMESTAMP
);

-- OAuth tokens (encrypted!)
CREATE TABLE oauth_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    access_token TEXT NOT NULL, -- Encrypted with AES-256
    refresh_token TEXT NOT NULL, -- Encrypted with AES-256
    expires_at TIMESTAMP NOT NULL,
    scope TEXT,
    token_type VARCHAR(50) DEFAULT 'Bearer',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Index for quick token lookup
CREATE INDEX idx_oauth_tokens_user_id ON oauth_tokens(user_id);

-- User preferences
CREATE TABLE user_preferences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    working_hours_start TIME DEFAULT '09:00',
    working_hours_end TIME DEFAULT '17:00',
    working_days JSONB DEFAULT '["Monday","Tuesday","Wednesday","Thursday","Friday"]',
    meeting_buffer_minutes INT DEFAULT 15, -- Time between meetings
    preferred_meeting_duration INT DEFAULT 30,
    timezone VARCHAR(50) DEFAULT 'America/Chicago',
    preferences_text TEXT, -- Natural language: "I prefer morning meetings, avoid Fridays after 2pm"
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(user_id)
);

-- Conversation history (for context in multi-turn dialogues)
CREATE TABLE conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(500), -- Auto-generated from first message
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_conversations_user_id ON conversations(user_id);
CREATE INDEX idx_conversations_updated_at ON conversations(updated_at DESC);

-- Messages in conversations
CREATE TABLE messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id UUID REFERENCES conversations(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL, -- 'user' or 'assistant'
    content TEXT NOT NULL,
    function_calls JSONB, -- Store Claude function calls made
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_messages_conversation_id ON messages(conversation_id);
```

## Core Components

### 1. OAuth Service (IOAuthService)

**Purpose**: Handle Google OAuth2 authentication flow

**Key Responsibilities**:
- Generate OAuth2 authorization URL
- Exchange authorization code for access/refresh tokens
- Store tokens securely (encrypted)
- Refresh expired access tokens automatically
- Revoke tokens on logout

**OAuth2 Flow**:
```
1. User clicks "Connect Google Calendar"
2. Redirect to Google: https://accounts.google.com/o/oauth2/v2/auth
   - client_id, redirect_uri, scope (calendar access)
3. User grants permission
4. Google redirects back with authorization code
5. Exchange code for access_token + refresh_token
6. Encrypt and store tokens in database
7. Access token expires after 1 hour
8. Automatically refresh using refresh_token
```

**Security Critical**:
- NEVER log tokens
- Store encrypted at rest (AES-256)
- Use HTTPS only
- Implement PKCE for extra security
- Rotate encryption keys periodically

### 2. Token Encryption Service (ITokenEncryptionService)

**Purpose**: Securely encrypt/decrypt OAuth tokens

**Key Responsibilities**:
- Encrypt tokens before database storage
- Decrypt tokens when needed for API calls
- Use strong encryption (AES-256-GCM)
- Key management (store in environment variables, not code)

**Implementation Pattern**:
```csharp
public class TokenEncryptionService : ITokenEncryptionService
{
    private readonly byte[] _encryptionKey; // From configuration
    
    public string Encrypt(string plaintext)
    {
        // Use AES-256-GCM
        // Return Base64 encoded: IV + ciphertext + auth tag
    }
    
    public string Decrypt(string ciphertext)
    {
        // Decode Base64
        // Decrypt using AES-256-GCM
        // Verify auth tag
    }
}
```

### 3. Google Calendar Service (IGoogleCalendarService)

**Purpose**: Wrapper around Google Calendar API

**Key Responsibilities**:
- Get user's calendar events (list, by date range)
- Create new calendar events
- Update existing events
- Delete events
- Check free/busy information
- Handle API errors and rate limits

**Core Operations**:
```csharp
public interface IGoogleCalendarService
{
    Task<List<CalendarEvent>> GetEventsAsync(DateTime start, DateTime end);
    Task<CalendarEvent> CreateEventAsync(CreateEventRequest request);
    Task<CalendarEvent> UpdateEventAsync(string eventId, UpdateEventRequest request);
    Task DeleteEventAsync(string eventId);
    Task<FreeBusyInfo> GetFreeBusyAsync(string email, DateTime start, DateTime end);
    Task<bool> ValidateTokenAsync(); // Check if token is still valid
}
```

**Google Calendar API Details**:
- Endpoint: `https://www.googleapis.com/calendar/v3`
- Auth: Bearer token (access_token in header)
- Rate limit: 1 million queries/day, 10 requests/second
- Scope needed: `https://www.googleapis.com/auth/calendar`

### 4. Claude Service with Function Calling (IClaudeService)

**Purpose**: Use Claude to understand natural language and execute calendar operations

**Key Responsibilities**:
- Parse user intent from natural language
- Define available tools (functions) for Claude
- Execute tool calls (calendar operations)
- Maintain conversation context
- Format responses naturally

**Function Definitions for Claude**:
```csharp
public static class CalendarTools
{
    public static List<ClaudeTool> GetToolDefinitions()
    {
        return new List<ClaudeTool>
        {
            new ClaudeTool
            {
                Name = "get_calendar_events",
                Description = "Get the user's calendar events for a specified date range",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        start_date = new
                        {
                            type = "string",
                            description = "Start date in ISO format (YYYY-MM-DD)"
                        },
                        end_date = new
                        {
                            type = "string",
                            description = "End date in ISO format (YYYY-MM-DD)"
                        }
                    },
                    required = new[] { "start_date", "end_date" }
                }
            },
            new ClaudeTool
            {
                Name = "create_calendar_event",
                Description = "Create a new calendar event",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        title = new { type = "string", description = "Event title" },
                        start_time = new { type = "string", description = "Start time in ISO 8601 format" },
                        end_time = new { type = "string", description = "End time in ISO 8601 format" },
                        description = new { type = "string", description = "Optional event description" },
                        attendees = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description = "Optional list of attendee emails"
                        },
                        location = new { type = "string", description = "Optional location" }
                    },
                    required = new[] { "title", "start_time", "end_time" }
                }
            },
            new ClaudeTool
            {
                Name = "update_calendar_event",
                Description = "Update an existing calendar event",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        event_id = new { type = "string", description = "The ID of the event to update" },
                        title = new { type = "string", description = "New event title" },
                        start_time = new { type = "string", description = "New start time" },
                        end_time = new { type = "string", description = "New end time" }
                    },
                    required = new[] { "event_id" }
                }
            },
            new ClaudeTool
            {
                Name = "delete_calendar_event",
                Description = "Delete a calendar event",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        event_id = new { type = "string", description = "The ID of the event to delete" }
                    },
                    required = new[] { "event_id" }
                }
            },
            new ClaudeTool
            {
                Name = "find_available_slots",
                Description = "Find available time slots for a meeting",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        duration_minutes = new { type = "integer", description = "Meeting duration in minutes" },
                        start_date = new { type = "string", description = "Start searching from this date" },
                        end_date = new { type = "string", description = "Search until this date" },
                        attendees = new
                        {
                            type = "array",
                            items = new { type = "string" },
                            description = "Optional list of attendee emails to check availability"
                        }
                    },
                    required = new[] { "duration_minutes", "start_date", "end_date" }
                }
            }
        };
    }
}
```

**Claude Integration Flow**:
```csharp
public async Task<ChatResponse> ProcessMessageAsync(string userMessage, Guid userId)
{
    // 1. Get conversation history
    var history = await GetConversationHistoryAsync(conversationId);
    
    // 2. Build system prompt
    var systemPrompt = @"You are an intelligent calendar assistant. 
You help users manage their Google Calendar through natural language commands.

Current date and time: {DateTime.Now}
User's timezone: {userTimezone}
User's preferences: {userPreferences}

When users ask to schedule meetings:
- Extract all relevant details (title, date, time, attendees)
- Check for conflicts before creating
- Suggest alternative times if conflicts exist
- Be conversational and helpful

When users ask about their schedule:
- Provide clear, organized information
- Highlight important meetings
- Mention any conflicts or busy periods

Always confirm actions before executing them.";

    // 3. Add user message to history
    history.Add(new { role = "user", content = userMessage });
    
    // 4. Call Claude with tools
    var request = new
    {
        model = "claude-sonnet-4-5-20250929",
        max_tokens = 2048,
        system = systemPrompt,
        messages = history,
        tools = CalendarTools.GetToolDefinitions()
    };
    
    var response = await CallClaudeApiAsync(request);
    
    // 5. Process tool calls if any
    while (response.StopReason == "tool_use")
    {
        // Execute each tool call
        foreach (var toolUse in response.Content.Where(c => c.Type == "tool_use"))
        {
            var toolResult = await ExecuteToolAsync(toolUse.Name, toolUse.Input, userId);
            
            // Add tool result to conversation
            history.Add(new
            {
                role = "user",
                content = new[]
                {
                    new
                    {
                        type = "tool_result",
                        tool_use_id = toolUse.Id,
                        content = toolResult
                    }
                }
            });
        }
        
        // Continue conversation with tool results
        response = await CallClaudeApiAsync(new { messages = history, tools = ... });
    }
    
    // 6. Return final response
    return new ChatResponse
    {
        Message = response.Content.First(c => c.Type == "text").Text,
        ToolCallsMade = GetToolCallsSummary(history)
    };
}
```

### 5. Availability Service (IAvailabilityService)

**Purpose**: Find optimal meeting times based on user preferences and calendar availability

**Key Responsibilities**:
- Analyze user's calendar for free slots
- Apply working hours constraints
- Respect meeting buffer times
- Check attendee availability (free/busy)
- Rank slots by preference (morning vs afternoon)
- Handle timezone conversions

**Smart Scheduling Algorithm**:
```csharp
public async Task<List<AvailabilitySlot>> FindAvailableSlotsAsync(
    Guid userId,
    int durationMinutes,
    DateTime searchStart,
    DateTime searchEnd,
    List<string> attendeeEmails = null)
{
    // 1. Get user preferences
    var prefs = await _context.UserPreferences
        .FirstAsync(p => p.UserId == userId);
    
    // 2. Get user's existing events in date range
    var userEvents = await _calendarService.GetEventsAsync(searchStart, searchEnd);
    
    // 3. Get attendees' free/busy if provided
    var attendeeBusy = new List<(DateTime Start, DateTime End)>();
    if (attendeeEmails?.Any() == true)
    {
        foreach (var email in attendeeEmails)
        {
            var freeBusy = await _calendarService.GetFreeBusyAsync(email, searchStart, searchEnd);
            attendeeBusy.AddRange(freeBusy.BusyPeriods);
        }
    }
    
    // 4. Find all potential slots
    var slots = new List<AvailabilitySlot>();
    var currentDate = searchStart.Date;
    
    while (currentDate <= searchEnd.Date)
    {
        // Skip non-working days
        if (!prefs.WorkingDays.Contains(currentDate.DayOfWeek.ToString()))
        {
            currentDate = currentDate.AddDays(1);
            continue;
        }
        
        // Working hours for this day
        var dayStart = currentDate.Add(prefs.WorkingHoursStart);
        var dayEnd = currentDate.Add(prefs.WorkingHoursEnd);
        
        // Scan for free slots
        var scanTime = dayStart;
        while (scanTime.AddMinutes(durationMinutes) <= dayEnd)
        {
            var slotEnd = scanTime.AddMinutes(durationMinutes);
            
            // Check if slot conflicts with user's events
            var hasUserConflict = userEvents.Any(e => 
                (scanTime >= e.Start && scanTime < e.End) ||
                (slotEnd > e.Start && slotEnd <= e.End) ||
                (scanTime <= e.Start && slotEnd >= e.End)
            );
            
            // Check if slot conflicts with attendees
            var hasAttendeeConflict = attendeeBusy.Any(b =>
                (scanTime >= b.Start && scanTime < b.End) ||
                (slotEnd > b.Start && slotEnd <= b.End) ||
                (scanTime <= b.Start && slotEnd >= b.End)
            );
            
            // Check buffer time with adjacent meetings
            var hasBufferConflict = userEvents.Any(e =>
                Math.Abs((scanTime - e.End).TotalMinutes) < prefs.MeetingBufferMinutes ||
                Math.Abs((slotEnd - e.Start).TotalMinutes) < prefs.MeetingBufferMinutes
            );
            
            if (!hasUserConflict && !hasAttendeeConflict && !hasBufferConflict)
            {
                slots.Add(new AvailabilitySlot
                {
                    Start = scanTime,
                    End = slotEnd,
                    Score = CalculateSlotScore(scanTime, prefs) // Prefer certain times
                });
            }
            
            // Move to next potential slot (15-minute increments)
            scanTime = scanTime.AddMinutes(15);
        }
        
        currentDate = currentDate.AddDays(1);
    }
    
    // 5. Sort by score (best times first)
    return slots.OrderByDescending(s => s.Score).ToList();
}

private double CalculateSlotScore(DateTime slotStart, UserPreferences prefs)
{
    double score = 100.0;
    
    // Prefer morning if preferences say so
    if (prefs.PreferencesText?.Contains("morning", StringComparison.OrdinalIgnoreCase) == true)
    {
        if (slotStart.Hour < 12)
            score += 20;
    }
    
    // Prefer afternoon if preferences say so
    if (prefs.PreferencesText?.Contains("afternoon", StringComparison.OrdinalIgnoreCase) == true)
    {
        if (slotStart.Hour >= 12 && slotStart.Hour < 17)
            score += 20;
    }
    
    // Penalize very early or very late times
    if (slotStart.Hour < 9)
        score -= 30;
    if (slotStart.Hour >= 17)
        score -= 20;
    
    // Prefer times not right after lunch
    if (slotStart.Hour == 13)
        score -= 10;
    
    // Prefer whole/half hour times
    if (slotStart.Minute == 0)
        score += 10;
    else if (slotStart.Minute == 30)
        score += 5;
    
    return score;
}
```

### 6. API Controllers

**AuthController**:
```csharp
// Initiate OAuth flow
GET /api/auth/google-login
Response: { authUrl: "https://accounts.google.com/..." }

// OAuth callback
GET /api/auth/callback?code={authCode}
Response: { success: true, user: { email, name } }

// Logout
POST /api/auth/logout
Response: { success: true }

// Check auth status
GET /api/auth/status
Response: { authenticated: true, user: { email, name } }
```

**CalendarController**:
```csharp
// Get events
GET /api/calendar/events?start=2024-02-10&end=2024-02-17
Response: [
  {
    id: "event123",
    title: "Team Standup",
    start: "2024-02-10T09:00:00Z",
    end: "2024-02-10T09:30:00Z",
    attendees: ["john@company.com"],
    location: "Conference Room A"
  }
]

// Create event
POST /api/calendar/events
Body: {
  title: "Lunch with John",
  start: "2024-02-10T12:00:00Z",
  end: "2024-02-10T13:00:00Z",
  attendees: ["john@email.com"],
  description: "Discuss Q1 planning"
}
Response: { id: "event456", success: true }

// Update event
PUT /api/calendar/events/{id}
Body: { title: "Updated title", start: "..." }
Response: { success: true }

// Delete event
DELETE /api/calendar/events/{id}
Response: { success: true }

// Find available slots
POST /api/calendar/find-slots
Body: {
  durationMinutes: 30,
  startDate: "2024-02-10",
  endDate: "2024-02-14",
  attendees: ["sarah@email.com"]
}
Response: [
  {
    start: "2024-02-10T10:00:00Z",
    end: "2024-02-10T10:30:00Z",
    score: 95
  }
]
```

**ChatController**:
```csharp
// Process natural language command
POST /api/chat/message
Body: {
  message: "Schedule a meeting with John tomorrow at 2pm",
  conversationId: "guid" // optional
}
Response: {
  reply: "I've scheduled a meeting with John for tomorrow (Feb 10) at 2:00 PM. The event has been created on your calendar.",
  actionsPerformed: [
    {
      type: "create_event",
      details: { title: "Meeting with John", start: "2024-02-10T14:00:00Z" }
    }
  ],
  conversationId: "guid"
}

// Get conversation history
GET /api/chat/conversations/{id}
Response: {
  id: "guid",
  messages: [
    { role: "user", content: "What's on my calendar today?" },
    { role: "assistant", content: "You have 3 meetings today..." }
  ]
}
```

**PreferencesController**:
```csharp
// Get preferences
GET /api/preferences
Response: {
  workingHoursStart: "09:00",
  workingHoursEnd: "17:00",
  workingDays: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
  meetingBufferMinutes: 15,
  preferredMeetingDuration: 30,
  timezone: "America/Chicago",
  preferencesText: "I prefer morning meetings, avoid Fridays after 2pm"
}

// Update preferences
PUT /api/preferences
Body: { workingHoursStart: "08:00", preferencesText: "..." }
Response: { success: true }
```

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ai_calendar_manager;Username=postgres;Password=yourpassword"
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-google-client-secret",
      "RedirectUri": "https://localhost:5001/api/auth/callback",
      "Scopes": [
        "https://www.googleapis.com/auth/calendar",
        "https://www.googleapis.com/auth/userinfo.email",
        "https://www.googleapis.com/auth/userinfo.profile"
      ]
    },
    "Encryption": {
      "Key": "your-32-byte-encryption-key-base64-encoded"
    }
  },
  "ApiKeys": {
    "ClaudeApiKey": "your-claude-api-key-here"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  }
}
```

### Required NuGet Packages
```xml
<!-- Database -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />

<!-- Google Calendar API -->
<PackageReference Include="Google.Apis.Calendar.v3" Version="1.68.0.3400" />
<PackageReference Include="Google.Apis.Auth" Version="1.68.0" />

<!-- Authentication -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />

<!-- Encryption -->
<PackageReference Include="System.Security.Cryptography.Algorithms" Version="4.3.1" />
```

### Setting Up Google Cloud Project

**Project Details**:
- Project ID: `ai-calendar-manager-487001`
- Project Name: "AI Calendar Manager"

**Setup Steps**:
1. Go to https://console.cloud.google.com/
2. Create new project: "AI Calendar Manager" ✅ (Project ID: ai-calendar-manager-487001)
3. Enable Google Calendar API:
   - APIs & Services → Library → Search "Google Calendar API" → Enable
4. Create OAuth 2.0 credentials:
   - APIs & Services → Credentials → Create Credentials → OAuth 2.0 Client ID
   - Application type: Web application
   - Authorized redirect URIs: 
     - `http://localhost:5047/api/auth/callback` (Development)
     - `https://yourdomain.com/api/auth/callback` (Production)
5. Copy Client ID and Client Secret to appsettings.json

**OAuth Consent Screen**:
- User Type: External (for testing) or Internal (for organization)
- Scopes: Add these scopes:
  - `https://www.googleapis.com/auth/calendar`
  - `https://www.googleapis.com/auth/userinfo.email`
  - `https://www.googleapis.com/auth/userinfo.profile`
- Test users: Add your email for testing

**Important URLs**:
- Google Cloud Console: https://console.cloud.google.com/welcome?project=ai-calendar-manager-487001
- API Credentials: https://console.cloud.google.com/apis/credentials?project=ai-calendar-manager-487001
- OAuth Consent Screen: https://console.cloud.google.com/apis/credentials/consent?project=ai-calendar-manager-487001

## Development Phases

### Phase 1: Authentication & Basic Setup (Days 1-3)
**Goal**: OAuth2 flow working, tokens stored securely

**Tasks**:
- Set up project structure (API + DB)
- Implement User and OAuthToken entities
- Create TokenEncryptionService (AES-256)
- Build OAuth flow in AuthController
- Test: Can authenticate with Google, store encrypted tokens
- Verify: Token refresh works automatically

**Success Metric**: User can click "Connect Google Calendar" and successfully authenticate

### Phase 2: Google Calendar Integration (Days 4-6)
**Goal**: CRUD operations on Google Calendar working

**Tasks**:
- Implement GoogleCalendarService
- Get events endpoint
- Create event endpoint
- Update event endpoint
- Delete event endpoint
- Test with Postman/Thunder Client
- Handle API errors gracefully

**Success Metric**: Can create/read/update/delete events via API without using natural language yet

### Phase 3: Claude Function Calling (Days 7-9)
**Goal**: Natural language calendar commands working

**Tasks**:
- Define tool schemas for Claude
- Implement ClaudeService with function calling
- Build ChatController
- Handle tool execution loop
- Add conversation history tracking
- Test natural language commands:
  - "What's on my calendar today?"
  - "Schedule lunch tomorrow at noon"
  - "Cancel my 3pm meeting"

**Success Metric**: Can manage calendar using conversational commands

### Phase 4: Smart Scheduling (Days 10-12)
**Goal**: AI finds optimal meeting times

**Tasks**:
- Implement AvailabilityService
- Build slot-finding algorithm
- Integrate user preferences
- Add free/busy checking for attendees
- Implement slot scoring (prefer certain times)
- Test: "Find 30 minutes with Sarah this week"

**Success Metric**: System suggests 3-5 optimal meeting times based on everyone's availability

### Phase 5: User Preferences (Days 13-14)
**Goal**: Personalization working

**Tasks**:
- Build preferences UI
- Store working hours, buffer times
- Parse natural language preferences: "I prefer morning meetings"
- Apply preferences in availability search
- Test preference impact on suggestions

**Success Metric**: User can set "prefer mornings" and system prioritizes morning slots

### Phase 6: Angular Frontend (Days 15-18)
**Goal**: User interface complete

**Tasks**:
- Set up Angular project
- Build login component (OAuth redirect)
- Create chat interface component
- Display calendar view (month/week)
- Preferences settings page
- Loading states and error handling
- Responsive design

**Success Metric**: Beautiful, functional UI for entire flow

### Phase 7: Polish & Demo (Days 19-21)
**Goal**: Production-ready and presentable

**Tasks**:
- Add message streaming (real-time responses)
- Implement optimistic UI updates
- Add analytics tracking (usage metrics)
- Error logging and monitoring
- Security audit (token handling, XSS, CSRF)
- Create demo video
- Write deployment guide

**Success Metric**: Can confidently demo to potential clients

## Key Implementation Details

### OAuth2 Implementation
```csharp
public class OAuthService : IOAuthService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;
    private readonly ITokenEncryptionService _encryption;
    
    public string GetAuthorizationUrl(string state)
    {
        var clientId = _config["Authentication:Google:ClientId"];
        var redirectUri = _config["Authentication:Google:RedirectUri"];
        var scopes = string.Join(" ", _config.GetSection("Authentication:Google:Scopes").Get<string[]>());
        
        // Use PKCE for extra security
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        
        // Store code_verifier temporarily (in cache or session)
        StoreCodeVerifier(state, codeVerifier);
        
        var authUrl = "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={clientId}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString(scopes)}" +
            $"&access_type=offline" + // Get refresh token
            $"&prompt=consent" + // Force consent to get refresh token
            $"&state={state}" +
            $"&code_challenge={codeChallenge}" +
            $"&code_challenge_method=S256";
        
        return authUrl;
    }
    
    public async Task<OAuthToken> ExchangeCodeForTokensAsync(string code, string state)
    {
        var clientId = _config["Authentication:Google:ClientId"];
        var clientSecret = _config["Authentication:Google:ClientSecret"];
        var redirectUri = _config["Authentication:Google:RedirectUri"];
        var codeVerifier = RetrieveCodeVerifier(state);
        
        var tokenRequest = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier
        };
        
        var httpClient = new HttpClient();
        var response = await httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(tokenRequest)
        );
        
        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        
        // Encrypt tokens before storage
        var encryptedAccessToken = _encryption.Encrypt(tokenResponse.AccessToken);
        var encryptedRefreshToken = _encryption.Encrypt(tokenResponse.RefreshToken);
        
        var token = new OAuthToken
        {
            AccessToken = encryptedAccessToken,
            RefreshToken = encryptedRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            Scope = tokenResponse.Scope,
            TokenType = tokenResponse.TokenType
        };
        
        return token;
    }
    
    public async Task<string> GetValidAccessTokenAsync(Guid userId)
    {
        var tokenEntity = await _context.OAuthTokens
            .FirstOrDefaultAsync(t => t.UserId == userId);
        
        if (tokenEntity == null)
            throw new UnauthorizedException("No token found for user");
        
        // Check if token is expired
        if (tokenEntity.ExpiresAt <= DateTime.UtcNow.AddMinutes(5)) // Refresh 5 min early
        {
            await RefreshAccessTokenAsync(tokenEntity);
        }
        
        // Decrypt and return
        return _encryption.Decrypt(tokenEntity.AccessToken);
    }
    
    private async Task RefreshAccessTokenAsync(OAuthToken tokenEntity)
    {
        var clientId = _config["Authentication:Google:ClientId"];
        var clientSecret = _config["Authentication:Google:ClientSecret"];
        var refreshToken = _encryption.Decrypt(tokenEntity.RefreshToken);
        
        var refreshRequest = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        };
        
        var httpClient = new HttpClient();
        var response = await httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(refreshRequest)
        );
        
        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        
        // Update with new access token
        tokenEntity.AccessToken = _encryption.Encrypt(tokenResponse.AccessToken);
        tokenEntity.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        tokenEntity.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }
}
```

### Token Encryption Implementation
```csharp
public class TokenEncryptionService : ITokenEncryptionService
{
    private readonly byte[] _encryptionKey;
    
    public TokenEncryptionService(IConfiguration config)
    {
        // Get encryption key from configuration (32 bytes for AES-256)
        var keyBase64 = config["Authentication:Encryption:Key"];
        _encryptionKey = Convert.FromBase64String(keyBase64);
        
        if (_encryptionKey.Length != 32)
            throw new InvalidOperationException("Encryption key must be 32 bytes (256 bits)");
    }
    
    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;
        
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);
        
        // Combine IV + ciphertext for storage
        var combined = new byte[aes.IV.Length + ciphertextBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(ciphertextBytes, 0, combined, aes.IV.Length, ciphertextBytes.Length);
        
        return Convert.ToBase64String(combined);
    }
    
    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
            return ciphertext;
        
        var combined = Convert.FromBase64String(ciphertext);
        
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        
        // Extract IV from beginning
        var iv = new byte[aes.IV.Length];
        var ciphertextBytes = new byte[combined.Length - iv.Length];
        
        Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(combined, iv.Length, ciphertextBytes, 0, ciphertextBytes.Length);
        
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        var plaintextBytes = decryptor.TransformFinalBlock(ciphertextBytes, 0, ciphertextBytes.Length);
        
        return Encoding.UTF8.GetString(plaintextBytes);
    }
}

// Generate encryption key (run once, store in config):
// var key = new byte[32];
// RandomNumberGenerator.Fill(key);
// var keyBase64 = Convert.ToBase64String(key);
```

### Google Calendar Service Implementation
```csharp
public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly IOAuthService _oauthService;
    private readonly IHttpClientFactory _httpClientFactory;
    
    public async Task<List<CalendarEvent>> GetEventsAsync(
        Guid userId, 
        DateTime start, 
        DateTime end)
    {
        var accessToken = await _oauthService.GetValidAccessTokenAsync(userId);
        var httpClient = _httpClientFactory.CreateClient();
        
        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", accessToken);
        
        var timeMin = start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        var timeMax = end.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        
        var url = $"https://www.googleapis.com/calendar/v3/calendars/primary/events" +
                  $"?timeMin={timeMin}" +
                  $"&timeMax={timeMax}" +
                  $"&singleEvents=true" +
                  $"&orderBy=startTime";
        
        var response = await httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new GoogleCalendarException($"Failed to get events: {error}");
        }
        
        var result = await response.Content.ReadFromJsonAsync<GoogleCalendarEventsResponse>();
        
        return result.Items.Select(item => new CalendarEvent
        {
            Id = item.Id,
            Title = item.Summary,
            Description = item.Description,
            Start = DateTime.Parse(item.Start.DateTime ?? item.Start.Date),
            End = DateTime.Parse(item.End.DateTime ?? item.End.Date),
            Location = item.Location,
            Attendees = item.Attendees?.Select(a => a.Email).ToList() ?? new List<string>(),
            HtmlLink = item.HtmlLink
        }).ToList();
    }
    
    public async Task<CalendarEvent> CreateEventAsync(
        Guid userId,
        CreateEventRequest request)
    {
        var accessToken = await _oauthService.GetValidAccessTokenAsync(userId);
        var httpClient = _httpClientFactory.CreateClient();
        
        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", accessToken);
        
        var eventBody = new
        {
            summary = request.Title,
            description = request.Description,
            location = request.Location,
            start = new
            {
                dateTime = request.Start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                timeZone = "UTC"
            },
            end = new
            {
                dateTime = request.End.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                timeZone = "UTC"
            },
            attendees = request.Attendees?.Select(email => new { email }).ToArray(),
            reminders = new
            {
                useDefault = true
            }
        };
        
        var response = await httpClient.PostAsJsonAsync(
            "https://www.googleapis.com/calendar/v3/calendars/primary/events",
            eventBody
        );
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new GoogleCalendarException($"Failed to create event: {error}");
        }
        
        var result = await response.Content.ReadFromJsonAsync<GoogleCalendarEventResponse>();
        
        return new CalendarEvent
        {
            Id = result.Id,
            Title = result.Summary,
            Start = DateTime.Parse(result.Start.DateTime),
            End = DateTime.Parse(result.End.DateTime),
            HtmlLink = result.HtmlLink
        };
    }
    
    public async Task<FreeBusyInfo> GetFreeBusyAsync(
        Guid userId,
        string emailToCheck,
        DateTime start,
        DateTime end)
    {
        var accessToken = await _oauthService.GetValidAccessTokenAsync(userId);
        var httpClient = _httpClientFactory.CreateClient();
        
        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", accessToken);
        
        var requestBody = new
        {
            timeMin = start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            timeMax = end.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            items = new[] { new { id = emailToCheck } }
        };
        
        var response = await httpClient.PostAsJsonAsync(
            "https://www.googleapis.com/calendar/v3/freeBusy",
            requestBody
        );
        
        var result = await response.Content.ReadFromJsonAsync<GoogleFreeBusyResponse>();
        
        var busyPeriods = result.Calendars[emailToCheck].Busy
            .Select(b => (
                Start: DateTime.Parse(b.Start),
                End: DateTime.Parse(b.End)
            ))
            .ToList();
        
        return new FreeBusyInfo
        {
            Email = emailToCheck,
            BusyPeriods = busyPeriods
        };
    }
}
```

## API Endpoints Summary

### Authentication
- `GET /api/auth/google-login` - Initiate OAuth flow
- `GET /api/auth/callback?code={code}` - OAuth callback
- `POST /api/auth/logout` - Revoke tokens
- `GET /api/auth/status` - Check if authenticated

### Calendar Operations
- `GET /api/calendar/events?start={date}&end={date}` - List events
- `POST /api/calendar/events` - Create event
- `PUT /api/calendar/events/{id}` - Update event
- `DELETE /api/calendar/events/{id}` - Delete event
- `POST /api/calendar/find-slots` - Find available time slots

### Chat Interface
- `POST /api/chat/message` - Process natural language command
- `GET /api/chat/conversations` - List conversations
- `GET /api/chat/conversations/{id}` - Get conversation history
- `DELETE /api/chat/conversations/{id}` - Delete conversation

### Preferences
- `GET /api/preferences` - Get user preferences
- `PUT /api/preferences` - Update preferences

## Performance Targets

- **OAuth flow**: < 2 seconds (redirect + token exchange)
- **Get events**: < 500ms (100 events)
- **Create event**: < 800ms (including conflict check)
- **Claude processing**: < 2 seconds (intent → tool call → response)
- **Find available slots**: < 1 second (5-day search)
- **Total natural language query**: < 3 seconds end-to-end

## Security Checklist

âœ… OAuth tokens encrypted at rest (AES-256)
âœ… HTTPS only in production
âœ… PKCE used in OAuth flow
âœ… Tokens never logged or exposed in errors
âœ… Input validation on all endpoints
âœ… Rate limiting on API endpoints
âœ… CORS configured correctly
âœ… SQL injection prevention (EF parameterized queries)
âœ… XSS prevention (Angular sanitization)
âœ… CSRF tokens for state-changing operations
âœ… Environment variables for secrets (never in code)

## Common Pitfalls & Solutions

### Problem: Token Refresh Fails
**Cause**: Refresh token expired or revoked
**Solution**: 
- Request offline access (`access_type=offline`)
- Force consent screen (`prompt=consent`)
- Handle revoked tokens gracefully (redirect to login)

### Problem: Claude Executes Wrong Action
**Cause**: Ambiguous user input or poor tool descriptions
**Solution**:
- Make tool descriptions extremely clear
- Add examples in tool schema
- Confirm destructive actions before executing
- Show Claude the current date/time in system prompt

### Problem: Timezone Confusion
**Cause**: User, server, and Google using different timezones
**Solution**:
- Store user's timezone in preferences
- Always convert to UTC for API calls
- Display times in user's timezone in UI
- Use ISO 8601 format for date strings

### Problem: Rate Limiting from Google
**Cause**: Too many API calls
**Solution**:
- Batch requests where possible
- Implement caching (cache events for 5 minutes)
- Use exponential backoff on 429 errors
- Monitor quota usage in Google Console

### Problem: Stale Conversation Context
**Cause**: Conversation history grows too large
**Solution**:
- Limit history to last 20 messages
- Summarize old context periodically
- Clear context after inactivity
- Prune function call details from history

## Success Criteria

This project is complete when:

âœ… Users can authenticate with Google Calendar via OAuth2
âœ… Tokens are stored securely (encrypted)
âœ… Users can view their calendar events
âœ… Users can create/update/delete events via natural language
âœ… Smart scheduling finds optimal meeting times
âœ… System checks for conflicts before scheduling
âœ… Multi-turn conversations work (context maintained)
âœ… User preferences are applied to scheduling
âœ… Response time is under 3 seconds
âœ… UI is polished and intuitive
âœ… Security audit passes (tokens, HTTPS, CORS)
âœ… Demo video is recorded

## Demo Script (For Client Presentations)

**Setup**: Authenticate with Google Calendar, have 3-5 existing meetings

**Demo Flow**:

1. **Authentication** (30 seconds)
   - Show login screen
   - Click "Connect Google Calendar"
   - OAuth flow completes
   - Redirects back authenticated

2. **View Schedule** (30 seconds)
   - Ask: "What's on my calendar today?"
   - Shows meetings in natural language
   - Ask: "What about this week?"
   - Shows weekly overview

3. **Create Meeting** (60 seconds)
   - Say: "Schedule a meeting with John tomorrow at 2pm for 1 hour"
   - System checks for conflicts
   - Shows: "You have a conflict at 2pm (Team Standup). Would you like me to find an alternative time?"
   - Say: "Yes, find another time"
   - Shows 3 suggested times
   - Say: "The 3pm slot works"
   - Creates meeting, sends calendar invite

4. **Smart Scheduling** (60 seconds)
   - Say: "I need to meet with Sarah for 30 minutes sometime this week"
   - System checks both calendars
   - Suggests: "I found 5 available slots when you're both free. The best option is Thursday at 10am based on your preference for morning meetings."
   - Say: "Perfect, schedule it"
   - Creates meeting with Sarah

5. **Preferences** (30 seconds)
   - Show preferences screen
   - Demonstrate: "I prefer afternoon meetings and need 15 minutes between meetings"
   - System applies these rules in future scheduling

6. **Multi-turn Context** (45 seconds)
   - Say: "What meetings do I have on Friday?"
   - Shows Friday schedule
   - Say: "Move the 2pm to 3pm"
   - System updates event
   - Say: "Actually, cancel it"
   - System deletes event

**Key Points to Emphasize**:
- Natural language (no clicking through calendar UI)
- Smart conflict detection
- Learns preferences
- Works with attendee availability
- Saves 10+ minutes per day on scheduling
- ROI: Reclaim 40+ hours/year on calendar management

## Technical Decisions & Rationale

### Why Function Calling over Prompt Engineering?
- More reliable action execution
- Structured outputs (no parsing JSON from text)
- Better error handling
- Native support in Claude API
- Easier to extend with new tools

### Why PostgreSQL over MongoDB?
- Andrew's expertise (faster development)
- ACID guarantees for token storage
- Better tooling and ORM support
- Simpler data model (relational)

### Why Store Conversation History?
- Multi-turn context ("move that meeting")
- Undo functionality ("cancel that")
- Analytics (what commands are common)
- Debugging (what went wrong)
- Minimal storage cost

### Why Not Use LangChain?
- Adds complexity for simple use case
- Direct Claude API is sufficient
- Easier to debug and customize
- Less dependencies
- Andrew wants to learn Claude API directly

## Future Enhancements (Post-MVP)

### Phase 2 Features:
- **Email integration**: Parse meeting requests from email
- **Recurring events**: "Schedule weekly standup every Monday"
- **Meeting templates**: Save common meeting types
- **Calendar analytics**: Time spent in meetings, most frequent attendees
- **Slack integration**: Schedule meetings from Slack
- **Voice input**: Speak commands instead of typing

### Phase 3 Features:
- **Meeting prep AI**: Summarize context before meetings
- **Automatic rescheduling**: Handle conflicts automatically
- **Travel time calculation**: Buffer for location changes
- **Focus time protection**: Block out deep work time
- **Meeting cost tracker**: Calculate time cost of meetings
- **Team scheduling**: Find time for multiple people at once

## Learning Resources

### OAuth2:
- Google OAuth2 docs: https://developers.google.com/identity/protocols/oauth2
- OAuth2 simplified: https://aaronparecki.com/oauth-2-simplified/

### Google Calendar API:
- Official docs: https://developers.google.com/calendar/api/guides/overview
- API reference: https://developers.google.com/calendar/api/v3/reference

### Claude Function Calling:
- Anthropic docs: https://docs.anthropic.com/claude/docs/tool-use
- Best practices: https://docs.anthropic.com/claude/docs/tool-use-best-practices

### Token Security:
- OWASP Token Storage: https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html
- AES Encryption in .NET: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes

## Context for Claude

When helping Andrew with this project:

1. **Security First**: This handles OAuth tokens - always encrypt, never log
2. **Assume Expertise**: He knows .NET, Angular, Entity Framework, PostgreSQL
3. **Be Specific**: Provide actual code, handle edge cases
4. **Production Ready**: Consider error handling, rate limits, token refresh
5. **Explain AI Concepts**: Function calling, tool definitions, intent parsing

**Andrew's Goals**:
- Learn OAuth2 implementation deeply
- Master Claude function calling
- Build production-grade API integrations
- Create another impressive portfolio piece
- Demonstrate value of AI automation to potential clients

**Current Status**: Just finished RAG project, ready to start calendar manager

**Next Steps**: Set up project structure, implement OAuth flow first
