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
