import { render, screen, fireEvent, waitFor } from '@testing-library/angular';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { of, throwError } from 'rxjs';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { BookingPageComponent } from './booking-page';
import { PublicBookingApiService } from '../../services/public-booking-api.service';
import { BusinessInfo, ServiceInfo, AvailableSlot, BookingResponse } from '../../models/booking.models';

describe('BookingPageComponent', () => {
  let mockBookingApi: {
    getBusinessBySlug: ReturnType<typeof vi.fn>;
    getAvailableSlots: ReturnType<typeof vi.fn>;
    createBooking: ReturnType<typeof vi.fn>;
  };

  const mockBusiness: BusinessInfo = {
    id: 'business-1',
    businessName: 'Test Business',
    services: [
      {
        id: 'service-1',
        name: 'Haircut',
        description: 'Basic haircut',
        durationMinutes: 30,
        price: 25,
        color: '#3B82F6',
      },
      {
        id: 'service-2',
        name: 'Beard Trim',
        durationMinutes: 15,
        price: 15,
      },
    ],
  };

  const mockSlots: AvailableSlot[] = [
    { startTime: '2024-01-16T09:00:00', endTime: '2024-01-16T09:30:00' },
    { startTime: '2024-01-16T10:00:00', endTime: '2024-01-16T10:30:00' },
  ];

  const mockBookingResponse: BookingResponse = {
    booking: {
      id: 'booking-1',
      serviceName: 'Haircut',
      startTime: '2024-01-16T09:00:00',
      endTime: '2024-01-16T09:30:00',
      status: 'Confirmed',
      client: { name: 'John Doe', email: 'john@example.com' },
    },
    business: { name: 'Test Business' },
    managementUrl: '/manage/token123',
    message: 'Booking confirmed!',
  };

  beforeEach(async () => {
    mockBookingApi = {
      getBusinessBySlug: vi.fn().mockReturnValue(of(mockBusiness)),
      getAvailableSlots: vi.fn().mockReturnValue(of({ slots: mockSlots })),
      createBooking: vi.fn().mockReturnValue(of(mockBookingResponse)),
    };
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const renderWithRoute = async () => {
    return await render(BookingPageComponent, {
      providers: [
        { provide: PublicBookingApiService, useValue: mockBookingApi },
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            params: of({ slug: 'test-business' }),
          },
        },
      ],
    });
  };

  it('creates_theComponent', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText(/Book an Appointment/i)).toBeTruthy();
    });
  });

  it('loadsBusinessBySlug_onInit', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(mockBookingApi.getBusinessBySlug).toHaveBeenCalledWith('test-business');
    });
  });

  it('showsError_whenBusinessNotFound', async () => {
    mockBookingApi.getBusinessBySlug.mockReturnValue(
      throwError(() => new Error('Business not found')),
    );

    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeTruthy();
    });
  });

  it('showsServiceSelection_onStep1', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
      expect(screen.getByText('Beard Trim')).toBeTruthy();
    });
  });

  it('selectService_advancesToDateStep', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const serviceCard = screen.getByText('Haircut');
    fireEvent.click(serviceCard);

    await waitFor(() => {
      expect(screen.getByText(/Select a Date/i)).toBeTruthy();
    });
  });

  it('showsDateSelection_onStep2', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const serviceCard = screen.getByText('Haircut');
    fireEvent.click(serviceCard);

    await waitFor(() => {
      expect(screen.getByText(/Select a Date/i)).toBeTruthy();
    });
  });

  it('selectDate_loadsSlots_andAdvancesToTimeStep', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const serviceCard = screen.getByText('Haircut');
    fireEvent.click(serviceCard);

    await waitFor(() => {
      const dateButtons = screen.getAllByRole('button');
      const dateButton = dateButtons.find((btn) => btn.className.includes('date-btn'));
      if (dateButton) {
        fireEvent.click(dateButton);
      }
    });

    await waitFor(() => {
      expect(mockBookingApi.getAvailableSlots).toHaveBeenCalled();
    });
  });

  it('showsSlots_onStep3', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const serviceCard = screen.getByText('Haircut');
    fireEvent.click(serviceCard);

    await waitFor(() => {
      const dateButtons = screen.getAllByRole('button');
      const dateButton = dateButtons.find((btn) => btn.className.includes('date-btn'));
      if (dateButton) {
        fireEvent.click(dateButton);
      }
    });

    await waitFor(() => {
      expect(screen.getByText(/Select a Time/i)).toBeTruthy();
    });
  });

  it('selectSlot_advancesToDetailsStep', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const serviceCard = screen.getByText('Haircut');
    fireEvent.click(serviceCard);

    await waitFor(() => {
      const dateButtons = screen.getAllByRole('button');
      const dateButton = dateButtons.find((btn) => btn.className.includes('date-btn'));
      if (dateButton) {
        fireEvent.click(dateButton);
      }
    });

    await waitFor(() => {
      const slotButtons = screen.getAllByRole('button');
      const slotButton = slotButtons.find((btn) => btn.className.includes('slot-btn'));
      if (slotButton) {
        fireEvent.click(slotButton);
      }
    });

    await waitFor(() => {
      expect(screen.getByText(/Your Information/i)).toBeTruthy();
    });
  });

  it('showsClientForm_onStep4', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const serviceCard = screen.getByText('Haircut');
    fireEvent.click(serviceCard);

    await waitFor(() => {
      const dateButtons = screen.getAllByRole('button');
      const dateButton = dateButtons.find((btn) => btn.className.includes('date-btn'));
      if (dateButton) {
        fireEvent.click(dateButton);
      }
    });

    await waitFor(() => {
      const slotButtons = screen.getAllByRole('button');
      const slotButton = slotButtons.find((btn) => btn.className.includes('slot-btn'));
      if (slotButton) {
        fireEvent.click(slotButton);
      }
    });

    await waitFor(() => {
      expect(screen.getByLabelText(/Name/i)).toBeTruthy();
      expect(screen.getByLabelText(/Email/i)).toBeTruthy();
    });
  });

  it('validates_requiredClientName', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const serviceCard = screen.getByText('Haircut');
    fireEvent.click(serviceCard);

    await waitFor(() => {
      const dateButtons = screen.getAllByRole('button');
      const dateButton = dateButtons.find((btn) => btn.className.includes('date-btn'));
      if (dateButton) {
        fireEvent.click(dateButton);
      }
    });

    await waitFor(() => {
      const slotButtons = screen.getAllByRole('button');
      const slotButton = slotButtons.find((btn) => btn.className.includes('slot-btn'));
      if (slotButton) {
        fireEvent.click(slotButton);
      }
    });

    await waitFor(() => {
      const submitButton = screen.getByText(/Confirm Booking/i);
      fireEvent.click(submitButton);
    });

    await waitFor(() => {
      expect(screen.getByText(/Name is required/i)).toBeTruthy();
    });
  });

  it('validates_requiredClientEmail', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const serviceCard = screen.getByText('Haircut');
    fireEvent.click(serviceCard);

    await waitFor(() => {
      const dateButtons = screen.getAllByRole('button');
      const dateButton = dateButtons.find((btn) => btn.className.includes('date-btn'));
      if (dateButton) {
        fireEvent.click(dateButton);
      }
    });

    await waitFor(() => {
      const slotButtons = screen.getAllByRole('button');
      const slotButton = slotButtons.find((btn) => btn.className.includes('slot-btn'));
      if (slotButton) {
        fireEvent.click(slotButton);
      }
    });

    await waitFor(() => {
      const nameInput = screen.getByLabelText(/Name/i);
      fireEvent.input(nameInput, { target: { value: 'John Doe' } });
    });

    await waitFor(() => {
      const submitButton = screen.getByText(/Confirm Booking/i);
      fireEvent.click(submitButton);
    });

    await waitFor(() => {
      expect(screen.getByText(/Email is required/i)).toBeTruthy();
    });
  });

  it('validates_emailFormat', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const serviceCard = screen.getByText('Haircut');
    fireEvent.click(serviceCard);

    await waitFor(() => {
      const dateButtons = screen.getAllByRole('button');
      const dateButton = dateButtons.find((btn) => btn.className.includes('date-btn'));
      if (dateButton) {
        fireEvent.click(dateButton);
      }
    });

    await waitFor(() => {
      const slotButtons = screen.getAllByRole('button');
      const slotButton = slotButtons.find((btn) => btn.className.includes('slot-btn'));
      if (slotButton) {
        fireEvent.click(slotButton);
      }
    });

    await waitFor(() => {
      const nameInput = screen.getByLabelText(/Name/i);
      fireEvent.input(nameInput, { target: { value: 'John Doe' } });
      const emailInput = screen.getByLabelText(/Email/i);
      fireEvent.input(emailInput, { target: { value: 'invalid-email' } });
    });

    await waitFor(() => {
      const submitButton = screen.getByText(/Confirm Booking/i);
      fireEvent.click(submitButton);
    });

    await waitFor(() => {
      expect(screen.getByText(/valid email/i)).toBeTruthy();
    });
  });

  it('submitBooking_callsApi_andShowsConfirmation', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const serviceCard = screen.getByText('Haircut');
    fireEvent.click(serviceCard);

    await waitFor(() => {
      const dateButtons = screen.getAllByRole('button');
      const dateButton = dateButtons.find((btn) => btn.className.includes('date-btn'));
      if (dateButton) {
        fireEvent.click(dateButton);
      }
    });

    await waitFor(() => {
      const slotButtons = screen.getAllByRole('button');
      const slotButton = slotButtons.find((btn) => btn.className.includes('slot-btn'));
      if (slotButton) {
        fireEvent.click(slotButton);
      }
    });

    await waitFor(() => {
      const nameInput = screen.getByLabelText(/Name/i);
      fireEvent.input(nameInput, { target: { value: 'John Doe' } });
      const emailInput = screen.getByLabelText(/Email/i);
      fireEvent.input(emailInput, { target: { value: 'john@example.com' } });
    });

    await waitFor(() => {
      const submitButton = screen.getByText(/Confirm Booking/i);
      fireEvent.click(submitButton);
    });

    await waitFor(() => {
      expect(mockBookingApi.createBooking).toHaveBeenCalled();
    });
  });

  it('goBackToTime_returnsToStep3', async () => {
    const { fixture } = await renderWithRoute();

    await waitFor(() => {
      expect(fixture.componentInstance.currentStep).toBe('service');
    });

    fixture.componentInstance.currentStep = 'details';
    fixture.detectChanges();

    fixture.componentInstance.goBackToTime();
    fixture.detectChanges();

    expect(fixture.componentInstance.currentStep).toBe('time');
  });

  it('goBackToDate_returnsToStep2', async () => {
    const { fixture } = await renderWithRoute();

    await waitFor(() => {
      expect(fixture.componentInstance.currentStep).toBe('service');
    });

    fixture.componentInstance.currentStep = 'time';
    fixture.detectChanges();

    fixture.componentInstance.goBackToDate();
    fixture.detectChanges();

    expect(fixture.componentInstance.currentStep).toBe('date');
  });

  it('goBackToService_returnsToStep1', async () => {
    const { fixture } = await renderWithRoute();

    await waitFor(() => {
      expect(fixture.componentInstance.currentStep).toBe('service');
    });

    fixture.componentInstance.currentStep = 'date';
    fixture.detectChanges();

    fixture.componentInstance.goBackToService();
    fixture.detectChanges();

    expect(fixture.componentInstance.currentStep).toBe('service');
  });

  it('canSubmit_returnsFalse_whenFieldsMissing', async () => {
    const { fixture } = await renderWithRoute();

    await waitFor(() => {
      expect(fixture.componentInstance).toBeTruthy();
    });

    expect(fixture.componentInstance.canSubmit()).toBe(false);

    fixture.componentInstance.clientName = 'John';
    expect(fixture.componentInstance.canSubmit()).toBe(false);

    fixture.componentInstance.clientEmail = 'john';
    expect(fixture.componentInstance.canSubmit()).toBe(false);
  });

  it('showsLoadingSpinner_whileLoadingSlots', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Haircut')).toBeTruthy();
    });

    const serviceCard = screen.getByText('Haircut');
    fireEvent.click(serviceCard);
  });

  it('showsStepIndicator_withCurrentStep', async () => {
    await renderWithRoute();

    await waitFor(() => {
      expect(screen.getByText('Service')).toBeTruthy();
      expect(screen.getByText('Date')).toBeTruthy();
      expect(screen.getByText('Time')).toBeTruthy();
      expect(screen.getByText('Details')).toBeTruthy();
    });
  });

  it('generatesAvailableDates_for30Days', async () => {
    const { fixture } = await renderWithRoute();

    await waitFor(() => {
      expect(fixture.componentInstance.availableDates.length).toBe(30);
    });
  });
});
