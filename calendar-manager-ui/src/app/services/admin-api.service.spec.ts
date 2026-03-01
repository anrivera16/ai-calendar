import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AdminApiService } from './admin-api.service';
import { environment } from '../../environments/environment';
import {
  BusinessProfile,
  BusinessProfileResponse,
  Service,
  AvailabilityRule,
  AdminBooking
} from '../models/admin.models';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

describe('AdminApiService', () => {
  let service: AdminApiService;
  let httpMock: HttpTestingController;
  const baseUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AdminApiService]
    });

    service = TestBed.inject(AdminApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('Profile', () => {
    it('getProfile calls correct endpoint', () => {
      const mockResponse: BusinessProfileResponse = { hasProfile: true, id: '1', businessName: 'Test Business' };

      service.getProfile().subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/profile`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('createProfile calls correct endpoint', () => {
      const newProfile = { businessName: 'New Business' };
      const mockResponse: BusinessProfile = {
        id: '1',
        businessName: 'New Business',
        slug: 'new-business',
        isActive: true,
        createdAt: '2024-01-01',
        updatedAt: '2024-01-01',
        bookingUrl: 'https://test.com/book/new-business'
      };

      service.createProfile(newProfile as any).subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/profile`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newProfile);
      req.flush(mockResponse);
    });

    it('updateProfile calls correct endpoint', () => {
      const updateProfile = { businessName: 'Updated Business' };
      const mockResponse: BusinessProfile = {
        id: '1',
        businessName: 'Updated Business',
        slug: 'updated-business',
        isActive: true,
        createdAt: '2024-01-01',
        updatedAt: '2024-01-02',
        bookingUrl: 'https://test.com/book/updated-business'
      };

      service.updateProfile(updateProfile as any).subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/profile`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateProfile);
      req.flush(mockResponse);
    });
  });

  describe('Services', () => {
    it('getServices calls correct endpoint', () => {
      const mockResponse: Service[] = [];

      service.getServices().subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/services`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('createService calls correct endpoint', () => {
      const newService = { name: 'Haircut', durationMinutes: 30, price: 25, color: '#FF0000', isActive: true, sortOrder: 0 };
      const mockResponse: Service = {
        id: '1',
        businessProfileId: 'bp1',
        ...newService,
        createdAt: '2024-01-01',
        updatedAt: '2024-01-01'
      };

      service.createService(newService as any).subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/services`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newService);
      req.flush(mockResponse);
    });

    it('updateService calls correct endpoint', () => {
      const updateService = { name: 'Updated Service' };
      const mockResponse: Service = {
        id: '1',
        businessProfileId: 'bp1',
        name: 'Updated Service',
        durationMinutes: 30,
        price: 25,
        color: '#FF0000',
        isActive: true,
        sortOrder: 0,
        createdAt: '2024-01-01',
        updatedAt: '2024-01-02'
      };

      service.updateService('1', updateService as any).subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/services/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateService);
      req.flush(mockResponse);
    });

    it('deleteService calls correct endpoint', () => {
      const mockResponse = { message: 'Service deleted' };

      service.deleteService('1').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/services/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(mockResponse);
    });
  });

  describe('Availability', () => {
    it('getAvailability calls correct endpoint', () => {
      const mockResponse: AvailabilityRule[] = [];

      service.getAvailability().subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/availability`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('createWeeklyAvailability calls correct endpoint', () => {
      const availability = { dayOfWeek: 1, startTime: '09:00:00', endTime: '17:00:00', isAvailable: true };
      const mockResponse = { message: 'Availability created' };

      service.createWeeklyAvailability(availability).subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/availability/weekly`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(availability);
      req.flush(mockResponse);
    });

    it('createDateOverride calls correct endpoint', () => {
      const override = { specificDate: '2024-01-15', startTime: '09:00:00', endTime: '17:00:00', isAvailable: false };
      const mockResponse = { message: 'Override created' };

      service.createDateOverride(override).subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/availability/override`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(override);
      req.flush(mockResponse);
    });

    it('createBreak calls correct endpoint', () => {
      const brk = { specificDate: '2024-01-15', startTime: '12:00:00', endTime: '13:00:00' };
      const mockResponse = { message: 'Break created' };

      service.createBreak(brk).subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/availability/break`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(brk);
      req.flush(mockResponse);
    });

    it('deleteAvailabilityRule calls correct endpoint', () => {
      const mockResponse = { message: 'Rule deleted' };

      service.deleteAvailabilityRule('1').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/availability/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(mockResponse);
    });
  });

  describe('Bookings', () => {
    it('getBookings calls correct endpoint', () => {
      const mockResponse: AdminBooking[] = [];

      service.getBookings().subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/bookings`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('getBookings sends filter params', () => {
      const mockResponse: AdminBooking[] = [];
      const filters = { status: 'Confirmed', fromDate: '2024-01-01', toDate: '2024-01-31' };

      service.getBookings(filters).subscribe();

      const req = httpMock.expectOne((request) =>
        request.url === `${baseUrl}/api/admin/bookings` &&
        request.params.get('status') === 'Confirmed' &&
        request.params.get('fromDate') === '2024-01-01' &&
        request.params.get('toDate') === '2024-01-31'
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('getTodaysBookings calls correct endpoint', () => {
      const mockResponse: AdminBooking[] = [];

      service.getTodaysBookings().subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/bookings/today`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('getUpcomingBookings calls correct endpoint', () => {
      const mockResponse: AdminBooking[] = [];

      service.getUpcomingBookings().subscribe();

      const req = httpMock.expectOne((request) =>
        request.url === `${baseUrl}/api/admin/bookings/upcoming`
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('getUpcomingBookings sends limit param', () => {
      const mockResponse: AdminBooking[] = [];

      service.getUpcomingBookings(20).subscribe();

      const req = httpMock.expectOne((request) =>
        request.url === `${baseUrl}/api/admin/bookings/upcoming` &&
        request.params.get('limit') === '20'
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('cancelBooking calls correct endpoint', () => {
      const mockResponse = { message: 'Booking cancelled', status: 'Cancelled' };

      service.cancelBooking('1').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/bookings/1/cancel`);
      expect(req.request.method).toBe('PUT');
      req.flush(mockResponse);
    });

    it('completeBooking calls correct endpoint', () => {
      const mockResponse = { message: 'Booking completed', status: 'Completed' };

      service.completeBooking('1').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/admin/bookings/1/complete`);
      expect(req.request.method).toBe('PUT');
      req.flush(mockResponse);
    });
  });
});
