# B2B Scheduling Platform - TODO

## Phase 1: Database & Core Backend ✅
> Foundation — all other phases depend on this

- [x] **1.1** Create `BusinessProfile` entity (`Data/Entities/BusinessProfile.cs`)
- [x] **1.2** Create `Service` entity (`Data/Entities/Service.cs`)
- [x] **1.3** Create `AvailabilityRule` entity (`Data/Entities/AvailabilityRule.cs`)
- [x] **1.4** Create `Client` entity (`Data/Entities/Client.cs`)
- [x] **1.5** Create `Booking` entity (`Data/Entities/Booking.cs`)
- [x] **1.6** Add `BusinessProfile?` nav property to `User.cs`
- [x] **1.7** Update `AppDbContext.cs` — 5 DbSets, OnModelCreating, UpdateTimestamps
- [x] **1.8** EF Core migration: `AddBookingPlatform`
- [x] **1.9** DTOs: BusinessProfileDto, ServiceDto, AvailabilityRuleDto, BookingDto, AvailableSlotDto
- [x] **1.10** `AvailabilityService` — slot calculation (rules + GCal free/busy + bookings)
- [x] **1.11** `BookingService` — create/cancel/list with GCal event creation
- [x] **1.12** Services registered in `Program.cs` DI container

---

## Phase 2: Client Booking Flow (Public) ✅
> This is the product's core value — build before admin dashboard

- [x] **2.1** `PublicBookingController.cs` — 5 endpoints, no auth required
- [x] **2.2** Rate limiting in `Program.cs` (30 req/min general, 10 req/5min POST booking)
- [x] **2.3** Angular booking models (`models/booking.models.ts`)
- [x] **2.4** `PublicBookingApiService` (`services/public-booking-api.service.ts`)
- [x] **2.5** `BookingPageComponent` — 5-step flow (service → date → slot → details → confirmation)
- [x] **2.6** `BookingManageComponent` — view/cancel via cancellation token
- [x] **2.7** Public routes in `app.routes.ts` (no AuthGuard)
- [x] **2.8** Mobile-first responsive design (media queries at 600px)

---

## Phase 3: Admin Dashboard ✅
> Business owner manages their services, availability, and bookings

- [x] **3.1** `AdminController.cs` — full CRUD for profile, services, availability, bookings (+ bonus: today/upcoming/complete endpoints)
- [x] **3.2** Auto-seed AvailabilityRules from UserPreferences on profile creation
- [x] **3.3** Auto-generate URL slug with uniqueness check
- [x] **3.4** Angular admin models (`models/admin.models.ts`)
- [x] **3.5** `AdminApiService` (`services/admin-api.service.ts`)
- [x] **3.6** `AdminDashboardComponent` — overview, today's bookings, upcoming, share booking link
- [x] **3.7** `BusinessSetupComponent` — first-time setup wizard (create + edit modes)
- [x] **3.8** `ServiceManagerComponent` — CRUD list with color picker, create/edit modals
- [x] **3.9** `AvailabilityManagerComponent` — weekly schedule grid + date overrides + breaks
- [x] **3.10** `BookingListComponent` — filterable table with cancel/complete actions
- [x] **3.11** Admin routes in `app.routes.ts` with AuthGuard
- [x] **3.12** Admin navigation link in dashboard header

---

## Phase 4: Notifications & AI Enhancement ✅
> Polish and extend

- [x] **4.1** `EmailService` (MailKit SMTP) — confirmation, cancellation, and new booking notification HTML templates
- [x] **4.2** BookingService sends confirmation email to client on booking creation
- [x] **4.3** BookingService notifies business owner of new bookings via email
- [x] **4.4** Claude system prompt enhanced with business/booking context
- [x] **4.5** Three new Claude tools: `check_booking_availability`, `view_bookings`, `cancel_booking`
- [x] **4.6** "Copy Booking Link" button on admin dashboard (clipboard API)
- [x] **4.7** Booking status badges with color classes (confirmed/cancelled/completed/noshow)
- [x] **4.8** Loading states with spinners on public booking page
- [x] **4.9** Form validation across booking and admin components (required, email, canSubmit guards)

---

## Key Architecture Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| User model | User = Business Owner (1:1) | Keeps auth simple, no multi-tenancy |
| Availability source | Business rules + Google Calendar free/busy | Catches personal events that block time |
| Booking → Calendar | Creates real Google Calendar event | Business sees it in their calendar app, auto-blocks future slots |
| Client auth | Cancellation token (no login) | Clients don't need accounts — industry standard (Calendly, Cal.com) |
| Build order | Phase 1 → Phase 2 → Phase 3 → Phase 4 | Client booking is the core product value |
