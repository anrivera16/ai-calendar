import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { ApiService } from './api';
import { CalendarEvent, CreateEvent, UpdateEvent } from '../models/calendar.models';

@Injectable({
  providedIn: 'root'
})
export class CalendarService {
  private eventsSubject = new BehaviorSubject<CalendarEvent[]>([]);
  public events$ = this.eventsSubject.asObservable();

  private loadingSubject = new BehaviorSubject<boolean>(false);
  public loading$ = this.loadingSubject.asObservable();

  constructor(private apiService: ApiService) { }

  loadEvents(start?: string, end?: string): void {
    this.loadingSubject.next(true);

    // Default to current month if no dates provided
    if (!start || !end) {
      const now = new Date();
      start = new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
      end = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString();
    }

    this.apiService.getCalendarEvents(start, end).subscribe({
      next: (events) => {
        console.log('Loaded events:', events.length, 'from', start, 'to', end);
        this.eventsSubject.next(events);
        this.loadingSubject.next(false);
      },
      error: (error) => {
        console.error('Failed to load calendar events:', error);
        this.loadingSubject.next(false);
        // Keep existing events on error
      }
    });
  }

  createEvent(event: CreateEvent): Observable<CalendarEvent> {
    this.loadingSubject.next(true);

    return new Observable(observer => {
      this.apiService.createCalendarEvent(event).subscribe({
        next: (newEvent) => {
          // Add the new event to current events
          const currentEvents = this.eventsSubject.value;
          this.eventsSubject.next([...currentEvents, newEvent]);
          this.loadingSubject.next(false);
          observer.next(newEvent);
          observer.complete();
        },
        error: (error) => {
          this.loadingSubject.next(false);
          observer.error(error);
        }
      });
    });
  }

  updateEvent(eventId: string, event: UpdateEvent): Observable<CalendarEvent> {
    this.loadingSubject.next(true);

    return new Observable(observer => {
      this.apiService.updateCalendarEvent(eventId, event).subscribe({
        next: (updatedEvent) => {
          // Update the event in current events
          const currentEvents = this.eventsSubject.value;
          const index = currentEvents.findIndex(e => e.id === eventId);
          if (index !== -1) {
            currentEvents[index] = updatedEvent;
            this.eventsSubject.next([...currentEvents]);
          }
          this.loadingSubject.next(false);
          observer.next(updatedEvent);
          observer.complete();
        },
        error: (error) => {
          this.loadingSubject.next(false);
          observer.error(error);
        }
      });
    });
  }

  deleteEvent(eventId: string): Observable<void> {
    this.loadingSubject.next(true);

    return new Observable(observer => {
      this.apiService.deleteCalendarEvent(eventId).subscribe({
        next: () => {
          // Remove the event from current events
          const currentEvents = this.eventsSubject.value;
          const filteredEvents = currentEvents.filter(e => e.id !== eventId);
          this.eventsSubject.next(filteredEvents);
          this.loadingSubject.next(false);
          observer.next();
          observer.complete();
        },
        error: (error) => {
          this.loadingSubject.next(false);
          observer.error(error);
        }
      });
    });
  }

  // Helper method to format events for display
  formatEventDate(event: CalendarEvent): string {
    const start = new Date(event.start);
    const end = new Date(event.end);

    const dateFormatter = new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });

    const timeFormatter = new Intl.DateTimeFormat('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });

    const startDate = dateFormatter.format(start);
    const startTime = timeFormatter.format(start);
    const endTime = timeFormatter.format(end);

    return `${startDate} from ${startTime} to ${endTime}`;
  }
}
