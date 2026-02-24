import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth';
import { map, take } from 'rxjs/operators';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  return authService.authStatus$.pipe(
    take(1),
    map(authStatus => {
      if (authStatus.authenticated) {
        return true;
      }
      
      // Redirect to login page if not authenticated
      return router.createUrlTree(['/login']);
    })
  );
};
