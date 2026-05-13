import { create } from 'zustand';

export interface CurrentUser {
  id: string;
  username: string;
  email: string;
  fullName: string;
  roles: string[];
  permissions: string[];
}

interface AuthState {
  accessToken: string | null;
  expiresAt: string | null;
  user: CurrentUser | null;
  setAuth: (token: string, expiresAt: string, user: CurrentUser) => void;
  setToken: (token: string, expiresAt: string) => void;
  setUser: (user: CurrentUser) => void;
  logout: () => void;
  isAuthenticated: () => boolean;
  isTokenExpired: () => boolean;
  hasPermission: (permission: string) => boolean;
  isInRole: (role: string) => boolean;
}

// Access token lives only in memory. On page reload it's gone; the boot flow
// in <AuthInit> trades the HttpOnly refresh cookie for a fresh access token
// via POST /auth/refresh, then loads the user via GET /auth/me.
export const useAuthStore = create<AuthState>((set, get) => ({
  accessToken: null,
  expiresAt: null,
  user: null,
  setAuth: (token, expiresAt, user) => set({ accessToken: token, expiresAt, user }),
  setToken: (token, expiresAt) => set({ accessToken: token, expiresAt }),
  setUser: (user) => set({ user }),
  logout: () => set({ accessToken: null, expiresAt: null, user: null }),
  isAuthenticated: () => {
    const { accessToken, user } = get();
    return !!accessToken && !!user;
  },
  isTokenExpired: () => {
    const { expiresAt } = get();
    if (!expiresAt) return false;
    return new Date(expiresAt).getTime() <= Date.now();
  },
  hasPermission: (permission) => get().user?.permissions.includes(permission) ?? false,
  isInRole: (role) => get().user?.roles.includes(role) ?? false,
}));
