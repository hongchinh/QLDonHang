import { describe, it, expect } from 'vitest';
import { parseOwnerIds } from './owner-ids';

describe('parseOwnerIds', () => {
  it('returns empty for empty input', () => {
    expect(parseOwnerIds('')).toEqual([]);
  });

  it('parses single guid', () => {
    const g = '11111111-1111-1111-1111-111111111111';
    expect(parseOwnerIds(g)).toEqual([g]);
  });

  it('parses csv guids', () => {
    const a = '11111111-1111-1111-1111-111111111111';
    const b = '22222222-2222-2222-2222-222222222222';
    expect(parseOwnerIds(`${a},${b}`)).toEqual([a, b]);
  });

  it('filters out non-guid tokens', () => {
    const a = '11111111-1111-1111-1111-111111111111';
    expect(parseOwnerIds(`${a},not-a-guid,abc`)).toEqual([a]);
  });

  it('trims whitespace around tokens', () => {
    const a = '11111111-1111-1111-1111-111111111111';
    const b = '22222222-2222-2222-2222-222222222222';
    expect(parseOwnerIds(` ${a} , ${b} `)).toEqual([a, b]);
  });
});
