import { Component, ElementRef, effect, input, output, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatMessage } from '../../../services/ai-chat.service';

@Component({
  selector: 'app-chat-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-panel.html',
  styleUrl: './chat-panel.scss',
})
export class ChatPanelComponent {
  messages = input<ChatMessage[]>([]);
  processing = input<boolean>(false);
  sendMessage = output<string>();

  messagesContainer = viewChild<ElementRef>('messagesContainer');

  messageInput: string = '';

  constructor() {
    effect(() => {
      const messages = this.messages();
      if (messages.length > 0) {
        this.scrollToBottom();
      }
    });
  }

  onSend(): void {
    if (this.messageInput.trim() && !this.processing()) {
      this.sendMessage.emit(this.messageInput.trim());
      this.messageInput = '';
    }
  }

  onKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSend();
    }
  }

  getMessageClass(message: ChatMessage): string {
    if (message.isUser) {
      return 'user-message';
    }
    return `ai-message ${message.type || 'info'}`;
  }

  isEventMessage(message: ChatMessage): boolean {
    return false;
  }

  private scrollToBottom(): void {
    try {
      const container = this.messagesContainer();
      if (container) {
        container.nativeElement.scrollTop = container.nativeElement.scrollHeight;
      }
    } catch (err) {
      // Ignore scroll errors
    }
  }
}
