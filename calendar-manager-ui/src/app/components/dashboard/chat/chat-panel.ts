import { Component, Input, Output, EventEmitter, ViewChild, ElementRef, AfterViewChecked, OnChanges, SimpleChanges } from '@angular/core';
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
export class ChatPanelComponent implements AfterViewChecked, OnChanges {
  @Input() messages: ChatMessage[] = [];
  @Input() processing: boolean = false;
  @Output() sendMessage = new EventEmitter<string>();

  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;

  messageInput: string = '';
  private shouldScrollToBottom: boolean = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['messages']) {
      this.shouldScrollToBottom = true;
    }
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  onSend(): void {
    if (this.messageInput.trim() && !this.processing) {
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
    return false; // Event message support can be added later
  }

  private scrollToBottom(): void {
    try {
      this.messagesContainer.nativeElement.scrollTop =
        this.messagesContainer.nativeElement.scrollHeight;
    } catch (err) {
      // Ignore scroll errors
    }
  }
}
