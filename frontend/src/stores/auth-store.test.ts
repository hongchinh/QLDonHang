import { beforeEach, describe, expect, it } from 'vitest';
import { useAuthStore, type CurrentUser } from './auth-store';

const user: CurrentUser = {
  id: 'u1',
  username: 'admin',
  email: 'a@b.com',
  fullName: 'Admin',
  roles: ['ADMIN'],
  permissions: ['customers.view', 'customers.create'],
};

describe('useAuthStore', () => {
  beforeEach(() => {
    useAuthStore.getState().logout();
  });

  it('starts unauthenticated', () => {
    expect(useAuthStore.getState().isAuthenticated()).toBe(false);
  });

  it('becomes authenticated after setAuth', () => {
    useAuthStore.getState().setAuth('token-123', new Date(Date.now() + 60_000).toISOString(), user);
    expect(useAuthStore.getState().isAuthenticated()).toBe(true);
  });

  it('hasPermission reflects current user', () => {
    useAuthStore.getState().setAuth('t', new Date(Date.now() + 60_000).toISOString(), user);
    expect(useAuthStore.getState().hasPermission('customers.view')).toBe(true);
    expect(useAuthStore.getState().hasPermission('orders.pay')).toBe(false);
  });

  it('isInRole reflects current user', () => {
    useAuthStore.getState().setAuth('t', new Date(Date.now() + 60_000).toISOString(), user);
    expect(useAuthStore.getState().isInRole('ADMIN')).toBe(true);
    expect(useAuthStore.getState().isInRole('SALES')).toBe(false);
  });

  it('isTokenExpired detects past timestamps', () => {
    useAuthStore.getState().setAuth('t', new Date(Date.now() - 1000).toISOString(), user);
    expect(useAuthStore.getState().isTokenExpired()).toBe(true);
  });

  it('isTokenExpired returns false for future timestamps', () => {
    useAuthStore.getState().setAuth('t', new Date(Date.now() + 60_000).toISOString(), user);
    expect(useAuthStore.getState().isTokenExpired()).toBe(false);
  });

  it('logout clears state', () => {
    useAuthStore.getState().setAuth('t', new Date(Date.now() + 60_000).toISOString(), user);
    useAuthStore.getState().logout();
    const s = useAuthStore.getState();
    expect(s.accessToken).toBeNull();
    expect(s.user).toBeNull();
    expect(s.isAuthenticated()).toBe(false);
  });

  it('setToken rotates token without touching user', () => {
    useAuthStore.getState().setAuth('old', new Date(Date.now() + 60_000).toISOString(), user);
    useAuthStore.getState().setToken('new', new Date(Date.now() + 120_000).toISOString());
    const s = useAuthStore.getState();
    expect(s.accessToken).toBe('new');
    expect(s.user).toEqual(user);
  });
});
