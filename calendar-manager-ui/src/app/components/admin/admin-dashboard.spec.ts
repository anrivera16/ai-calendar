import { render, screen, fireEvent, waitFor } from '@testing-library/angular';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { of, throwError } from 'rxjs';
import { provideRouter } from '@angular/router';
import { AdminDashboardComponent } from './admin-dashboard';
import { AdminApiService } from '../../services/admin-api.service';
import { BusinessProfile, AdminBooking } from '../../models/admin.models';

describe('AdminDashboardComponent', () => {
  let mockAdminApi: {
    getProfile: ReturnType<typeof vi.fn>;
    getTodaysBookings: ReturnType<typeof vi.fn>;
    getUpcomingBookings: ReturnType<typeof vi.fn>;
  };

  const mockProfile: BusinessProfile = {
    id: 'profile-1',
    businessName: 'Test Business',
    slug: 'test-business',
    isActive: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    bookingUrl: '/book/test-business',
  };

  const mockBookings: AdminBooking[] = [
    {
      id: 'booking-1',
      serviceId: 'service-1',
      serviceName: 'Haircut',
      clientName: 'John Doe',
      clientEmail: 'john@example.com',
      startTime: new Date().toISOString(),
      endTime: new Date(Date.now() + 3600000).toISOString(),
      status: 'Confirmed',
      createdAt: new Date().toISOString(),
    },
  ];

  beforeEach(async () => {
    mockAdminApi = {
      getProfile: vi.fn().mockReturnValue(
        of({
          hasProfile: true,
          ...mockProfile,
        }),
      ),
      getTodaysBookings: vi.fn().mockReturnValue(of(mockBookings)),
      getUpcomingBookings: vi.fn().mockReturnValue(of(mockBookings)),
    };

    vi.spyOn(window, 'alert').mockImplementation(() => {});
    
    Object.defineProperty(navigator, 'clipboard', {
      value: {
        writeText: vi.fn().mockResolvedValue(undefined),
      },
      writable: true,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('creates_theComponent', async () => {
    await render(AdminDashboardComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    await waitFor(() => {
      expect(screen.getByText('Admin Dashboard')).toBeTruthy();
    });
  });

  it('loadsProfile_onInit', async () => {
    await render(AdminDashboardComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    expect(mockAdminApi.getProfile).toHaveBeenCalled();
  });

  it('showsSetupPrompt_whenNoProfile', async () => {
    mockAdminApi.getProfile.mockReturnValue(
      of({
        hasProfile: false,
      }),
    );

    await render(AdminDashboardComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    await waitFor(() => {
      expect(screen.getByText(/Set up your business/i)).toBeTruthy();
    });
  });

  it('showsProfileInfo_whenProfileExists', async () => {
    await render(AdminDashboardComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    await waitFor(() => {
      expect(screen.getByText('Test Business')).toBeTruthy();
    });
  });

  it('loadsTodaysBookings', async () => {
    await render(AdminDashboardComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    expect(mockAdminApi.getTodaysBookings).toHaveBeenCalled();
    await waitFor(() => {
      expect(screen.getByText("Today's Schedule")).toBeTruthy();
    });
  });

  it('loadsUpcomingBookings', async () => {
    await render(AdminDashboardComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    expect(mockAdminApi.getUpcomingBookings).toHaveBeenCalledWith(5);
    await waitFor(() => {
      expect(screen.getByText(/Upcoming Bookings/i)).toBeTruthy();
    });
  });

  it('copyBookingLink_copiesToClipboard', async () => {
    await render(AdminDashboardComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    await waitFor(() => {
      expect(screen.getByText(/Copy Booking Link/i)).toBeTruthy();
    });

    const copyButton = screen.getByText(/Copy Booking Link/i);
    fireEvent.click(copyButton);

    expect(navigator.clipboard.writeText).toHaveBeenCalled();
  });

  it('showsAdminNavLinks', async () => {
    await render(AdminDashboardComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    await waitFor(() => {
      expect(screen.getByText(/Manage Services/i)).toBeTruthy();
      expect(screen.getByText(/Availability/i)).toBeTruthy();
      expect(screen.getByText(/All Bookings/i)).toBeTruthy();
    });
  });

  it('formatsTime_correctly', async () => {
    const { fixture } = await render(AdminDashboardComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    const component = fixture.componentInstance;
    const formatted = component.formatTime('2024-01-15T14:30:00');
    expect(formatted).toMatch(/\d+:\d+\s*(AM|PM)/i);
  });

  it('formatsDate_correctly', async () => {
    const { fixture } = await render(AdminDashboardComponent, {
      providers: [
        { provide: AdminApiService, useValue: mockAdminApi },
        provideRouter([]),
      ],
    });

    const component = fixture.componentInstance;
    const formatted = component.formatDate('2024-01-15T14:30:00');
    expect(formatted).toBeTruthy();
  });
});
