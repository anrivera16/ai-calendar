import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { PublicBookingApiService } from '../../services/public-booking-api.service';
import { BookingManageResponse, CancelBookingResponse } from '../../models/booking.models';

@Component({
  selector: 'app-booking-manage',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './booking-manage.html',
  styleUrl: './booking-manage.scss',
})
export class BookingManageComponent implements OnInit {
  token: string = '';
  booking: BookingManageResponse | null = null;
  loading = true;
  error: string | null = null;
  cancelling = false;
  cancelSuccess = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private bookingApi: PublicBookingApiService
  ) {}

  ngOnInit() {
    this.route.params.subscribe((params) => {
      this.token = params['token'];
      if (this.token) {
        this.loadBooking();
      }
    });
  }

  loadBooking() {
    this.loading = true;
    this.error = null;
    this.bookingApi.getBookingByToken(this.token).subscribe({
      next: (response) => {
        this.booking = response;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load booking';
        this.loading = false;
      },
    });
  }

  cancelBooking() {
    if (!this.token) return;

    this.cancelling = true;
    this.bookingApi.cancelBooking(this.token).subscribe({
      next: (response) => {
        this.cancelSuccess = true;
        this.cancelling = false;
        if (this.booking) {
          this.booking.booking.status = 'Cancelled';
        }
      },
      error: (err) => {
        this.error = err.message || 'Failed to cancel booking';
        this.cancelling = false;
      },
    });
  }

  formatDateTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleString('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    });
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'confirmed':
        return 'status-confirmed';
      case 'cancelled':
        return 'status-cancelled';
      case 'completed':
        return 'status-completed';
      case 'noshow':
        return 'status-noshow';
      default:
        return '';
    }
  }
}
