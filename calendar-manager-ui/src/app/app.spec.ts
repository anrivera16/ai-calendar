import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { App } from './app';
import { AuthService } from './services/auth';

describe('App', () => {
  let mockAuthService: {
    handleCallback: ReturnType<typeof vi.fn>;
  };
  let mockRouter: {
    navigate: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockAuthService = {
      handleCallback: vi.fn().mockReturnValue({ success: false }),
    };
    mockRouter = {
      navigate: vi.fn(),
    };

    vi.spyOn(window, 'alert').mockImplementation(() => {});

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('creates_theComponent', () => {
    mockAuthService.handleCallback.mockReturnValue({ success: false });
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('ngOnInit_handlesOAuthCallback_whenCodeInUrl', () => {
    vi.spyOn(window, 'setTimeout').mockImplementation((fn: () => void) => {
      fn();
      return 1 as any;
    });

    mockAuthService.handleCallback.mockReturnValue({
      success: true,
      message: 'Successfully logged in',
      user: 'test@example.com',
    });

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    expect(mockAuthService.handleCallback).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('ngOnInit_showsError_whenErrorInUrl', () => {
    vi.spyOn(window, 'setTimeout').mockImplementation((fn: () => void) => {
      fn();
      return 1 as any;
    });

    mockAuthService.handleCallback.mockReturnValue({
      success: false,
      message: 'Authentication failed',
    });

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    expect(mockAuthService.handleCallback).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('ngOnInit_doesNothing_whenNoCallbackParams', () => {
    mockAuthService.handleCallback.mockReturnValue({ success: false });

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    expect(mockAuthService.handleCallback).toHaveBeenCalled();
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });
});
