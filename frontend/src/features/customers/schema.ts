import { z } from 'zod';
import { optionalEmail, optionalString } from '@/lib/zod-helpers';

export const customerSchema = z.object({
  code: optionalString(50),
  name: z.string().min(1, 'Tên khách hàng là bắt buộc').max(255),
  taxCode: optionalString(20),
  companyAddress: optionalString(1000),
  defaultShippingAddress: optionalString(1000),
  contactPerson: optionalString(255),
  phoneNumber: optionalString(30),
  email: optionalEmail(),
  group: z.enum(['Company', 'Agent', 'Retail', 'Project']),
  note: optionalString(2000),
  status: z.enum(['Active', 'Inactive']).optional(),
});

export type CustomerFormValues = z.input<typeof customerSchema>;
export type CustomerFormParsed = z.output<typeof customerSchema>;
