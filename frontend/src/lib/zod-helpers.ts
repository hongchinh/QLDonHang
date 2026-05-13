import { z } from 'zod';

// Optional free-text field with a max length. Empty strings are treated as
// "not provided" so the API payload can drop them rather than store "".
export function optionalString(max: number) {
  return z
    .string()
    .max(max)
    .optional()
    .or(z.literal(''))
    .transform((v) => (v ? v : undefined));
}

export function optionalEmail(max = 255) {
  return z
    .string()
    .max(max)
    .email('Email không hợp lệ')
    .optional()
    .or(z.literal(''))
    .transform((v) => (v ? v : undefined));
}

// Optional numeric field — accepts the raw <input type="number"> value
// (a string, possibly empty) or a parsed number, and emits `number | undefined`
// after enforcing optional min/max bounds.
export function optionalNumber(opts: { min?: number; max?: number } = {}) {
  return z
    .union([z.string(), z.number(), z.undefined()])
    .transform((value, ctx) => {
      if (value === '' || value === undefined) return undefined;
      const n = typeof value === 'number' ? value : Number(value);
      if (!Number.isFinite(n)) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, message: 'Phải là số hợp lệ' });
        return z.NEVER;
      }
      if (opts.min !== undefined && n < opts.min) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, message: `Giá trị phải >= ${opts.min}` });
        return z.NEVER;
      }
      if (opts.max !== undefined && n > opts.max) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, message: `Giá trị phải <= ${opts.max}` });
        return z.NEVER;
      }
      return n;
    });
}
