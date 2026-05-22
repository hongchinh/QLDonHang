// Keep this list in lock-step with backend Permissions.cs. Drift here only affects
// compile-time validation of `<Can permission="...">`; runtime auth always uses the
// backend's claims.
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
  'quotations.cancel_confirmed',
  'quotations.view_cost',
  'quotations.view_all',
  'quotations.transfer_own',
  'quotations.transfer_any',
  'quotations.clone_orphan',
  'quotations.bypass_lock',
  'quotations.accounting_confirm',
  'quotations.cancel_accounting_confirmed',
  'users.view',
  'users.create',
  'users.update',
  'users.delete',
  'roles.view',
  'roles.manage',
  'user_settings.manage',
  'system.manage_settings',
  'reports.revenue',
  'reports.profit',
  'reports.debt',
  'reports.delivery',
] as const;

export type Permission = (typeof PERMISSIONS)[number];

export const ROLES = ['ADMIN', 'SALES', 'WAREHOUSE', 'ACCOUNTANT', 'MANAGER'] as const;
export type Role = (typeof ROLES)[number];
