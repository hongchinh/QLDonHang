import { useEffect, useState } from 'react';
import { authApi } from '@/features/auth/api';
import { useAuthStore } from '@/stores/auth-store';

interface Props {
  children: React.ReactNode;
}

export function AuthInit({ children }: Props) {
  const setToken = useAuthStore((s) => s.setToken);
  const setUser = useAuthStore((s) => s.setUser);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    let cancelled = false;

    // Blindly attempt to mint an access token from the HttpOnly refresh cookie.
    // If the cookie is missing or expired we just stay logged out — no user-
    // visible error needed because login screen is the right next step anyway.
    (async () => {
      try {
        const pair = await authApi.refresh();
        if (cancelled) return;
        setToken(pair.accessToken, pair.accessTokenExpiresAt);
        const user = await authApi.me();
        if (cancelled) return;
        setUser(user);
      } catch (error) {
        if (import.meta.env.DEV) {
          console.warn('Auth boot refresh failed; no valid refresh cookie/session was available.', error);
        }
        // No valid session — boot to the login screen.
      } finally {
        if (!cancelled) setReady(true);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [setToken, setUser]);

  if (!ready) {
    return (
      <div className="flex h-screen items-center justify-center text-sm text-muted-foreground">
        Đang tải...
      </div>
    );
  }
  return <>{children}</>;
}
