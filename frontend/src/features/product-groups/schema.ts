import { z } from 'zod';

export const productGroupSchema = z.object({
  code: z.string().max(50).optional(),
  name: z.string().min(1, 'Tên nhóm không được để trống').max(255),
  description: z.string().max(500).optional(),
  sortOrder: z.coerce.number({ invalid_type_error: 'Phải là số' }).int().min(0, 'Phải >= 0'),
  isActive: z.boolean(),
});

export type ProductGroupFormValues = z.input<typeof productGroupSchema>;
export type ProductGroupFormParsed = z.output<typeof productGroupSchema>;
