import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable, Subscription } from 'rxjs';
import { AuthService } from '../../services/auth';
import { CalendarService } from '../../services/calendar.service';
import { AiChatService, ChatMessage } from '../../services/ai-chat.service';
import { User } from '../../models/auth.models';
import { CalendarEvent } from '../../models/calendar.models';
import { ChatPanelComponent } from './chat/chat-panel';
import { CalendarViewComponent } from './calendar/calendar-view';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, ChatPanelComponent, CalendarViewComponent],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardComponent implements OnInit, OnDestroy {
  user$: Observable<User | undefined>;
  loading$: Observable<boolean>;
  isAuthenticated$: Observable<boolean>;

  // Calendar properties
  events$: Observable<CalendarEvent[]>;
  calendarLoading$: Observable<boolean>;

  // AI Chat properties
  messages$: Observable<ChatMessage[]>;
  chatProcessing$: Observable<boolean>;

  // Local state for new event highlighting
  highlightedEventId: string | null = null;
  private subscriptions: Subscription = new Subscription();

  // Mobile view toggle
  activePanel: 'chat' | 'calendar' = 'chat';
  showMobileToggle: boolean = false;

  constructor(
    private authService: AuthService,
    private calendarService: CalendarService,
    private aiChatService: AiChatService
  ) {
    this.user$ = this.authService.user$;
    this.loading$ = this.authService.loading$;
    this.isAuthenticated$ = this.authService.isAuthenticated$;
    this.events$ = this.calendarService.events$;
    this.calendarLoading$ = this.calendarService.loading$;
    this.messages$ = this.aiChatService.messages$;
    this.chatProcessing$ = this.aiChatService.processing$;
  }

  ngOnInit() {
    // Check screen size for mobile toggle
    this.checkScreenSize();
    if (typeof window !== 'undefined') {
      window.addEventListener('resize', () => this.checkScreenSize());
    }

    // Subscribe to authentication status and load calendar when authenticated
    this.subscriptions.add(
      this.isAuthenticated$.subscribe((isAuthenticated) => {
        if (isAuthenticated) {
          this.loadCalendarEvents();
        }
      })
    );

    // Subscribe to calendar events to detect new events
    this.subscriptions.add(
      this.events$.subscribe((events) => {
        if (events.length > 0) {
          // Check for the most recently created/updated event
          const latestEvent = events.reduce((latest, event) => {
            const eventTime = new Date(event.start).getTime();
            const latestTime = latest ? new Date(latest.start).getTime() : 0;
            return eventTime > latestTime ? event : latest;
          }, null as CalendarEvent | null);

          if (latestEvent) {
            this.highlightEvent(latestEvent.id);
          }
        }
      })
    );
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
    if (typeof window !== 'undefined') {
      window.removeEventListener('resize', () => this.checkScreenSize());
    }
  }

  private checkScreenSize() {
    if (typeof window !== 'undefined') {
      this.showMobileToggle = window.innerWidth < 768;
    }
  }

  private highlightEvent(eventId: string) {
    this.highlightedEventId = eventId;
    // Remove highlight after 3 seconds
    setTimeout(() => {
      this.highlightedEventId = null;
    }, 3000);
  }

  onLogout() {
    if (confirm('Are you sure you want to logout?')) {
      this.authService.logout().subscribe({
        next: (success) => {
          if (success) {
            console.log('Logged out successfully');
          } else {
            alert('Logout failed. Please try again.');
          }
        },
        error: (error) => {
          console.error('Logout error:', error);
          alert('Logout failed: ' + error.message);
        }
      });
    }
  }

  onTestToken() {
    this.authService.testToken().subscribe({
      next: (success) => {
        if (success) {
          alert('✅ Access token is valid and working!');
        } else {
          alert('❌ Token test failed');
        }
      },
      error: (error) => {
        console.error('Token test error:', error);
        alert('❌ Token test failed: ' + error.message);
      }
    });
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
    // Could filter events or show more details
  }

  onEventSelected(event: CalendarEvent) {
    console.log('Event selected:', event);
    // Could show event details modal
  }

  setActivePanel(panel: 'chat' | 'calendar') {
    this.activePanel = panel;
  }

  private loadCalendarEvents() {
    // Load events from 6 months ago to 6 months ahead
    const now = new Date();
    const start = new Date(now.getFullYear(), now.getMonth() - 6, 1).toISOString();
    const end = new Date(now.getFullYear(), now.getMonth() + 6, 0).toISOString();
    console.log('Loading events from', start, 'to', end);
    this.calendarService.loadEvents(start, end);
  }
}
