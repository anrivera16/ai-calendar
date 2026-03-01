import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { PublicBookingApiService } from '../../services/public-booking-api.service';
import {
  BusinessInfo,
  ServiceInfo,
  AvailableSlot,
  BookingRequest,
  BookingResponse,
} from '../../models/booking.models';

type BookingStep = 'service' | 'date' | 'time' | 'details' | 'confirmation';

@Component({
  selector: 'app-booking-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './booking-page.html',
  styleUrl: './booking-page.scss',
})
export class BookingPageComponent implements OnInit {
  slug: string = '';
  business: BusinessInfo | null = null;
  loading = true;
  error: string | null = null;

  // Current step
  currentStep: BookingStep = 'service';
  stepIndex = 0;

  // Step 1: Service selection
  selectedService: ServiceInfo | null = null;

  // Step 2: Date selection
  selectedDate: Date = new Date();
  availableDates: Date[] = [];
  maxDate: Date = new Date();

  // Step 3: Time slots
  slots: AvailableSlot[] = [];
  selectedSlot: AvailableSlot | null = null;
  loadingSlots = false;

  // Step 4: Client details
  clientName = '';
  clientEmail = '';
  clientPhone = '';
  clientNotes = '';
  submitting = false;

  // Validation
  emailError = '';
  nameError = '';

  // Step 5: Confirmation
  bookingResponse: BookingResponse | null = null;

  // Router injected publicly for template access
  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private bookingApi: PublicBookingApiService
  ) {
    // Initialize max date to 30 days from now
    this.maxDate = new Date();
    this.maxDate.setDate(this.maxDate.getDate() + 30);

    // Generate available dates (next 30 days)
    this.generateAvailableDates();
  }

  ngOnInit() {
    this.route.params.subscribe((params) => {
      this.slug = params['slug'];
      if (this.slug) {
        this.loadBusiness();
      }
    });
  }

  generateAvailableDates() {
    this.availableDates = [];
    const today = new Date();
    for (let i = 0; i < 30; i++) {
      const date = new Date(today);
      date.setDate(today.getDate() + i);
      this.availableDates.push(date);
    }
  }

  loadBusiness() {
    this.loading = true;
    this.error = null;
    this.bookingApi.getBusinessBySlug(this.slug).subscribe({
      next: (business) => {
        this.business = business;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load business';
        this.loading = false;
      },
    });
  }

  selectService(service: ServiceInfo) {
    this.selectedService = service;
    this.currentStep = 'date';
    this.stepIndex = 1;
  }

  selectDate(date: Date) {
    this.selectedDate = date;
    this.loadSlots();
    this.currentStep = 'time';
    this.stepIndex = 2;
  }

  loadSlots() {
    if (!this.selectedService || !this.selectedDate) return;

    this.loadingSlots = true;
    this.slots = [];
    this.selectedSlot = null;

    const dateStr = this.selectedDate.toISOString().split('T')[0];
    this.bookingApi
      .getAvailableSlots(this.slug, this.selectedService.id, dateStr)
      .subscribe({
        next: (response) => {
          this.slots = response.slots;
          this.loadingSlots = false;
        },
        error: (err) => {
          console.error('Failed to load slots:', err);
          this.loadingSlots = false;
        },
      });
  }

  selectSlot(slot: AvailableSlot) {
    this.selectedSlot = slot;
    this.currentStep = 'details';
    this.stepIndex = 3;
  }

  goBackToTime() {
    this.currentStep = 'time';
    this.stepIndex = 2;
  }

  goBackToDate() {
    this.currentStep = 'date';
    this.stepIndex = 1;
  }

  goBackToService() {
    this.currentStep = 'service';
    this.stepIndex = 0;
  }

  submitBooking() {
    if (!this.selectedService || !this.selectedSlot) return;

    // Validate client details before submitting
    this.nameError = '';
    this.emailError = '';

    if (!this.clientName.trim()) {
      this.nameError = 'Name is required';
      return;
    }

    if (!this.clientEmail.trim()) {
      this.emailError = 'Email is required';
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.clientEmail)) {
      this.emailError = 'Please enter a valid email address';
      return;
    }

    this.submitting = true;

    const booking: BookingRequest = {
      serviceId: this.selectedService.id,
      startTime: this.selectedSlot.startTime,
      clientName: this.clientName,
      clientEmail: this.clientEmail,
      clientPhone: this.clientPhone || undefined,
      notes: this.clientNotes || undefined,
    };

    this.bookingApi.createBooking(this.slug, booking).subscribe({
      next: (response) => {
        this.bookingResponse = response;
        this.currentStep = 'confirmation';
        this.stepIndex = 4;
        this.submitting = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to create booking';
        this.submitting = false;
      },
    });
  }

  canSubmit(): boolean {
    return (
      !!this.clientName.trim() &&
      !!this.clientEmail.trim() &&
      this.clientEmail.includes('@')
    );
  }

  formatDate(date: Date): string {
    return date.toLocaleDateString('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
    });
  }

  formatTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
    });
  }

  formatDateTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleString('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    });
  }
}
