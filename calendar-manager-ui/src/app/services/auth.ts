import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, tap, catchError } from 'rxjs/operators';
import { ApiService } from './api';
import { AuthStatus, User } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private authStatusSubject = new BehaviorSubject<AuthStatus>({ authenticated: false });
  private loadingSubject = new BehaviorSubject<boolean>(false);

  public authStatus$ = this.authStatusSubject.asObservable();
  public loading$ = this.loadingSubject.asObservable();
  public user$ = this.authStatus$.pipe(map(status => status.user));
  public isAuthenticated$ = this.authStatus$.pipe(map(status => status.authenticated));

  constructor(private apiService: ApiService) {
    this.checkAuthStatus();
  }

  checkAuthStatus(): void {
    this.loadingSubject.next(true);
    this.apiService.getAuthStatus()
      .pipe(
        tap(status => {
          this.authStatusSubject.next(status);
          this.loadingSubject.next(false);
        }),
        catchError(error => {
          console.error('Auth status check failed:', error);
          this.authStatusSubject.next({ authenticated: false });
          this.loadingSubject.next(false);
          return of({ authenticated: false });
        })
      )
      .subscribe();
  }

  initiateLogin(): Observable<string> {
    this.loadingSubject.next(true);
    return this.apiService.initiateGoogleLogin()
      .pipe(
        map(response => {
          this.loadingSubject.next(false);
          return response.authUrl;
        }),
        catchError(error => {
          console.error('Login initiation failed:', error);
          this.loadingSubject.next(false);
          throw error;
        })
      );
  }

  logout(): Observable<boolean> {
    this.loadingSubject.next(true);
    return this.apiService.logout()
      .pipe(
        tap(response => {
          if (response.success) {
            this.authStatusSubject.next({ authenticated: false });
          }
          this.loadingSubject.next(false);
        }),
        map(response => response.success),
        catchError(error => {
          console.error('Logout failed:', error);
          this.loadingSubject.next(false);
          return of(false);
        })
      );
  }

  testToken(): Observable<boolean> {
    return this.apiService.testToken()
      .pipe(
        map(response => response.success),
        catchError(error => {
          console.error('Token test failed:', error);
          return of(false);
        })
      );
  }

  // Handle OAuth callback from backend redirect
  handleCallback(): { success: boolean, message?: string, user?: string } {
    const urlParams = new URLSearchParams(window.location.search);
    const auth = urlParams.get('auth');
    const user = urlParams.get('user');
    const message = urlParams.get('message');
    
    // Clear URL parameters
    window.history.replaceState({}, document.title, window.location.pathname);
    
    if (auth === 'success') {
      console.log('OAuth success, user authenticated');
      // Refresh auth status
      setTimeout(() => this.checkAuthStatus(), 500);
      return { 
        success: true, 
        message: user ? `Successfully logged in as ${user}` : 'Successfully logged in with Google',
        user: user || undefined 
      };
    } else if (auth === 'error') {
      console.error('OAuth error:', message);
      return { 
        success: false, 
        message: message || 'Authentication failed. Please try again.' 
      };
    }
    
    // Legacy handling for direct OAuth callbacks (if any)
    const code = urlParams.get('code');
    const error = urlParams.get('error');
    
    if (error) {
      console.error('OAuth error:', error);
      return { success: false, message: 'Authentication was cancelled or failed.' };
    } else if (code) {
      console.log('OAuth callback received with code');
      setTimeout(() => this.checkAuthStatus(), 1000);
      return { success: true, message: 'Processing authentication...' };
    }
    
    return { success: false };
  }

  getCurrentUser(): User | null {
    return this.authStatusSubject.value.user || null;
  }

  isAuthenticated(): boolean {
    return this.authStatusSubject.value.authenticated;
  }
}
