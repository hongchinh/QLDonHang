import { useEffect, useState } from 'react';
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  Package,
  FileText,
  BarChart3,
  UserCog,
  Users2,
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

interface NavGroup {
  label: string;
  items: NavItem[];
}

const navGroups: NavGroup[] = [
  {
    label: 'Chức năng',
    items: [
      { to: '/customers', label: 'Khách hàng', icon: Users, permission: 'customers.view' },
      { to: '/products', label: 'Hàng hóa', icon: Package, permission: 'products.view' },
      { to: '/quotations', label: 'Báo giá', icon: FileText, permission: 'quotations.view' },
    ],
  },
  {
    label: 'Báo cáo',
    items: [
      { to: '/reports/revenue', label: 'Doanh thu', icon: BarChart3, permission: 'reports.revenue' },
      { to: '/reports/sales-revenue', label: 'Doanh thu sale', icon: BarChart3, permission: 'reports.revenue' },
      { to: '/reports/sales-performance', label: 'Hiệu suất sale', icon: BarChart3, permission: 'quotations.view_all' },
    ],
  },
  {
    label: 'Setting',
    items: [
      { to: '/settings/my-quotation-settings', label: 'Cài đặt của tôi', icon: UserCog },
      { to: '/admin/users', label: 'Quản lý người dùng', icon: Users2, permission: 'user_settings.manage' },
    ],
  },
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

  const dashboardItem: NavItem = hasPermission('quotations.view_all')
    ? { to: '/admin/dashboard', label: 'Tổng quan', icon: LayoutDashboard }
    : { to: '/', label: 'Tổng quan', icon: LayoutDashboard };

  const visibleGroups = navGroups
    .map((group) => ({
      ...group,
      items: group.items.filter((item) => {
        if (item.permission && !hasPermission(item.permission)) return false;
        if (item.role && !isInRole(item.role)) return false;
        return true;
      }),
    }))
    .filter((group) => group.items.length > 0);

  const handleLogout = () => logout.mutate();

  return (
    <div className="flex h-screen overflow-hidden bg-muted/30">
      {/* Desktop sidebar */}
      <aside className="hidden w-64 flex-col border-r bg-card md:flex">
        <SidebarContent
          dashboardItem={dashboardItem}
          groups={visibleGroups}
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
          dashboardItem={dashboardItem}
          groups={visibleGroups}
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
        <div className="flex-1 overflow-y-auto p-4 md:p-3">
          <Outlet />
        </div>
      </main>
    </div>
  );
}

interface SidebarContentProps {
  dashboardItem: NavItem;
  groups: NavGroup[];
  user: ReturnType<typeof useAuthStore.getState>['user'];
  onLogout: () => void;
  onClose?: () => void;
}

function NavLinkItem({ to, label, icon: Icon }: NavItem) {
  return (
    <NavLink
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
  );
}

function SidebarContent({ dashboardItem, groups, user, onLogout, onClose }: SidebarContentProps) {
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
      <nav className="flex-1 overflow-y-auto p-3">
        <div className="space-y-1">
          <NavLinkItem {...dashboardItem} />
        </div>
        {groups.map((group) => (
          <div key={group.label} className="mt-4 space-y-1">
            <div className="px-3 pb-1 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              {group.label}
            </div>
            {group.items.map((item) => (
              <NavLinkItem key={item.to} {...item} />
            ))}
          </div>
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
