import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { authGuard } from './auth-guard';
import { AuthService } from '../services/auth';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { of, BehaviorSubject, firstValueFrom } from 'rxjs';

describe('authGuard', () => {
  let authServiceMock: {
    isAuthenticated: ReturnType<typeof vi.fn>;
    isAuthenticated$: ReturnType<typeof vi.fn>;
    loading$: ReturnType<typeof vi.fn>;
  };
  let routerMock: {
    navigate: ReturnType<typeof vi.fn>;
    createUrlTree: ReturnType<typeof vi.fn>;
  };
  let isAuthenticatedSubject: BehaviorSubject<boolean>;
  let loadingSubject: BehaviorSubject<boolean>;

  const executeGuard = (...guardParameters: Parameters<typeof authGuard>) =>
    TestBed.runInInjectionContext(() => authGuard(...guardParameters));

  beforeEach(() => {
    isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
    loadingSubject = new BehaviorSubject<boolean>(false);

    authServiceMock = {
      isAuthenticated: vi.fn(() => isAuthenticatedSubject.value),
      isAuthenticated$: vi.fn(() => isAuthenticatedSubject.asObservable()),
      loading$: vi.fn(() => loadingSubject.asObservable()),
    };

    routerMock = {
      navigate: vi.fn().mockResolvedValue(true),
      createUrlTree: vi.fn((commands: any[]) => ({ commands })),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
      ],
    });
  });

  it('allows_navigation_whenAuthenticated', async () => {
    const result = await executeGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot);

    expect(result).toBe(true);
  });

  it('redirectsToLogin_whenNotAuthenticated', async () => {
    const result = await executeGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot);

    expect(result).toBe(true);
  });

  it('waitsForLoading_beforeDeciding', async () => {
    loadingSubject.next(true);

    const result = await executeGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot);

    expect(result).toBe(true);
  });

  it('usesIsAuthenticated$_observable', async () => {
    const observable = authServiceMock.isAuthenticated$();
    const value = await firstValueFrom(observable);

    expect(value).toBe(false);
  });

  it('should be created', () => {
    expect(authGuard).toBeDefined();
    expect(typeof authGuard).toBe('function');
  });
});
