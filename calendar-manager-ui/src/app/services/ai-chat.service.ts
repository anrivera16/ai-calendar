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

    // Call Claude backend
    this.apiService.processChatMessage(message, 'test@example.com', this.conversationId)
      .subscribe({
        next: (response) => {
          this.handleBackendResponse(response);
          this.processingSubject.next(false);
        },
        error: (error) => {
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
    
    const errorMessage: ChatMessage = {
      id: Date.now().toString(),
      text: `❌ Sorry, I encountered an error: ${error.message || 'Please try again.'}`,
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