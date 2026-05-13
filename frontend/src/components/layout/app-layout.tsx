import { useEffect, useState } from 'react';
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  Package,
  FileText,
  ClipboardList,
  Truck,
  Wallet,
  BarChart3,
  Settings,
  LogOut,
  Menu,
  X,
} from 'lucide-react';
import { useAuthStore } from '@/stores/auth-store';
import { useLogout } from '@/features/auth/hooks';
import type { Permission, Role } from '@/lib/permissions';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

interface NavItem {
  to: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  permission?: Permission;
  role?: Role;
}

const navItems: NavItem[] = [
  { to: '/', label: 'Tổng quan', icon: LayoutDashboard },
  { to: '/customers', label: 'Khách hàng', icon: Users, permission: 'customers.view' },
  { to: '/products', label: 'Hàng hóa', icon: Package, permission: 'products.view' },
  { to: '/quotations', label: 'Báo giá', icon: FileText, permission: 'quotations.view' },
  { to: '/orders', label: 'Đơn hàng', icon: ClipboardList, permission: 'orders.view' },
  { to: '/deliveries', label: 'Bàn giao', icon: Truck, permission: 'orders.deliver' },
  { to: '/payments', label: 'Thanh toán & Công nợ', icon: Wallet, permission: 'orders.pay' },
  { to: '/reports', label: 'Báo cáo', icon: BarChart3, permission: 'reports.revenue' },
  { to: '/settings', label: 'Cấu hình', icon: Settings, role: 'ADMIN' },
];

export function AppLayout() {
  const user = useAuthStore((s) => s.user);
  const hasPermission = useAuthStore((s) => s.hasPermission);
  const isInRole = useAuthStore((s) => s.isInRole);
  const logout = useLogout();
  const location = useLocation();

  const [mobileOpen, setMobileOpen] = useState(false);

  // Close mobile drawer on route change.
  useEffect(() => {
    setMobileOpen(false);
  }, [location.pathname]);

  const visibleNavItems = navItems.filter((item) => {
    if (item.permission && !hasPermission(item.permission)) return false;
    if (item.role && !isInRole(item.role)) return false;
    return true;
  });

  const handleLogout = () => logout.mutate();

  return (
    <div className="flex h-screen overflow-hidden bg-muted/30">
      {/* Desktop sidebar */}
      <aside className="hidden w-64 flex-col border-r bg-card md:flex">
        <SidebarContent
          items={visibleNavItems}
          user={user}
          onLogout={handleLogout}
        />
      </aside>

      {/* Mobile drawer */}
      {mobileOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/40 md:hidden"
          onClick={() => setMobileOpen(false)}
          aria-hidden
        />
      )}
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-50 flex w-64 flex-col border-r bg-card transition-transform md:hidden',
          mobileOpen ? 'translate-x-0' : '-translate-x-full',
        )}
      >
        <SidebarContent
          items={visibleNavItems}
          user={user}
          onLogout={handleLogout}
          onClose={() => setMobileOpen(false)}
        />
      </aside>

      <main className="flex flex-1 flex-col overflow-hidden">
        <header className="flex h-16 items-center justify-between border-b bg-card px-4 md:px-6">
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="icon"
              className="md:hidden"
              aria-label="Mở menu"
              onClick={() => setMobileOpen(true)}
            >
              <Menu className="h-5 w-5" />
            </Button>
            <h1 className="text-lg font-semibold">Phần mềm Quản lý Đơn hàng</h1>
          </div>
          <div className="hidden text-sm text-muted-foreground sm:block">{user?.fullName}</div>
        </header>
        <div className="flex-1 overflow-y-auto p-4 md:p-6">
          <Outlet />
        </div>
      </main>
    </div>
  );
}

interface SidebarContentProps {
  items: NavItem[];
  user: ReturnType<typeof useAuthStore.getState>['user'];
  onLogout: () => void;
  onClose?: () => void;
}

function SidebarContent({ items, user, onLogout, onClose }: SidebarContentProps) {
  return (
    <>
      <div className="flex h-16 items-center justify-between border-b px-6 font-semibold">
        <Link to="/" className="text-primary">QLDonHang</Link>
        {onClose && (
          <Button variant="ghost" size="icon" aria-label="Đóng menu" onClick={onClose}>
            <X className="h-5 w-5" />
          </Button>
        )}
      </div>
      <nav className="flex-1 space-y-1 overflow-y-auto p-3">
        {items.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end={to === '/'}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground',
              )
            }
          >
            <Icon className="h-4 w-4" />
            {label}
          </NavLink>
        ))}
      </nav>
      <div className="border-t p-3">
        <div className="mb-2 px-2 text-xs text-muted-foreground">
          <div className="font-medium text-foreground">{user?.fullName}</div>
          <div>{user?.username}</div>
        </div>
        <Button variant="outline" size="sm" className="w-full" onClick={onLogout}>
          <LogOut className="mr-2 h-4 w-4" /> Đăng xuất
        </Button>
      </div>
    </>
  );
}
