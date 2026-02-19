import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { AuthService } from '../../services/auth';
import { CalendarService } from '../../services/calendar.service';
import { AiChatService, ChatMessage } from '../../services/ai-chat.service';
import { User } from '../../models/auth.models';
import { CalendarEvent } from '../../models/calendar.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardComponent implements OnInit {
  user$: Observable<User | undefined>;
  loading$: Observable<boolean>;
  
  // Calendar properties
  events$: Observable<CalendarEvent[]>;
  calendarLoading$: Observable<boolean>;
  
  // AI Chat properties
  messages$: Observable<ChatMessage[]>;
  chatProcessing$: Observable<boolean>;
  chatMessage: string = '';
  showChat: boolean = false;
  showCalendar: boolean = false;

  constructor(
    private authService: AuthService,
    private calendarService: CalendarService,
    private aiChatService: AiChatService
  ) {
    this.user$ = this.authService.user$;
    this.loading$ = this.authService.loading$;
    this.events$ = this.calendarService.events$;
    this.calendarLoading$ = this.calendarService.loading$;
    this.messages$ = this.aiChatService.messages$;
    this.chatProcessing$ = this.aiChatService.processing$;
  }

  ngOnInit() {
    // Component initialization
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
    this.showCalendar = true;
    this.calendarService.loadEvents();
  }

  onToggleChat() {
    this.showChat = !this.showChat;
  }

  onSendMessage() {
    if (this.chatMessage.trim()) {
      this.aiChatService.sendMessage(this.chatMessage.trim());
      this.chatMessage = '';
    }
  }

  onKeyPress(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSendMessage();
    }
  }

  formatEventDate(event: CalendarEvent): string {
    return this.calendarService.formatEventDate(event);
  }

  getMessageClass(message: ChatMessage): string {
    if (message.isUser) return 'user-message';
    return `ai-message ${message.type || 'info'}`;
  }
}
