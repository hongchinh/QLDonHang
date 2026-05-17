import { describe, expect, it } from 'vitest';
import { canAccessRoute } from './route-permissions';

const SALES_PERMS = [
  'customers.view', 'customers.create', 'customers.update',
  'products.view',
  'quotations.view', 'quotations.create', 'quotations.update',
  'quotations.print', 'quotations.transfer_own',
];

describe('canAccessRoute', () => {
  it('allows route không có rule cho mọi user', () => {
    expect(canAccessRoute('/', [], [])).toBe(true);
    expect(canAccessRoute('/settings', [], [])).toBe(true);
    expect(canAccessRoute('/settings/my-quotation-settings', [], [])).toBe(true);
  });

  it('chặn /admin/dashboard với sale (thiếu view_all)', () => {
    expect(canAccessRoute('/admin/dashboard', SALES_PERMS, ['SALES'])).toBe(false);
  });

  it('cho phép /admin/dashboard nếu có view_all', () => {
    expect(canAccessRoute('/admin/dashboard', ['quotations.view_all'], ['ADMIN'])).toBe(true);
  });

  it('cho phép /quotations và /quotations/:id với sale', () => {
    expect(canAccessRoute('/quotations', SALES_PERMS, ['SALES'])).toBe(true);
    expect(canAccessRoute('/quotations/abc-123', SALES_PERMS, ['SALES'])).toBe(true);
    expect(canAccessRoute('/quotations/new', SALES_PERMS, ['SALES'])).toBe(true);
  });

  it('chặn /reports/sales-performance nếu thiếu view_all (kể cả có reports.revenue)', () => {
    expect(canAccessRoute('/reports/sales-performance', ['reports.revenue'], [])).toBe(false);
  });

  it('chặn /reports/revenue nếu thiếu reports.revenue, cho phép nếu có', () => {
    expect(canAccessRoute('/reports/revenue', [], [])).toBe(false);
    expect(canAccessRoute('/reports/revenue', ['reports.revenue'], [])).toBe(true);
    expect(canAccessRoute('/reports/sales-revenue', ['reports.revenue'], [])).toBe(true);
  });

  it('chặn /admin/users và /admin/users/:id nếu thiếu user_settings.manage', () => {
    expect(canAccessRoute('/admin/users', SALES_PERMS, ['SALES'])).toBe(false);
    expect(canAccessRoute('/admin/users/abc', SALES_PERMS, ['SALES'])).toBe(false);
  });

  it('chặn /admin/roles với sale (thiếu roles.view)', () => {
    expect(canAccessRoute('/admin/roles', SALES_PERMS, ['SALES'])).toBe(false);
  });

  it('cho phép /admin/roles nếu có roles.view', () => {
    expect(canAccessRoute('/admin/roles', ['roles.view'], [])).toBe(true);
  });

  it('phân biệt /admin/users/:id/transfer-quotations cần transfer_any', () => {
    // user_settings.manage không đủ — pattern cụ thể hơn match trước
    expect(canAccessRoute(
      '/admin/users/abc/transfer-quotations',
      ['user_settings.manage'],
      [],
    )).toBe(false);
    expect(canAccessRoute(
      '/admin/users/abc/transfer-quotations',
      ['quotations.transfer_any'],
      [],
    )).toBe(true);
  });
});
