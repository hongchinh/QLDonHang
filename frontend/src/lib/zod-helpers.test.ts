import { describe, expect, it } from 'vitest';
import { z } from 'zod';
import { optionalEmail, optionalString } from './zod-helpers';

describe('optionalString', () => {
  const schema = z.object({ value: optionalString(10) });

  it('accepts empty string and returns undefined', () => {
    const result = schema.parse({ value: '' });
    expect(result.value).toBeUndefined();
  });

  it('accepts missing value', () => {
    const result = schema.parse({});
    expect(result.value).toBeUndefined();
  });

  it('keeps non-empty value', () => {
    const result = schema.parse({ value: 'abc' });
    expect(result.value).toBe('abc');
  });

  it('rejects oversized value', () => {
    const result = schema.safeParse({ value: 'a'.repeat(11) });
    expect(result.success).toBe(false);
  });
});

describe('optionalEmail', () => {
  const schema = z.object({ email: optionalEmail() });

  it('accepts empty', () => {
    const r = schema.parse({ email: '' });
    expect(r.email).toBeUndefined();
  });

  it('accepts valid email', () => {
    const r = schema.parse({ email: 'a@b.com' });
    expect(r.email).toBe('a@b.com');
  });

  it('rejects malformed email', () => {
    const r = schema.safeParse({ email: 'not-email' });
    expect(r.success).toBe(false);
  });
});
