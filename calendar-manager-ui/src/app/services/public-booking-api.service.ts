import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  BusinessInfo,
  SlotsResponse,
  BookingRequest,
  BookingResponse,
  BookingManageResponse,
  CancelBookingResponse,
} from '../models/booking.models';

@Injectable({
  providedIn: 'root',
})
export class PublicBookingApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  /**
   * GET /api/book/{slug}
   * Get business info and services by slug
   */
  getBusinessBySlug(slug: string): Observable<BusinessInfo> {
    return this.http.get<BusinessInfo>(`${this.baseUrl}/api/book/${slug}`)
      .pipe(catchError(this.handleError));
  }

  /**
   * GET /api/book/{slug}/slots?serviceId=&date=
   * Get available time slots for a service on a specific date
   */
  getAvailableSlots(
    slug: string,
    serviceId: string,
    date: string
  ): Observable<SlotsResponse> {
    const params = {
      serviceId,
      date,
    };
    return this.http.get<SlotsResponse>(
      `${this.baseUrl}/api/book/${slug}/slots`,
      { params }
    ).pipe(catchError(this.handleError));
  }

  /**
   * POST /api/book/{slug}
   * Create a new booking
   */
  createBooking(slug: string, booking: BookingRequest): Observable<BookingResponse> {
    return this.http.post<BookingResponse>(
      `${this.baseUrl}/api/book/${slug}`,
      booking
    ).pipe(catchError(this.handleError));
  }

  /**
   * GET /api/book/manage/{cancellationToken}
   * Get booking details by cancellation token
   */
  getBookingByToken(cancellationToken: string): Observable<BookingManageResponse> {
    return this.http.get<BookingManageResponse>(
      `${this.baseUrl}/api/book/manage/${cancellationToken}`
    ).pipe(catchError(this.handleError));
  }

  /**
   * POST /api/book/manage/{cancellationToken}/cancel
   * Cancel a booking by cancellation token
   */
  cancelBooking(cancellationToken: string): Observable<CancelBookingResponse> {
    return this.http.post<CancelBookingResponse>(
      `${this.baseUrl}/api/book/manage/${cancellationToken}/cancel`,
      {}
    ).pipe(catchError(this.handleError));
  }

  private handleError(error: any): Observable<never> {
    let errorMessage = 'An unknown error occurred!';

    if (error.error instanceof ErrorEvent) {
      errorMessage = `Error: ${error.error.message}`;
    } else {
      if (error.error?.error) {
        errorMessage = error.error.error;
      } else if (error.error?.details) {
        errorMessage = error.error.details;
      } else {
        errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
      }
    }

    console.error('Public Booking API Error:', errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}
