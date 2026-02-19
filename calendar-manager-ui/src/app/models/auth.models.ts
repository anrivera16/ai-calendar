export interface User {
  id: string;
  email: string;
  displayName: string;
}

export interface AuthStatus {
  authenticated: boolean;
  user?: User;
  tokenCount?: number;
  nextExpiry?: string;
}

export interface AuthResponse {
  success: boolean;
  message: string;
  user?: User;
  tokenExpiry?: string;
}

export interface GoogleLoginResponse {
  authUrl: string;
  state: string;
}

export interface ApiError {
  error: string;
  details?: string;
}