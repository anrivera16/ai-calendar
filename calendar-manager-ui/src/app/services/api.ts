import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { 
  AuthStatus, 
  AuthResponse, 
  GoogleLoginResponse, 
  ApiError 
} from '../models/auth.models';
import { CalendarEvent, CreateEvent, UpdateEvent } from '../models/calendar.models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  // Auth endpoints
  getAuthStatus(): Observable<AuthStatus> {
    return this.http.get<AuthStatus>(`${this.baseUrl}/api/auth/status`)
      .pipe(catchError(this.handleError));
  }

  initiateGoogleLogin(): Observable<GoogleLoginResponse> {
    return this.http.get<GoogleLoginResponse>(`${this.baseUrl}/api/auth/google-login`)
      .pipe(catchError(this.handleError));
  }

  logout(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/api/auth/logout`, {})
      .pipe(catchError(this.handleError));
  }

  testToken(): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/auth/test-token`, {})
      .pipe(catchError(this.handleError));
  }

  // Chat endpoints
  processChatMessage(message: string, userEmail?: string, conversationId?: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/chat/message`, {
      message,
      userEmail: userEmail || 'test@example.com',
      conversationId
    }).pipe(catchError(this.handleError));
  }

  getChatConversation(conversationId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/api/chat/conversation/${conversationId}`)
      .pipe(catchError(this.handleError));
  }

  // Calendar endpoints (when implemented)
  getCalendarEvents(start: string, end: string): Observable<CalendarEvent[]> {
    return this.http.get<CalendarEvent[]>(`${this.baseUrl}/api/calendar/events`, {
      params: { start, end }
    }).pipe(catchError(this.handleError));
  }

  createCalendarEvent(event: CreateEvent): Observable<CalendarEvent> {
    return this.http.post<CalendarEvent>(`${this.baseUrl}/api/calendar/events`, event)
      .pipe(catchError(this.handleError));
  }

  updateCalendarEvent(eventId: string, event: UpdateEvent): Observable<CalendarEvent> {
    return this.http.put<CalendarEvent>(`${this.baseUrl}/api/calendar/events/${eventId}`, event)
      .pipe(catchError(this.handleError));
  }

  deleteCalendarEvent(eventId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/calendar/events/${eventId}`)
      .pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An unknown error occurred!';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Error: ${error.error.message}`;
    } else {
      // Server-side error
      if (error.error?.error) {
        errorMessage = error.error.error;
      } else if (error.error?.details) {
        errorMessage = error.error.details;
      } else {
        errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
      }
    }

    console.error('API Error:', errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}
