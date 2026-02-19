export interface CalendarEvent {
  id: string;
  title: string;
  description?: string;
  start: string;
  end: string;
  location?: string;
  attendees?: string[];
  htmlLink?: string;
}

export interface CreateEvent {
  title: string;
  description?: string;
  start: string;
  end: string;
  location?: string;
  attendees?: string[];
}

export interface UpdateEvent {
  title?: string;
  description?: string;
  start?: string;
  end?: string;
  location?: string;
  attendees?: string[];
}

export interface FreeBusyInfo {
  email: string;
  busyPeriods: Array<{
    start: string;
    end: string;
  }>;
}