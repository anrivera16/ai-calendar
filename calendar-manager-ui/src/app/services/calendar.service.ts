import { Injectable, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { toObservable } from '@angular/core/rxjs-interop';
import { ApiService } from './api';
import { CalendarEvent, CreateEvent, UpdateEvent } from '../models/calendar.models';

@Injectable({
  providedIn: 'root'
})
export class CalendarService {
  private eventsSignal = signal<CalendarEvent[]>([]);
  public readonly events$ = toObservable(this.eventsSignal);
  public readonly events = this.eventsSignal.asReadonly();

  private loadingSignal = signal<boolean>(false);
  public readonly loading$ = toObservable(this.loadingSignal);
  public readonly loading = this.loadingSignal.asReadonly();

  constructor(private apiService: ApiService) { }

  loadEvents(start?: string, end?: string): void {
    this.loadingSignal.set(true);

    // Default to current month if no dates provided
    if (!start || !end) {
      const now = new Date();
      start = new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
      end = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString();
    }

    this.apiService.getCalendarEvents(start, end).subscribe({
      next: (events) => {
        console.log('Loaded events:', events.length, 'from', start, 'to', end);
        this.eventsSignal.set(events);
        this.loadingSignal.set(false);
      },
      error: (error) => {
        console.error('Failed to load calendar events:', error);
        this.loadingSignal.set(false);
        // Keep existing events on error
      }
    });
  }

  createEvent(event: CreateEvent): Observable<CalendarEvent> {
    this.loadingSignal.set(true);

    return new Observable(observer => {
      this.apiService.createCalendarEvent(event).subscribe({
        next: (newEvent) => {
          // Add the new event to current events
          this.eventsSignal.update(events => [...events, newEvent]);
          this.loadingSignal.set(false);
          observer.next(newEvent);
          observer.complete();
        },
        error: (error) => {
          this.loadingSignal.set(false);
          observer.error(error);
        }
      });
    });
  }

  updateEvent(eventId: string, event: UpdateEvent): Observable<CalendarEvent> {
    this.loadingSignal.set(true);

    return new Observable(observer => {
      this.apiService.updateCalendarEvent(eventId, event).subscribe({
        next: (updatedEvent) => {
          // Update the event in current events
          this.eventsSignal.update(events => {
            const index = events.findIndex(e => e.id === eventId);
            if (index !== -1) {
              const updated = [...events];
              updated[index] = updatedEvent;
              return updated;
            }
            return events;
          });
          this.loadingSignal.set(false);
          observer.next(updatedEvent);
          observer.complete();
        },
        error: (error) => {
          this.loadingSignal.set(false);
          observer.error(error);
        }
      });
    });
  }

  deleteEvent(eventId: string): Observable<void> {
    this.loadingSignal.set(true);

    return new Observable(observer => {
      this.apiService.deleteCalendarEvent(eventId).subscribe({
        next: () => {
          // Remove the event from current events
          this.eventsSignal.update(events => events.filter(e => e.id !== eventId));
          this.loadingSignal.set(false);
          observer.next();
          observer.complete();
        },
        error: (error) => {
          this.loadingSignal.set(false);
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
