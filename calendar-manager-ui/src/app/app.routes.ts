import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { DashboardComponent } from './components/dashboard/dashboard';
import { BookingPageComponent } from './components/booking/booking-page';
import { BookingManageComponent } from './components/booking/booking-manage';
import { authGuard } from './guards/auth.guard';
import { AdminDashboardComponent } from './components/admin/admin-dashboard';
import { BusinessSetupComponent } from './components/admin/business-setup';
import { ServiceManagerComponent } from './components/admin/service-manager';
import { AvailabilityManagerComponent } from './components/admin/availability-manager';
import { BookingListComponent } from './components/admin/booking-list';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard]
  },
  {
    path: 'admin/dashboard',
    component: AdminDashboardComponent,
    canActivate: [authGuard]
  },
  {
    path: 'admin/setup',
    component: BusinessSetupComponent,
    canActivate: [authGuard]
  },
  {
    path: 'admin/services',
    component: ServiceManagerComponent,
    canActivate: [authGuard]
  },
  {
    path: 'admin/availability',
    component: AvailabilityManagerComponent,
    canActivate: [authGuard]
  },
  {
    path: 'admin/bookings',
    component: BookingListComponent,
    canActivate: [authGuard]
  },
  {
    path: 'book/:slug',
    component: BookingPageComponent
  },
  {
    path: 'book/manage/:token',
    component: BookingManageComponent
  },
  {
    path: '**',
    redirectTo: '/dashboard'
  }
];
