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
  'user_settings.manage',
  'orders.view',
  'orders.create',
  'orders.update',
  'orders.deliver',
  'orders.pay',
  'reports.revenue',
] as const;

export type Permission = (typeof PERMISSIONS)[number];

export const ROLES = ['ADMIN', 'SALES', 'WAREHOUSE', 'ACCOUNTANT'] as const;
export type Role = (typeof ROLES)[number];
