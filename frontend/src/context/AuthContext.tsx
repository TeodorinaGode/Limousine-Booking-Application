import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { login as loginRequest } from "../services/authService";
import type { AuthenticatedUser } from "../types/auth";

interface AuthState {
  user: AuthenticatedUser | null;
  accessToken: string | null;
  expiresAt: string | null;
}

interface AuthContextValue extends AuthState {
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<AuthenticatedUser>;
  logout: () => void;
}

const STORAGE_KEY = "limousine-booking.auth";
const EMPTY_STATE: AuthState = { user: null, accessToken: null, expiresAt: null };

function readStoredAuth(): AuthState {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) return EMPTY_STATE;

  try {
    const parsed = JSON.parse(raw) as AuthState;
    if (parsed.expiresAt && new Date(parsed.expiresAt) > new Date()) {
      return parsed;
    }
  } catch {
    // Malformed storage — fall through and clear it below.
  }

  localStorage.removeItem(STORAGE_KEY);
  return EMPTY_STATE;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(readStoredAuth);

  useEffect(() => {
    if (state.user && state.accessToken) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  }, [state]);

  const login = async (email: string, password: string) => {
    const response = await loginRequest({ email, password });
    setState({ user: response.user, accessToken: response.accessToken, expiresAt: response.expiresAt });
    return response.user;
  };

  const logout = () => setState(EMPTY_STATE);

  const value: AuthContextValue = {
    ...state,
    isAuthenticated: Boolean(state.user && state.accessToken),
    login,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
