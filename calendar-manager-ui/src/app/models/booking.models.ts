// Booking Models for Public Booking Flow

export interface BusinessInfo {
  id: string;
  businessName: string;
  description?: string;
  logoUrl?: string;
  phone?: string;
  website?: string;
  address?: string;
  services: ServiceInfo[];
}

export interface ServiceInfo {
  id: string;
  name: string;
  description?: string;
  durationMinutes: number;
  price?: number;
  color?: string;
}

export interface AvailableSlot {
  startTime: string;
  endTime: string;
}

export interface SlotsResponse {
  date: string;
  serviceId: string;
  durationMinutes: number;
  slots: AvailableSlot[];
}

export interface ClientInfo {
  name: string;
  email: string;
  phone?: string;
  notes?: string;
}

export interface BookingRequest {
  serviceId: string;
  startTime: string;
  clientName: string;
  clientEmail: string;
  clientPhone?: string;
  notes?: string;
}

export interface BookingResponse {
  booking: {
    id: string;
    serviceName?: string;
    startTime: string;
    endTime: string;
    status: string;
    client: ClientInfo;
  };
  business: {
    name: string;
    address?: string;
    phone?: string;
  };
  managementUrl: string;
  message: string;
}

export interface BookingManageResponse {
  booking: {
    id: string;
    serviceName?: string;
    startTime: string;
    endTime: string;
    status: string;
    notes?: string;
    client: ClientInfo;
  };
  business: {
    name: string;
    phone?: string;
    address?: string;
  };
}

export interface CancelBookingResponse {
  message: string;
  booking: {
    id: string;
    status: string;
  };
}
