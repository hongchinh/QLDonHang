import axios, {
  type AxiosError,
  type AxiosInstance,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios';
import { useAuthStore } from '@/stores/auth-store';
import { queryClient } from '@/lib/query-client';

export interface ApiError {
  code: string;
  message: string;
  details?: Record<string, string[]>;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: ApiError;
  timestamp: string;
}

// Endpoints that must never trigger the refresh-on-401 dance: a 401 from these
// IS the auth state. Refreshing would loop or mask the real failure.
const NO_REFRESH_PATHS = ['/auth/login', '/auth/refresh', '/auth/logout'];

interface RetriableConfig extends InternalAxiosRequestConfig {
  _retried?: boolean;
}

// VITE_API_BASE_URL is the canonical name (Railway / standard convention).
// VITE_API_BASE is the legacy name; kept for backwards compatibility.
// Falls back to '/api' so the Vite dev proxy keeps working in `npm run dev`.
const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ?? import.meta.env.VITE_API_BASE ?? '/api';

const api: AxiosInstance = axios.create({
  baseURL: apiBaseUrl,
  timeout: 30_000,
  // Required so the browser sends the HttpOnly refresh cookie on /auth/* calls.
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Single-flight refresh: if many requests fail with 401 at once, only one
// /auth/refresh fires and the others wait for its result.
let refreshInFlight: Promise<string | null> | null = null;

async function attemptRefresh(): Promise<string | null> {
  if (refreshInFlight) return refreshInFlight;
  refreshInFlight = (async () => {
    try {
      const res = await api.post<ApiResponse<{ accessToken: string; accessTokenExpiresAt: string }>>(
        '/auth/refresh',
        {},
      );
      const payload = res.data;
      if (!payload.success || !payload.data) return null;
      const { accessToken, accessTokenExpiresAt } = payload.data;
      useAuthStore.getState().setToken(accessToken, accessTokenExpiresAt);
      return accessToken;
    } catch {
      return null;
    } finally {
      // Release the lock on the next tick so concurrent callers can read the result.
      setTimeout(() => {
        refreshInFlight = null;
      }, 0);
    }
  })();
  return refreshInFlight;
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiResponse<unknown>>) => {
    const original = error.config as RetriableConfig | undefined;
    const status = error.response?.status;
    const url = original?.url ?? '';

    const shouldTryRefresh =
      status === 401 &&
      original &&
      !original._retried &&
      !NO_REFRESH_PATHS.some((p) => url.includes(p));

    if (shouldTryRefresh) {
      const newToken = await attemptRefresh();
      if (newToken) {
        original._retried = true;
        original.headers = original.headers ?? {};
        original.headers.Authorization = `Bearer ${newToken}`;
        return api.request(original);
      }
      // Refresh failed → session is dead. Clear state; ProtectedRoute redirects.
      useAuthStore.getState().logout();
      queryClient.clear();
    }

    return Promise.reject(error);
  },
);

export async function apiGet<T>(url: string, params?: unknown, config?: AxiosRequestConfig): Promise<T> {
  const res = await api.get<ApiResponse<T>>(url, { ...config, params });
  return unwrap(res.data);
}

export async function apiPost<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
  const res = await api.post<ApiResponse<T>>(url, data, config);
  return unwrap(res.data);
}

export async function apiPut<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
  const res = await api.put<ApiResponse<T>>(url, data, config);
  return unwrap(res.data);
}

export async function apiPatch<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
  const res = await api.patch<ApiResponse<T>>(url, data, config);
  return unwrap(res.data);
}

export async function apiDelete<T = void>(url: string, config?: AxiosRequestConfig): Promise<T> {
  const res = await api.delete<ApiResponse<T>>(url, config);
  return unwrap(res.data);
}

function unwrap<T>(payload: ApiResponse<T>): T {
  if (!payload.success) {
    throw new ApiCallError(payload.error ?? { code: 'UNKNOWN', message: 'Unknown error' });
  }
  return payload.data as T;
}

export class ApiCallError extends Error {
  code: string;
  details?: Record<string, string[]>;

  constructor(error: ApiError) {
    super(error.message);
    this.code = error.code;
    this.details = error.details;
  }
}

export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiCallError) return error.message;
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiResponse<unknown> | undefined;
    if (data?.error?.message) return data.error.message;
    return error.message;
  }
  if (error instanceof Error) return error.message;
  return 'Đã xảy ra lỗi không mong muốn.';
}

export default api;
