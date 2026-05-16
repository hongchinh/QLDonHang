export const PERMISSIONS = [
  'customers.view',
  'customers.create',
  'customers.update',
  'customers.delete',
  'products.view',
  'products.create',
  'products.update',
  'products.delete',
  'quotations.view',
  'quotations.create',
  'quotations.update',
  'quotations.delete',
  'quotations.print',
  'quotations.approve',
  'quotations.view_all',
  'quotations.transfer_own',
  'quotations.transfer_any',
  'quotations.clone_orphan',
  'quotations.bypass_lock',
  'users.view',
  'users.create',
  'users.update',
  'users.delete',
  'user_settings.manage',
  'reports.revenue',
] as const;

export type Permission = (typeof PERMISSIONS)[number];

export const ROLES = ['ADMIN', 'SALES', 'WAREHOUSE', 'ACCOUNTANT', 'MANAGER'] as const;
export type Role = (typeof ROLES)[number];
