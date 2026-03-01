import { TestBed } from '@angular/core/testing';
import { render, screen, fireEvent } from '@testing-library/angular';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { LoginComponent } from './login';
import { AuthService } from '../../services/auth';
import { BehaviorSubject, of, Observable } from 'rxjs';

describe('LoginComponent', () => {
  let mockAuthService: {
    loading$: BehaviorSubject<boolean>;
    isAuthenticated$: BehaviorSubject<boolean>;
    initiateLogin: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockAuthService = {
      loading$: new BehaviorSubject<boolean>(false),
      isAuthenticated$: new BehaviorSubject<boolean>(false),
      initiateLogin: vi.fn().mockReturnValue(of({ authUrl: 'https://accounts.google.com/oauth' })),
    };
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders_loginButton', async () => {
    await render(LoginComponent, {
      providers: [{ provide: AuthService, useValue: mockAuthService }],
    });

    expect(screen.getByText(/Connect Google Calendar/i)).toBeTruthy();
  });

  it('renders_featureList', async () => {
    await render(LoginComponent, {
      providers: [{ provide: AuthService, useValue: mockAuthService }],
    });

    expect(screen.getByText(/View your calendar events/i)).toBeTruthy();
    expect(screen.getByText(/Create new meetings/i)).toBeTruthy();
  });

  it('showsSpinner_whenLoading', async () => {
    mockAuthService.loading$.next(true);

    await render(LoginComponent, {
      providers: [{ provide: AuthService, useValue: mockAuthService }],
    });

    expect(screen.getByText(/Connecting to Google Calendar/i)).toBeTruthy();
  });

  it('callsOnLogin_whenButtonClicked', async () => {
    await render(LoginComponent, {
      providers: [{ provide: AuthService, useValue: mockAuthService }],
    });

    const loginButton = screen.getByText(/Connect Google Calendar/i);
    fireEvent.click(loginButton);

    expect(mockAuthService.initiateLogin).toHaveBeenCalled();
  });

  it('onLogin_initiatesGoogleLogin', async () => {
    const authUrl = 'https://accounts.google.com/oauth/authorize';
    mockAuthService.initiateLogin.mockReturnValue(of(authUrl));

    await render(LoginComponent, {
      providers: [{ provide: AuthService, useValue: mockAuthService }],
    });

    const loginButton = screen.getByText(/Connect Google Calendar/i);
    fireEvent.click(loginButton);

    expect(mockAuthService.initiateLogin).toHaveBeenCalled();
  });

  it('redirectsToAuthUrl_onLoginSuccess', async () => {
    const authUrl = 'https://accounts.google.com/oauth/authorize';
    mockAuthService.initiateLogin.mockReturnValue(of(authUrl));

    await render(LoginComponent, {
      providers: [{ provide: AuthService, useValue: mockAuthService }],
    });

    const loginButton = screen.getByText(/Connect Google Calendar/i);
    fireEvent.click(loginButton);

    expect(mockAuthService.initiateLogin).toHaveBeenCalled();
  });

  it('handlesLoginError_gracefully', async () => {
    const mockAlert = vi.spyOn(window, 'alert').mockImplementation(() => {});

    mockAuthService.initiateLogin.mockReturnValue(
      new Observable((observer) => {
        observer.error(new Error('Network error'));
      }),
    );

    await render(LoginComponent, {
      providers: [{ provide: AuthService, useValue: mockAuthService }],
    });

    const loginButton = screen.getByText(/Connect Google Calendar/i);
    fireEvent.click(loginButton);

    expect(mockAlert).toHaveBeenCalledWith(expect.stringContaining('Login failed'));
  });
});
