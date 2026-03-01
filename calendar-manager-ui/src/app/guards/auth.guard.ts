import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { combineLatest, filter, map, tap } from 'rxjs';
import { AuthService } from '../services/auth';

export const authGuard: CanActivateFn = (): ReturnType<CanActivateFn> => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return combineLatest([
    authService.isAuthenticated$,
    authService.loading$
  ]).pipe(
    filter(([_, loading]) => !loading),
    map(([isAuthenticated, _]) => isAuthenticated),
    tap(isAuthenticated => {
      if (!isAuthenticated) {
        console.log('User not authenticated, redirecting to login');
        router.navigate(['/login']);
      }
    }),
    map(isAuthenticated => isAuthenticated)
  );
};
