# AI Calendar Manager - TODO

## Phases 1-4: COMPLETED

Phases 1-4 (Database, Public Booking, Admin Dashboard, Notifications & AI) are complete. See git history for details.

---

## Phase 5: Fix Authentication — COMPLETED ✓

> Google OAuth login is working end-to-end. Users can log in, reach the calendar screen, and use the AI chat without auth issues.

- [x] **5.1** Debug and fix the Google OAuth callback flow end-to-end
- [x] **5.2** Remove all Console.WriteLine debug statements from OAuthService
- [x] **5.3** Implement JWT session auth to replace demo mode

---

## Phase 6: Complete Core Features

> Features that are partially built — the database schema and service plumbing exist but aren't wired up.

- [x] **6.1** Implement conversation history persistence for AI chat
  - **Problem**: Chat works per-request but doesn't remember previous messages. The `Conversation` and `Message` entities exist in the DB (created in Phase 1 migration) but are never read from or written to.
  - **Files modified**:
    - `CalendarManager.API/Services/Implementations/ClaudeService.cs`:
      - Added `UserContext` record to pass user info via method parameters (thread-safe)
      - Updated `ProcessMessageAsync()` to load conversation history from DB (up to 20 messages)
      - After getting Claude's response, saves both user and assistant messages to the `Message` table
      - Updates conversation title and timestamp
    - `CalendarManager.API/Controllers/ChatController.cs`:
      - Updated `ProcessMessage()` to create new Conversation if none provided, validate existing conversations
      - Implemented `GetConversation()` to load messages for a given conversationId
      - Added `GetConversations()` endpoint to list all user's conversations
      - Added `DeleteConversation()` endpoint to delete a conversation
    - `calendar-manager-ui/src/app/services/ai-chat.service.ts`:
      - Added `startNewConversation()` method
      - Added `loadConversation()` to load past conversations
      - Added `getConversations()` and `deleteConversation()` methods
    - `calendar-manager-ui/src/app/services/api.ts`:
      - Added `getChatConversations()` and `deleteChatConversation()` API methods
    - `calendar-manager-ui/src/app/components/dashboard/chat/chat-panel.ts`:
      - Added `onNewConversation()` method and "New" button in header
    - `calendar-manager-ui/src/app/components/dashboard/chat/chat-panel.html` & `.scss`:
      - Added "New Conversation" button in chat header
  - **Database entities already exist**:
    - `Conversation`: Id, UserId, Title, CreatedAt, UpdatedAt
    - `Message`: Id, ConversationId, Role (user/assistant), Content, Timestamp
  - **Acceptance criteria**: ✅ User sends a message, it gets saved to DB. Claude receives the full conversation history (up to 20 messages) for context. User can continue a conversation across page reloads. `GetConversation` endpoint returns real message history. `GetConversations` lists all conversations.

- [x] **6.2** Fix thread safety in ClaudeService user context
  - **Problem**: `ClaudeService` stored request-specific data in instance fields: `_currentUserId`, `_currentUserDbId`, `_currentBusinessProfileId`. These were set in `ProcessWithClaudeAsync()` and used by all Execute* methods.
  - **File**: `CalendarManager.API/Services/Implementations/ClaudeService.cs`
  - **Solution**: Created a `UserContext` record with `UserId`, `UserDbId`, `BusinessProfileId`. Passed it as a parameter through `ProcessWithClaudeAsync()` -> `CallClaudeAPIAsync()` -> `ExecuteToolAsync()` and all the Execute* helper methods. Removed the three instance fields.
  - **Acceptance criteria**: ✅ No mutable instance fields storing per-request user data. All user context flows via method parameters.

---

## Phase 7: Production Hardening

> Security and reliability improvements needed before deploying for real users.

- [ ] **7.1** Add Redis for distributed PKCE verifier storage
  - **Problem**: PKCE code verifiers are stored in a static `ConcurrentDictionary` at `OAuthService.cs` line 22. This means: (1) verifiers are lost on server restart, (2) multi-instance deployments can't share state, (3) expired entries are never cleaned up (memory leak over time).
  - **File**: `CalendarManager.API/Services/Implementations/OAuthService.cs`
  - **What to do**:
    1. Add `Microsoft.Extensions.Caching.StackExchangeRedis` NuGet package.
    2. Register `AddStackExchangeRedisCache()` in `Program.cs`.
    3. Inject `IDistributedCache` into `OAuthService`.
    4. Replace `_codeVerifiers` ConcurrentDictionary with Redis get/set operations.
    5. Use `DistributedCacheEntryOptions` with `AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)` (matching current 10-min expiry at line 311).
    6. `StoreCodeVerifier()` (line 306-316): serialize `(codeVerifier, expiresAt)` to JSON, store with key `pkce:{state}`.
    7. `RetrieveCodeVerifier()` (line 318-347): get from Redis by key, deserialize, validate expiry, delete after retrieval.
  - **Fallback**: If Redis isn't available in dev, keep `IDistributedCache` with the in-memory provider (`AddDistributedMemoryCache()`) as a dev fallback.
  - **Acceptance criteria**: PKCE verifiers survive server restarts. No static ConcurrentDictionary. Verifiers auto-expire via Redis TTL.

- [ ] **7.2** Add DataAnnotation validation to all DTOs
  - **Problem**: Only `CreateBookingDto` has validation attributes (`[Required]`, `[MaxLength]`, `[EmailAddress]` at lines 40-46 of `BookingDto.cs`). All other DTOs accept anything the client sends.
  - **Directory**: `CalendarManager.API/Models/DTOs/`
  - **Files to update (10 DTO files)**:
    - `CreateEventDto.cs` — Add `[Required]` to Title, Start, End. Add `[StringLength(200)]` to Title. Validate End > Start.
    - `ServiceDto.cs` — `CreateServiceDto` needs `[Required]` on Name, Duration. `[Range(1, 480)]` on DurationMinutes. `[Range(0, 10000)]` on Price.
    - `BusinessProfileDto.cs` — `CreateBusinessProfileDto` needs `[Required]` on BusinessName. `[StringLength(100)]` on BusinessName, `[StringLength(500)]` on Description.
    - `AvailabilityRuleDto.cs` — Create DTOs need `[Required]` on DayOfWeek, StartTime, EndTime. Validate EndTime > StartTime.
    - `ChatMessageDto.cs` — `ChatMessageRequest` needs `[Required]` on Message. `[StringLength(5000)]` to prevent abuse.
  - **Note**: Controllers already have `[ApiController]` attribute which enables automatic model validation (returns 400 on invalid models). Just needs the attributes on the DTOs.
  - **Acceptance criteria**: All public-facing create/update DTOs have `[Required]`, `[StringLength]`, `[Range]`, and `[EmailAddress]` where appropriate. Invalid requests return 400 with field-level error messages.

- [ ] **7.3** Add Polly retry policies for Google Calendar API calls
  - **Problem**: `GoogleCalendarService` makes HTTP calls to Google Calendar API with no retry logic. If Google returns 429 (rate limit) or a transient 5xx error, the request just fails.
  - **Files**:
    - `CalendarManager.API/Services/Implementations/GoogleCalendarService.cs` — uses `HttpClient` for all Google API calls.
    - `CalendarManager.API/Program.cs` — needs Polly policy registration.
  - **What to do**:
    1. Add `Microsoft.Extensions.Http.Polly` NuGet package.
    2. In `Program.cs`, register GoogleCalendarService's `HttpClient` with a typed client and Polly policies:
       - Retry 3 times with exponential backoff (2s, 4s, 8s) on transient errors (5xx, 408, 429).
       - Circuit breaker: break after 5 consecutive failures, stay open for 30 seconds.
    3. Handle `429 Too Many Requests` specifically — respect Google's `Retry-After` header if present.
  - **Acceptance criteria**: Transient Google API failures retry automatically. Sustained failures trigger circuit breaker. No unhandled 429 responses crash the booking flow.

- [ ] **7.4** Verify encryption key is loaded from configuration (not hardcoded)
  - **Problem**: `TokenEncryptionService.cs` has a TODO comment at lines 13-17 about loading the encryption key from config. However, the actual implementation at lines 19-30 already reads from `IConfiguration`. Verify this works correctly.
  - **File**: `CalendarManager.API/Services/Implementations/TokenEncryptionService.cs`
  - **What to do**:
    1. Read the constructor (lines 19-30) and confirm it reads `Authentication:Encryption:Key` from config.
    2. Check `Program.cs` to confirm the `ENCRYPTION_KEY` env var is mapped to this config path (it's at lines 23-27).
    3. Remove the TODO comment at lines 13-17 since the implementation is done.
    4. Add a startup validation check: if ENCRYPTION_KEY is missing or not 32 bytes, throw immediately on startup rather than failing later on first encrypt/decrypt.
  - **Acceptance criteria**: TODO comment removed. App fails fast on startup if encryption key is missing or invalid.

---

## Phase 8: UI/UX Redesign

> A full redesign plan exists at `plans/ui-ux-sleek-redesign.md`. Move from gradient-heavy to a minimal Linear/Vercel aesthetic.

- [ ] **8.1** Redesign the app shell and layout
  - **Reference**: Read `plans/ui-ux-sleek-redesign.md` for the full design spec.
  - **Current state**: Dashboard has heavy purple gradients, floating container, separate chat/calendar boxes with their own headers.
  - **Target**: Full-viewport seamless split view. Calendar takes 60% left, chat takes 40% right, separated by a single 1px border. Minimal top nav bar with app name and user actions.
  - **Color palette**: Background `#F9FAFB`, surfaces `#FFFFFF`, primary text `#111827`, secondary `#6B7280`, accent `#2563EB` or `#000000`.
  - **Typography**: System font stack (Inter, SF, Segoe UI). `600` weight headers, `400` body.
  - **Files**: `dashboard.ts` and its SCSS/CSS, `chat-panel.ts`, `calendar-view.ts`, global styles.

- [ ] **8.2** Redesign the calendar component
  - **Current state**: Monthly grid with full-cell background fills for today/selected dates. Heavy look.
  - **Target**: Clean borderless grid or subtle 1px lines. Today = black circle behind date number with white text. Selected = light gray circle. Events shown as rounded pills or subtle dots.
  - **Files**: `calendar-view.ts` and its styles.

- [ ] **8.3** Redesign the chat panel
  - **Current state**: Traditional chat bubbles with strong background colors.
  - **Target**: Feed-style messages (no bubbles). User messages right-aligned with subtle background, assistant messages left-aligned plain. Minimal input bar at bottom.
  - **Files**: `chat-panel.ts` and its styles.

- [ ] **8.4** Add day and week calendar view modes
  - **Current state**: Only monthly grid view exists.
  - **What to do**: Add a view mode toggle (Month / Week / Day). Consider integrating FullCalendar.js or building custom views. Week view should show 7-day columns with hourly rows. Day view should show single-day hourly schedule.
  - **Files**: `calendar-view.ts`, potentially new component files.

- [ ] **8.5** Make admin dashboard responsive for mobile
  - **Current state**: Public booking page has mobile media queries at 600px breakpoint. Admin dashboard likely doesn't.
  - **What to do**: Add responsive breakpoints to all admin components: `admin-dashboard`, `service-manager`, `availability-manager`, `booking-list`, `business-setup`. Tables should collapse to card layouts on mobile. Forms should stack vertically.
  - **Files**: All files under `calendar-manager-ui/src/app/components/admin/`.

---

## Phase 9: Testing

> Backend unit tests exist (XUnit + Moq). Frontend tests are boilerplate. E2E tests exist but most fail on auth.

- [ ] **9.1** Fix E2E tests by adding test auth support
  - **Problem**: Playwright E2E tests exist but most fail because the frontend auth is mocked/hardcoded and doesn't support a test login flow. See `plans/e2e-test-status.md` for the full breakdown.
  - **Test status summary**:
    - `e2e/auth.spec.ts`: 4 passed, 4 failed (demo mode dashboard/admin access rejected by guard)
    - `e2e/dashboard.spec.ts`: Depends on auth working
    - `e2e/chat.spec.ts`: Depends on auth + Chat API
    - All admin tests: Depend on auth
    - Public booking tests: Should work independently (no auth)
  - **What to do**: After Phase 5 (real auth) is done, add a `/api/auth/test-login` endpoint that's only available when `ASPNETCORE_ENVIRONMENT=Development`. This endpoint accepts an email, creates/finds the user, and returns a JWT — no Google OAuth needed. Update Playwright test helpers to use this endpoint. Alternatively, mock auth at the Angular level in test setup.
  - **Files**: `AuthController.cs` (new endpoint), `calendar-manager-ui/e2e/helpers/`, test spec files.
  - **Acceptance criteria**: All E2E tests pass in CI. Auth is properly mocked or bypassed in test environment only.

- [ ] **9.2** Verify and fix existing backend unit tests
  - **Problem**: Unit tests exist in `CalendarManager.API.Tests/` but haven't been verified recently. Some may be broken after recent code changes.
  - **Test files**: `BookingServiceTests.cs`, `ClaudeServiceTests.cs`, `AvailabilityServiceTests.cs`, `GoogleCalendarServiceTests.cs`, `OAuthServiceTests.cs`, `EmailServiceTests.cs`, plus controller test files.
  - **What to do**: Run `dotnet test` in the `CalendarManager.API.Tests/` directory. Fix any compilation errors or assertion failures. Update mocks if service signatures changed.
  - **Acceptance criteria**: `dotnet test` passes with 0 failures.

- [ ] **9.3** Add frontend unit tests with Vitest
  - **Problem**: Vitest is configured in the Angular project but tests are boilerplate only (auto-generated `dashboard.spec.ts` etc.).
  - **What to test (priority order)**:
    1. `PublicBookingApiService` — mock HTTP calls, verify slot fetching and booking creation
    2. `AdminApiService` — mock HTTP calls, verify CRUD operations
    3. `BookingPageComponent` — test the 5-step flow state machine (service select -> date -> slot -> details -> confirm)
    4. `AuthService` — test auth state management (will need updating after Phase 5)
    5. `AuthGuard` — test redirect behavior
  - **Files**: Create test files alongside each service/component (e.g., `public-booking-api.service.spec.ts`).
  - **Acceptance criteria**: At least 10 meaningful frontend unit tests covering the booking flow and API services.

---

## Phase 10: Future Features (Backlog)

> Lower priority. Build these after the core product is solid and deployed.

- [ ] **10.1** Recurring events support — extend `CreateEventDto` with RRULE (iCal format), update Claude `create_calendar_event` tool
- [ ] **10.2** Event reminders — background worker service that sends email reminders N hours before bookings
- [ ] **10.3** Booking page customization — let businesses set logo, brand colors, welcome message on their `/book/{slug}` page. Extend `BusinessProfile` entity.
- [ ] **10.4** Stripe payment integration — collect payment at booking time for paid services. Add `StripePaymentIntentId` to `Booking` entity.
- [ ] **10.5** Multi-calendar support — Microsoft Graph API for Outlook, CalDAV for Apple Calendar. Abstract `IGoogleCalendarService` into `ICalendarProvider` interface.
- [ ] **10.6** Team scheduling — multi-person free/busy checking, group availability grid, meeting polls
- [ ] **10.7** Meeting analytics dashboard — aggregate booking stats, busiest days/times, revenue tracking, charts

---

## Key Architecture Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| User model | User = Business Owner (1:1) | Keeps auth simple, no multi-tenancy |
| Availability source | Business rules + Google Calendar free/busy | Catches personal events that block time |
| Booking → Calendar | Creates real Google Calendar event | Business sees it in their calendar app, auto-blocks future slots |
| Client auth | Cancellation token (no login) | Clients don't need accounts — industry standard (Calendly, Cal.com) |
| DI lifetime | All services Scoped | One instance per HTTP request, safe for DB context sharing |
| AI model | Claude Sonnet (line 191 of ClaudeService) | Balance of speed and quality for tool calling |
