export interface AdminUserListItem {
  id: string;
  username: string;
  fullName: string;
  roleCode: string | null;
  isActive: boolean;
  lastLoginAt: string | null;
}

export interface AdminUserListParams {
  search?: string;
  activeOnly?: boolean;
}
