import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AdminApiService } from '../../services/admin-api.service';
import { AdminBooking } from '../../models/admin.models';

@Component({
  selector: 'app-booking-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './booking-list.html',
  styleUrl: './booking-list.scss',
})
export class BookingListComponent implements OnInit {
  bookings: AdminBooking[] = [];
  loading: boolean = true;
  error: string = '';
  success: string = '';

  // Filters
  statusFilter: string = '';
  dateFrom: string = '';
  dateTo: string = '';

  // Actions
  actionLoading: string | null = null;

  // Show cancel confirm
  showCancelConfirm: boolean = false;
  cancelingBookingId: string | null = null;

  statusOptions = [
    { value: '', label: 'All Statuses' },
    { value: 'Confirmed', label: 'Confirmed' },
    { value: 'Cancelled', label: 'Cancelled' },
    { value: 'Completed', label: 'Completed' },
    { value: 'NoShow', label: 'No Show' },
  ];

  constructor(private adminApi: AdminApiService) {}

  ngOnInit() {
    this.loadBookings();
  }

  loadBookings() {
    this.loading = true;
    const filters: any = {};
    if (this.statusFilter) filters.status = this.statusFilter;
    if (this.dateFrom) filters.fromDate = this.dateFrom;
    if (this.dateTo) filters.toDate = this.dateTo;

    this.adminApi.getBookings(filters).subscribe({
      next: (bookings) => {
        this.bookings = bookings;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message;
        this.loading = false;
      },
    });
  }

  applyFilters() {
    this.loadBookings();
  }

  clearFilters() {
    this.statusFilter = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.loadBookings();
  }

  confirmCancel(bookingId: string) {
    this.cancelingBookingId = bookingId;
    this.showCancelConfirm = true;
  }

  cancelDelete() {
    this.cancelingBookingId = null;
    this.showCancelConfirm = false;
  }

  cancelBooking() {
    if (!this.cancelingBookingId) return;

    this.actionLoading = this.cancelingBookingId;
    this.adminApi.cancelBooking(this.cancelingBookingId).subscribe({
      next: () => {
        this.showSuccess('Booking cancelled successfully');
        this.cancelingBookingId = null;
        this.showCancelConfirm = false;
        this.actionLoading = null;
        this.loadBookings();
      },
      error: (err) => {
        this.error = err.message;
        this.actionLoading = null;
        this.cancelingBookingId = null;
        this.showCancelConfirm = false;
      },
    });
  }

  completeBooking(bookingId: string) {
    if (!confirm('Mark this booking as completed?')) return;

    this.actionLoading = bookingId;
    this.adminApi.completeBooking(bookingId).subscribe({
      next: () => {
        this.showSuccess('Booking marked as completed');
        this.actionLoading = null;
        this.loadBookings();
      },
      error: (err) => {
        this.error = err.message;
        this.actionLoading = null;
      },
    });
  }

  formatDateTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    });
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  }

  formatTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
    });
  }

  private showSuccess(message: string) {
    this.success = message;
    setTimeout(() => {
      this.success = '';
    }, 3000);
  }
}
