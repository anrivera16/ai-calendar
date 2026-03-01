# E2E Tests - Playwright

## Overview
End-to-end tests covering full user flows through the application using Playwright. Tests run against the real frontend + backend (with a test database).

## Project Setup

### 1. Install Playwright
```bash
cd calendar-manager-ui
npm install -D @playwright/test
npx playwright install
```

### 2. Configure Playwright
Create `playwright.config.ts` in the project root (`calendar-manager-ui/`):
```ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  retries: 1,
  workers: 1,
  reporter: 'html',
  timeout: 30_000,
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
    { name: 'mobile-chrome', use: { ...devices['Pixel 5'] } },
  ],
  webServer: [
    {
      command: 'cd ../CalendarManager.API && dotnet run',
      url: 'http://localhost:5047/health',
      timeout: 30_000,
      reuseExistingServer: true,
    },
    {
      command: 'ng serve',
      url: 'http://localhost:4200',
      timeout: 30_000,
      reuseExistingServer: true,
    },
  ],
});
```

### 3. Add npm Scripts
```json
{
  "scripts": {
    "e2e": "playwright test",
    "e2e:ui": "playwright test --ui",
    "e2e:headed": "playwright test --headed",
    "e2e:report": "playwright show-report"
  }
}
```

### 4. Test Helpers
Create `e2e/helpers/test-data.ts` — factory functions for creating test business profiles, services, availability rules, and bookings via API calls for test setup/teardown.

Create `e2e/helpers/auth.ts` — helper to set up a demo-mode authenticated session (since the app supports `DEMO_MODE=true` with a hardcoded user).

---

## Test Files & Cases

---

### A. Authentication Flow

#### File: `e2e/auth.spec.ts`

**Login Page:**
- **shows_loginPage_whenNotAuthenticated** — navigate to `/`, expect redirect to `/login`
- **renders_googleLoginButton**
- **renders_featureDescriptions**
- **protectedRoutes_redirectToLogin** — `/dashboard`, `/admin/dashboard` redirect to `/login`

**Demo Mode Auth (since Google OAuth can't be tested in e2e):**
- **dashboard_isAccessible_inDemoMode** — with demo mode user
- **adminDashboard_isAccessible_inDemoMode**
- **logout_redirectsToLogin**
- **logout_preventsAccessToDashboard**

---

### B. Dashboard & Calendar

#### File: `e2e/dashboard.spec.ts`

Precondition: Authenticated via demo mode.

**Layout:**
- **renders_splitLayout_calendar_andChat** — calendar on left, chat on right
- **renders_headerWithUserName**
- **renders_adminLink_inHeader**
- **renders_logoutButton**

**Calendar:**
- **calendar_showsCurrentMonth**
- **calendar_highlightsToday**
- **calendar_navigatesToPreviousMonth**
- **calendar_navigatesToNextMonth**
- **calendar_goToTodayButton_returnsToCurrentMonth**
- **calendar_displaysEvents_onCorrectDates** — pre-seed events via API
- **calendar_clickOnDay_selectsDay**
- **calendar_clickOnEvent_selectsEvent**

**Mobile Responsive:**
- **mobile_showsToggleButton** — viewport set to mobile
- **mobile_togglesBetween_calendarAndChat**
- **mobile_defaultsToCalendarView**

---

### C. AI Chat

#### File: `e2e/chat.spec.ts`

Precondition: Authenticated via demo mode.

**Basic Chat:**
- **chat_showsGreetingMessage_onLoad**
- **chat_sendsMessage_andReceivesResponse** — type a message, see AI reply
- **chat_showsProcessingIndicator_whileWaiting**
- **chat_inputClears_afterSending**
- **chat_sendViaEnterKey**
- **chat_sendViaButton**
- **chat_doesNotSend_emptyMessage**
- **chat_autoScrolls_onNewMessages**

**Calendar Integration (if Claude API is available):**
- **chat_createEvent_updatesCalendar** — "Create a meeting tomorrow at 2pm" → event appears
- **chat_listEvents_showsEvents** — "What events do I have this week?"
- **chat_showsActionResults_afterToolExecution**

**Error Handling:**
- **chat_showsErrorMessage_onApiFailure** — mock backend failure
- **chat_handlesTimeout_gracefully** — long-running request

---

### D. Admin Dashboard

#### File: `e2e/admin-dashboard.spec.ts`

Precondition: Authenticated via demo mode.

**No Profile State:**
- **showsSetupPrompt_whenNoBusinessProfile**
- **setupLink_navigatesToBusinessSetup**

**With Profile State (pre-seed via API):**
- **showsBusinessProfileSummary**
- **showsBookingLink**
- **copyBookingLink_copiesToClipboard**
- **showsTodaysBookings** — pre-seed a booking for today
- **showsUpcomingBookings**
- **navigationLinks_workCorrectly** — setup, services, availability, bookings

---

### E. Business Setup

#### File: `e2e/admin-business-setup.spec.ts`

Precondition: Authenticated via demo mode.

**Create Profile:**
- **showsCreateForm_whenNoProfile**
- **validates_requiredBusinessName** — submit empty, expect error
- **createsProfile_withAllFields** — fill all fields, submit, verify success
- **generatesSlug_fromBusinessName** — "My Business" → "my-business"
- **navigatesToAdminDashboard_afterSave**

**Edit Profile:**
- **showsEditForm_withExistingData** — pre-seed profile
- **updatesProfile_successfully**
- **cancelButton_navigatesBack_withoutSaving**

---

### F. Service Management

#### File: `e2e/admin-services.spec.ts`

Precondition: Authenticated via demo mode, business profile exists.

- **showsEmptyState_whenNoServices**
- **createService_viaModal** — open modal, fill form, save
- **createdService_appearsInList**
- **editService_viaModal** — click edit, modify fields, save
- **editedService_showsUpdatedData**
- **deleteService_withConfirmation** — click delete, confirm, verify removed
- **cancelDelete_keepsService**
- **serviceList_showsName_Duration_Price_Color**
- **createModal_hasColorPicker**
- **createMultipleServices** — add 3 services, verify all appear

---

### G. Availability Management

#### File: `e2e/admin-availability.spec.ts`

Precondition: Authenticated via demo mode, business profile exists.

**Weekly Schedule:**
- **showsWeeklySchedule_forAll7Days**
- **saveWeeklyAvailability_forMonday** — set 9am-5pm
- **savedAvailability_persistsOnReload**
- **canDisableDay** — toggle a day off

**Date Overrides:**
- **openOverrideForm_clickButton**
- **createDateOverride_markDayUnavailable**
- **createDateOverride_customHours**
- **overrideAppears_inOverrideList**
- **deleteOverride_removesFromList**

**Break Slots:**
- **openBreakForm_clickButton**
- **createBreak_forSpecificDate** — lunch break 12-1pm
- **breakAppears_inBreakList**
- **deleteBreak_removesFromList**

---

### H. Booking Management (Admin)

#### File: `e2e/admin-bookings.spec.ts`

Precondition: Authenticated via demo mode, business profile + services + availability + bookings exist (pre-seeded).

- **showsBookingsList**
- **filterByStatus_confirmed** — select "Confirmed" filter
- **filterByStatus_cancelled**
- **filterByDateRange** — set from/to dates
- **clearFilters_showsAll**
- **cancelBooking_withConfirmation** — click cancel, confirm, verify status changes
- **completeBooking** — click complete, verify status changes
- **showsBookingDetails** — service name, client info, time
- **sortsByDate** — most recent first

---

### I. Public Booking Flow (Client-Facing)

#### File: `e2e/public-booking.spec.ts`

Precondition: Business profile + services + availability rules exist (pre-seeded via API).

**Full Booking Flow:**
- **loadsBusinessPage_bySlug** — navigate to `/book/{slug}`
- **showsBusinessName_andDescription**
- **showsActiveServices_withPriceAndDuration**
- **step1_selectService** — click a service, advance to date picker
- **step2_selectDate** — pick a date with availability
- **step2_showsAvailableDates**
- **step3_selectTimeSlot** — pick from available slots
- **step3_showsAvailableSlots**
- **step4_fillClientDetails** — name, email, phone, notes
- **step4_validates_requiredName** — submit without name
- **step4_validates_requiredEmail**
- **step4_validates_emailFormat**
- **step5_showsConfirmation** — booking details + management link
- **fullFlow_serviceToConfirmation** — complete end-to-end booking

**Navigation:**
- **backButton_fromDate_returnsToService**
- **backButton_fromTime_returnsToDate**
- **backButton_fromDetails_returnsToTime**
- **stepIndicator_showsCurrentStep**

**Error States:**
- **shows404_forInvalidSlug**
- **showsNoSlots_whenFullyBooked**

---

### J. Booking Management (Client-Facing)

#### File: `e2e/public-booking-manage.spec.ts`

Precondition: Booking exists (pre-seeded via API).

- **loadsBookingDetails_byToken** — navigate to `/book/manage/{token}`
- **showsBookingInfo** — service, date, time, status
- **showsBusinessInfo**
- **cancelBooking_updatesStatus** — click cancel, confirm, verify
- **showsCancelledStatus_afterCancellation**
- **disablesCancelButton_forAlreadyCancelled**
- **shows404_forInvalidToken**

---

### K. Cross-Cutting Concerns

#### File: `e2e/navigation-and-errors.spec.ts`

- **unknownRoute_redirectsToDashboard** — `/nonexistent` → `/dashboard`
- **healthEndpoint_returns200** — fetch `/health`
- **responsiveLayout_desktop** — verify split pane layout
- **responsiveLayout_mobile** — verify stacked/toggle layout
- **pageTitle_isSet**
- **refreshPage_maintainsState** — auth persists

---

## Test Data Seeding

### API Helper (`e2e/helpers/test-data.ts`)
Helper functions to seed test data via the backend API before tests:

```ts
// Functions to implement:
createBusinessProfile(data) → BusinessProfile
createService(data) → Service
createWeeklyAvailability(data) → void
createBooking(slug, data) → BookingResponse
cleanupTestData() → void
```

### Fixtures (`e2e/fixtures/`)
- `business.json` — sample business profile data
- `services.json` — sample service definitions
- `availability.json` — sample weekly rules
- `booking.json` — sample booking request

---

## Test Count Summary

| Category | File Count | Test Cases |
|----------|-----------|------------|
| Auth | 1 | ~8 |
| Dashboard & Calendar | 1 | ~15 |
| AI Chat | 1 | ~14 |
| Admin Dashboard | 1 | ~7 |
| Business Setup | 1 | ~8 |
| Service Management | 1 | ~10 |
| Availability | 1 | ~12 |
| Admin Bookings | 1 | ~9 |
| Public Booking Flow | 1 | ~18 |
| Booking Management | 1 | ~7 |
| Cross-Cutting | 1 | ~6 |
| **Total** | **11** | **~114** |

## Running Tests
```bash
cd calendar-manager-ui
npx playwright test                    # run all tests
npx playwright test --headed           # run with browser visible
npx playwright test --ui               # interactive UI mode
npx playwright test e2e/public-booking.spec.ts  # run single file
npx playwright test --project=chromium # specific browser
npx playwright show-report             # view HTML report
```

## CI/CD Integration
```yaml
# Example GitHub Actions step
- name: Run E2E Tests
  run: |
    cd calendar-manager-ui
    npx playwright install --with-deps
    npx playwright test
  env:
    DEMO_MODE: "true"
    DATABASE_URL: ${{ secrets.TEST_DATABASE_URL }}
```
