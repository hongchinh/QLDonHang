import { describe, expect, it } from 'vitest';
import { ApiCallError, getErrorMessage } from './api-client';

describe('ApiCallError', () => {
  it('preserves code, message, details', () => {
    const err = new ApiCallError({
      code: 'VALIDATION',
      message: 'Invalid input',
      details: { name: ['Required'] },
    });
    expect(err.code).toBe('VALIDATION');
    expect(err.message).toBe('Invalid input');
    expect(err.details).toEqual({ name: ['Required'] });
    expect(err).toBeInstanceOf(Error);
  });
});

describe('getErrorMessage', () => {
  it('returns ApiCallError message', () => {
    const err = new ApiCallError({ code: 'X', message: 'API down' });
    expect(getErrorMessage(err)).toBe('API down');
  });

  it('returns native Error message', () => {
    expect(getErrorMessage(new Error('boom'))).toBe('boom');
  });

  it('returns fallback for unknown shape', () => {
    expect(getErrorMessage({ weird: true })).toBe('Đã xảy ra lỗi không mong muốn.');
  });

  it('returns fallback for null', () => {
    expect(getErrorMessage(null)).toBe('Đã xảy ra lỗi không mong muốn.');
  });
});
