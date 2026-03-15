import { Injectable, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { toObservable } from '@angular/core/rxjs-interop';
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

export interface ConversationSummary {
  conversationId: string;
  title: string;
  createdAt: Date;
  updatedAt: Date;
  messageCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class AiChatService {
  private messagesSignal = signal<ChatMessage[]>([
    {
      id: '1',
      text: '👋 Hi! I\'m your AI calendar assistant. Try saying things like:\n\n• "Schedule a meeting with John tomorrow at 2pm"\n• "Show me my events for next week"\n• "Create a lunch appointment on Friday at noon"\n• "Cancel my 3pm meeting today"',
      isUser: false,
      timestamp: new Date(),
      type: 'info'
    }
  ]);

  public readonly messages$ = toObservable(this.messagesSignal);
  public readonly messages = this.messagesSignal.asReadonly();

  private processingSignal = signal<boolean>(false);
  public readonly processing$ = toObservable(this.processingSignal);
  public readonly processing = this.processingSignal.asReadonly();

  private conversationId: string | undefined;
  public get currentConversationId(): string | undefined { return this.conversationId; }

  constructor(
    private calendarService: CalendarService,
    private apiService: ApiService
  ) { }

  /**
   * Start a new conversation - clears the current conversation ID and messages
   */
  startNewConversation(): void {
    this.conversationId = undefined;
    this.messagesSignal.set([
      {
        id: Date.now().toString(),
        text: '👋 Hi! I\'m your AI calendar assistant. Try saying things like:\n\n• "Schedule a meeting with John tomorrow at 2pm"\n• "Show me my events for next week"\n• "Create a lunch appointment on Friday at noon"\n• "Cancel my 3pm meeting today"',
        isUser: false,
        timestamp: new Date(),
        type: 'info'
      }
    ]);
  }

  /**
   * Load all conversations for the current user
   */
  getConversations(): Observable<{ conversations: ConversationSummary[] }> {
    return this.apiService.getChatConversations();
  }

  /**
   * Load a specific conversation with its message history
   */
  loadConversation(conversationId: string): void {
    this.conversationId = conversationId;
    this.processingSignal.set(true);

    this.apiService.getChatConversation(conversationId).subscribe({
      next: (response) => {
        // Convert backend messages to ChatMessage format
        const loadedMessages: ChatMessage[] = response.messages.map((m: any) => ({
          id: m.id,
          text: m.content,
          isUser: m.role === 'user',
          timestamp: new Date(m.timestamp),
          type: m.role === 'user' ? undefined : 'info'
        }));

        if (loadedMessages.length === 0) {
          // No messages yet, show welcome
          loadedMessages.push({
            id: Date.now().toString(),
            text: '👋 This conversation is empty. Start by sending a message!',
            isUser: false,
            timestamp: new Date(),
            type: 'info'
          });
        }

        this.messagesSignal.set(loadedMessages);
        this.processingSignal.set(false);
      },
      error: (error) => {
        console.error('Error loading conversation:', error);
        this.handleError(error);
        this.processingSignal.set(false);
      }
    });
  }

  /**
   * Delete a conversation
   */
  deleteConversation(conversationId: string): Observable<any> {
    return this.apiService.deleteChatConversation(conversationId);
  }

  sendMessage(message: string): void {
    // Add user message
    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      text: message,
      isUser: true,
      timestamp: new Date()
    };

    this.addMessage(userMessage);
    this.processingSignal.set(true);

    // Add a safety timeout to prevent infinite loading
    const safetyTimeout = setTimeout(() => {
      if (this.processingSignal()) {
        this.processingSignal.set(false);
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
    console.log('🔄 Sending message to backend:', message);
    this.apiService.processChatMessage(message, this.conversationId)
      .subscribe({
        next: (response) => {
          console.log('✅ Received backend response:', response);
          clearTimeout(safetyTimeout);
          this.handleBackendResponse(response);
          this.processingSignal.set(false);
        },
        error: (error) => {
          console.error('❌ Backend error:', error);
          clearTimeout(safetyTimeout);
          this.handleError(error);
          this.processingSignal.set(false);
        }
      });
  }

  private addMessage(message: ChatMessage): void {
    this.messagesSignal.update(messages => [...messages, message]);
  }



  private handleBackendResponse(response: any): void {
    console.log('📝 Processing backend response:', response);

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

  private mapMessageType(backendType: string | number): 'info' | 'success' | 'error' | 'warning' {
    // Handle both string and numeric backend types
    const typeStr = typeof backendType === 'number' ?
      this.getMessageTypeFromNumber(backendType) :
      backendType?.toString()?.toLowerCase();

    switch (typeStr) {
      case 'success': return 'success';
      case 'error': return 'error';
      case 'warning': return 'warning';
      default: return 'info';
    }
  }

  private getMessageTypeFromNumber(type: number): string {
    // Based on the backend MessageType enum
    switch (type) {
      case 0: return 'info';
      case 1: return 'success';
      case 2: return 'warning';
      case 3: return 'error';
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
    this.messagesSignal.set([]);
  }
}
