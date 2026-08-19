import type { LoginRequest, LoginResponse } from "../types/auth";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000/api";

export async function login(credentials: LoginRequest): Promise<LoginResponse> {
  const response = await fetch(`${API_BASE_URL}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(credentials),
  });

  if (!response.ok) {
    throw new Error("Invalid email or password.");
  }

  return (await response.json()) as LoginResponse;
}
