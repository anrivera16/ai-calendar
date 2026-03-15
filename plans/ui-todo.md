# UI Manual Validation Checklist

Use this to manually test every feature of the app end-to-end.
Check off items as you go. Note bugs inline with `❌ BUG:` and working items with `✅`.

---

## 1. Login & Authentication

- [ ] Navigate to `/` → should redirect to login page
- [ ] Login page shows "Sign in with Google" button
- [ ] Click button → redirected to Google OAuth consent screen
- [ ] Authorize → callback returns to app, user is now logged in
- [ ] Refresh page → still authenticated (JWT persists in cookie)
- [ ] Try accessing `/admin/dashboard` without auth → redirects to login
- [ ] Logout button works → removes session, returns to login

---

## 2. Admin Dashboard

- [ ] After login, navigate to `/admin/dashboard`
- [ ] Page loads with no console errors
- [ ] "Today's Bookings" section is visible
- [ ] "Upcoming Bookings" section is visible
- [ ] If no business profile: "Create Business Profile" prompt visible
- [ ] If profile exists: business name, slug, phone, website displayed

---

## 3. Business Profile Setup

- [ ] Navigate to `/admin/business-setup`
- [ ] Form shows fields: Business Name, Description, Phone, Website, Address, Logo URL
- [ ] Fill form and submit → success message appears
- [ ] Profile displays after save with auto-generated URL slug
- [ ] Slug is URL-safe (lowercase, hyphens only)
- [ ] Public booking link `/book/{slug}` is displayed
- [ ] Edit existing profile → form pre-fills with current data

---

## 4. Service Management

- [ ] Navigate to `/admin/services`
- [ ] Existing services listed (if any)
- [ ] "Add Service" → form appears with: Name, Description, Duration, Price, Color
- [ ] Fill and submit → service appears in list
- [ ] Edit service → form pre-fills
- [ ] Delete service → removed from list (with confirmation)
- [ ] Drag to reorder → order persists on reload

---

## 5. Availability Setup

- [ ] Navigate to `/admin/availability-manager`
- [ ] Weekly grid shows 7 days with start/end time inputs
- [ ] Toggle a day on/off → saves correctly
- [ ] Add a date override (holiday/day off) → shows in override list
- [ ] Add a break (e.g. lunch 12–1pm) → shows in breaks list
- [ ] Delete a rule → removed
- [ ] Rules persist on page reload

---

## 6. Booking Management

- [ ] Navigate to `/admin/booking-list`
- [ ] Table shows columns: client name, service, date/time, status
- [ ] Filter by date range → table updates
- [ ] Filter by status (confirmed / cancelled / completed) → table updates
- [ ] Click booking → details view opens (client info, notes, etc.)
- [ ] "Cancel Booking" → status changes to cancelled
- [ ] "Mark Complete" → status changes to completed
- [ ] Empty state: "No bookings yet" shown when table is empty

---

## 7. Dashboard Calendar View

- [ ] Navigate to `/dashboard`
- [ ] Left panel: monthly calendar grid visible
- [ ] Right panel: AI chat panel visible
- [ ] Today's date is highlighted
- [ ] Previous/next month buttons work
- [ ] Click a date → events for that date shown
- [ ] Google Calendar events appear in the grid
- [ ] No console errors

---

## 8. AI Chat

- [ ] Chat input at bottom of right panel with placeholder text
- [ ] Type message → send button is enabled
- [ ] Send → AI response appears within a few seconds
- [ ] "New Conversation" button starts a fresh chat
- [ ] Conversation history persists on page refresh
- [ ] Test commands:
  - [ ] "What's on my calendar today?" → lists today's events
  - [ ] "Schedule lunch tomorrow at 1pm" → creates calendar event
  - [ ] "Show me my bookings" → lists bookings
- [ ] If Claude API unavailable → fallback message shown (not blank/crash)

---

## 9. Public Booking Flow

Navigate to `/book/{slug}` — no login required.

- [ ] Page loads with business name, description, and service list
- **Step 1 — Select Service**
  - [ ] Services listed with name, duration, price
  - [ ] Click service → advances to date picker
- **Step 2 — Select Date**
  - [ ] Calendar shows selectable dates (next ~30 days)
  - [ ] Past dates are disabled
  - [ ] Select date → advances to time slots
- **Step 3 — Select Time Slot**
  - [ ] Available slots fetched from API and displayed
  - [ ] If no slots available: "No availability" message
  - [ ] Select slot → advances to details form
- **Step 4 — Enter Details**
  - [ ] Fields: Name (required), Email (required), Phone (optional), Notes
  - [ ] Submit with empty Name → validation error
  - [ ] Submit with invalid email → validation error
  - [ ] Valid form → booking created
- **Step 5 — Confirmation**
  - [ ] Shows: service, date/time, client name, "Booking Confirmed"
  - [ ] "View/Manage Booking" link is present

---

## 10. Client Booking Management (No Login)

- [ ] Open the management link from booking confirmation
- [ ] Booking details shown (service, time, client info)
- [ ] "Cancel Booking" button present
- [ ] Click cancel → booking marked cancelled
- [ ] After cancellation: confirmation message shown

---

## 11. Google Calendar Integration

- [ ] When booking is created, event appears on business owner's Google Calendar
- [ ] Event title = service name, time matches slot selected
- [ ] If owner has existing calendar events, those times show as unavailable for booking
- [ ] Chat command to view calendar events works

---

## 12. Email Notifications (if SMTP configured)

- [ ] After booking: client receives confirmation email
- [ ] After booking: owner receives notification email
- [ ] After cancellation: client receives cancellation email
- [ ] After cancellation: owner receives notification email

---

## 13. Input Validation

- [ ] Admin forms: empty required fields show errors
- [ ] Booking form: invalid email rejected
- [ ] Booking form: required fields enforced
- [ ] Public booking: no injection or XSS via text fields

---

## 14. Error & Edge Cases

- [ ] Navigate to `/book/nonexistent-slug` → "Business not found" message
- [ ] Try booking when no slots exist → graceful "no availability" message
- [ ] Network error during booking → error message shown (no blank screen)
- [ ] Expired JWT → redirected to login (not stuck on blank page)

---

## 15. UI/UX Polish

- [ ] No console errors (excluding unrelated third-party)
- [ ] Loading indicators visible during API calls
- [ ] Buttons have visible hover states
- [ ] Public booking page is usable on mobile
- [ ] Admin dashboard is usable on mobile (may be limited — note any issues)
- [ ] Chat panel readable on small screens

---

## Bug Log

Use this section to record issues found during testing.

| # | Location | Description | Severity |
|---|----------|-------------|----------|
| 1 | | | |
