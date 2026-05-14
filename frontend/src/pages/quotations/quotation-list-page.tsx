import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
} from '@tanstack/react-table';
import { Plus, Pencil, Printer, Ban, Search, Copy } from 'lucide-react';
import {
  useQuotations,
  useTransitionQuotation,
  useCloneQuotation,
} from '@/features/quotations/hooks';
import { useAuthStore } from '@/stores/auth-store';
import { quotationsApi } from '@/features/quotations/api';
import type {
  QuotationListItem,
  QuotationStatus,
} from '@/features/quotations/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Card, CardContent } from '@/components/ui/card';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { Can } from '@/components/auth/can';
import { useDebouncedValue } from '@/lib/use-debounced-value';
import { useSearchParamNumber, useSearchParamString } from '@/lib/use-search-param-state';
import { toast } from '@/lib/use-toast';
import { getErrorMessage } from '@/lib/api-client';
import { StatusPill } from './components/status-pill';

const PAGE_SIZE = 20;
const ALL = '__all__';

const currency = new Intl.NumberFormat('vi-VN');

function formatDate(iso?: string) {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString('vi-VN');
}

async function downloadPdf(id: string, code: string) {
  const blob = await quotationsApi.downloadPdf(id);
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `BaoGia_${code}.pdf`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

export function QuotationListPage() {
  const [search, setSearch] = useSearchParamString('q');
  const [page, setPage] = useSearchParamNumber('page', 1);
  const [statusFilter, setStatusFilter] = useSearchParamString('status');
  const [fromDate, setFromDate] = useSearchParamString('from');
  const [toDate, setToDate] = useSearchParamString('to');
  const debouncedSearch = useDebouncedValue(search, 300);

  const [pendingCancel, setPendingCancel] = useState<QuotationListItem | null>(null);

  const { data, isLoading, isError, error } = useQuotations({
    page,
    pageSize: PAGE_SIZE,
    search: debouncedSearch || undefined,
    status: (statusFilter as QuotationStatus) || undefined,
    from: fromDate || undefined,
    to: toDate || undefined,
  });
  const transition = useTransitionQuotation();
  const clone = useCloneQuotation();
  const hasViewAll = useAuthStore((s) => s.hasPermission('quotations.view_all'));

  const columns = useMemo<ColumnDef<QuotationListItem>[]>(
    () => [
      { header: 'Số báo giá', accessorKey: 'code' },
      {
        header: 'Ngày',
        accessorKey: 'quotationDate',
        cell: ({ row }) => formatDate(row.original.quotationDate),
      },
      { header: 'Khách hàng', accessorKey: 'customerName' },
      { header: 'SĐT', accessorKey: 'contactPhone' },
      {
        header: 'Tổng tiền',
        accessorKey: 'total',
        cell: ({ row }) => (
          <span className="tabular-nums">{currency.format(row.original.total)}</span>
        ),
      },
      {
        header: 'Trạng thái',
        accessorKey: 'status',
        cell: ({ row }) => <StatusPill status={row.original.status} />,
      },
      ...(hasViewAll
        ? [{
            header: 'Chủ sở hữu',
            id: 'owner',
            cell: ({ row }: { row: { original: QuotationListItem } }) => (
              <span>
                {row.original.ownerFullName ?? '—'}
                {row.original.isOwnerDeleted && (
                  <span className="ml-2 rounded bg-amber-100 px-2 py-0.5 text-xs text-amber-900">
                    đã nghỉ
                  </span>
                )}
              </span>
            ),
          } as ColumnDef<QuotationListItem>]
        : []),
      { header: 'Người lập', accessorKey: 'createdByName' },
      {
        id: 'actions',
        header: '',
        cell: ({ row }) => {
          const q = row.original;
          const canCancel = q.status !== 'Cancelled' && q.status !== 'ConvertedToOrder';
          return (
            <div className="flex justify-end gap-1">
              <Can permission="quotations.update">
                <Button asChild variant="ghost" size="icon" aria-label="Sửa">
                  <Link to={`/quotations/${q.id}`}><Pencil className="h-4 w-4" /></Link>
                </Button>
              </Can>
              {q.canClone && (
                <Can permission="quotations.create">
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label="Clone"
                    onClick={() => {
                      clone.mutate(q.id, {
                        onSuccess: (cloned) =>
                          toast({ variant: 'success', title: 'Đã clone báo giá', description: cloned.code }),
                        onError: (err) =>
                          toast({ variant: 'destructive', title: 'Không thể clone', description: getErrorMessage(err) }),
                      });
                    }}
                  >
                    <Copy className="h-4 w-4" />
                  </Button>
                </Can>
              )}
              <Can permission="quotations.print">
                <Button
                  variant="ghost"
                  size="icon"
                  aria-label="In PDF"
                  onClick={() => {
                    downloadPdf(q.id, q.code).catch((err) =>
                      toast({ variant: 'destructive', title: 'Không tải được PDF', description: getErrorMessage(err) }),
                    );
                  }}
                >
                  <Printer className="h-4 w-4" />
                </Button>
              </Can>
              {canCancel && (
                <Can permission="quotations.update">
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label="Hủy"
                    onClick={() => setPendingCancel(q)}
                  >
                    <Ban className="h-4 w-4" />
                  </Button>
                </Can>
              )}
            </div>
          );
        },
      },
    ],
    [hasViewAll, clone],
  );

  const table = useReactTable({
    data: data?.items ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  const onConfirmCancel = () => {
    if (!pendingCancel) return;
    const target = pendingCancel;
    transition.mutate(
      { id: target.id, action: 'Cancel' },
      {
        onSuccess: () => {
          toast({ variant: 'success', title: 'Đã hủy báo giá', description: target.code });
          setPendingCancel(null);
        },
        onError: (err) => {
          toast({ variant: 'destructive', title: 'Không thể hủy', description: getErrorMessage(err) });
        },
      },
    );
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Báo giá</h1>
          <p className="text-sm text-muted-foreground">Quản lý báo giá gửi khách</p>
        </div>
        <Can permission="quotations.create">
          <Button asChild>
            <Link to="/quotations/new">
              <Plus className="mr-2 h-4 w-4" /> Thêm báo giá
            </Link>
          </Button>
        </Can>
      </div>

      <Card>
        <CardContent className="p-4">
          <div className="mb-4 flex flex-wrap items-center gap-2">
            <div className="relative max-w-sm flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Tìm theo số / tên khách..."
                value={search}
                onChange={(e) => { setSearch(e.target.value); if (page !== 1) setPage(1); }}
                className="pl-9"
              />
            </div>

            <Select
              value={statusFilter || ALL}
              onValueChange={(v) => { setStatusFilter(v === ALL ? '' : v); if (page !== 1) setPage(1); }}
            >
              <SelectTrigger className="w-40" aria-label="Trạng thái">
                <SelectValue placeholder="Trạng thái" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ALL}>Tất cả</SelectItem>
                <SelectItem value="Draft">Nháp</SelectItem>
                <SelectItem value="Sent">Đã gửi</SelectItem>
                <SelectItem value="Confirmed">Đã xác nhận</SelectItem>
                <SelectItem value="Cancelled">Đã hủy</SelectItem>
              </SelectContent>
            </Select>

            <Input
              type="date"
              value={fromDate}
              onChange={(e) => { setFromDate(e.target.value); if (page !== 1) setPage(1); }}
              className="w-44"
              aria-label="Từ ngày"
            />
            <Input
              type="date"
              value={toDate}
              onChange={(e) => { setToDate(e.target.value); if (page !== 1) setPage(1); }}
              className="w-44"
              aria-label="Đến ngày"
            />
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
                    Chưa có báo giá nào.
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

          {data && data.totalPages > 1 && (
            <div className="mt-4 flex items-center justify-between text-sm">
              <div className="text-muted-foreground">
                Trang {data.page} / {data.totalPages} • Tổng {data.totalItems}
              </div>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" disabled={!data.hasPreviousPage} onClick={() => setPage(page - 1)}>
                  Trước
                </Button>
                <Button variant="outline" size="sm" disabled={!data.hasNextPage} onClick={() => setPage(page + 1)}>
                  Sau
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <ConfirmDialog
        open={!!pendingCancel}
        onOpenChange={(open) => !open && setPendingCancel(null)}
        title="Hủy báo giá?"
        description={
          pendingCancel ? (
            <>
              Bạn chắc chắn muốn hủy báo giá <strong>{pendingCancel.code}</strong>? Báo giá đã hủy không thể chỉnh sửa lại.
            </>
          ) : null
        }
        destructive
        confirmLabel="Hủy báo giá"
        loading={transition.isPending}
        onConfirm={onConfirmCancel}
      />
    </div>
  );
}
