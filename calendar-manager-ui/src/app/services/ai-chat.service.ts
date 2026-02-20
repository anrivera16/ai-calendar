import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { CalendarService } from './calendar.service';
import { ApiService } from './api';
import { CreateEvent } from '../models/calendar.models';

export interface ChatMessage {
  id: string;
  text: string;
  isUser: boolean;
  timestamp: Date;
  type?: 'info' | 'success' | 'error' | 'warning';
}

@Injectable({
  providedIn: 'root'
})
export class AiChatService {
  private messagesSubject = new BehaviorSubject<ChatMessage[]>([
    {
      id: '1',
      text: '👋 Hi! I\'m your AI calendar assistant. Try saying things like:\n\n• "Schedule a meeting with John tomorrow at 2pm"\n• "Show me my events for next week"\n• "Create a lunch appointment on Friday at noon"\n• "Cancel my 3pm meeting today"',
      isUser: false,
      timestamp: new Date(),
      type: 'info'
    }
  ]);
  
  public messages$ = this.messagesSubject.asObservable();

  private processingSubject = new BehaviorSubject<boolean>(false);
  public processing$ = this.processingSubject.asObservable();

  private conversationId: string | undefined;

  constructor(
    private calendarService: CalendarService,
    private apiService: ApiService
  ) { }

  sendMessage(message: string): void {
    // Add user message
    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      text: message,
      isUser: true,
      timestamp: new Date()
    };
    
    this.addMessage(userMessage);
    this.processingSubject.next(true);

    // Add a safety timeout to prevent infinite loading
    const safetyTimeout = setTimeout(() => {
      if (this.processingSubject.value) {
        this.processingSubject.next(false);
        const timeoutMessage: ChatMessage = {
          id: Date.now().toString(),
          text: `⏱️ **Request timed out**\n\nThe AI service is taking longer than expected. Please try your request again.`,
          isUser: false,
          timestamp: new Date(),
          type: 'warning'
        };
        this.addMessage(timeoutMessage);
      }
    }, 35000); // 35 second safety timeout

    // Call Claude backend
    this.apiService.processChatMessage(message, 'test@example.com', this.conversationId)
      .subscribe({
        next: (response) => {
          clearTimeout(safetyTimeout);
          this.handleBackendResponse(response);
          this.processingSubject.next(false);
        },
        error: (error) => {
          clearTimeout(safetyTimeout);
          this.handleError(error);
          this.processingSubject.next(false);
        }
      });
  }

  private addMessage(message: ChatMessage): void {
    const currentMessages = this.messagesSubject.value;
    this.messagesSubject.next([...currentMessages, message]);
  }



  private handleBackendResponse(response: any): void {
    // Update conversation ID
    if (response.conversationId) {
      this.conversationId = response.conversationId;
    }

    // Add Claude's response message
    const aiMessage: ChatMessage = {
      id: Date.now().toString(),
      text: response.message || 'I processed your request.',
      isUser: false,
      timestamp: new Date(),
      type: this.mapMessageType(response.type)
    };
    
    this.addMessage(aiMessage);

    // Handle any calendar actions that were executed
    if (response.actions && response.actions.length > 0) {
      this.handleCalendarActions(response.actions);
    }
  }

  private handleError(error: any): void {
    console.error('Chat API error:', error);
    
    let errorText = '';
    
    // Check for specific error types
    if (error.name === 'TimeoutError') {
      errorText = `⏱️ **Request timed out**\n\nThe AI service is taking longer than expected. This might be due to:\n• High API usage\n• Network connectivity issues\n• Service maintenance\n\nPlease try again in a moment.`;
    } else if (error.status === 500) {
      errorText = `🛠️ **Service temporarily unavailable**\n\nOur AI assistant is experiencing technical difficulties. We're working to resolve this quickly.\n\nYou can still:\n• View your calendar events using the dashboard\n• Access Google Calendar directly\n• Try again in a few minutes`;
    } else if (error.message?.includes('credit') || error.message?.includes('billing')) {
      errorText = `💰 **AI Credits Required**\n\nOur Claude AI assistant needs credits to provide advanced responses.\n\n**In the meantime, I can still help with:**\n• Basic calendar guidance\n• Scheduling advice\n• Time management tips\n\nFor full AI features, please add credits to your Claude API account.`;
    } else {
      errorText = `❌ **Connection Issue**\n\nI'm having trouble connecting to the AI service right now.\n\n**You can:**\n• Try refreshing the page\n• Check your internet connection\n• Use the dashboard for calendar operations\n• Try again in a few moments\n\n*Error: ${error.message || 'Network error'}*`;
    }
    
    const errorMessage: ChatMessage = {
      id: Date.now().toString(),
      text: errorText,
      isUser: false,
      timestamp: new Date(),
      type: 'error'
    };
    
    this.addMessage(errorMessage);
  }

  private mapMessageType(backendType: string): 'info' | 'success' | 'error' | 'warning' {
    switch (backendType?.toLowerCase()) {
      case 'success': return 'success';
      case 'error': return 'error';
      case 'warning': return 'warning';
      default: return 'info';
    }
  }

  private handleCalendarActions(actions: any[]): void {
    const executedActions = actions.filter(a => a.executed);
    const failedActions = actions.filter(a => !a.executed);

    // Refresh calendar if events were created/modified
    if (executedActions.some(a => a.type === 'create_calendar_event' || a.type === 'list_calendar_events')) {
      this.calendarService.loadEvents();
    }

    // Show action results
    executedActions.forEach(action => {
      if (action.result) {
        const resultMessage: ChatMessage = {
          id: Date.now().toString(),
          text: `✅ ${action.result}`,
          isUser: false,
          timestamp: new Date(),
          type: 'success'
        };
        this.addMessage(resultMessage);
      }
    });

    failedActions.forEach(action => {
      if (action.errorMessage) {
        const errorMessage: ChatMessage = {
          id: Date.now().toString(),
          text: `❌ ${action.errorMessage}`,
          isUser: false,
          timestamp: new Date(),
          type: 'error'
        };
        this.addMessage(errorMessage);
      }
    });
  }

  clearChat(): void {
    this.messagesSubject.next([]);
  }
}