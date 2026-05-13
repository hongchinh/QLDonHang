import { useAuthStore } from '@/stores/auth-store';
import type { Permission, Role } from '@/lib/permissions';

interface Props {
  permission?: Permission;
  requireRole?: Role;
  fallback?: React.ReactNode;
  children: React.ReactNode;
}

export function Can({ permission, requireRole, fallback = null, children }: Props) {
  const hasPermission = useAuthStore((s) => s.hasPermission);
  const isInRole = useAuthStore((s) => s.isInRole);

  if (permission && !hasPermission(permission)) return <>{fallback}</>;
  if (requireRole && !isInRole(requireRole)) return <>{fallback}</>;
  return <>{children}</>;
}
