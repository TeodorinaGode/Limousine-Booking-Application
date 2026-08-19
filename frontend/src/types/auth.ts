export type UserRole = "Administrator" | "Driver";

export interface AuthenticatedUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: AuthenticatedUser;
}
