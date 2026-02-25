import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CalendarEvent } from '../../../models/calendar.models';

export interface CalendarDay {
  date: Date;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  events: CalendarEvent[];
}

@Component({
  selector: 'app-calendar-view',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './calendar-view.html',
  styleUrl: './calendar-view.scss',
})
export class CalendarViewComponent implements OnChanges {
  @Input() events: CalendarEvent[] = [];
  @Input() highlightedEventId: string | null = null;
  @Input() loading: boolean = false;
  @Output() daySelected = new EventEmitter<Date>();
  @Output() eventSelected = new EventEmitter<CalendarEvent>();

  currentMonth: Date = new Date();
  calendarDays: CalendarDay[] = [];
  weekDays: string[] = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
  selectedDay: Date | null = null;
  today: Date = new Date();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['events'] || changes['currentMonth']) {
      this.generateCalendar();
    }
  }

  generateCalendar(): void {
    const year = this.currentMonth.getFullYear();
    const month = this.currentMonth.getMonth();

    const firstDayOfMonth = new Date(year, month, 1);
    const lastDayOfMonth = new Date(year, month + 1, 0);
    const startingDayOfWeek = firstDayOfMonth.getDay();
    const daysInMonth = lastDayOfMonth.getDate();

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    this.calendarDays = [];

    // Previous month days
    const prevMonth = new Date(year, month, 0);
    const daysInPrevMonth = prevMonth.getDate();

    for (let i = startingDayOfWeek - 1; i >= 0; i--) {
      const dayNumber = daysInPrevMonth - i;
      const date = new Date(year, month - 1, dayNumber);
      this.calendarDays.push({
        date,
        dayNumber,
        isCurrentMonth: false,
        isToday: false,
        events: this.getEventsForDate(date),
      });
    }

    // Current month days
    for (let day = 1; day <= daysInMonth; day++) {
      const date = new Date(year, month, day);
      const dateOnly = new Date(date);
      dateOnly.setHours(0, 0, 0, 0);

      this.calendarDays.push({
        date,
        dayNumber: day,
        isCurrentMonth: true,
        isToday: dateOnly.getTime() === today.getTime(),
        events: this.getEventsForDate(date),
      });
    }

    // Next month days to fill the grid (6 rows)
    const remainingDays = 42 - this.calendarDays.length;
    for (let day = 1; day <= remainingDays; day++) {
      const date = new Date(year, month + 1, day);
      this.calendarDays.push({
        date,
        dayNumber: day,
        isCurrentMonth: false,
        isToday: false,
        events: this.getEventsForDate(date),
      });
    }
  }

  getEventsForDate(date: Date): CalendarEvent[] {
    const dateOnly = new Date(date);
    dateOnly.setHours(0, 0, 0, 0);

    return this.events.filter((event) => {
      const eventStart = new Date(event.start);
      eventStart.setHours(0, 0, 0, 0);
      return eventStart.getTime() === dateOnly.getTime();
    });
  }

  previousMonth(): void {
    this.currentMonth = new Date(
      this.currentMonth.getFullYear(),
      this.currentMonth.getMonth() - 1,
      1
    );
    this.generateCalendar();
  }

  nextMonth(): void {
    this.currentMonth = new Date(
      this.currentMonth.getFullYear(),
      this.currentMonth.getMonth() + 1,
      1
    );
    this.generateCalendar();
  }

  goToToday(): void {
    this.currentMonth = new Date();
    this.generateCalendar();
  }

  onDayClick(day: CalendarDay): void {
    this.selectedDay = day.date;
    this.daySelected.emit(day.date);
  }

  onEventClick(event: CalendarEvent, event_: MouseEvent): void {
    event_.stopPropagation();
    this.eventSelected.emit(event);
  }

  getMonthYear(): string {
    return this.currentMonth.toLocaleDateString('en-US', {
      month: 'long',
      year: 'numeric',
    });
  }

  isEventHighlighted(event: CalendarEvent): boolean {
    return this.highlightedEventId === event.id;
  }

  formatEventTime(event: CalendarEvent): string {
    const start = new Date(event.start);
    return start.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  }
}
