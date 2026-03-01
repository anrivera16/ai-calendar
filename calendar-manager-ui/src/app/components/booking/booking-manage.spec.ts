import { render, screen, fireEvent, waitFor } from '@testing-library/angular';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { of, throwError } from 'rxjs';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { BookingManageComponent } from './booking-manage';
import { PublicBookingApiService } from '../../services/public-booking-api.service';
import { BookingManageResponse, CancelBookingResponse } from '../../models/booking.models';

describe('BookingManageComponent', () => {
  let mockBookingApi: {
    getBookingByToken: ReturnType<typeof vi.fn>;
    cancelBooking: ReturnType<typeof vi.fn>;
  };

  const mockBookingResponse: BookingManageResponse = {
    booking: {
      id: 'booking-1',
      serviceName: 'Haircut',
      startTime: '2024-01-16T09:00:00',
      endTime: '2024-01-16T09:30:00',
      status: 'Confirmed',
      notes: 'First visit',
      client: {
        name: 'John Doe',
        email: 'john@example.com',
        phone: '555-1234',
      },
    },
    business: {
      name: 'Test Business',
      phone: '555-0000',
      address: '123 Main St',
    },
  };

  const mockCancelResponse: CancelBookingResponse = {
    message: 'Booking cancelled successfully',
    booking: {
      id: 'booking-1',
      status: 'Cancelled',
    },
  };

  beforeEach(async () => {
    mockBookingApi = {
      getBookingByToken: vi.fn().mockReturnValue(of(mockBookingResponse)),
      cancelBooking: vi.fn().mockReturnValue(of(mockCancelResponse)),
    };
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const renderWithRoute = async () => {
    return await render(BookingManageComponent, {
      providers: [
        { provide: PublicBookingApiService, useValue: mockBookingApi },
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            params: of({ token: 'token123' }),
          },
        },
      ],
    });
  };

  it('creates_theComponent', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText(/Manage Your Booking/i)).toBeTruthy();
    });
  });

  it('loadsBooking_byToken_onInit', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(mockBookingApi.getBookingByToken).toHaveBeenCalledWith('token123');
    });
  });

  it('showsBookingDetails', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
      expect(screen.getByText('John Doe')).toBeTruthy();
      expect(screen.getByText('john@example.com')).toBeTruthy();
    });
  });

  it('showsError_whenTokenNotFound', async () => {
    mockBookingApi.getBookingByToken.mockReturnValue(
      throwError(() => new Error('Booking not found')),
    );

    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeTruthy();
    });
  });

  it('cancelBooking_callsApi_andUpdatesUI', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Cancel Booking/i })).toBeTruthy();
    });

    const cancelButton = screen.getByRole('button', { name: /Cancel Booking/i });
    fireEvent.click(cancelButton);

    await waitFor(() => {
      expect(mockBookingApi.cancelBooking).toHaveBeenCalledWith('token123');
    });
  });

  it('showsCancelSuccess_message', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Cancel Booking/i })).toBeTruthy();
    });

    const cancelButton = screen.getByRole('button', { name: /Cancel Booking/i });
    fireEvent.click(cancelButton);

    await waitFor(() => {
      expect(screen.getByText(/cancelled/i)).toBeTruthy();
    });
  });

  it('disablesCancelButton_whileCancelling', async () => {
    const { fixture } = await renderWithRoute();

    fixture.componentInstance.cancelling = true;
    fixture.detectChanges();

    await waitFor(() => {
      const cancelButton = screen.getByRole('button', { name: /Cancelling/i });
      expect(cancelButton).toHaveAttribute('disabled');
    });
  });

  it('formatsDateTime_correctly', async () => {
    const { fixture } = await renderWithRoute();

    const component = fixture.componentInstance;
    const formatted = component.formatDateTime('2024-01-15T14:30:00');

    expect(formatted).toMatch(/january/i);
    expect(formatted).toMatch(/15/);
    expect(formatted).toMatch(/\d+:\d+\s*(AM|PM)/i);
  });

  it('appliesCorrectStatusClass_confirmed', async () => {
    const { fixture } = await renderWithRoute();

    const component = fixture.componentInstance;
    expect(component.getStatusClass('Confirmed')).toBe('status-confirmed');
  });

  it('appliesCorrectStatusClass_cancelled', async () => {
    const { fixture } = await renderWithRoute();

    const component = fixture.componentInstance;
    expect(component.getStatusClass('Cancelled')).toBe('status-cancelled');
  });

  it('appliesCorrectStatusClass_completed', async () => {
    const { fixture } = await renderWithRoute();

    const component = fixture.componentInstance;
    expect(component.getStatusClass('Completed')).toBe('status-completed');
  });

  it('appliesCorrectStatusClass_noshow', async () => {
    const { fixture } = await renderWithRoute();

    const component = fixture.componentInstance;
    expect(component.getStatusClass('NoShow')).toBe('status-noshow');
  });
});
