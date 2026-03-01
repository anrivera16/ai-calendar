import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  BusinessProfile,
  BusinessProfileResponse,
  CreateBusinessProfile,
  UpdateBusinessProfile,
  Service,
  CreateService,
  UpdateService,
  AvailabilityRule,
  CreateWeeklyAvailability,
  CreateDateOverride,
  CreateBreak,
  AdminBooking,
} from '../models/admin.models';

@Injectable({
  providedIn: 'root',
})
export class AdminApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // ==================== Business Profile ====================

  /**
   * GET /api/admin/profile
   * Get current user's business profile
   */
  getProfile(): Observable<BusinessProfileResponse> {
    return this.http.get<BusinessProfileResponse>(`${this.baseUrl}/api/admin/profile`)
      .pipe(catchError(this.handleError));
  }

  /**
   * POST /api/admin/profile
   * Create a new business profile
   */
  createProfile(profile: CreateBusinessProfile): Observable<BusinessProfile> {
    return this.http.post<BusinessProfile>(`${this.baseUrl}/api/admin/profile`, profile)
      .pipe(catchError(this.handleError));
  }

  /**
   * PUT /api/admin/profile
   * Update business profile
   */
  updateProfile(profile: UpdateBusinessProfile): Observable<BusinessProfile> {
    return this.http.put<BusinessProfile>(`${this.baseUrl}/api/admin/profile`, profile)
      .pipe(catchError(this.handleError));
  }

  // ==================== Services ====================

  /**
   * GET /api/admin/services
   * Get all services for the business
   */
  getServices(): Observable<Service[]> {
    return this.http.get<Service[]>(`${this.baseUrl}/api/admin/services`)
      .pipe(catchError(this.handleError));
  }

  /**
   * POST /api/admin/services
   * Create a new service
   */
  createService(service: CreateService): Observable<Service> {
    return this.http.post<Service>(`${this.baseUrl}/api/admin/services`, service)
      .pipe(catchError(this.handleError));
  }

  /**
   * PUT /api/admin/services/{id}
   * Update a service
   */
  updateService(id: string, service: UpdateService): Observable<Service> {
    return this.http.put<Service>(`${this.baseUrl}/api/admin/services/${id}`, service)
      .pipe(catchError(this.handleError));
  }

  /**
   * DELETE /api/admin/services/{id}
   * Delete a service
   */
  deleteService(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/api/admin/services/${id}`)
      .pipe(catchError(this.handleError));
  }

  // ==================== Availability ====================

  /**
   * GET /api/admin/availability
   * Get all availability rules
   */
  getAvailability(): Observable<AvailabilityRule[]> {
    return this.http.get<AvailabilityRule[]>(`${this.baseUrl}/api/admin/availability`)
      .pipe(catchError(this.handleError));
  }

  /**
   * POST /api/admin/availability/weekly
   * Create or update weekly availability
   */
  createWeeklyAvailability(availability: CreateWeeklyAvailability): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/api/admin/availability/weekly`, availability)
      .pipe(catchError(this.handleError));
  }

  /**
   * POST /api/admin/availability/override
   * Create a date override
   */
  createDateOverride(override: CreateDateOverride): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/api/admin/availability/override`, override)
      .pipe(catchError(this.handleError));
  }

  /**
   * POST /api/admin/availability/break
   * Create a break
   */
  createBreak(brk: CreateBreak): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/api/admin/availability/break`, brk)
      .pipe(catchError(this.handleError));
  }

  /**
   * DELETE /api/admin/availability/{id}
   * Delete an availability rule
   */
  deleteAvailabilityRule(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/api/admin/availability/${id}`)
      .pipe(catchError(this.handleError));
  }

  // ==================== Bookings ====================

  /**
   * GET /api/admin/bookings
   * Get all bookings with optional filters
   */
  getBookings(filters?: { status?: string; fromDate?: string; toDate?: string }): Observable<AdminBooking[]> {
    let params = new HttpParams();
    if (filters?.status) {
      params = params.set('status', filters.status);
    }
    if (filters?.fromDate) {
      params = params.set('fromDate', filters.fromDate);
    }
    if (filters?.toDate) {
      params = params.set('toDate', filters.toDate);
    }
    return this.http.get<AdminBooking[]>(`${this.baseUrl}/api/admin/bookings`, { params })
      .pipe(catchError(this.handleError));
  }

  /**
   * GET /api/admin/bookings/today
   * Get today's bookings
   */
  getTodaysBookings(): Observable<AdminBooking[]> {
    return this.http.get<AdminBooking[]>(`${this.baseUrl}/api/admin/bookings/today`)
      .pipe(catchError(this.handleError));
  }

  /**
   * GET /api/admin/bookings/upcoming
   * Get upcoming bookings
   */
  getUpcomingBookings(limit: number = 10): Observable<AdminBooking[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<AdminBooking[]>(`${this.baseUrl}/api/admin/bookings/upcoming`, { params })
      .pipe(catchError(this.handleError));
  }

  /**
   * PUT /api/admin/bookings/{id}/cancel
   * Cancel a booking
   */
  cancelBooking(id: string): Observable<{ message: string; status: string }> {
    return this.http.put<{ message: string; status: string }>(`${this.baseUrl}/api/admin/bookings/${id}/cancel`, {})
      .pipe(catchError(this.handleError));
  }

  /**
   * PUT /api/admin/bookings/{id}/complete
   * Mark a booking as completed
   */
  completeBooking(id: string): Observable<{ message: string; status: string }> {
    return this.http.put<{ message: string; status: string }>(`${this.baseUrl}/api/admin/bookings/${id}/complete`, {})
      .pipe(catchError(this.handleError));
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

    console.error('Admin API Error:', errorMessage);
    return throwError(() => new Error(errorMessage));
  }
}
