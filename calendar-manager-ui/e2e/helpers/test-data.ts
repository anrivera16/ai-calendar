import { APIRequestContext } from '@playwright/test';
import businessFixture from '../fixtures/business.json';
import servicesFixture from '../fixtures/services.json';
import availabilityFixture from '../fixtures/availability.json';
import bookingFixture from '../fixtures/booking.json';

const API_BASE_URL = 'http://localhost:8080';
const DEMO_USER_EMAIL = 'test@example.com';

export interface BusinessProfile {
  id: number;
  userId: string;
  businessName: string;
  slug: string;
  description?: string;
  timezone: string;
  email?: string;
  phone?: string;
  address?: string;
  isActive: boolean;
}

export interface Service {
  id: number;
  businessProfileId: number;
  name: string;
  description?: string;
  durationMinutes: number;
  price: number;
  color: string;
  isActive: boolean;
}

export interface AvailabilityRule {
  id: number;
  businessProfileId: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isActive: boolean;
}

export interface Booking {
  id: number;
  serviceId: number;
  clientName: string;
  clientEmail: string;
  clientPhone?: string;
  startTime: string;
  endTime: string;
  status: string;
  notes?: string;
  managementToken: string;
}

export interface CreateBusinessData {
  businessName?: string;
  slug?: string;
  description?: string;
  timezone?: string;
  email?: string;
  phone?: string;
  address?: string;
}

export interface CreateServiceData {
  name?: string;
  description?: string;
  durationMinutes?: number;
  price?: number;
  color?: string;
  businessProfileId?: number;
}

export interface CreateAvailabilityData {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  businessProfileId?: number;
}

export interface CreateBookingData {
  serviceId?: number;
  clientName?: string;
  clientEmail?: string;
  clientPhone?: string;
  startTime?: string;
  endTime?: string;
  notes?: string;
}

async function ensureUserExists(request: APIRequestContext): Promise<void> {
  const response = await request.get(`${API_BASE_URL}/api/auth/status?userEmail=${DEMO_USER_EMAIL}`);
  if (!response.ok()) {
    console.warn('Auth status check failed, continuing anyway');
  }
}

export async function createBusinessProfile(
  request: APIRequestContext,
  data: CreateBusinessData = {}
): Promise<BusinessProfile> {
  await ensureUserExists(request);

  const businessData = {
    businessName: data.businessName || businessFixture.businessName,
    description: data.description || businessFixture.description,
    phone: data.phone || businessFixture.phone,
    website: data.website || 'https://testbusiness.example.com',
    address: data.address || businessFixture.address,
  };

  const response = await request.post(`${API_BASE_URL}/api/admin/profile`, {
    data: businessData,
  });

  if (!response.ok()) {
    const errorBody = await response.text();
    throw new Error(`Failed to create business profile: ${response.status()} - ${errorBody}`);
  }

  return response.json();
}

export async function getBusinessProfile(
  request: APIRequestContext
): Promise<BusinessProfile | null> {
  await ensureUserExists(request);

  const response = await request.get(`${API_BASE_URL}/api/admin/profile`);

  if (response.status() === 404) {
    return null;
  }

  if (!response.ok()) {
    throw new Error(`Failed to get business profile: ${response.status()}`);
  }

  const data = await response.json();
  if (!data.hasProfile) {
    return null;
  }

  return {
    id: data.id,
    userId: '',
    businessName: data.businessName,
    slug: data.slug,
    description: data.description,
    timezone: 'America/New_York',
    email: '',
    phone: data.phone,
    address: data.address,
    isActive: data.isActive,
  };
}

export async function deleteBusinessProfile(
  request: APIRequestContext,
  businessProfileId: number
): Promise<void> {
  const response = await request.delete(
    `${API_BASE_URL}/api/admin/profile`
  );

  if (!response.ok() && response.status() !== 404) {
    console.warn(`Failed to delete business profile: ${response.status()}`);
  }
}

export async function createService(
  request: APIRequestContext,
  businessProfileId: number,
  data: CreateServiceData = {}
): Promise<Service> {
  const serviceData = {
    name: data.name || servicesFixture[0].name,
    description: data.description || servicesFixture[0].description,
    durationMinutes: data.durationMinutes || servicesFixture[0].durationMinutes,
    price: data.price || servicesFixture[0].price,
    color: data.color || servicesFixture[0].color,
    isActive: true,
  };

  const response = await request.post(`${API_BASE_URL}/api/admin/services`, {
    data: serviceData,
  });

  if (!response.ok()) {
    const errorBody = await response.text();
    throw new Error(`Failed to create service: ${response.status()} - ${errorBody}`);
  }

  return response.json();
}

export async function getServices(
  request: APIRequestContext,
  businessProfileId: number
): Promise<Service[]> {
  const response = await request.get(`${API_BASE_URL}/api/admin/services`);

  if (!response.ok()) {
    throw new Error(`Failed to get services: ${response.status()}`);
  }

  return response.json();
}

export async function createWeeklyAvailability(
  request: APIRequestContext,
  businessProfileId: number,
  rules?: CreateAvailabilityData[]
): Promise<AvailabilityRule[]> {
  const availabilityRules = rules || availabilityFixture.weeklyRules;

  for (const rule of availabilityRules) {
    const response = await request.post(`${API_BASE_URL}/api/admin/availability/weekly`, {
      data: {
        dayOfWeek: rule.dayOfWeek,
        startTime: rule.startTime,
        endTime: rule.endTime,
        isAvailable: true,
      },
    });

    if (!response.ok()) {
      const errorBody = await response.text();
      throw new Error(`Failed to create weekly availability: ${response.status()} - ${errorBody}`);
    }
  }

  return [];
}

export async function createBooking(
  request: APIRequestContext,
  slug: string,
  serviceId: number,
  data: CreateBookingData = {}
): Promise<Booking> {
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  tomorrow.setHours(14, 0, 0, 0);

  const bookingData = {
    serviceId: serviceId,
    clientName: data.clientName || bookingFixture.clientName,
    clientEmail: data.clientEmail || bookingFixture.clientEmail,
    clientPhone: data.clientPhone || bookingFixture.clientPhone,
    startTime: data.startTime || tomorrow.toISOString(),
    endTime: data.endTime || new Date(tomorrow.getTime() + 60 * 60 * 1000).toISOString(),
    notes: data.notes || bookingFixture.notes,
  };

  const response = await request.post(`${API_BASE_URL}/api/book/${slug}`, {
    data: bookingData,
  });

  if (!response.ok()) {
    const errorBody = await response.text();
    throw new Error(`Failed to create booking: ${response.status()} - ${errorBody}`);
  }

  return response.json();
}

export async function createBookingForToday(
  request: APIRequestContext,
  slug: string,
  serviceId: number,
  data: CreateBookingData = {}
): Promise<Booking> {
  const today = new Date();
  today.setHours(14, 0, 0, 0);

  const bookingData = {
    serviceId: serviceId,
    clientName: data.clientName || bookingFixture.clientName,
    clientEmail: data.clientEmail || bookingFixture.clientEmail,
    clientPhone: data.clientPhone || bookingFixture.clientPhone,
    startTime: data.startTime || today.toISOString(),
    endTime: data.endTime || new Date(today.getTime() + 60 * 60 * 1000).toISOString(),
    notes: data.notes || bookingFixture.notes,
  };

  const response = await request.post(`${API_BASE_URL}/api/book/${slug}`, {
    data: bookingData,
  });

  if (!response.ok()) {
    const errorBody = await response.text();
    throw new Error(`Failed to create booking: ${response.status()} - ${errorBody}`);
  }

  return response.json();
}

export async function getBookings(
  request: APIRequestContext,
  businessProfileId: number
): Promise<Booking[]> {
  const response = await request.get(`${API_BASE_URL}/api/admin/bookings`);

  if (!response.ok()) {
    throw new Error(`Failed to get bookings: ${response.status()}`);
  }

  return response.json();
}

export async function cleanupTestData(request: APIRequestContext): Promise<void> {
  try {
    const profile = await getBusinessProfile(request);
    if (profile) {
      await deleteBusinessProfile(request, profile.id);
    }
  } catch (error) {
    console.warn('Cleanup failed:', error);
  }
}

export { businessFixture, servicesFixture, availabilityFixture, bookingFixture };
