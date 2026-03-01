# E2E Test Status - Missing Functionality

## Overview
This document tracks functionality that's missing or not working correctly, causing e2e tests to fail.

---

## Authentication Issues (Root Cause)

### Core Problem: No Demo Mode in Frontend Auth
**Status:** Missing Implementation

The Angular auth service (`AuthService.checkAuthStatus()`) makes a real HTTP call to `/api/auth/status`. Without valid OAuth tokens, this returns `authenticated: false`, causing the auth guard to redirect to login.

The auth guard doesn't check localStorage for demo mode - it only relies on the API response.

**Required Fix:**
1. Option A: Add demo mode support to Angular auth service that checks localStorage first
2. Option B: Add proper OAuth tokens to the test environment
3. Option C: Mock all auth-related API calls in tests

---

## Test Files - Status Summary

### `e2e/auth.spec.ts` (4 failed, 4 passed)
- ✅ Login page redirect works
- ✅ Google login button renders  
- ✅ Feature descriptions render
- ✅ Protected routes redirect to login
- ❌ Demo mode dashboard access (auth guard rejects)
- ❌ Demo mode admin access (auth guard rejects)
- ❌ Logout flow (depends on demo auth working)

### `e2e/dashboard.spec.ts` 
- Layout tests depend on demo auth working
- Calendar tests depend on business profile APIs

### `e2e/chat.spec.ts`
- Depends on demo auth + Chat API (`/api/chat/process`)

### Admin Tests (all depend on demo auth)
- `e2e/admin-dashboard.spec.ts`
- `e2e/admin-business-setup.spec.ts`
- `e2e/admin-services.spec.ts`
- `e2e/admin-availability.spec.ts`
- `e2e/admin-bookings.spec.ts`

### Public Booking Tests
- `e2e/public-booking.spec.ts`
- `e2e/public-booking-manage.spec.ts`

---

## Fixes Applied So Far

### 1. API Path Corrections
Fixed `e2e/helpers/test-data.ts` to use correct API endpoints:

| Old (Wrong) | New (Correct) |
|-------------|--------------|
| `/api/business-profile` | `/api/admin/profile` |
| `/api/business-profile/{id}/services` | `/api/admin/services` |
| `/api/business-profile/{id}/bookings` | `/api/admin/bookings` |
| `/api/public/{slug}/book` | `/api/book/{slug}` |

### 2. Port Configuration
Updated to use Docker API at `http://localhost:8080` instead of local dev at `5047`.

### 3. Auth Helper Update
Removed `/api/auth/demo-login` call (endpoint doesn't exist).

---

## What's Still Missing

### 1. Demo Mode in Auth Service
The frontend auth service needs to recognize demo mode from localStorage without making API calls.

**Files to modify:**
- `calendar-manager-ui/src/app/services/auth.ts`
- `calendar-manager-ui/src/app/guards/auth.guard.ts`

### 2. Demo Login Endpoint (Alternative)
Add `/api/auth/demo-login` to backend:

```csharp
// In AuthController.cs
[HttpPost("demo-login")]
public async Task<IActionResult> DemoLogin([FromBody] DemoLoginRequest request)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user == null)
    {
        user = new User { Email = request.Email, DisplayName = "Demo User" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }
    // Return a demo token or mark user as authenticated
    return Ok(new { success = true, userId = user.Id, demoMode = true });
}
```

### 3. Frontend Routes for Admin
Need to verify admin routes exist:
- `/admin/dashboard`
- `/admin/setup`
- `/admin/services`
- `/admin/availability`
- `/admin/bookings`

---

## Backend API Endpoints (Verified Working)

| Endpoint | Method | Status |
|----------|--------|--------|
| `/api/auth/status` | GET | ✅ Works (requires OAuth) |
| `/api/auth/google-login` | GET | ✅ Works |
| `/api/admin/profile` | GET/POST/PUT | ✅ Works |
| `/api/admin/services` | GET/POST/PUT/DELETE | ✅ Works |
| `/api/admin/availability` | GET/POST/DELETE | ✅ Works |
| `/api/admin/bookings` | GET | ✅ Works |
| `/api/admin/bookings/today` | GET | ✅ Works |
| `/api/admin/bookings/upcoming` | GET | ✅ Works |
| `/api/book/{slug}` | GET | ✅ Works |
| `/api/book/{slug}/slots` | GET | ✅ Works |
| `/api/book/{slug}` | POST | ✅ Works |
| `/api/book/manage/{token}` | GET | ✅ Works |
| `/api/book/manage/{token}/cancel` | POST | ✅ Works |
