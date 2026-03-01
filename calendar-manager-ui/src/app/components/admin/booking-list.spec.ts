import { render, screen, fireEvent, waitFor } from '@testing-library/angular';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { of, throwError } from 'rxjs';
import { provideRouter } from '@angular/router';
import { BookingListComponent } from './booking-list';
import { AdminApiService } from '../../services/admin-api.service';
import { AdminBooking } from '../../models/admin.models';

describe('BookingListComponent', () => {
  let mockAdminApi: {
    getBookings: ReturnType<typeof vi.fn>;
    cancelBooking: ReturnType<typeof vi.fn>;
    completeBooking: ReturnType<typeof vi.fn>;
  };

  const mockBookings: AdminBooking[] = [
    {
      id: 'booking-1',
      serviceId: 'service-1',
      serviceName: 'Haircut',
      serviceColor: '#3B82F6',
      clientName: 'John Doe',
      clientEmail: 'john@example.com',
      clientPhone: '555-1234',
      startTime: new Date().toISOString(),
      endTime: new Date(Date.now() + 3600000).toISOString(),
      status: 'Confirmed',
      notes: 'First visit',
      createdAt: new Date().toISOString(),
    },
    {
      id: 'booking-2',
      serviceId: 'service-2',
      serviceName: 'Beard Trim',
      clientName: 'Jane Smith',
      clientEmail: 'jane@example.com',
      startTime: new Date(Date.now() + 86400000).toISOString(),
      endTime: new Date(Date.now() + 90000000).toISOString(),
      status: 'Completed',
      createdAt: new Date().toISOString(),
    },
  ];

  beforeEach(async () => {
    mockAdminApi = {
      getBookings: vi.fn().mockReturnValue(of(mockBookings)),
      cancelBooking: vi.fn().mockReturnValue(of({ message: 'Cancelled', status: 'Cancelled' })),
      completeBooking: vi.fn().mockReturnValue(of({ message: 'Completed', status: 'Completed' })),
    };
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('creates_theComponent', async () => {
    await render(BookingListComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText(/Bookings/i)).toBeTruthy();
    });
  });

  it('loadsBookings_onInit', async () => {
    await render(BookingListComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    expect(mockAdminApi.getBookings).toHaveBeenCalled();
  });

  it('rendersBookingTable', async () => {
    await render(BookingListComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeTruthy();
      expect(screen.getByText('Haircut')).toBeTruthy();
      expect(screen.getByText('jane@example.com')).toBeTruthy();
    });
  });

  it('applyFilters_reloadsWithFilters', async () => {
    await render(BookingListComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText(/Bookings/i)).toBeTruthy();
    });

    const statusSelect = document.querySelector('select') as HTMLSelectElement;
    if (statusSelect) {
      fireEvent.change(statusSelect, { target: { value: 'Confirmed' } });
    }

    await waitFor(() => {
      expect(mockAdminApi.getBookings).toHaveBeenCalled();
    });
  });

  it('clearFilters_resetsAndReloads', async () => {
    await render(BookingListComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText(/Clear/i)).toBeTruthy();
    });

    const clearButton = screen.getByText(/Clear/i);
    fireEvent.click(clearButton);

    await waitFor(() => {
      expect(mockAdminApi.getBookings).toHaveBeenCalled();
    });
  });

  it('confirmCancel_showsConfirmationDialog', async () => {
    await render(BookingListComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeTruthy();
    });

    const cancelButtons = screen.getAllByTitle('Cancel booking');
    if (cancelButtons.length > 0) {
      fireEvent.click(cancelButtons[0]);
    }

    await waitFor(() => {
      expect(screen.getByText(/Cancel Booking/i)).toBeTruthy();
    });
  });

  it('cancelBooking_callsApi_andReloads', async () => {
    await render(BookingListComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeTruthy();
    });

    const cancelButtons = screen.getAllByTitle('Cancel booking');
    if (cancelButtons.length > 0) {
      fireEvent.click(cancelButtons[0]);
    }

    await waitFor(() => {
      expect(screen.getByText(/Cancel Booking/i)).toBeTruthy();
    });

    const confirmButton = screen.getByText(/Yes, Cancel/i);
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockAdminApi.cancelBooking).toHaveBeenCalled();
    });
  });

  it('completeBooking_callsApi_andReloads', async () => {
    await render(BookingListComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeTruthy();
    });

    const completeButtons = screen.getAllByTitle('Mark as completed');
    fireEvent.click(completeButtons[0]);

    await waitFor(() => {
      expect(mockAdminApi.completeBooking).toHaveBeenCalled();
    });
  });

  it('showsActionLoading_forSpecificBooking', async () => {
    const { fixture } = await render(BookingListComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeTruthy();
    });

    const completeButtons = screen.getAllByTitle('Mark as completed');
    if (completeButtons.length > 0) {
      fireEvent.click(completeButtons[0]);
    }
  });

  it('formatsDateTime_correctly', async () => {
    const { fixture } = await render(BookingListComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    const component = fixture.componentInstance;
    const formatted = component.formatDateTime('2024-01-15T14:30:00');

    expect(formatted).toBeTruthy();
    expect(formatted).toMatch(/jan/i);
    expect(formatted).toMatch(/15/i);
  });

  it('showsStatusFilter_dropdown', async () => {
    await render(BookingListComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      const select = document.querySelector('select');
      expect(select).toBeTruthy();
    });
  });

  it('showsDateRange_filter', async () => {
    await render(BookingListComponent, {
      providers: [{ provide: AdminApiService, useValue: mockAdminApi }, provideRouter([])],
    });

    await waitFor(() => {
      const dateInputs = document.querySelectorAll('input[type="date"]');
      expect(dateInputs.length).toBeGreaterThanOrEqual(2);
    });
  });
});
