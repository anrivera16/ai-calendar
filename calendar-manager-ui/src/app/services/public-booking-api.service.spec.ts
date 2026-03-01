import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PublicBookingApiService } from './public-booking-api.service';
import { environment } from '../../environments/environment';
import { BusinessInfo, SlotsResponse, BookingResponse, BookingManageResponse, CancelBookingResponse } from '../models/booking.models';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

describe('PublicBookingApiService', () => {
  let service: PublicBookingApiService;
  let httpMock: HttpTestingController;
  const baseUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PublicBookingApiService]
    });

    service = TestBed.inject(PublicBookingApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('getBusinessBySlug', () => {
    it('calls correct endpoint', () => {
      const mockResponse: BusinessInfo = {
        id: '1',
        businessName: 'Test Business',
        services: []
      };

      service.getBusinessBySlug('test-business').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/book/test-business`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('getAvailableSlots', () => {
    it('calls correct endpoint', () => {
      const mockResponse: SlotsResponse = {
        date: '2024-01-15',
        serviceId: 'service-1',
        durationMinutes: 30,
        slots: []
      };

      service.getAvailableSlots('test-business', 'service-1', '2024-01-15').subscribe();

      const req = httpMock.expectOne((request) =>
        request.url === `${baseUrl}/api/book/test-business/slots`
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('sends serviceId and date params', () => {
      const mockResponse: SlotsResponse = {
        date: '2024-01-15',
        serviceId: 'service-1',
        durationMinutes: 30,
        slots: []
      };

      service.getAvailableSlots('test-business', 'service-1', '2024-01-15').subscribe();

      const req = httpMock.expectOne((request) =>
        request.url === `${baseUrl}/api/book/test-business/slots` &&
        request.params.get('serviceId') === 'service-1' &&
        request.params.get('date') === '2024-01-15'
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('createBooking', () => {
    it('calls correct endpoint', () => {
      const bookingRequest = {
        serviceId: 'service-1',
        startTime: '2024-01-15T10:00:00Z',
        clientName: 'John Doe',
        clientEmail: 'john@example.com'
      };
      const mockResponse: BookingResponse = {
        booking: {
          id: 'booking-1',
          serviceName: 'Haircut',
          startTime: '2024-01-15T10:00:00Z',
          endTime: '2024-01-15T10:30:00Z',
          status: 'Confirmed',
          client: { name: 'John Doe', email: 'john@example.com' }
        },
        business: { name: 'Test Business' },
        managementUrl: 'https://test.com/manage/token123',
        message: 'Booking confirmed'
      };

      service.createBooking('test-business', bookingRequest).subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/book/test-business`);
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
    });

    it('sends correct payload', () => {
      const bookingRequest = {
        serviceId: 'service-1',
        startTime: '2024-01-15T10:00:00Z',
        clientName: 'John Doe',
        clientEmail: 'john@example.com',
        clientPhone: '555-1234',
        notes: 'First time customer'
      };
      const mockResponse: BookingResponse = {
        booking: {
          id: 'booking-1',
          serviceName: 'Haircut',
          startTime: '2024-01-15T10:00:00Z',
          endTime: '2024-01-15T10:30:00Z',
          status: 'Confirmed',
          client: { name: 'John Doe', email: 'john@example.com', phone: '555-1234' }
        },
        business: { name: 'Test Business' },
        managementUrl: 'https://test.com/manage/token123',
        message: 'Booking confirmed'
      };

      service.createBooking('test-business', bookingRequest).subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/book/test-business`);
      expect(req.request.body).toEqual(bookingRequest);
      req.flush(mockResponse);
    });
  });

  describe('getBookingByToken', () => {
    it('calls correct endpoint', () => {
      const mockResponse: BookingManageResponse = {
        booking: {
          id: 'booking-1',
          serviceName: 'Haircut',
          startTime: '2024-01-15T10:00:00Z',
          endTime: '2024-01-15T10:30:00Z',
          status: 'Confirmed',
          client: { name: 'John Doe', email: 'john@example.com' }
        },
        business: { name: 'Test Business' }
      };

      service.getBookingByToken('token123').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/book/manage/token123`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('cancelBooking', () => {
    it('calls correct endpoint', () => {
      const mockResponse: CancelBookingResponse = {
        message: 'Booking cancelled',
        booking: { id: 'booking-1', status: 'Cancelled' }
      };

      service.cancelBooking('token123').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/book/manage/token123/cancel`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(mockResponse);
    });
  });
});
