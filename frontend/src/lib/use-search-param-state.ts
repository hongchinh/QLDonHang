import { useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';

export function useSearchParamString(key: string, defaultValue = ''): [string, (next: string) => void] {
  const [params, setParams] = useSearchParams();
  const value = params.get(key) ?? defaultValue;
  const setValue = useCallback(
    (next: string) => {
      setParams(
        (prev) => {
          const out = new URLSearchParams(prev);
          if (!next) out.delete(key);
          else out.set(key, next);
          return out;
        },
        { replace: true },
      );
    },
    [key, setParams],
  );
  return [value, setValue];
}

export function useSearchParamNumber(key: string, defaultValue = 1): [number, (next: number) => void] {
  const [params, setParams] = useSearchParams();
  const raw = params.get(key);
  const parsed = raw ? Number(raw) : NaN;
  const value = Number.isFinite(parsed) && parsed > 0 ? parsed : defaultValue;
  const setValue = useCallback(
    (next: number) => {
      setParams(
        (prev) => {
          const out = new URLSearchParams(prev);
          if (next === defaultValue) out.delete(key);
          else out.set(key, String(next));
          return out;
        },
        { replace: true },
      );
    },
    [key, defaultValue, setParams],
  );
  return [value, setValue];
}
