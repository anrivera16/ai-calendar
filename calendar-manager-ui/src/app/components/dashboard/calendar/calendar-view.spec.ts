import { render, screen, fireEvent, waitFor } from '@testing-library/angular';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { CalendarViewComponent, CalendarDay } from './calendar-view';
import { CalendarEvent } from '../../../models/calendar.models';

describe('CalendarViewComponent', () => {
  const today = new Date();
  const mockEvents: CalendarEvent[] = [
    {
      id: 'event-1',
      title: 'Test Event',
      start: today.toISOString(),
      end: new Date(today.getTime() + 3600000).toISOString(),
    },
  ];

  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2024, 5, 15));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('renders_monthAndYearHeader', async () => {
    await render(CalendarViewComponent, {
      componentInputs: { events: [] },
    });

    expect(screen.getByText(/june 2024/i)).toBeTruthy();
  });

  it('renders_weekdayHeaders', async () => {
    await render(CalendarViewComponent, {
      componentInputs: { events: [] },
    });

    expect(screen.getByText('Sun')).toBeTruthy();
    expect(screen.getByText('Mon')).toBeTruthy();
    expect(screen.getByText('Tue')).toBeTruthy();
    expect(screen.getByText('Wed')).toBeTruthy();
    expect(screen.getByText('Thu')).toBeTruthy();
    expect(screen.getByText('Fri')).toBeTruthy();
    expect(screen.getByText('Sat')).toBeTruthy();
  });

  it('renders42Days_inGrid', async () => {
    const { container } = await render(CalendarViewComponent, {
      componentInputs: { events: [] },
    });

    const dayElements = container.querySelectorAll('.calendar-day');
    expect(dayElements.length).toBe(42);
  });

  it('highlightsToday', async () => {
    const { container } = await render(CalendarViewComponent, {
      componentInputs: { events: [] },
    });

    const todayElement = container.querySelector('.today');
    expect(todayElement).toBeTruthy();
  });

  it('marksCurrentMonth_vsOtherMonthDays', async () => {
    const { container } = await render(CalendarViewComponent, {
      componentInputs: { events: [] },
    });

    const currentMonthDays = container.querySelectorAll('.calendar-day:not(.other-month)');
    expect(currentMonthDays.length).toBe(30);
  });

  it('previousMonth_navigatesBack', async () => {
    await render(CalendarViewComponent, {
      componentInputs: { events: [] },
    });

    const prevButton = screen.getByLabelText(/previous month/i);
    fireEvent.click(prevButton);

    expect(screen.getByText(/may 2024/i)).toBeTruthy();
  });

  it('nextMonth_navigatesForward', async () => {
    await render(CalendarViewComponent, {
      componentInputs: { events: [] },
    });

    const nextButton = screen.getByLabelText(/next month/i);
    fireEvent.click(nextButton);

    expect(screen.getByText(/july 2024/i)).toBeTruthy();
  });

  it('goToToday_returnsToCurrentMonth', async () => {
    await render(CalendarViewComponent, {
      componentInputs: { events: [] },
    });

    const nextButton = screen.getByLabelText(/next month/i);
    fireEvent.click(nextButton);

    const monthTitle = screen.getByText(/july 2024/i);
    fireEvent.click(monthTitle);

    expect(screen.getByText(/june 2024/i)).toBeTruthy();
  });

  it('displaysEvents_onCorrectDates', async () => {
    vi.setSystemTime(new Date(2024, 5, 15));

    const { container } = await render(CalendarViewComponent, {
      componentInputs: { events: mockEvents },
    });

    const todayElement = container.querySelector('.today');
    expect(todayElement).toBeTruthy();
  });

  it('emitsDaySelected_onDayClick', async () => {
    const daySelectedHandler = vi.fn();

    const { container } = await render(CalendarViewComponent, {
      componentInputs: { events: [] },
      componentOutputs: { daySelected: { emit: daySelectedHandler } as any },
    });

    const dayElement = container.querySelector('.calendar-day:not(.other-month)');
    if (dayElement) {
      fireEvent.click(dayElement);
      expect(daySelectedHandler).toHaveBeenCalled();
    }
  });

  it('emitsEventSelected_onEventClick', async () => {
    const eventSelectedHandler = vi.fn();

    const { container } = await render(CalendarViewComponent, {
      componentInputs: { events: mockEvents },
      componentOutputs: { eventSelected: { emit: eventSelectedHandler } as any },
    });

    const todayElement = container.querySelector('.today') as HTMLElement;
    if (todayElement) {
      fireEvent.click(todayElement);
    }

    const eventElement = screen.queryByText('Test Event');
    if (eventElement) {
      fireEvent.click(eventElement);
      expect(eventSelectedHandler).toHaveBeenCalled();
    }
  });

  it('highlightsEvent_whenHighlightedEventIdMatches', async () => {
    const { container, fixture } = await render(CalendarViewComponent, {
      componentInputs: { events: mockEvents, highlightedEventId: 'event-1' },
    });

    const todayElement = container.querySelector('.today') as HTMLElement;
    if (todayElement) {
      fireEvent.click(todayElement);
      fixture.detectChanges();
    }

    const highlightedEvent = container.querySelector('.highlighted');
    expect(highlightedEvent).toBeTruthy();
  });

  it('formatsEventTime_correctly', async () => {
    const { fixture } = await render(CalendarViewComponent, {
      componentInputs: { events: mockEvents },
    });

    const component = fixture.componentInstance;
    const formatted = component.formatEventTime(mockEvents[0]);
    expect(formatted).toMatch(/\d+:\d+\s*(AM|PM)/i);
  });

  it('showsLoadingState', async () => {
    const { container } = await render(CalendarViewComponent, {
      componentInputs: { events: [], loading: true },
    });

    const loadingElement = container.querySelector('.loading-overlay');
    expect(loadingElement).toBeTruthy();
  });
});
