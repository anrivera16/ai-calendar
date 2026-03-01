# Frontend Unit Tests - Vitest

## Overview
Unit tests for the Angular frontend (`calendar-manager-ui`) using Vitest with Angular Testing Library. These replace the default Karma/Jasmine setup.

## Project Setup

### 1. Install Dependencies
```bash
cd calendar-manager-ui
npm install -D vitest @analogjs/vitest-angular jsdom @testing-library/angular @testing-library/jest-dom
```

### 2. Configure Vitest
Create `vite.config.ts` in the `calendar-manager-ui/` root:
```ts
/// <reference types="vitest" />
import { defineConfig } from 'vite';
import angular from '@analogjs/vite-plugin-angular';

export default defineConfig({
  plugins: [angular()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['src/test-setup.ts'],
    include: ['src/**/*.spec.ts'],
    reporters: ['default'],
  },
});
```

### 3. Create Test Setup File
Create `src/test-setup.ts`:
```ts
import '@testing-library/jest-dom';
import 'zone.js';
import 'zone.js/testing';
import { getTestBed } from '@angular/core/testing';
import { BrowserDynamicTestingModule, platformBrowserDynamicTesting } from '@angular/platform-browser-dynamic/testing';

getTestBed().initTestEnvironment(BrowserDynamicTestingModule, platformBrowserDynamicTesting());
```

### 4. Add npm Script
In `package.json`:
```json
{
  "scripts": {
    "test": "vitest",
    "test:run": "vitest run",
    "test:coverage": "vitest run --coverage"
  }
}
```

---

## Test Files & Cases

---

### A. Service Tests

#### File: `src/app/services/auth.spec.ts`
Mock: `ApiService` (HttpClient calls)
- **checkAuthStatus_setsAuthenticatedTrue_whenUserHasTokens**
- **checkAuthStatus_setsAuthenticatedFalse_whenNotLoggedIn**
- **checkAuthStatus_setsLoadingDuringRequest**
- **initiateLogin_returnsAuthUrl_fromApi**
- **initiateLogin_handlesApiError**
- **logout_clearsAuthStatus**
- **logout_setsAuthenticatedToFalse**
- **testToken_returnsTrue_whenTokenValid**
- **testToken_returnsFalse_whenTokenInvalid**
- **handleCallback_callsCheckAuthStatus**
- **getCurrentUser_returnsUser_whenAuthenticated**
- **getCurrentUser_returnsNull_whenNotAuthenticated**
- **isAuthenticated_returnsBoolean_basedOnAuthStatus**
- **userSignal_updatesReactively_whenAuthStatusChanges**
- **isAuthenticatedSignal_updatesReactively**

#### File: `src/app/services/api.spec.ts`
Mock: `HttpClient` (via `HttpClientTestingModule`)
- **getAuthStatus_callsCorrectEndpoint** — GET `/api/auth/status`
- **getAuthStatus_passesUserEmailParam**
- **initiateGoogleLogin_callsCorrectEndpoint** — GET `/api/auth/google-login`
- **logout_callsCorrectEndpoint** — POST `/api/auth/logout`
- **testToken_callsCorrectEndpoint** — POST `/api/auth/test-token`
- **processChatMessage_callsCorrectEndpoint** — POST `/api/chat/message`
- **processChatMessage_sendsCorrectPayload** — message, userEmail, conversationId
- **processChatMessage_has30sTimeout**
- **getChatConversation_callsCorrectEndpoint** — GET `/api/chat/conversation/{id}`
- **getCalendarEvents_callsCorrectEndpoint** — GET `/api/calendar/events`
- **getCalendarEvents_sendsDateParams**
- **createCalendarEvent_callsCorrectEndpoint** — POST `/api/calendar/events`
- **updateCalendarEvent_callsCorrectEndpoint** — PUT `/api/calendar/events/{id}`
- **deleteCalendarEvent_callsCorrectEndpoint** — DELETE `/api/calendar/events/{id}`
- **handleError_throwsFormattedError_onApiFailure**
- **handleError_throwsGenericMessage_onNetworkError**

#### File: `src/app/services/calendar.service.spec.ts`
Mock: `ApiService`
- **loadEvents_fetchesEvents_andUpdatesSignal**
- **loadEvents_setsLoadingDuringRequest**
- **loadEvents_handlesError_gracefully**
- **loadEvents_usesDefaultDateRange_whenNoneProvided**
- **createEvent_callsApi_andAddsToLocalState**
- **createEvent_returnsNewEvent**
- **updateEvent_callsApi_andUpdatesLocalState**
- **deleteEvent_callsApi_andRemovesFromLocalState**
- **formatEventDate_formatsCorrectly_forTimedEvent**
- **formatEventDate_formatsCorrectly_forAllDayEvent**
- **events_signal_reflectsCurrentState**
- **loading_signal_reflectsCurrentState**

#### File: `src/app/services/ai-chat.service.spec.ts`
Mock: `ApiService`, `CalendarService`
- **initialState_hasGreetingMessage**
- **sendMessage_addsUserMessage_toState**
- **sendMessage_setsProcessingTrue**
- **sendMessage_callsApiWithMessage**
- **sendMessage_addsAiResponse_toState**
- **sendMessage_setsProcessingFalse_afterResponse**
- **sendMessage_handlesError_withErrorMessage**
- **sendMessage_handles35sTimeout_withTimeoutMessage**
- **handleBackendResponse_mapsMessageTypes_correctly** — info, success, warning, error
- **handleBackendResponse_handlesNumericTypes** — 0, 1, 2, 3
- **handleCalendarActions_addsSuccessMessage_forExecutedActions**
- **handleCalendarActions_addsErrorMessage_forFailedActions**
- **handleCalendarActions_refreshesCalendar_onEventCreation**
- **clearChat_removesAllMessages**
- **messages_signal_reflectsCurrentState**

#### File: `src/app/services/admin-api.service.spec.ts`
Mock: `HttpClient` (via `HttpClientTestingModule`)

**Profile:**
- **getProfile_callsCorrectEndpoint** — GET `/api/admin/profile`
- **createProfile_callsCorrectEndpoint** — POST `/api/admin/profile`
- **updateProfile_callsCorrectEndpoint** — PUT `/api/admin/profile`

**Services:**
- **getServices_callsCorrectEndpoint** — GET `/api/admin/services`
- **createService_callsCorrectEndpoint** — POST `/api/admin/services`
- **updateService_callsCorrectEndpoint** — PUT `/api/admin/services/{id}`
- **deleteService_callsCorrectEndpoint** — DELETE `/api/admin/services/{id}`

**Availability:**
- **getAvailability_callsCorrectEndpoint** — GET `/api/admin/availability`
- **createWeeklyAvailability_callsCorrectEndpoint** — POST `/api/admin/availability/weekly`
- **createDateOverride_callsCorrectEndpoint** — POST `/api/admin/availability/override`
- **createBreak_callsCorrectEndpoint** — POST `/api/admin/availability/break`
- **deleteAvailabilityRule_callsCorrectEndpoint** — DELETE `/api/admin/availability/{id}`

**Bookings:**
- **getBookings_callsCorrectEndpoint** — GET `/api/admin/bookings`
- **getBookings_sendsFilterParams** — status, fromDate, toDate
- **getTodaysBookings_callsCorrectEndpoint** — GET `/api/admin/bookings/today`
- **getUpcomingBookings_callsCorrectEndpoint** — GET `/api/admin/bookings/upcoming`
- **getUpcomingBookings_sendsLimitParam**
- **cancelBooking_callsCorrectEndpoint** — PUT `/api/admin/bookings/{id}/cancel`
- **completeBooking_callsCorrectEndpoint** — PUT `/api/admin/bookings/{id}/complete`

#### File: `src/app/services/public-booking-api.service.spec.ts`
Mock: `HttpClient` (via `HttpClientTestingModule`)
- **getBusinessBySlug_callsCorrectEndpoint** — GET `/api/book/{slug}`
- **getAvailableSlots_callsCorrectEndpoint** — GET `/api/book/{slug}/slots`
- **getAvailableSlots_sendsServiceIdAndDateParams**
- **createBooking_callsCorrectEndpoint** — POST `/api/book/{slug}`
- **createBooking_sendsCorrectPayload**
- **getBookingByToken_callsCorrectEndpoint** — GET `/api/book/manage/{token}`
- **cancelBooking_callsCorrectEndpoint** — POST `/api/book/manage/{token}/cancel`

---

### B. Guard Tests

#### File: `src/app/guards/auth.guard.spec.ts`
Mock: `AuthService`, `Router`
- **allows_navigation_whenAuthenticated**
- **redirectsToLogin_whenNotAuthenticated**
- **waitsForLoading_beforeDeciding** — filters until `loading$` is false
- **usesIsAuthenticated$_observable**

---

### C. Component Tests

#### File: `src/app/app.spec.ts`
Mock: `AuthService`, `Router`
- **creates_theComponent**
- **ngOnInit_handlesOAuthCallback_whenCodeInUrl**
- **ngOnInit_showsError_whenErrorInUrl**
- **ngOnInit_doesNothing_whenNoCallbackParams**

#### File: `src/app/components/login/login.spec.ts`
Mock: `AuthService`
- **renders_loginButton**
- **renders_featureList**
- **showsSpinner_whenLoading**
- **callsOnLogin_whenButtonClicked**
- **onLogin_initiatesGoogleLogin**
- **redirectsToAuthUrl_onLoginSuccess**
- **handlesLoginError_gracefully**

#### File: `src/app/components/dashboard/dashboard.spec.ts`
Mock: `AuthService`, `CalendarService`, `AiChatService`, `Router`
- **creates_theComponent**
- **renders_calendarView_andChatPanel**
- **renders_userDisplayName_inHeader**
- **renders_adminLink**
- **onLogout_callsAuthService** — with confirmation
- **onSendMessage_callsChatService**
- **onLoadCalendar_refreshesEvents**
- **detectsMobileScreen_andShowsToggle**
- **setActivePanel_togglesBetween_chatAndCalendar** — mobile only
- **highlightsEvent_for3Seconds_afterCreation**
- **loadsCalendarEvents_onInit** — 6 months past/future

#### File: `src/app/components/dashboard/calendar/calendar-view.spec.ts`
No mocks needed (pure presentation component).
- **renders_monthAndYearHeader**
- **renders_weekdayHeaders**
- **renders42Days_inGrid**
- **highlightsToday**
- **marksCurrentMonth_vsOtherMonthDays**
- **previousMonth_navigatesBack**
- **nextMonth_navigatesForward**
- **goToToday_returnsToCurrentMonth**
- **displaysEvents_onCorrectDates**
- **emitsDaySelected_onDayClick**
- **emitsEventSelected_onEventClick**
- **highlightsEvent_whenHighlightedEventIdMatches**
- **formatsEventTime_correctly**
- **showsLoadingState**

#### File: `src/app/components/dashboard/chat/chat-panel.spec.ts`
No mocks needed (pure presentation component).
- **renders_messagesList**
- **renders_inputField_andSendButton**
- **distinguishes_userVsAiMessages** — CSS classes
- **emitsSendMessage_onButtonClick**
- **emitsSendMessage_onEnterKey**
- **doesNotSend_emptyMessage**
- **disablesSendButton_whenProcessing**
- **showsProcessingIndicator_whenProcessing**
- **autoScrolls_toBottom_onNewMessage**
- **appliesCorrectClass_forMessageType** — info, success, error, warning

#### File: `src/app/components/admin/admin-dashboard.spec.ts`
Mock: `AdminApiService`, `Router`
- **creates_theComponent**
- **loadsProfile_onInit**
- **showsSetupPrompt_whenNoProfile**
- **showsProfileInfo_whenProfileExists**
- **loadsTodaysBookings**
- **loadsUpcomingBookings**
- **copyBookingLink_copiesToClipboard**
- **showsAdminNavLinks** — setup, services, availability, bookings
- **formatsTime_correctly**
- **formatsDate_correctly**

#### File: `src/app/components/admin/business-setup.spec.ts`
Mock: `AdminApiService`, `Router`
- **creates_theComponent**
- **loadsExistingProfile_onInit**
- **showsCreateForm_whenNoProfile**
- **showsEditForm_whenProfileExists**
- **populatesForm_withExistingData**
- **onSubmit_createsProfile_whenNew**
- **onSubmit_updatesProfile_whenEditing**
- **showsValidationErrors_whenFieldsMissing**
- **onCancel_navigatesToAdminDashboard**
- **showsLoadingState_whileSaving**

#### File: `src/app/components/admin/service-manager.spec.ts`
Mock: `AdminApiService`
- **creates_theComponent**
- **loadsServices_onInit**
- **rendersServiceList**
- **openCreateModal_showsEmptyForm**
- **openEditModal_populatesFormWithServiceData**
- **closeModal_hidesForm**
- **onSubmit_createsNewService**
- **onSubmit_updatesExistingService**
- **confirmDelete_showsConfirmation**
- **deleteService_removesFromList**
- **cancelDelete_hidesConfirmation**
- **formatsPrice_asCurrency**
- **formatsDuration_correctly** — "1h 30m"
- **showsColorOptions**

#### File: `src/app/components/admin/availability-manager.spec.ts`
Mock: `AdminApiService`
- **creates_theComponent**
- **loadsAvailabilityRules_onInit**
- **initializesWeeklySchedule_forAll7Days**
- **syncsScheduleFromRules_whenLoaded**
- **saveWeeklySchedule_callsApi**
- **toggleOverrideForm_showsAndHidesForm**
- **saveOverride_callsApi_andReloads**
- **toggleBreakForm_showsAndHidesForm**
- **saveBreak_callsApi_andReloads**
- **deleteRule_callsApi_andReloads**
- **getWeeklyRules_filtersCorrectly**
- **getOverrideRules_filtersCorrectly**
- **getBreakRules_filtersCorrectly**
- **showsSuccessMessage_for3Seconds**

#### File: `src/app/components/admin/booking-list.spec.ts`
Mock: `AdminApiService`
- **creates_theComponent**
- **loadsBookings_onInit**
- **rendersBookingTable**
- **applyFilters_reloadsWithFilters**
- **clearFilters_resetsAndReloads**
- **confirmCancel_showsConfirmationDialog**
- **cancelBooking_callsApi_andReloads**
- **completeBooking_callsApi_andReloads**
- **showsActionLoading_forSpecificBooking**
- **formatsDateTime_correctly**
- **showsStatusFilter_dropdown**
- **showsDateRange_filter**

#### File: `src/app/components/booking/booking-page.spec.ts`
Mock: `PublicBookingApiService`, `ActivatedRoute`
- **creates_theComponent**
- **loadsBusinessBySlug_onInit**
- **showsError_whenBusinessNotFound**
- **showsServiceSelection_onStep1**
- **selectService_advancesToDateStep**
- **showsDateSelection_onStep2**
- **selectDate_loadsSlots_andAdvancesToTimeStep**
- **showsSlots_onStep3**
- **selectSlot_advancesToDetailsStep**
- **showsClientForm_onStep4**
- **validates_requiredClientName**
- **validates_requiredClientEmail**
- **validates_emailFormat**
- **submitBooking_callsApi_andShowsConfirmation**
- **goBackToTime_returnsToStep3**
- **goBackToDate_returnsToStep2**
- **goBackToService_returnsToStep1**
- **canSubmit_returnsFalse_whenFieldsMissing**
- **showsLoadingSpinner_whileLoadingSlots**
- **showsStepIndicator_withCurrentStep**
- **generatesAvailableDates_for30Days**

#### File: `src/app/components/booking/booking-manage.spec.ts`
Mock: `PublicBookingApiService`, `ActivatedRoute`
- **creates_theComponent**
- **loadsBooking_byToken_onInit**
- **showsBookingDetails**
- **showsError_whenTokenNotFound**
- **cancelBooking_callsApi_andUpdatesUI**
- **showsCancelSuccess_message**
- **disablesCancelButton_whileCancelling**
- **formatsDateTime_correctly**
- **appliesCorrectStatusClass**

---

## Test Count Summary

| Category | File Count | Test Cases |
|----------|-----------|------------|
| Services | 6 | ~82 |
| Guards | 1 | ~4 |
| Components | 11 | ~124 |
| **Total** | **18** | **~210** |

## Running Tests
```bash
cd calendar-manager-ui
npm test           # watch mode
npm run test:run   # single run
npm run test:coverage  # with coverage report
```
