import { Component, input, output, signal, computed } from '@angular/core';
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
export class CalendarViewComponent {
  events = input<CalendarEvent[]>([]);
  highlightedEventId = input<string | null>(null);
  loading = input<boolean>(false);
  daySelected = output<Date>();
  eventSelected = output<CalendarEvent>();

  currentMonth = signal(new Date());
  selectedDay = signal<Date | null>(null);
  today = new Date();
  weekDays: string[] = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  calendarDays = computed(() => this.generateCalendarDays());

  private generateCalendarDays(): CalendarDay[] {
    const month = this.currentMonth();
    const year = month.getFullYear();
    const monthIndex = month.getMonth();

    const firstDayOfMonth = new Date(year, monthIndex, 1);
    const lastDayOfMonth = new Date(year, monthIndex + 1, 0);
    const startingDayOfWeek = firstDayOfMonth.getDay();
    const daysInMonth = lastDayOfMonth.getDate();

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const days: CalendarDay[] = [];

    const prevMonth = new Date(year, monthIndex, 0);
    const daysInPrevMonth = prevMonth.getDate();

    for (let i = startingDayOfWeek - 1; i >= 0; i--) {
      const dayNumber = daysInPrevMonth - i;
      const date = new Date(year, monthIndex - 1, dayNumber);
      days.push({
        date,
        dayNumber,
        isCurrentMonth: false,
        isToday: false,
        events: this.getEventsForDate(date),
      });
    }

    for (let day = 1; day <= daysInMonth; day++) {
      const date = new Date(year, monthIndex, day);
      const dateOnly = new Date(date);
      dateOnly.setHours(0, 0, 0, 0);

      days.push({
        date,
        dayNumber: day,
        isCurrentMonth: true,
        isToday: dateOnly.getTime() === today.getTime(),
        events: this.getEventsForDate(date),
      });
    }

    const remainingDays = 42 - days.length;
    for (let day = 1; day <= remainingDays; day++) {
      const date = new Date(year, monthIndex + 1, day);
      days.push({
        date,
        dayNumber: day,
        isCurrentMonth: false,
        isToday: false,
        events: this.getEventsForDate(date),
      });
    }

    return days;
  }

  getEventsForDate(date: Date): CalendarEvent[] {
    const dateOnly = new Date(date);
    dateOnly.setHours(0, 0, 0, 0);

    return this.events().filter((event) => {
      const eventStart = new Date(event.start);
      eventStart.setHours(0, 0, 0, 0);
      return eventStart.getTime() === dateOnly.getTime();
    });
  }

  previousMonth(): void {
    const current = this.currentMonth();
    this.currentMonth.set(new Date(current.getFullYear(), current.getMonth() - 1, 1));
  }

  nextMonth(): void {
    const current = this.currentMonth();
    this.currentMonth.set(new Date(current.getFullYear(), current.getMonth() + 1, 1));
  }

  goToToday(): void {
    this.currentMonth.set(new Date());
  }

  onDayClick(day: CalendarDay): void {
    this.selectedDay.set(day.date);
    this.daySelected.emit(day.date);
  }

  onEventClick(event: CalendarEvent, event_: MouseEvent): void {
    event_.stopPropagation();
    this.eventSelected.emit(event);
  }

  getMonthYear(): string {
    return this.currentMonth().toLocaleDateString('en-US', {
      month: 'long',
      year: 'numeric',
    });
  }

  isEventHighlighted(event: CalendarEvent): boolean {
    return this.highlightedEventId() === event.id;
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
