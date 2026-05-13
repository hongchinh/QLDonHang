import { apiGet, apiPost } from '@/lib/api-client';
import type { CurrentUser } from '@/stores/auth-store';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: CurrentUser;
  // refreshToken / refreshTokenExpiresAt are still in the response body for
  // legacy clients, but the browser ignores them — the HttpOnly cookie set by
  // the backend is what we actually use.
}

export interface TokenPairResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
}

export const authApi = {
  login: (request: LoginRequest) => apiPost<LoginResponse>('/auth/login', request),
  me: () => apiGet<CurrentUser>('/auth/me'),
  refresh: () => apiPost<TokenPairResponse>('/auth/refresh', {}),
  logout: () => apiPost<void>('/auth/logout', {}),
};
