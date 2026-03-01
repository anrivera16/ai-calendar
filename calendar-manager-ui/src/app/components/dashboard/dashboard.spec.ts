import { TestBed } from '@angular/core/testing';
import { render, screen, fireEvent } from '@testing-library/angular';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { BehaviorSubject, of } from 'rxjs';
import { provideRouter } from '@angular/router';
import { DashboardComponent } from './dashboard';
import { AuthService } from '../../services/auth';
import { CalendarService } from '../../services/calendar.service';
import { AiChatService } from '../../services/ai-chat.service';
import { signal } from '@angular/core';
import { User } from '../../models/auth.models';
import { CalendarEvent } from '../../models/calendar.models';
import { ChatMessage } from '../../services/ai-chat.service';

describe('DashboardComponent', () => {
  let mockAuthService: {
    user$: BehaviorSubject<User | undefined>;
    loading$: BehaviorSubject<boolean>;
    isAuthenticated$: BehaviorSubject<boolean>;
    logout: ReturnType<typeof vi.fn>;
  };
  let mockCalendarService: {
    events: ReturnType<typeof signal<CalendarEvent[]>>;
    loading: ReturnType<typeof signal<boolean>>;
    loadEvents: ReturnType<typeof vi.fn>;
  };
  let mockAiChatService: {
    messages: ReturnType<typeof signal<ChatMessage[]>>;
    processing: ReturnType<typeof signal<boolean>>;
    sendMessage: ReturnType<typeof vi.fn>;
  };

  const mockUser: User = {
    id: '1',
    email: 'test@example.com',
    displayName: 'Test User',
  };

  const mockEvents: CalendarEvent[] = [
    {
      id: 'event-1',
      title: 'Test Event',
      start: new Date().toISOString(),
      end: new Date(Date.now() + 3600000).toISOString(),
    },
  ];

  const mockMessages: ChatMessage[] = [
    {
      id: '1',
      text: 'Hello!',
      isUser: false,
      timestamp: new Date(),
      type: 'info',
    },
  ];

  beforeEach(async () => {
    mockAuthService = {
      user$: new BehaviorSubject<User | undefined>(mockUser),
      loading$: new BehaviorSubject<boolean>(false),
      isAuthenticated$: new BehaviorSubject<boolean>(true),
      logout: vi.fn().mockReturnValue(of({ success: true })),
    };
    mockCalendarService = {
      events: signal<CalendarEvent[]>(mockEvents),
      loading: signal<boolean>(false),
      loadEvents: vi.fn(),
    };
    mockAiChatService = {
      messages: signal<ChatMessage[]>(mockMessages),
      processing: signal<boolean>(false),
      sendMessage: vi.fn(),
    };

    vi.spyOn(window, 'confirm').mockReturnValue(true);
    vi.spyOn(window, 'alert').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('creates_theComponent', async () => {
    await render(DashboardComponent, {
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: CalendarService, useValue: mockCalendarService },
        { provide: AiChatService, useValue: mockAiChatService },
        provideRouter([]),
      ],
    });

    expect(screen.getByText(/AI Calendar/i)).toBeTruthy();
  });

  it('renders_calendarView_andChatPanel', async () => {
    await render(DashboardComponent, {
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: CalendarService, useValue: mockCalendarService },
        { provide: AiChatService, useValue: mockAiChatService },
        provideRouter([]),
      ],
    });

    expect(screen.getByText(/today/i)).toBeTruthy();
    expect(screen.getByPlaceholderText(/Ask me to schedule/i)).toBeTruthy();
  });

  it('renders_userDisplayName_inHeader', async () => {
    await render(DashboardComponent, {
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: CalendarService, useValue: mockCalendarService },
        { provide: AiChatService, useValue: mockAiChatService },
        provideRouter([]),
      ],
    });

    expect(screen.getByText('Test User')).toBeTruthy();
  });

  it('renders_adminLink', async () => {
    await render(DashboardComponent, {
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: CalendarService, useValue: mockCalendarService },
        { provide: AiChatService, useValue: mockAiChatService },
        provideRouter([]),
      ],
    });

    expect(screen.getByText(/Admin/i)).toBeTruthy();
  });

  it('onLogout_callsAuthService', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    await render(DashboardComponent, {
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: CalendarService, useValue: mockCalendarService },
        { provide: AiChatService, useValue: mockAiChatService },
        provideRouter([]),
      ],
    });

    const logoutButtons = screen.getAllByTitle('Logout');
    fireEvent.click(logoutButtons[0]);

    expect(window.confirm).toHaveBeenCalled();
    expect(mockAuthService.logout).toHaveBeenCalled();
  });

  it('onSendMessage_callsChatService', async () => {
    await render(DashboardComponent, {
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: CalendarService, useValue: mockCalendarService },
        { provide: AiChatService, useValue: mockAiChatService },
        provideRouter([]),
      ],
    });

    const input = screen.getByPlaceholderText(/Ask me to schedule/i);
    fireEvent.input(input, { target: { value: 'Test message' } });

    const sendButton = screen.getByRole('button', { name: /send/i });
    fireEvent.click(sendButton);

    expect(mockAiChatService.sendMessage).toHaveBeenCalledWith('Test message');
  });

  it('onLoadCalendar_refreshesEvents', async () => {
    await render(DashboardComponent, {
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: CalendarService, useValue: mockCalendarService },
        { provide: AiChatService, useValue: mockAiChatService },
        provideRouter([]),
      ],
    });

    const refreshButtons = screen.getAllByTitle('Refresh Calendar');
    fireEvent.click(refreshButtons[0]);

    expect(mockCalendarService.loadEvents).toHaveBeenCalled();
  });

  it('detectsMobileScreen_andShowsToggle', async () => {
    vi.spyOn(window, 'innerWidth', 'get').mockReturnValue(500);

    await render(DashboardComponent, {
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: CalendarService, useValue: mockCalendarService },
        { provide: AiChatService, useValue: mockAiChatService },
        provideRouter([]),
      ],
    });

    expect(screen.getByText(/Calendar/i)).toBeTruthy();
    expect(screen.getByText(/Assistant/i)).toBeTruthy();
  });

  it('setActivePanel_togglesBetween_chatAndCalendar', async () => {
    vi.spyOn(window, 'innerWidth', 'get').mockReturnValue(500);

    await render(DashboardComponent, {
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: CalendarService, useValue: mockCalendarService },
        { provide: AiChatService, useValue: mockAiChatService },
        provideRouter([]),
      ],
    });

    const calendarTab = screen.getByText(/Calendar/i);
    fireEvent.click(calendarTab);

    expect(screen.getByPlaceholderText(/Ask me to schedule/i)).toBeTruthy();
  });

  it('highlightsEvent_for3Seconds_afterCreation', async () => {
    vi.useFakeTimers();

    const { fixture } = await render(DashboardComponent, {
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: CalendarService, useValue: mockCalendarService },
        { provide: AiChatService, useValue: mockAiChatService },
        provideRouter([]),
      ],
    });

    const component = fixture.componentInstance;

    expect(component.highlightedEventId()).toBe('event-1');

    vi.advanceTimersByTime(3000);

    expect(component.highlightedEventId()).toBe(null);

    vi.useRealTimers();
  });

  it('loadsCalendarEvents_onInit', async () => {
    await render(DashboardComponent, {
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: CalendarService, useValue: mockCalendarService },
        { provide: AiChatService, useValue: mockAiChatService },
        provideRouter([]),
      ],
    });

    expect(mockCalendarService.loadEvents).toHaveBeenCalled();
  });
});
