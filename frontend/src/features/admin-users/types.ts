export type UserStatus = 'Active' | 'Disabled';

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

export interface AdminUserDetail {
  id: string;
  username: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  roleCode: string | null;
  status: UserStatus;
  isDeleted: boolean;
  lastLoginAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateUserPayload {
  username: string;
  email: string;
  fullName: string;
  phoneNumber?: string | null;
  roleCode: string;
  password: string;
  status: UserStatus;
}

export interface UpdateUserPayload {
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  roleCode: string;
  status: UserStatus;
}

export interface ResetPasswordPayload {
  newPassword: string;
}

export interface SetUserStatusPayload {
  status: UserStatus;
}
