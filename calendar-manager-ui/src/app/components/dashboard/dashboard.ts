import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { fromEvent } from 'rxjs';
import { AuthService } from '../../services/auth';
import { CalendarService } from '../../services/calendar.service';
import { AiChatService } from '../../services/ai-chat.service';
import { User } from '../../models/auth.models';
import { CalendarEvent } from '../../models/calendar.models';
import { ChatPanelComponent } from './chat/chat-panel';
import { CalendarViewComponent } from './calendar/calendar-view';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, ChatPanelComponent, CalendarViewComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardComponent {
  private readonly authService = inject(AuthService);
  private readonly calendarService = inject(CalendarService);
  private readonly aiChatService = inject(AiChatService);
  private readonly destroyRef = inject(DestroyRef);

  user = toSignal(this.authService.user$, { initialValue: undefined as User | undefined });
  loading = toSignal(this.authService.loading$, { initialValue: false });
  isAuthenticated = toSignal(this.authService.isAuthenticated$, { initialValue: false });

  events = this.calendarService.events;
  calendarLoading = this.calendarService.loading;

  messages = this.aiChatService.messages;
  chatProcessing = this.aiChatService.processing;

  highlightedEventId = signal<string | null>(null);
  activePanel = signal<'chat' | 'calendar'>('chat');
  showMobileToggle = signal<boolean>(false);

  constructor() {
    effect(() => {
      if (this.isAuthenticated()) {
        this.loadCalendarEvents();
      }
    });

    effect(() => {
      const events = this.events();
      if (events.length > 0) {
        const latestEvent = events.reduce(
          (latest, event) => {
            const eventTime = new Date(event.start).getTime();
            const latestTime = latest ? new Date(latest.start).getTime() : 0;
            return eventTime > latestTime ? event : latest;
          },
          null as CalendarEvent | null,
        );

        if (latestEvent) {
          this.highlightEvent(latestEvent.id);
        }
      }
    });

    if (typeof window !== 'undefined') {
      this.checkScreenSize();
      fromEvent(window, 'resize')
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this.checkScreenSize());
    }
  }

  private checkScreenSize() {
    if (typeof window !== 'undefined') {
      this.showMobileToggle.set(window.innerWidth < 768);
    }
  }

  private highlightEvent(eventId: string) {
    this.highlightedEventId.set(eventId);
    setTimeout(() => {
      this.highlightedEventId.set(null);
    }, 3000);
  }

  onLogout() {
    if (confirm('Are you sure you want to logout?')) {
      this.authService.logout();
    }
  }

  onTestToken() {
    this.authService.testToken();
  }

  onLoadCalendar() {
    this.calendarService.loadEvents();
  }

  onSendMessage(message: string) {
    if (message.trim()) {
      this.aiChatService.sendMessage(message.trim());
    }
  }

  onDaySelected(date: Date) {
    console.log('Day selected:', date);
  }

  onEventSelected(event: CalendarEvent) {
    console.log('Event selected:', event);
  }

  setActivePanel(panel: 'chat' | 'calendar') {
    this.activePanel.set(panel);
  }

  private loadCalendarEvents() {
    const now = new Date();
    const start = new Date(now.getFullYear(), now.getMonth() - 6, 1).toISOString();
    const end = new Date(now.getFullYear(), now.getMonth() + 6, 0).toISOString();
    console.log('Loading events from', start, 'to', end);
    this.calendarService.loadEvents(start, end);
  }
}
