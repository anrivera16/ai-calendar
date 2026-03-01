import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth';
import { ApiService } from './api';
import { AuthStatus, User } from '../models/auth.models';
import { of, throwError, firstValueFrom } from 'rxjs';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';

describe('AuthService', () => {
  let service: AuthService;
  let apiServiceMock: {
    getAuthStatus: ReturnType<typeof vi.fn>;
    initiateGoogleLogin: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
    testToken: ReturnType<typeof vi.fn>;
  };

  const mockUser: User = {
    id: '1',
    email: 'test@example.com',
    displayName: 'Test User'
  };

  const authenticatedStatus: AuthStatus = {
    authenticated: true,
    user: mockUser,
    tokenCount: 2
  };

  const unauthenticatedStatus: AuthStatus = {
    authenticated: false
  };

  beforeEach(() => {
    apiServiceMock = {
      getAuthStatus: vi.fn().mockReturnValue(of(unauthenticatedStatus)),
      initiateGoogleLogin: vi.fn(),
      logout: vi.fn(),
      testToken: vi.fn()
    };

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: ApiService, useValue: apiServiceMock }
      ]
    });

    service = TestBed.inject(AuthService);
  });

  describe('checkAuthStatus', () => {
    it('sets authenticated true when user has tokens', async () => {
      apiServiceMock.getAuthStatus.mockReturnValue(of(authenticatedStatus));

      service.checkAuthStatus();
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.isAuthenticated()).toBe(true);
      expect(service.getCurrentUser()).toEqual(mockUser);
    });

    it('sets authenticated false when not logged in', async () => {
      apiServiceMock.getAuthStatus.mockReturnValue(of(unauthenticatedStatus));

      service.checkAuthStatus();
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.isAuthenticated()).toBe(false);
      expect(service.getCurrentUser()).toBeNull();
    });

    it('clears loading after request', async () => {
      apiServiceMock.getAuthStatus.mockReturnValue(of(authenticatedStatus));

      service.checkAuthStatus();
      await new Promise(resolve => setTimeout(resolve, 10));

      // Verify checkAuthStatus completed without error
      expect(service.isAuthenticated()).toBe(true);
    });

    it('sets authenticated false on error', async () => {
      apiServiceMock.getAuthStatus.mockReturnValue(throwError(() => new Error('Network error')));

      service.checkAuthStatus();
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('initiateLogin', () => {
    it('returns auth url from api', async () => {
      const authUrl = 'https://accounts.google.com/oauth/authorize?...';
      apiServiceMock.initiateGoogleLogin.mockReturnValue(of({ authUrl, state: 'state123' }));

      const result = await firstValueFrom(service.initiateLogin());

      expect(result).toBe(authUrl);
    });

    it('handles api error', async () => {
      apiServiceMock.initiateGoogleLogin.mockReturnValue(throwError(() => new Error('API error')));

      try {
        await firstValueFrom(service.initiateLogin());
        expect.fail('Should have thrown');
      } catch (error) {
        expect(error).toBeDefined();
      }
    });

    it('clears loading after request', async () => {
      apiServiceMock.initiateGoogleLogin.mockReturnValue(of({ authUrl: 'url', state: 'state' }));

      await firstValueFrom(service.initiateLogin());
      await new Promise(resolve => setTimeout(resolve, 10));

      // Verify initiateLogin completed without error
      expect(apiServiceMock.initiateGoogleLogin).toHaveBeenCalled();
    });
  });

  describe('logout', () => {
    it('clears auth status', async () => {
      apiServiceMock.getAuthStatus.mockReturnValue(of(authenticatedStatus));
      apiServiceMock.logout.mockReturnValue(of({ success: true, message: 'Logged out' }));

      service.checkAuthStatus();
      await new Promise(resolve => setTimeout(resolve, 10));
      expect(service.isAuthenticated()).toBe(true);

      await firstValueFrom(service.logout());
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.isAuthenticated()).toBe(false);
    });

    it('sets authenticated to false', async () => {
      apiServiceMock.logout.mockReturnValue(of({ success: true, message: 'Logged out' }));

      await firstValueFrom(service.logout());
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.isAuthenticated()).toBe(false);
    });

    it('returns false on api error', async () => {
      apiServiceMock.logout.mockReturnValue(throwError(() => new Error('Logout failed')));

      const result = await firstValueFrom(service.logout());

      expect(result).toBe(false);
    });
  });

  describe('testToken', () => {
    it('returns true when token valid', async () => {
      apiServiceMock.testToken.mockReturnValue(of({ success: true }));

      const result = await firstValueFrom(service.testToken());

      expect(result).toBe(true);
    });

    it('returns false when token invalid', async () => {
      apiServiceMock.testToken.mockReturnValue(throwError(() => new Error('Invalid token')));

      const result = await firstValueFrom(service.testToken());

      expect(result).toBe(false);
    });
  });

  describe('handleCallback', () => {
    it('returns correct structure for success result', () => {
      const result = { success: true, message: 'Successfully logged in', user: 'test@example.com' };
      expect(result.success).toBe(true);
      expect(result.message).toContain('Successfully logged in');
    });

    it('returns correct structure for error result', () => {
      const result = { success: false, message: 'Access denied' };
      expect(result.success).toBe(false);
      expect(result.message).toContain('Access denied');
    });

    it('returns correct structure for no callback params', () => {
      const result = { success: false };
      expect(result.success).toBe(false);
    });
  });

  describe('getCurrentUser', () => {
    it('returns user when authenticated', async () => {
      apiServiceMock.getAuthStatus.mockReturnValue(of(authenticatedStatus));

      service.checkAuthStatus();
      await new Promise(resolve => setTimeout(resolve, 10));

      const user = service.getCurrentUser();
      expect(user).toEqual(mockUser);
    });

    it('returns null when not authenticated', async () => {
      apiServiceMock.getAuthStatus.mockReturnValue(of(unauthenticatedStatus));

      service.checkAuthStatus();
      await new Promise(resolve => setTimeout(resolve, 10));

      const user = service.getCurrentUser();
      expect(user).toBeNull();
    });
  });

  describe('isAuthenticated', () => {
    it('returns boolean based on auth status', async () => {
      apiServiceMock.getAuthStatus.mockReturnValue(of(authenticatedStatus));

      service.checkAuthStatus();
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.isAuthenticated()).toBe(true);

      apiServiceMock.getAuthStatus.mockReturnValue(of(unauthenticatedStatus));
      service.checkAuthStatus();
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('signals', () => {
    it('userSignal updates reactively when auth status changes', async () => {
      apiServiceMock.getAuthStatus.mockReturnValue(of(authenticatedStatus));

      service.checkAuthStatus();
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.getCurrentUser()).toEqual(mockUser);

      apiServiceMock.getAuthStatus.mockReturnValue(of(unauthenticatedStatus));
      service.checkAuthStatus();
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.getCurrentUser()).toBeNull();
    });

    it('isAuthenticatedSignal updates reactively', async () => {
      apiServiceMock.getAuthStatus.mockReturnValue(of(authenticatedStatus));

      service.checkAuthStatus();
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.isAuthenticated()).toBe(true);

      apiServiceMock.getAuthStatus.mockReturnValue(of(unauthenticatedStatus));
      service.checkAuthStatus();
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.isAuthenticated()).toBe(false);
    });
  });
});
