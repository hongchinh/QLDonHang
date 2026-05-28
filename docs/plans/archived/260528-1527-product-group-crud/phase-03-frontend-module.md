# Phase 03 — Frontend: Feature Module

**Status:** [ ] pending
**Complexity:** M

## Objective

Create the `features/product-groups/` module: TypeScript types, React Query keys, API
client, hooks, and Zod schema. No pages yet — wiring comes in Phase 04.

## Files

- `frontend/src/features/product-groups/types.ts` *(new)*
- `frontend/src/features/product-groups/keys.ts` *(new)*
- `frontend/src/features/product-groups/api.ts` *(new)*
- `frontend/src/features/product-groups/hooks.ts` *(new)*
- `frontend/src/features/product-groups/schema.ts` *(new)*

## Tasks

### Task 1 — Create types.ts

Create `frontend/src/features/product-groups/types.ts`:

```typescript
export interface ProductGroup {
  id: string;
  code: string;
  name: string;
  description?: string;
  sortOrder: number;
  isActive: boolean;
  productCount: number;
  createdAt: string;
}

export interface ProductGroupListItem {
  id: string;
  code: string;
  name: string;
  description?: string;
  sortOrder: number;
  isActive: boolean;
  productCount: number;
}

export interface ProductGroupListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface CreateProductGroupRequest {
  code?: string;
  name: string;
  description?: string;
  sortOrder: number;
  isActive: boolean;
}

export interface UpdateProductGroupRequest {
  name: string;
  description?: string;
  sortOrder: number;
  isActive: boolean;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
```

### Task 2 — Create keys.ts

Create `frontend/src/features/product-groups/keys.ts`:

```typescript
import type { ProductGroupListParams } from './types';

export const productGroupKeys = {
  all: ['product-groups'] as const,
  lists: () => [...productGroupKeys.all, 'list'] as const,
  list: (params: ProductGroupListParams) => [...productGroupKeys.lists(), params] as const,
  details: () => [...productGroupKeys.all, 'detail'] as const,
  detail: (id: string) => [...productGroupKeys.details(), id] as const,
};
```

### Task 3 — Create api.ts

Create `frontend/src/features/product-groups/api.ts`:

```typescript
import { apiDelete, apiGet, apiPost, apiPut } from '@/lib/api-client';
import type {
  CreateProductGroupRequest,
  PagedResult,
  ProductGroup,
  ProductGroupListItem,
  ProductGroupListParams,
  UpdateProductGroupRequest,
} from './types';

export const productGroupsApi = {
  list: (params: ProductGroupListParams) =>
    apiGet<PagedResult<ProductGroupListItem>>('/product-groups', params),
  get: (id: string) => apiGet<ProductGroup>(`/product-groups/${id}`),
  create: (data: CreateProductGroupRequest) =>
    apiPost<ProductGroup>('/product-groups', data),
  update: (id: string, data: UpdateProductGroupRequest) =>
    apiPut<ProductGroup>(`/product-groups/${id}`, data),
  remove: (id: string) => apiDelete(`/product-groups/${id}`),
};
```

### Task 4 — Create hooks.ts

Create `frontend/src/features/product-groups/hooks.ts`:

```typescript
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { lookupKeys } from '@/features/products/keys';
import { productGroupsApi } from './api';
import { productGroupKeys } from './keys';
import type {
  CreateProductGroupRequest,
  ProductGroupListParams,
  UpdateProductGroupRequest,
} from './types';

export function useProductGroups(params: ProductGroupListParams) {
  return useQuery({
    queryKey: productGroupKeys.list(params),
    queryFn: () => productGroupsApi.list(params),
    placeholderData: keepPreviousData,
  });
}

export function useProductGroup(id: string | undefined) {
  return useQuery({
    queryKey: productGroupKeys.detail(id ?? ''),
    queryFn: () => productGroupsApi.get(id!),
    enabled: !!id,
  });
}

export function useCreateProductGroup() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateProductGroupRequest) => productGroupsApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: productGroupKeys.lists() });
      // Keep ProductForm group dropdown fresh.
      qc.invalidateQueries({ queryKey: lookupKeys.productGroups() });
    },
  });
}

export function useUpdateProductGroup() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateProductGroupRequest }) =>
      productGroupsApi.update(id, data),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: productGroupKeys.lists() });
      qc.invalidateQueries({ queryKey: productGroupKeys.detail(id) });
      qc.invalidateQueries({ queryKey: lookupKeys.productGroups() });
    },
  });
}

export function useDeleteProductGroup() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => productGroupsApi.remove(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: productGroupKeys.lists() });
      qc.invalidateQueries({ queryKey: lookupKeys.productGroups() });
    },
  });
}
```

### Task 5 — Create schema.ts

Create `frontend/src/features/product-groups/schema.ts`:

```typescript
import { z } from 'zod';

export const productGroupSchema = z.object({
  code: z.string().max(50).optional(),
  name: z.string().min(1, 'Tên nhóm không được để trống').max(255),
  description: z.string().max(500).optional(),
  sortOrder: z.coerce.number({ invalid_type_error: 'Phải là số' }).int().min(0, 'Phải >= 0'),
  isActive: z.boolean(),
});

export type ProductGroupFormValues = z.input<typeof productGroupSchema>;
export type ProductGroupFormParsed = z.output<typeof productGroupSchema>;
```

### Task 6 — Verify type-check

```bash
cd frontend && npm run typecheck
```

Expected: 0 errors related to the new `features/product-groups/` files.

### Task 7 — Commit

```bash
git add frontend/src/features/product-groups/
git commit -m "feat: add product-groups frontend feature module (types, keys, api, hooks, schema)"
```

## Verification

```bash
cd frontend && npm run typecheck
```

Expected: exits with 0 errors.

## Exit Criteria

- All 5 files exist under `frontend/src/features/product-groups/`.
- `npm run typecheck` exits with 0 errors.
