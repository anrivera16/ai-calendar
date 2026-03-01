# Backend Unit Tests - xUnit

## Overview
Unit tests for the CalendarManager.API project using xUnit, Moq for mocking, and an in-memory database for EF Core.

## Project Setup

### 1. Create Test Project
```bash
cd CalendarManager.API
dotnet new xunit -n CalendarManager.API.Tests
cd CalendarManager.API.Tests
dotnet add reference ../CalendarManager.API.csproj
dotnet add package Moq
dotnet add package Microsoft.EntityFrameworkCore.InMemoryDatabase
dotnet add package FluentAssertions
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

### 2. Test Helper: InMemory DbContext Factory
Create `Helpers/TestDbContextFactory.cs` — a helper that creates a fresh `AppDbContext` backed by an in-memory database for each test.

---

## Test Files & Cases

---

### A. Service Tests

#### File: `Services/TokenEncryptionServiceTests.cs`
- **Encrypt_ReturnsNonEmptyBase64String**
- **Decrypt_ReturnsOriginalPlaintext** — round-trip encrypt then decrypt
- **Encrypt_ProducesDifferentCiphertexts_ForSameInput** — verifies random IV
- **Decrypt_ThrowsOnTamperedCiphertext** — modify a byte mid-ciphertext, expect failure
- **Decrypt_ThrowsOnEmptyString**
- **Encrypt_HandlesUnicodeCharacters**
- **Encrypt_HandlesLongStrings** — 10KB+ input

#### File: `Services/OAuthServiceTests.cs`
Mock: `ITokenEncryptionService`, `HttpClient` (via `HttpMessageHandler`), `AppDbContext`, `IConfiguration`, `ILogger`
- **GetAuthorizationUrl_ReturnsValidUrl_WithRequiredParams** — verify client_id, redirect_uri, scope, PKCE challenge, state in URL
- **GetAuthorizationUrl_StoresCodeVerifier_ForState**
- **StoreCodeVerifier_And_RetrieveCodeVerifier_RoundTrip**
- **RetrieveCodeVerifier_ReturnsNull_ForUnknownState**
- **ExchangeCodeForTokensAsync_ReturnsTokens_OnSuccess** — mock HTTP 200 from Google
- **ExchangeCodeForTokensAsync_ThrowsOnGoogleError** — mock HTTP 400
- **GetValidAccessTokenAsync_ReturnsExistingToken_WhenNotExpired**
- **GetValidAccessTokenAsync_RefreshesToken_WhenExpiringSoon** — token expires within 5 min
- **GetValidAccessTokenAsync_ThrowsWhenNoTokenFound**
- **RefreshAccessTokenAsync_UpdatesTokenInDb**
- **RefreshAccessTokenAsync_ThrowsOnGoogleError**
- **RevokeTokensAsync_DeletesTokensFromDb**
- **RevokeTokensAsync_HandlesRevokeEndpointFailure_Gracefully**

#### File: `Services/GoogleCalendarServiceTests.cs`
Mock: `IOAuthService`, `HttpClient`, `ILogger`
- **GetEventsAsync_ReturnsEvents_FromGoogleApi** — mock Google Calendar response
- **GetEventsAsync_ReturnsEmptyList_WhenNoEvents**
- **GetEventsAsync_HandlesAllDayEvents** — events with `date` instead of `dateTime`
- **CreateEventAsync_SendsCorrectPayload_ToGoogleApi** — verify request body
- **CreateEventAsync_ReturnsCreatedEvent**
- **UpdateEventAsync_SendsCorrectPayload** — verify PATCH/PUT body
- **UpdateEventAsync_Throws_WhenEventNotFound** — mock 404
- **DeleteEventAsync_SendsDeleteRequest**
- **DeleteEventAsync_Throws_WhenEventNotFound**
- **GetFreeBusyAsync_ReturnsBusyPeriods**
- **ValidateTokenAsync_ReturnsTrue_WhenTokenValid**
- **ValidateTokenAsync_ReturnsFalse_WhenTokenInvalid**
- **AllMethods_Handle401_ByThrowingUnauthorized** — expired/revoked token
- **AllMethods_Handle429_ByThrowingRateLimitException**

#### File: `Services/AvailabilityServiceTests.cs`
Mock: `AppDbContext` (in-memory), `IGoogleCalendarService`, `ILogger`
- **GetAvailableSlotsAsync_ReturnsSlots_WhenAvailable** — weekly rule exists, no bookings
- **GetAvailableSlotsAsync_ExcludesBookedSlots** — booking overlaps a slot
- **GetAvailableSlotsAsync_ExcludesGoogleCalendarBusyTimes** — Google Calendar shows busy
- **GetAvailableSlotsAsync_RespectsServiceDuration** — 60-min service shouldn't fit in 30-min window
- **GetAvailableSlotsAsync_ReturnsEmpty_WhenNoRulesForDay**
- **GetAvailableSlotsAsync_UsesDateOverride_InsteadOfWeeklyRule**
- **GetAvailableSlotsAsync_ExcludesBreakTimes**
- **GetAvailableSlotsAsync_HandlesMultipleRules_SameDay** — morning + afternoon shifts
- **GetRulesForDateAsync_ReturnsWeeklyRule_ForMatchingDay**
- **GetRulesForDateAsync_ReturnsDateOverride_WhenExists**
- **GetRulesForDateAsync_ReturnsEmpty_WhenNoMatch**

#### File: `Services/BookingServiceTests.cs`
Mock: `AppDbContext` (in-memory), `IGoogleCalendarService`, `IAvailabilityService`, `IEmailService`, `ILogger`
- **CreateBookingAsync_CreatesBooking_WithNewClient**
- **CreateBookingAsync_ReusesExistingClient_ByEmail**
- **CreateBookingAsync_CreatesGoogleCalendarEvent_WhenOAuthExists**
- **CreateBookingAsync_SkipsGoogleEvent_WhenNoOAuth**
- **CreateBookingAsync_SendsConfirmationEmail**
- **CreateBookingAsync_SendsBusinessNotificationEmail**
- **CreateBookingAsync_GeneratesUniqueCancellationToken**
- **CreateBookingAsync_SetsStatusToConfirmed**
- **CreateBookingAsync_HandlesRaceCondition_DbUpdateException**
- **CancelBookingAsync_SetsStatusToCancelled**
- **CancelBookingAsync_Throws_WhenBookingNotFound**
- **CancelBookingAsync_Throws_WhenAlreadyCancelled**
- **CancelBookingByTokenAsync_CancelsCorrectBooking**
- **CancelBookingByTokenAsync_Throws_WhenTokenNotFound**
- **GetBookingsAsync_FiltersBy_DateRange**
- **GetBookingsAsync_ReturnsAll_WhenNoFilters**
- **GetBookingByIdAsync_ReturnsBooking_WithServiceAndClient**
- **GetBookingByIdAsync_ReturnsNull_WhenNotFound**
- **GetBookingByTokenAsync_ReturnsBooking_WithDetails**

#### File: `Services/EmailServiceTests.cs`
Mock: `IConfiguration`, `ILogger`, SMTP client (or verify method calls)
- **SendBookingConfirmationAsync_SendsEmail_WhenEnabled**
- **SendBookingConfirmationAsync_DoesNothing_WhenDisabled** — `Email:Enabled` = false
- **SendBookingCancellationAsync_SendsEmail_WithCorrectSubject**
- **SendNewBookingNotificationAsync_SendsEmail_ToBusiness**
- **SendEmailAsync_UsesCorrectFromAddress**
- **SendEmailAsync_HandlesSmtpFailure_Gracefully** — logs error, doesn't throw
- **SendEmailAsync_SetsHtmlBody_WhenIsHtmlTrue**
- **SendEmailAsync_SetsTextBody_WhenIsHtmlFalse**

#### File: `Services/ClaudeServiceTests.cs`
Mock: `Anthropic SDK client`, `IGoogleCalendarService`, `IBookingService`, `IAvailabilityService`, `AppDbContext` (in-memory), `ILogger`
- **ProcessMessageAsync_ReturnsResponse_ForSimpleMessage**
- **ProcessMessageAsync_CreatesConversation_WhenNoConversationId**
- **ProcessMessageAsync_ReusesConversation_WhenConversationIdProvided**
- **ProcessMessageAsync_StoresUserAndAssistantMessages**
- **ProcessMessageAsync_HandlesToolCall_CreateEvent** — mock Claude returning tool_use
- **ProcessMessageAsync_HandlesToolCall_ListEvents**
- **ProcessMessageAsync_HandlesToolCall_FindFreeTime**
- **ProcessMessageAsync_ReturnsActions_WithExecutedStatus**
- **ProcessMessageAsync_HandlesClaudeApiError_Gracefully**
- **CreateCalendarEventAsync_DelegatesToGoogleCalendarService**
- **ListCalendarEventsAsync_DelegatesToGoogleCalendarService**
- **FindFreeTimeAsync_ReturnsFreeSlots**

---

### B. Controller Tests

#### File: `Controllers/AuthControllerTests.cs`
Mock: `IOAuthService`, `AppDbContext` (in-memory), `ILogger`
- **GoogleLogin_Returns200_WithAuthUrlAndState**
- **Callback_Returns200_WhenCodeValid** — creates user, exchanges code
- **Callback_Returns400_WhenCodeMissing**
- **Callback_Returns400_WhenStateMissing**
- **Callback_CreatesNewUser_WhenNotExists**
- **Callback_UpdatesExistingUser_WhenExists**
- **Status_ReturnsAuthenticated_WhenUserHasTokens**
- **Status_ReturnsUnauthenticated_WhenUserNotFound**
- **Logout_RevokesTokens_And_Returns200**
- **Logout_Returns400_WhenEmailMissing**
- **TestToken_ReturnsSuccess_WhenTokenValid**
- **TestToken_ReturnsFailure_WhenTokenExpired**

#### File: `Controllers/CalendarControllerTests.cs`
Mock: `IGoogleCalendarService`, `AppDbContext` (in-memory), `ILogger`
- **GetEvents_Returns200_WithEvents**
- **GetEvents_Returns200_WithEmptyList_WhenNoEvents**
- **GetEvents_UsesDefaultDateRange_WhenNotProvided**
- **CreateEvent_Returns201_WithCreatedEvent**
- **CreateEvent_Returns400_WhenTitleMissing**
- **CreateEvent_Returns400_WhenStartAfterEnd**
- **UpdateEvent_Returns200_WithUpdatedEvent**
- **UpdateEvent_Returns404_WhenEventNotFound**
- **DeleteEvent_Returns204_OnSuccess**
- **DeleteEvent_Returns404_WhenEventNotFound**
- **AllEndpoints_Return401_WhenNoAuthenticatedUser** — no user with OAuth tokens

#### File: `Controllers/ChatControllerTests.cs`
Mock: `IClaudeService`, `ILogger`
- **Message_Returns200_WithChatResponse**
- **Message_Returns400_WhenMessageEmpty**
- **Message_ReturnsActions_WhenToolsUsed**
- **Message_Returns500_WhenClaudeServiceThrows**
- **Health_Returns200_WithStatus**

#### File: `Controllers/AdminControllerTests.cs`
Mock: `AppDbContext` (in-memory), `IAvailabilityService`, `IBookingService`, `ILogger`

**Profile:**
- **GetProfile_Returns200_WithProfile_WhenExists**
- **GetProfile_Returns200_WithHasProfileFalse_WhenNotExists**
- **CreateProfile_Returns201_WithCreatedProfile**
- **CreateProfile_Returns400_WhenNameMissing**
- **CreateProfile_GeneratesSlug_FromBusinessName**
- **CreateProfile_Returns409_WhenProfileAlreadyExists**
- **UpdateProfile_Returns200_WithUpdatedProfile**
- **UpdateProfile_Returns404_WhenNotExists**

**Services:**
- **GetServices_Returns200_WithServiceList**
- **CreateService_Returns201_WithCreatedService**
- **CreateService_Returns400_WhenNameMissing**
- **UpdateService_Returns200_WithUpdatedService**
- **UpdateService_Returns404_WhenNotFound**
- **DeleteService_Returns200_OnSuccess**
- **DeleteService_Returns404_WhenNotFound**

**Availability:**
- **GetAvailability_Returns200_WithRules**
- **CreateWeeklyAvailability_Returns201_WithRule**
- **CreateDateOverride_Returns201_WithRule**
- **CreateBreak_Returns201_WithRule**
- **DeleteAvailability_Returns200_OnSuccess**
- **DeleteAvailability_Returns404_WhenNotFound**

**Bookings:**
- **GetBookings_Returns200_WithList**
- **GetBookings_AppliesFilters_WhenProvided**
- **GetTodaysBookings_ReturnsOnlyTodayBookings**
- **GetUpcomingBookings_RespectsLimit**
- **CancelBooking_Returns200_WithUpdatedStatus**
- **CancelBooking_Returns404_WhenNotFound**
- **CompleteBooking_Returns200_WithUpdatedStatus**
- **CompleteBooking_Returns404_WhenNotFound**

#### File: `Controllers/PublicBookingControllerTests.cs`
Mock: `AppDbContext` (in-memory), `IAvailabilityService`, `IBookingService`, `ILogger`
- **GetBusiness_Returns200_WithBusinessAndServices**
- **GetBusiness_Returns404_WhenSlugNotFound**
- **GetBusiness_Returns404_WhenBusinessInactive**
- **GetSlots_Returns200_WithAvailableSlots**
- **GetSlots_Returns400_WhenServiceIdMissing**
- **GetSlots_Returns400_WhenDateMissing**
- **CreateBooking_Returns201_WithConfirmation**
- **CreateBooking_Returns400_WhenClientNameMissing**
- **CreateBooking_Returns400_WhenClientEmailInvalid**
- **CreateBooking_Returns400_WhenNoteExceeds2000Chars**
- **CreateBooking_Returns404_WhenSlugNotFound**
- **GetBookingByToken_Returns200_WithDetails**
- **GetBookingByToken_Returns404_WhenTokenNotFound**
- **CancelBookingByToken_Returns200_OnSuccess**
- **CancelBookingByToken_Returns404_WhenTokenNotFound**

---

### C. Model / DTO Validation Tests

#### File: `Models/DtoValidationTests.cs`
- **CreateEventDto_Validates_RequiredTitle**
- **CreateEventDto_Validates_RequiredStart**
- **CreateEventDto_Validates_RequiredEnd**
- **CreateBookingRequest_Validates_RequiredClientName**
- **CreateBookingRequest_Validates_RequiredClientEmail**
- **CreateBookingRequest_Validates_EmailFormat**
- **CreateBookingRequest_Validates_ClientNameMaxLength**
- **CreateBookingRequest_Validates_ClientEmailMaxLength**
- **CreateBookingRequest_Validates_NotesMaxLength_2000**
- **CreateBookingRequest_Validates_PhoneFormat**
- **CreateBusinessProfileDto_Validates_RequiredBusinessName**
- **CreateServiceDto_Validates_RequiredName**
- **CreateServiceDto_Validates_RequiredDurationMinutes**
- **CreateWeeklyAvailabilityDto_Validates_RequiredFields**

---

### D. Database / Entity Tests

#### File: `Data/AppDbContextTests.cs`
Uses in-memory database.
- **CanCreateUser_WithRequiredFields**
- **UserEmail_MustBeUnique** — expect `DbUpdateException`
- **CreatedAt_IsAutoSet_OnInsert**
- **UpdatedAt_IsAutoSet_OnSaveChanges**
- **CascadeDelete_User_DeletesOAuthTokens**
- **CascadeDelete_User_DeletesConversations**
- **CascadeDelete_BusinessProfile_DeletesServices**
- **CascadeDelete_BusinessProfile_DeletesAvailabilityRules**
- **RestrictDelete_Service_WhenBookingsExist** — Booking → Service is Restrict
- **RestrictDelete_Client_WhenBookingsExist** — Booking → Client is Restrict
- **BookingStatus_DefaultsToConfirmed**
- **Business_Slug_MustBeUnique**

---

## Test Count Summary

| Category | File Count | Test Cases |
|----------|-----------|------------|
| Services | 7 | ~85 |
| Controllers | 5 | ~70 |
| Models/DTOs | 1 | ~14 |
| Database | 1 | ~12 |
| **Total** | **14** | **~181** |

## Running Tests
```bash
cd CalendarManager.API.Tests
dotnet test
dotnet test --verbosity normal           # detailed output
dotnet test --collect:"XPlat Code Coverage"  # with coverage
```
