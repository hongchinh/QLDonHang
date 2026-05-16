import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
} from '@tanstack/react-table';
import { Search, Settings2, ArrowRightLeft } from 'lucide-react';
import { useAdminUsers } from '@/features/admin-users/hooks';
import type { AdminUserListItem } from '@/features/admin-users/types';
import { useSearchParamString } from '@/lib/use-search-param-state';
import { useDebouncedValue } from '@/lib/use-debounced-value';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Can } from '@/components/auth/can';
import { getErrorMessage } from '@/lib/api-client';

export function UsersListPage() {
  const [search, setSearch] = useSearchParamString('q');
  const [activeOnlyRaw, setActiveOnlyRaw] = useSearchParamString('active');
  const activeOnly = activeOnlyRaw === '1';
  const debouncedSearch = useDebouncedValue(search, 300);

  const { data, isLoading, isError, error } = useAdminUsers({
    search: debouncedSearch || undefined,
    activeOnly,
  });

  const columns = useMemo<ColumnDef<AdminUserListItem>[]>(
    () => [
      { header: 'Username', accessorKey: 'username' },
      { header: 'Họ tên', accessorKey: 'fullName' },
      {
        header: 'Vai trò',
        accessorKey: 'roleCode',
        cell: ({ row }) =>
          row.original.roleCode ? (
            <Badge variant="outline">{row.original.roleCode}</Badge>
          ) : (
            <span className="text-muted-foreground">—</span>
          ),
      },
      {
        header: 'Trạng thái',
        accessorKey: 'isActive',
        cell: ({ row }) =>
          row.original.isActive ? (
            <Badge variant="success">Đang dùng</Badge>
          ) : (
            <Badge variant="secondary">Đã nghỉ</Badge>
          ),
      },
      {
        id: 'actions',
        header: '',
        cell: ({ row }) => (
          <div className="flex justify-end gap-2">
            <Button asChild variant="ghost" size="icon" aria-label="Cấu hình báo giá">
              <Link to={`/admin/user-settings/${row.original.id}`}>
                <Settings2 className="h-4 w-4" />
              </Link>
            </Button>
            <Can permission="quotations.transfer_any">
              <Button asChild variant="ghost" size="icon" aria-label="Chuyển nhượng báo giá">
                <Link to={`/admin/users/${row.original.id}/transfer-quotations`}>
                  <ArrowRightLeft className="h-4 w-4" />
                </Link>
              </Button>
            </Can>
          </div>
        ),
      },
    ],
    [],
  );

  const table = useReactTable({
    data: data ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold">Quản lý người dùng</h1>
        <p className="text-sm text-muted-foreground">
          Cấu hình báo giá theo từng user, chuyển nhượng báo giá hàng loạt.
        </p>
      </div>

      <Card>
        <CardContent className="p-4">
          <div className="mb-4 flex items-center gap-3">
            <div className="relative max-w-sm flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Tìm theo username / họ tên..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-9"
              />
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={activeOnly}
                onChange={(e) => setActiveOnlyRaw(e.target.checked ? '1' : '')}
              />
              Chỉ user đang hoạt động
            </label>
          </div>

          {isError && (
            <div className="mb-3 rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
              {getErrorMessage(error)}
            </div>
          )}

          <Table>
            <TableHeader>
              {table.getHeaderGroups().map((hg) => (
                <TableRow key={hg.id}>
                  {hg.headers.map((h) => (
                    <TableHead key={h.id}>{flexRender(h.column.columnDef.header, h.getContext())}</TableHead>
                  ))}
                </TableRow>
              ))}
            </TableHeader>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">
                    Đang tải...
                  </TableCell>
                </TableRow>
              ) : table.getRowModel().rows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">
                    Không có user nào.
                  </TableCell>
                </TableRow>
              ) : (
                table.getRowModel().rows.map((row) => (
                  <TableRow key={row.id}>
                    {row.getVisibleCells().map((c) => (
                      <TableCell key={c.id}>{flexRender(c.column.columnDef.cell, c.getContext())}</TableCell>
                    ))}
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
