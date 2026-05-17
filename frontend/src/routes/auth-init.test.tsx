import { StrictMode } from 'react';
import { act, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthInit } from './auth-init';
import { useAuthStore, type CurrentUser } from '@/stores/auth-store';

const user: CurrentUser = {
  id: 'u1',
  username: 'admin',
  email: 'a@b.com',
  fullName: 'Admin',
  roles: ['ADMIN'],
  permissions: [],
};

const refreshMock = vi.fn();
const meMock = vi.fn();

vi.mock('@/features/auth/api', () => ({
  authApi: {
    refresh: (...args: unknown[]) => refreshMock(...args),
    me: (...args: unknown[]) => meMock(...args),
  },
}));

describe('AuthInit', () => {
  beforeEach(() => {
    refreshMock.mockReset();
    meMock.mockReset();
    useAuthStore.getState().logout();
  });

  afterEach(() => {
    return act(async () => {
      await Promise.resolve();
    });
  });

  it('calls /auth/refresh and /auth/me exactly once under StrictMode', async () => {
    refreshMock.mockResolvedValue({ accessToken: 'tok', accessTokenExpiresAt: new Date(Date.now() + 60_000).toISOString() });
    meMock.mockResolvedValue(user);

    render(
      <StrictMode>
        <AuthInit>
          <div>app-content</div>
        </AuthInit>
      </StrictMode>,
    );

    await waitFor(() => expect(screen.getByText('app-content')).toBeInTheDocument());

    expect(refreshMock).toHaveBeenCalledTimes(1);
    expect(meMock).toHaveBeenCalledTimes(1);
    expect(useAuthStore.getState().accessToken).toBe('tok');
    expect(useAuthStore.getState().user).toEqual(user);
  });

  it('sets token BEFORE calling /auth/me so the request can attach Authorization', async () => {
    refreshMock.mockResolvedValue({ accessToken: 'tok-A', accessTokenExpiresAt: new Date(Date.now() + 60_000).toISOString() });
    meMock.mockImplementation(() => {
      // At the moment /auth/me is invoked, the store must already hold the
      // token so the request interceptor can attach the Bearer header.
      expect(useAuthStore.getState().accessToken).toBe('tok-A');
      return Promise.resolve(user);
    });

    render(
      <StrictMode>
        <AuthInit>
          <div>app-content</div>
        </AuthInit>
      </StrictMode>,
    );

    await waitFor(() => expect(screen.getByText('app-content')).toBeInTheDocument());
    expect(meMock).toHaveBeenCalledTimes(1);
  });

  it('renders children with no session when refresh fails', async () => {
    refreshMock.mockRejectedValue(new Error('401'));

    render(
      <StrictMode>
        <AuthInit>
          <div>app-content</div>
        </AuthInit>
      </StrictMode>,
    );

    await waitFor(() => expect(screen.getByText('app-content')).toBeInTheDocument());

    expect(refreshMock).toHaveBeenCalledTimes(1);
    expect(meMock).not.toHaveBeenCalled();
    expect(useAuthStore.getState().isAuthenticated()).toBe(false);
  });
});
