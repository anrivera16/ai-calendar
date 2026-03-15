# AI Calendar Manager - Next Steps

A prioritized roadmap of what needs to be done, based on the current state of the project.

## Priority 1: Fix What's Broken

These are blockers — the app can't function end-to-end without them.

### 1.1 Fix Google OAuth Integration
The OAuth flow is implemented (PKCE, token refresh, encryption) but has integration bugs preventing real authentication from working. Recent commits confirm this is an active issue.

**What needs to happen:**
- Debug the OAuth callback flow end-to-end (auth code exchange, token storage, redirect back to frontend)
- Fix CORS issues between frontend and backend during the OAuth dance
- Verify token refresh works when tokens expire
- Remove `Console.WriteLine` debug statements in `OAuthService.cs` and replace with `ILogger`

**Files:** `AuthController.cs`, `OAuthService.cs`, `auth.ts` (frontend)

### 1.2 Replace Demo Mode with Real Authentication
The admin dashboard currently bypasses auth with a hardcoded `test@example.com` user. This needs to be replaced with real session/JWT auth tied to the OAuth login.

**What needs to happen:**
- Wire up the authenticated Google user to admin endpoints (replace hardcoded email lookup)
- Implement JWT or cookie-based session that persists after OAuth callback
- Update the Angular `AuthGuard` to validate real sessions, not just check a flag
- Remove the `DemoMode` configuration switch from `AdminController.cs`

**Files:** `AdminController.cs`, `ChatController.cs`, `CalendarController.cs`, `auth.ts`, `auth.guard.ts`

---

## Priority 2: Complete Core Features

These are implemented partially — the plumbing exists but they need to be finished.

### 2.1 Conversation History Persistence
Chat works per-request but doesn't remember previous messages. The `Conversation` and `Message` entities exist in the database but are never written to or read from.

**What needs to happen:**
- Save each user message and Claude response to the `Message` table
- Load conversation history and include it in Claude API calls for multi-turn context
- Implement the `GetConversation` endpoint in `ChatController.cs` (currently returns empty)
- Add a "new conversation" button in the chat UI

**Files:** `ClaudeService.cs`, `ChatController.cs`, `chat-panel.ts`

### 2.2 Thread Safety in ClaudeService
`ClaudeService` stores `_currentUserId` and `_currentUserDbId` as instance fields. If the service is registered as scoped this works, but it's fragile.

**What needs to happen:**
- Pass user context as method parameters instead of instance fields
- Or confirm the service is strictly scoped (one instance per request)

**Files:** `ClaudeService.cs`

---

## Priority 3: Production Readiness

Things that need to happen before this can be deployed for real users.

### 3.1 Distributed Session Storage
PKCE verifiers are stored in an in-memory `ConcurrentDictionary`. This breaks on server restart or multi-instance deployments.

**What needs to happen:**
- Add Redis (or another distributed cache)
- Move PKCE verifier storage to Redis with TTL expiration
- Consider using Redis for session storage too

**Files:** `OAuthService.cs`, `Program.cs`

### 3.2 Input Validation on DTOs
Most DTOs (`CreateEventDto`, booking request models) lack `[Required]`, `[StringLength]`, and other DataAnnotation attributes. The backend trusts whatever the frontend sends.

**What needs to happen:**
- Add validation attributes to all public-facing DTOs
- Enable `[ApiController]` model validation (likely already on, just needs the attributes)
- Validate email formats, string lengths, date ranges

**Files:** `Models/DTOs/` directory

### 3.3 Encryption Key Configuration
`TokenEncryptionService.cs` has a TODO comment about getting the encryption key from configuration. Verify this is actually reading from environment variables and not using a fallback/hardcoded key.

**Files:** `TokenEncryptionService.cs`, `Program.cs`

### 3.4 Google API Rate Limiting
No retry/backoff logic for Google Calendar API calls. If Google rate-limits the app, requests just fail.

**What needs to happen:**
- Add Polly retry policies with exponential backoff to the Google Calendar HTTP client
- Handle `429 Too Many Requests` responses gracefully

**Files:** `GoogleCalendarService.cs`, `Program.cs`

---

## Priority 4: UI/UX Improvements

A redesign plan already exists in `plans/ui-ux-sleek-redesign.md`. Key items:

### 4.1 Sleek Redesign
Move from the current gradient-heavy look to a minimal Linear/Vercel aesthetic. The plan covers:
- Clean app shell with seamless split view (calendar 60% / chat 40%)
- Minimal top nav, subtle borders instead of shadows
- Calendar: borderless grid, circle date indicators, clean event pills
- Chat: feed-style messages instead of colored bubbles
- System font stack (Inter, SF, Segoe UI)

### 4.2 Calendar View Modes
Currently only shows a monthly grid. Add day and week views for more detail.

### 4.3 Mobile Responsive Admin
The public booking page is mobile-friendly, but the admin dashboard likely needs responsive work.

---

## Priority 5: Testing

### 5.1 Fix E2E Tests
Playwright E2E tests exist but most fail because the auth system doesn't support demo/test mode properly. See `plans/e2e-test-status.md` for the full breakdown.

**Options:**
- Add a `/api/auth/demo-login` endpoint for test environments
- Or mock auth in Playwright tests
- Or add demo mode support to the Angular auth service via localStorage

### 5.2 Expand Unit Tests
Backend unit tests exist (XUnit + Moq) covering services and controllers. Frontend tests are boilerplate only.

**What needs to happen:**
- Review existing backend tests — make sure they actually run and pass
- Add frontend unit tests (Vitest is configured but unused)
- Target the booking flow and availability calculation as the highest-value test areas

---

## Priority 6: Future Features

Lower priority — nice to have after the core product is solid.

| Feature | Effort | Notes |
|---------|--------|-------|
| Recurring events | Medium | Extend `CreateEventDto` with RRULE support |
| Event reminders/notifications | Medium | Background worker + email/push |
| Multi-calendar support (Outlook, Apple) | High | Microsoft Graph API, CalDAV |
| Team scheduling | High | Multi-person free/busy, group availability |
| Meeting analytics dashboard | Medium | Aggregate stats, charts |
| Booking page customization (colors, logo) | Low | Extend `BusinessProfile` entity |
| Stripe payment integration | Medium | Collect payment at booking time |

---

## Recommended Order of Work

1. Fix OAuth (1.1) — everything depends on real auth working
2. Replace demo mode (1.2) — makes the app actually usable
3. Conversation history (2.1) — biggest missing feature for the AI experience
4. Input validation (3.2) — quick win for security
5. E2E test fixes (5.1) — unlock automated testing
6. UI redesign (4.1) — visual polish
7. Everything else as time allows
