import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient, HttpErrorResponse, HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  AuthStatus,
  AuthResponse,
  GoogleLoginResponse,
  ApiError
} from '../models/auth.models';
import { CalendarEvent, CreateEvent, UpdateEvent } from '../models/calendar.models';

const TOKEN_KEY = 'jwt_token';

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
  processChatMessage(message: string, conversationId?: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/api/chat/message`, {
      message,
      conversationId
    }).pipe(
      timeout(30000), // 30 second timeout
      catchError(this.handleError)
    );
  }

  getChatConversation(conversationId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/api/chat/conversation/${conversationId}`)
      .pipe(catchError(this.handleError));
  }

  getChatConversations(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/api/chat/conversations`)
      .pipe(catchError(this.handleError));
  }

  deleteChatConversation(conversationId: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/api/chat/conversation/${conversationId}`)
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

// Http interceptor to add JWT token to all requests and handle 401s
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private router: Router) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = localStorage.getItem(TOKEN_KEY);

    const cloned = token
      ? req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`) })
      : req;

    return next.handle(cloned).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          // Clear auth state on any 401 to prevent redirect loops
          localStorage.removeItem(TOKEN_KEY);
          localStorage.removeItem('user_data');
          this.router.navigate(['/login']);
        }
        return throwError(() => error);
      })
    );
  }
}
