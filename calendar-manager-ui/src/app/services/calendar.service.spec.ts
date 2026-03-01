import { TestBed } from '@angular/core/testing';
import { CalendarService } from './calendar.service';
import { ApiService } from './api';
import { CalendarEvent, CreateEvent, UpdateEvent } from '../models/calendar.models';
import { of, throwError, firstValueFrom } from 'rxjs';
import { vi, describe, it, expect, beforeEach } from 'vitest';

describe('CalendarService', () => {
  let service: CalendarService;
  let apiServiceMock: {
    getCalendarEvents: ReturnType<typeof vi.fn>;
    createCalendarEvent: ReturnType<typeof vi.fn>;
    updateCalendarEvent: ReturnType<typeof vi.fn>;
    deleteCalendarEvent: ReturnType<typeof vi.fn>;
  };

  const mockEvents: CalendarEvent[] = [
    {
      id: '1',
      title: 'Event 1',
      start: '2024-01-15T10:00:00Z',
      end: '2024-01-15T11:00:00Z'
    },
    {
      id: '2',
      title: 'Event 2',
      start: '2024-01-16T14:00:00Z',
      end: '2024-01-16T15:00:00Z'
    }
  ];

  const newEvent: CreateEvent = {
    title: 'New Event',
    start: '2024-01-17T09:00:00Z',
    end: '2024-01-17T10:00:00Z'
  };

  const updateEvent: UpdateEvent = {
    title: 'Updated Event'
  };

  beforeEach(() => {
    apiServiceMock = {
      getCalendarEvents: vi.fn(),
      createCalendarEvent: vi.fn(),
      updateCalendarEvent: vi.fn(),
      deleteCalendarEvent: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        CalendarService,
        { provide: ApiService, useValue: apiServiceMock }
      ]
    });

    service = TestBed.inject(CalendarService);
  });

  describe('loadEvents', () => {
    it('fetches events and updates signal', async () => {
      apiServiceMock.getCalendarEvents.mockReturnValue(of(mockEvents));

      service.loadEvents('2024-01-01', '2024-01-31');
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.events()).toEqual(mockEvents);
    });

    it('sets loading state during request', async () => {
      apiServiceMock.getCalendarEvents.mockReturnValue(of(mockEvents));

      service.loadEvents('2024-01-01', '2024-01-31');
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.loading()).toBe(false);
    });

    it('handles error gracefully', async () => {
      apiServiceMock.getCalendarEvents.mockReturnValue(throwError(() => new Error('API error')));

      service.loadEvents('2024-01-01', '2024-01-31');
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.loading()).toBe(false);
    });

    it('uses default date range when none provided', async () => {
      apiServiceMock.getCalendarEvents.mockReturnValue(of(mockEvents));

      service.loadEvents();
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(apiServiceMock.getCalendarEvents).toHaveBeenCalled();
      const callArgs = apiServiceMock.getCalendarEvents.mock.calls[0];
      expect(callArgs[0]).toBeDefined();
      expect(callArgs[1]).toBeDefined();
    });
  });

  describe('createEvent', () => {
    it('calls api and adds to local state', async () => {
      const createdEvent: CalendarEvent = { id: '3', ...newEvent };
      apiServiceMock.createCalendarEvent.mockReturnValue(of(createdEvent));

      const result = await firstValueFrom(service.createEvent(newEvent));

      expect(apiServiceMock.createCalendarEvent).toHaveBeenCalledWith(newEvent);
      expect(service.events()).toContain(createdEvent);
      expect(result).toEqual(createdEvent);
    });

    it('returns new event', async () => {
      const createdEvent: CalendarEvent = { id: '3', ...newEvent };
      apiServiceMock.createCalendarEvent.mockReturnValue(of(createdEvent));

      const result = await firstValueFrom(service.createEvent(newEvent));

      expect(result).toEqual(createdEvent);
    });

    it('sets loading state during request', async () => {
      const createdEvent: CalendarEvent = { id: '3', ...newEvent };
      apiServiceMock.createCalendarEvent.mockReturnValue(of(createdEvent));

      await firstValueFrom(service.createEvent(newEvent));
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.loading()).toBe(false);
    });
  });

  describe('updateEvent', () => {
    it('calls api and updates local state', async () => {
      apiServiceMock.getCalendarEvents.mockReturnValue(of(mockEvents));
      service.loadEvents();
      await new Promise(resolve => setTimeout(resolve, 10));

      const updatedEvent: CalendarEvent = { ...mockEvents[0], title: 'Updated Event' };
      apiServiceMock.updateCalendarEvent.mockReturnValue(of(updatedEvent));

      await firstValueFrom(service.updateEvent('1', updateEvent));

      expect(apiServiceMock.updateCalendarEvent).toHaveBeenCalledWith('1', updateEvent);
      const events = service.events();
      const updated = events.find(e => e.id === '1');
      expect(updated?.title).toBe('Updated Event');
    });
  });

  describe('deleteEvent', () => {
    it('calls api and removes from local state', async () => {
      apiServiceMock.getCalendarEvents.mockReturnValue(of(mockEvents));
      service.loadEvents();
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.events().length).toBe(2);

      apiServiceMock.deleteCalendarEvent.mockReturnValue(of(void 0));

      await firstValueFrom(service.deleteEvent('1'));

      expect(apiServiceMock.deleteCalendarEvent).toHaveBeenCalledWith('1');
      expect(service.events().length).toBe(1);
      expect(service.events().find(e => e.id === '1')).toBeUndefined();
    });
  });

  describe('formatEventDate', () => {
    it('formats correctly for timed event', () => {
      const event: CalendarEvent = {
        id: '1',
        title: 'Test Event',
        start: '2024-01-15T10:00:00Z',
        end: '2024-01-15T11:30:00Z'
      };

      const result = service.formatEventDate(event);

      expect(result).toContain('Jan');
      expect(result).toContain('15');
      expect(result).toContain('2024');
    });

    it('includes time range', () => {
      const event: CalendarEvent = {
        id: '1',
        title: 'Test Event',
        start: '2024-01-15T10:00:00Z',
        end: '2024-01-15T11:30:00Z'
      };

      const result = service.formatEventDate(event);

      expect(result).toContain('from');
      expect(result).toContain('to');
    });
  });

  describe('signals', () => {
    it('events signal reflects current state', async () => {
      apiServiceMock.getCalendarEvents.mockReturnValue(of(mockEvents));

      service.loadEvents();
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.events()).toEqual(mockEvents);
    });

    it('loading signal reflects current state', async () => {
      apiServiceMock.getCalendarEvents.mockReturnValue(of(mockEvents));

      expect(service.loading()).toBe(false);

      service.loadEvents();
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.loading()).toBe(false);
    });
  });
});
