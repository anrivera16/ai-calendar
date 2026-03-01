import { Injectable, signal, computed } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { toObservable } from '@angular/core/rxjs-interop';
import { AuthStatus, User } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private static readonly DEMO_USER: User = {
    id: '1',
    email: 'test@example.com',
    displayName: 'Test User'
  };

  private authStatusSignal = signal<AuthStatus>({ authenticated: false });
  private loadingSignal = signal<boolean>(false);

  public readonly authStatus$ = toObservable(this.authStatusSignal);
  public readonly loading$ = toObservable(this.loadingSignal);
  
  private userSignal = computed(() => this.authStatusSignal().user);
  private isAuthenticatedSignal = computed(() => this.authStatusSignal().authenticated);
  
  public readonly user$ = toObservable(this.userSignal);
  public readonly isAuthenticated$ = toObservable(this.isAuthenticatedSignal);

  constructor() {
    this.checkAuthStatus();
  }

  checkAuthStatus(): void {
    this.authStatusSignal.set({
      authenticated: true,
      user: AuthService.DEMO_USER,
      tokenCount: 1,
      nextExpiry: new Date(Date.now() + 3600000).toISOString()
    });
  }

  initiateLogin(): Observable<string> {
    return of('');
  }

  logout(): Observable<boolean> {
    return of(true);
  }

  testToken(): Observable<boolean> {
    return of(true);
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
    return this.authStatusSignal().user || null;
  }

  isAuthenticated(): boolean {
    return this.authStatusSignal().authenticated;
  }
}
