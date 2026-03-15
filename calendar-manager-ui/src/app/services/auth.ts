import { Injectable, signal, computed, inject } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { AuthStatus, User } from '../models/auth.models';
import { ApiService } from './api';

const TOKEN_KEY = 'jwt_token';
const USER_KEY = 'user_data';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private api = inject(ApiService);

  private authStatusSignal = signal<AuthStatus>({ authenticated: false });
  private loadingSignal = signal<boolean>(true);

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
    const token = this.getToken();

    if (!token) {
      this.authStatusSignal.set({ authenticated: false });
      this.loadingSignal.set(false);
      return;
    }

    // Verify token with backend
    this.loadingSignal.set(true);
    this.api.getAuthStatus()
      .subscribe({
        next: (response) => {
          if (response.authenticated && response.user) {
            this.authStatusSignal.set({
              authenticated: true,
              user: {
                id: response.user.id,
                email: response.user.email,
                displayName: response.user.displayName
              }
            });
          } else {
            this.clearAuth();
          }
          this.loadingSignal.set(false);
        },
        error: () => {
          this.clearAuth();
          this.loadingSignal.set(false);
        }
      });
  }

  initiateLogin(): void {
    this.api.initiateGoogleLogin().subscribe({
      next: (response) => {
        if (response.authUrl) {
          window.location.href = response.authUrl;
        }
      },
      error: (err) => {
        console.error('Failed to initiate login:', err);
      }
    });
  }

  logout(): void {
    this.api.logout().subscribe({
      next: () => {
        this.clearAuth();
      },
      error: () => {
        // Even if API call fails, clear local auth
        this.clearAuth();
      }
    });
  }

  testToken(): void {
    this.api.testToken().subscribe({
      next: (response) => {
        if (!response.success) {
          this.clearAuth();
        }
      },
      error: () => {
        this.clearAuth();
      }
    });
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

      // Token is set via cookie by the backend - read from cookie for localStorage
      let tokenFound = false;
      const cookies = document.cookie.split(';');
      for (let cookie of cookies) {
        const [name, ...valueParts] = cookie.trim().split('=');
        if (name === 'jwt_token') {
          this.setToken(decodeURIComponent(valueParts.join('=')));
          tokenFound = true;
          break;
        }
      }

      if (!tokenFound) {
        console.warn('JWT cookie not found after OAuth callback');
      }

      // Store user info and set auth status immediately (don't wait for backend verification)
      if (user) {
        const userData = { email: user, displayName: user.split('@')[0] };
        localStorage.setItem(USER_KEY, JSON.stringify(userData));
        this.authStatusSignal.set({
          authenticated: true,
          user: { id: '', email: user, displayName: user.split('@')[0] }
        });
        // Mark loading as done so the auth guard can proceed immediately
        this.loadingSignal.set(false);
      }

      // Verify with backend in background (will update user ID and re-validate)
      if (this.getToken()) {
        this.api.getAuthStatus().subscribe({
          next: (response) => {
            if (response.authenticated && response.user) {
              this.authStatusSignal.set({
                authenticated: true,
                user: {
                  id: response.user.id,
                  email: response.user.email,
                  displayName: response.user.displayName
                }
              });
            }
          },
          error: () => { /* keep optimistic status */ }
        });
      }
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

  // JWT token management
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
  }

  clearAuth(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.authStatusSignal.set({ authenticated: false });
  }
}
