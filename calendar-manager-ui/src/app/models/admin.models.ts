// Admin Models for Business Management

export interface BusinessProfile {
  id: string;
  businessName: string;
  slug: string;
  description?: string;
  logoUrl?: string;
  phone?: string;
  website?: string;
  address?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  bookingUrl: string;
}

export interface BusinessProfileResponse {
  hasProfile: boolean;
  id?: string;
  businessName?: string;
  slug?: string;
  description?: string;
  logoUrl?: string;
  phone?: string;
  website?: string;
  address?: string;
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
  bookingUrl?: string;
}

export interface CreateBusinessProfile {
  businessName: string;
  description?: string;
  logoUrl?: string;
  phone?: string;
  website?: string;
  address?: string;
}

export interface UpdateBusinessProfile {
  businessName?: string;
  description?: string;
  logoUrl?: string;
  phone?: string;
  website?: string;
  address?: string;
  isActive?: boolean;
}

// Service Models
export interface Service {
  id: string;
  businessProfileId: string;
  name: string;
  description?: string;
  durationMinutes: number;
  price: number;
  color: string;
  isActive: boolean;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateService {
  name: string;
  description?: string;
  durationMinutes: number;
  price: number;
  color: string;
  isActive: boolean;
  sortOrder: number;
}

export interface UpdateService {
  name?: string;
  description?: string;
  durationMinutes?: number;
  price?: number;
  color?: string;
  isActive?: boolean;
  sortOrder?: number;
}

// Availability Models
export interface AvailabilityRule {
  id: string;
  businessProfileId: string;
  ruleType: 'Weekly' | 'DateOverride' | 'Break';
  dayOfWeek?: number; // 0 = Sunday, 1 = Monday, etc.
  startTime?: string; // "09:00:00"
  endTime?: string; // "17:00:00"
  specificDate?: string; // "2024-01-15"
  isAvailable: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateWeeklyAvailability {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isAvailable: boolean;
}

export interface CreateDateOverride {
  specificDate: string;
  startTime?: string;
  endTime?: string;
  isAvailable: boolean;
}

export interface CreateBreak {
  specificDate: string;
  startTime: string;
  endTime: string;
}

// Booking Models for Admin
export interface AdminBooking {
  id: string;
  serviceId: string;
  serviceName?: string;
  serviceColor?: string;
  clientName?: string;
  clientEmail?: string;
  clientPhone?: string;
  startTime: string;
  endTime: string;
  status: 'Confirmed' | 'Cancelled' | 'Completed' | 'NoShow';
  notes?: string;
  createdAt: string;
}

export interface BookingFilter {
  status?: string;
  fromDate?: string;
  toDate?: string;
}
