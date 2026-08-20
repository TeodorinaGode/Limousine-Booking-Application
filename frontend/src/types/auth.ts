export type UserRole = "Administrator" | "Driver";

export interface AuthenticatedUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  /** Null if the user has never saved a preference — the frontend then falls back to the browser's language. */
  languageCode: string | null;
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
