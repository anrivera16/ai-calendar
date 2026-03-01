import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ApiService } from './api';
import { environment } from '../../environments/environment';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';

describe('ApiService', () => {
  let service: ApiService;
  let httpMock: HttpTestingController;
  const baseUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ApiService]
    });

    service = TestBed.inject(ApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('getAuthStatus', () => {
    it('calls correct endpoint', () => {
      const mockResponse = { authenticated: true };

      service.getAuthStatus().subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/auth/status`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('initiateGoogleLogin', () => {
    it('calls correct endpoint', () => {
      const mockResponse = { authUrl: 'https://google.com/auth', state: 'state123' };

      service.initiateGoogleLogin().subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/auth/google-login`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('logout', () => {
    it('calls correct endpoint', () => {
      const mockResponse = { success: true, message: 'Logged out' };

      service.logout().subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/auth/logout`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(mockResponse);
    });
  });

  describe('testToken', () => {
    it('calls correct endpoint', () => {
      const mockResponse = { success: true };

      service.testToken().subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/auth/test-token`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(mockResponse);
    });
  });

  describe('processChatMessage', () => {
    it('calls correct endpoint', () => {
      const mockResponse = { message: 'Response', type: 'info' };

      service.processChatMessage('Hello').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/chat/message`);
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
    });

    it('sends correct payload', () => {
      const mockResponse = { message: 'Response' };

      service.processChatMessage('Hello', 'user@test.com', 'conv-123').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/chat/message`);
      expect(req.request.body).toEqual({
        message: 'Hello',
        userEmail: 'user@test.com',
        conversationId: 'conv-123'
      });
      req.flush(mockResponse);
    });

    it('uses default email when not provided', () => {
      const mockResponse = { message: 'Response' };

      service.processChatMessage('Hello').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/chat/message`);
      expect(req.request.body.userEmail).toBe('test@example.com');
      req.flush(mockResponse);
    });
  });

  describe('getChatConversation', () => {
    it('calls correct endpoint', () => {
      const mockResponse = { id: 'conv-123', messages: [] };

      service.getChatConversation('conv-123').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/chat/conversation/conv-123`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('getCalendarEvents', () => {
    it('calls correct endpoint', () => {
      const mockResponse = [{ id: '1', title: 'Event' }];

      service.getCalendarEvents('2024-01-01', '2024-01-31').subscribe();

      const req = httpMock.expectOne((request) => 
        request.url === `${baseUrl}/api/calendar/events` &&
        request.params.get('start') === '2024-01-01' &&
        request.params.get('end') === '2024-01-31'
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('sends date params', () => {
      const mockResponse = [];

      service.getCalendarEvents('2024-01-01T00:00:00Z', '2024-01-31T23:59:59Z').subscribe();

      const req = httpMock.expectOne((request) =>
        request.url === `${baseUrl}/api/calendar/events`
      );
      expect(req.request.params.get('start')).toBe('2024-01-01T00:00:00Z');
      expect(req.request.params.get('end')).toBe('2024-01-31T23:59:59Z');
      req.flush(mockResponse);
    });
  });

  describe('createCalendarEvent', () => {
    it('calls correct endpoint', () => {
      const newEvent = {
        title: 'New Event',
        start: '2024-01-15T10:00:00Z',
        end: '2024-01-15T11:00:00Z'
      };
      const mockResponse = { id: '1', ...newEvent };

      service.createCalendarEvent(newEvent).subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/calendar/events`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newEvent);
      req.flush(mockResponse);
    });
  });

  describe('updateCalendarEvent', () => {
    it('calls correct endpoint', () => {
      const updateEvent = { title: 'Updated Event' };
      const mockResponse = { id: '1', title: 'Updated Event', start: '', end: '' };

      service.updateCalendarEvent('1', updateEvent).subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/calendar/events/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateEvent);
      req.flush(mockResponse);
    });
  });

  describe('deleteCalendarEvent', () => {
    it('calls correct endpoint', () => {
      service.deleteCalendarEvent('1').subscribe();

      const req = httpMock.expectOne(`${baseUrl}/api/calendar/events/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  describe('handleError', () => {
    it('throws formatted error on api failure', async () => {
      let thrownError: Error | undefined;
      service.getAuthStatus().subscribe({
        error: (err) => thrownError = err
      });

      const req = httpMock.expectOne(`${baseUrl}/api/auth/status`);
      req.flush({ error: 'Something went wrong' }, { status: 400, statusText: 'Bad Request' });

      await new Promise(resolve => setTimeout(resolve, 10));
      expect(thrownError?.message).toContain('Something went wrong');
    });

    it('throws generic message on network error', async () => {
      let thrownError: Error | undefined;
      service.getAuthStatus().subscribe({
        error: (err) => thrownError = err
      });

      const req = httpMock.expectOne(`${baseUrl}/api/auth/status`);
      req.flush('Network error', { status: 0, statusText: 'Unknown Error' });

      await new Promise(resolve => setTimeout(resolve, 10));
      expect(thrownError).toBeDefined();
    });

    it('handles error with details field', async () => {
      let thrownError: Error | undefined;
      service.getAuthStatus().subscribe({
        error: (err) => thrownError = err
      });

      const req = httpMock.expectOne(`${baseUrl}/api/auth/status`);
      req.flush({ details: 'Detailed error message' }, { status: 400, statusText: 'Bad Request' });

      await new Promise(resolve => setTimeout(resolve, 10));
      expect(thrownError?.message).toContain('Detailed error message');
    });
  });
});
