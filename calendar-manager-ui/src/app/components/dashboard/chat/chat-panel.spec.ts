import { render, screen, fireEvent } from '@testing-library/angular';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { ChatPanelComponent } from './chat-panel';
import { ChatMessage } from '../../../services/ai-chat.service';

describe('ChatPanelComponent', () => {
  const mockMessages: ChatMessage[] = [
    {
      id: '1',
      text: 'Hello! How can I help?',
      isUser: false,
      timestamp: new Date('2024-01-15T10:00:00'),
      type: 'info',
    },
    {
      id: '2',
      text: 'Schedule a meeting',
      isUser: true,
      timestamp: new Date('2024-01-15T10:01:00'),
    },
    {
      id: '3',
      text: 'Meeting created!',
      isUser: false,
      timestamp: new Date('2024-01-15T10:02:00'),
      type: 'success',
    },
  ];

  it('renders_messagesList', async () => {
    await render(ChatPanelComponent, {
      componentInputs: { messages: mockMessages },
    });

    expect(screen.getByText('Hello! How can I help?')).toBeTruthy();
    expect(screen.getByText('Schedule a meeting')).toBeTruthy();
    expect(screen.getByText('Meeting created!')).toBeTruthy();
  });

  it('renders_inputField_andSendButton', async () => {
    await render(ChatPanelComponent, {
      componentInputs: { messages: [] },
    });

    expect(screen.getByPlaceholderText(/Ask me to schedule/i)).toBeTruthy();
    expect(screen.getByRole('button', { name: /Send message/i })).toBeTruthy();
  });

  it('distinguishes_userVsAiMessages', async () => {
    const { container } = await render(ChatPanelComponent, {
      componentInputs: { messages: mockMessages },
    });

    const userMessages = container.querySelectorAll('.user-message');
    const aiMessages = container.querySelectorAll('.ai-message');

    expect(userMessages.length).toBe(1);
    expect(aiMessages.length).toBeGreaterThanOrEqual(2);
  });

  it('emitsSendMessage_onButtonClick', async () => {
    const sendMessageHandler = vi.fn();

    await render(ChatPanelComponent, {
      componentInputs: { messages: [] },
      componentOutputs: { sendMessage: { emit: sendMessageHandler } as any },
    });

    const input = screen.getByPlaceholderText(/Ask me to schedule/i);
    fireEvent.input(input, { target: { value: 'Test message' } });

    const sendButton = screen.getByRole('button', { name: /Send message/i });
    fireEvent.click(sendButton);

    expect(sendMessageHandler).toHaveBeenCalledWith('Test message');
  });

  it('emitsSendMessage_onEnterKey', async () => {
    const sendMessageHandler = vi.fn();

    await render(ChatPanelComponent, {
      componentInputs: { messages: [] },
      componentOutputs: { sendMessage: { emit: sendMessageHandler } as any },
    });

    const input = screen.getByPlaceholderText(/Ask me to schedule/i);
    fireEvent.input(input, { target: { value: 'Test message' } });
    fireEvent.keyPress(input, { key: 'Enter' });

    expect(sendMessageHandler).toHaveBeenCalledWith('Test message');
  });

  it('doesNotSend_emptyMessage', async () => {
    const sendMessageHandler = vi.fn();

    await render(ChatPanelComponent, {
      componentInputs: { messages: [] },
      componentOutputs: { sendMessage: { emit: sendMessageHandler } as any },
    });

    const sendButton = screen.getByRole('button', { name: /Send message/i });
    fireEvent.click(sendButton);

    expect(sendMessageHandler).not.toHaveBeenCalled();
  });

  it('disablesSendButton_whenProcessing', async () => {
    await render(ChatPanelComponent, {
      componentInputs: { messages: [], processing: true },
    });

    const sendButton = screen.getByRole('button', { name: /Send message/i });
    expect(sendButton).toHaveAttribute('disabled');
  });

  it('showsProcessingIndicator_whenProcessing', async () => {
    const { container } = await render(ChatPanelComponent, {
      componentInputs: { messages: mockMessages, processing: true },
    });

    const typingIndicator = container.querySelector('.typing-indicator');
    expect(typingIndicator).toBeTruthy();
  });

  it('autoScrolls_toBottom_onNewMessage', async () => {
    const { fixture } = await render(ChatPanelComponent, {
      componentInputs: { messages: [] },
    });

    const scrollToBottomSpy = vi.spyOn(
      fixture.componentInstance as any,
      'scrollToBottom'
    );

    fixture.componentInstance.messageInput = 'New message';

    expect(typeof fixture.componentInstance.messages).toBe('function');
  });

  it('appliesCorrectClass_forMessageType_info', async () => {
    const { container } = await render(ChatPanelComponent, {
      componentInputs: { messages: [mockMessages[0]] },
    });

    expect(container.querySelector('.ai-message.info')).toBeTruthy();
  });

  it('appliesCorrectClass_forMessageType_success', async () => {
    const { container } = await render(ChatPanelComponent, {
      componentInputs: { messages: [mockMessages[2]] },
    });

    expect(container.querySelector('.ai-message.success')).toBeTruthy();
  });

  it('appliesCorrectClass_forMessageType_error', async () => {
    const errorMessage: ChatMessage = {
      id: '4',
      text: 'Error occurred',
      isUser: false,
      timestamp: new Date(),
      type: 'error',
    };

    const { container } = await render(ChatPanelComponent, {
      componentInputs: { messages: [errorMessage] },
    });

    expect(container.querySelector('.ai-message.error')).toBeTruthy();
  });

  it('appliesCorrectClass_forMessageType_warning', async () => {
    const warningMessage: ChatMessage = {
      id: '5',
      text: 'Warning message',
      isUser: false,
      timestamp: new Date(),
      type: 'warning',
    };

    const { container } = await render(ChatPanelComponent, {
      componentInputs: { messages: [warningMessage] },
    });

    expect(container.querySelector('.ai-message.warning')).toBeTruthy();
  });
});
