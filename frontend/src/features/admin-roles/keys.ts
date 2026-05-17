export const adminRolesKeys = {
  all: ['admin', 'roles'] as const,
  lists: () => [...adminRolesKeys.all, 'list'] as const,
  details: () => [...adminRolesKeys.all, 'detail'] as const,
  detail: (id: string) => [...adminRolesKeys.details(), id] as const,
  permissionsCatalog: ['admin', 'permissions-catalog'] as const,
};
