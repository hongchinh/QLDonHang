import { describe, expect, it } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { MemoryRouter, useLocation } from 'react-router-dom';
import React from 'react';
import { useDashboardParams } from './use-dashboard-params';

function wrap(initialEntries: string[]) {
  return ({ children }: { children: React.ReactNode }) => (
    <MemoryRouter initialEntries={initialEntries}>{children}</MemoryRouter>
  );
}

describe('useDashboardParams', () => {
  it('reads from/to from URL', () => {
    const { result } = renderHook(() => useDashboardParams(), {
      wrapper: wrap(['/?from=2026-05-01&to=2026-05-15']),
    });
    expect(result.current.from).toBe('2026-05-01');
    expect(result.current.to).toBe('2026-05-15');
  });

  it('defaults to month-start..today when params missing', () => {
    const { result } = renderHook(() => useDashboardParams(), {
      wrapper: wrap(['/']),
    });
    const now = new Date();
    const yyyy = now.getFullYear();
    const mm = String(now.getMonth() + 1).padStart(2, '0');
    expect(result.current.from.startsWith(`${yyyy}-${mm}-01`)).toBe(true);
  });

  it('setRange updates URL params', () => {
    function Probe() {
      const params = useDashboardParams();
      const loc = useLocation();
      return { params, loc };
    }
    const { result } = renderHook(() => Probe(), { wrapper: wrap(['/']) });
    act(() => {
      result.current.params.setRange('2026-01-01', '2026-01-31');
    });
    expect(result.current.loc.search).toContain('from=2026-01-01');
    expect(result.current.loc.search).toContain('to=2026-01-31');
  });

  it('setSaleUserId adds and removes saleUserId param', () => {
    function Probe() {
      const params = useDashboardParams();
      const loc = useLocation();
      return { params, loc };
    }
    const { result } = renderHook(() => Probe(), { wrapper: wrap(['/']) });
    act(() => {
      result.current.params.setSaleUserId('abc');
    });
    expect(result.current.loc.search).toContain('saleUserId=abc');
    act(() => {
      result.current.params.setSaleUserId(undefined);
    });
    expect(result.current.loc.search).not.toContain('saleUserId');
  });

  it('setPreset 7d sets a 7-day range ending today', () => {
    function Probe() {
      const params = useDashboardParams();
      const loc = useLocation();
      return { params, loc };
    }
    const { result } = renderHook(() => Probe(), { wrapper: wrap(['/']) });
    act(() => {
      result.current.params.setPreset('7d');
    });
    const search = new URLSearchParams(result.current.loc.search);
    const from = new Date(search.get('from')!);
    const to = new Date(search.get('to')!);
    const days = Math.round((to.getTime() - from.getTime()) / 86_400_000);
    expect(days).toBe(6);
  });
});
