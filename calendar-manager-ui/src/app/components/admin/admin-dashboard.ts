import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AdminApiService } from '../../services/admin-api.service';
import { AdminBooking, BusinessProfile } from '../../models/admin.models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.scss',
})
export class AdminDashboardComponent implements OnInit {
  profile: BusinessProfile | null = null;
  hasProfile: boolean = false;
  loading: boolean = true;
  error: string = '';

  // Today's bookings
  todaysBookings: AdminBooking[] = [];
  todaysLoading: boolean = true;

  // Upcoming bookings
  upcomingBookings: AdminBooking[] = [];
  upcomingLoading: boolean = true;

  constructor(
    private adminApi: AdminApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadProfile();
    this.loadTodaysBookings();
    this.loadUpcomingBookings();
  }

  private loadProfile() {
    console.log('[AdminDashboard] Loading profile...');
    this.adminApi.getProfile().subscribe({
      next: (response) => {
        console.log('[AdminDashboard] Profile response:', response);
        this.hasProfile = response.hasProfile;
        if (response.hasProfile) {
          this.profile = {
            id: response.id!,
            businessName: response.businessName!,
            slug: response.slug!,
            description: response.description,
            logoUrl: response.logoUrl,
            phone: response.phone,
            website: response.website,
            address: response.address,
            isActive: response.isActive!,
            createdAt: response.createdAt!,
            updatedAt: response.updatedAt!,
            bookingUrl: response.bookingUrl!,
          };
        }
        this.loading = false;
        console.log('[AdminDashboard] Profile loaded, loading=', this.loading, 'hasProfile=', this.hasProfile);
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('[AdminDashboard] Profile error:', err);
        this.error = err.message;
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  private loadTodaysBookings() {
    console.log('[AdminDashboard] Loading today\'s bookings...');
    this.todaysLoading = true;
    this.adminApi.getTodaysBookings().subscribe({
      next: (bookings) => {
        console.log('[AdminDashboard] Today\'s bookings response:', bookings);
        this.todaysBookings = bookings;
        this.todaysLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('[AdminDashboard] Today\'s bookings error:', err);
        this.todaysLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  private loadUpcomingBookings() {
    console.log('[AdminDashboard] Loading upcoming bookings...');
    this.upcomingLoading = true;
    this.adminApi.getUpcomingBookings(5).subscribe({
      next: (bookings) => {
        console.log('[AdminDashboard] Upcoming bookings response:', bookings);
        this.upcomingBookings = bookings;
        this.upcomingLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('[AdminDashboard] Upcoming bookings error:', err);
        this.upcomingLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  copyBookingLink() {
    if (this.profile?.bookingUrl) {
      const url = window.location.origin + this.profile.bookingUrl;
      navigator.clipboard.writeText(url).then(() => {
        alert('Booking link copied to clipboard!');
      });
    }
  }

  formatTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
  }
}
