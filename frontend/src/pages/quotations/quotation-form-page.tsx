import { useEffect, useRef, useState } from 'react';
import { useForm, useWatch, type Resolver } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Send, CheckCircle2, Ban, Printer, FileSpreadsheet, Copy } from 'lucide-react';
import {
  useCloneQuotation,
  useCreateQuotation,
  useQuotation,
  useTransitionQuotation,
  useUpdateQuotation,
} from '@/features/quotations/hooks';
import { quotationsApi } from '@/features/quotations/api';
import {
  quotationSchema,
  type QuotationFormParsed,
  type QuotationFormValues,
  type QuotationLineFormValues,
} from '@/features/quotations/schema';
import type {
  Quotation,
  QuotationAction,
  QuotationStatus,
  UpsertQuotationLineRequest,
  UpsertQuotationRequest,
} from '@/features/quotations/types';
import { useCustomer } from '@/features/customers/hooks';
import type { Customer, CustomerSearchItem } from '@/features/customers/types';
import { CustomerAutocomplete } from '@/components/customer-autocomplete/customer-autocomplete';
import { CustomerQuickAddDialog } from '@/components/customer-autocomplete/customer-quick-add-dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { ButtonLoader } from '@/components/ui/button-loader';
import { Can } from '@/components/auth/can';
import { getErrorMessage } from '@/lib/api-client';
import { toast } from '@/lib/use-toast';
import { StatusPill } from './components/status-pill';
import { LineItemsGrid, type LineItemsGridHandle } from './components/line-items-grid';
import { TotalsPanel } from './components/totals-panel';
import type { HeaderLike, LineLike } from './utils/compute-line';

type QuotationSubmitIntent = 'save-exit' | 'save-stay' | 'save-print';
type QuotationButtonAction = 'send' | 'confirm' | 'cancel' | 'clone' | 'print' | 'excel';

export function QuotationFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = !!id && id !== 'new';
  const navigate = useNavigate();

  const { data: quotation, isLoading } = useQuotation(isEdit ? id : undefined);
  const { data: initialSelectedCustomer } = useCustomer(quotation?.customerId);
  const create = useCreateQuotation();
  const update = useUpdateQuotation();
  const transition = useTransitionQuotation();

  const submitting = create.isPending || update.isPending || transition.isPending;
  const submitError = getErrorMessage(create.error ?? update.error ?? transition.error);
  const hasError = create.isError || update.isError || transition.isError;

  if (isEdit && isLoading) {
    return <div className="text-sm text-muted-foreground">Đang tải...</div>;
  }

  return (
    <QuotationFormInner
      isEdit={isEdit}
      initial={quotation}
      initialSelectedCustomer={initialSelectedCustomer}
      submitting={submitting}
      submitError={submitError}
      hasSubmitError={hasError}
      onSubmit={async (parsed, intent) => {
        try {
          if (isEdit && id) {
            await update.mutateAsync({ id, data: toPayload(parsed) });
            toast({ variant: 'success', title: 'Đã cập nhật báo giá' });
            navigate('/quotations');
          } else {
            const result = await create.mutateAsync(toPayload(parsed));
            toast({ variant: 'success', title: 'Đã tạo báo giá', description: result.code });
            if (intent === 'save-print') {
              try {
                await openQuotationPdfInNewTab(result.id);
              } catch (err) {
                toast({ variant: 'destructive', title: 'Không mở được PDF', description: getErrorMessage(err) });
              }
              navigate(`/quotations/${result.id}`);
            } else if (intent === 'save-stay') {
              navigate(`/quotations/${result.id}`);
            } else {
              navigate('/quotations');
            }
          }
        } catch (err) {
          toast({ variant: 'destructive', title: 'Không thể lưu', description: getErrorMessage(err) });
        }
      }}
      onTransition={async (action) => {
        if (!id || !isEdit) return;
        try {
          await transition.mutateAsync({ id, action });
          toast({ variant: 'success', title: actionLabel(action) + ' thành công' });
        } catch (err) {
          toast({ variant: 'destructive', title: 'Không thể chuyển trạng thái', description: getErrorMessage(err) });
        }
      }}
      onPrint={async () => {
        if (!id || !isEdit) return;
        try {
          await openQuotationPdfInNewTab(id);
        } catch (err) {
          toast({ variant: 'destructive', title: 'Không mở được PDF', description: getErrorMessage(err) });
        }
      }}
      onDownloadExcel={async () => {
        if (!id || !isEdit || !quotation) return;
        try {
          const blob = await quotationsApi.downloadExcel(id);
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `BaoGia_${quotation.code}.xlsx`;
          document.body.appendChild(a);
          a.click();
          a.remove();
          URL.revokeObjectURL(url);
        } catch (err) {
          toast({ variant: 'destructive', title: 'Không tải được Excel', description: getErrorMessage(err) });
        }
      }}
    />
  );
}

async function openQuotationPdfInNewTab(quotationId: string) {
  const blob = await quotationsApi.downloadPdf(quotationId);
  const url = URL.createObjectURL(blob);
  window.open(url, '_blank', 'noopener,noreferrer');
  window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
}

interface InnerProps {
  isEdit: boolean;
  initial?: Quotation;
  initialSelectedCustomer?: Customer;
  submitting: boolean;
  submitError: string;
  hasSubmitError: boolean;
  onSubmit: (parsed: QuotationFormParsed, intent: QuotationSubmitIntent) => Promise<void>;
  onTransition: (action: QuotationAction) => Promise<void>;
  onPrint: () => Promise<void>;
  onDownloadExcel: () => Promise<void>;
}

function QuotationFormInner({
  isEdit,
  initial,
  initialSelectedCustomer,
  submitting,
  submitError,
  hasSubmitError,
  onSubmit,
  onTransition,
  onPrint,
  onDownloadExcel,
}: InnerProps) {
  const navigateInner = useNavigate();
  const clone = useCloneQuotation();
  const form = useForm<QuotationFormValues, unknown, QuotationFormParsed>({
    resolver: zodResolver(quotationSchema) as unknown as Resolver<QuotationFormValues, unknown, QuotationFormParsed>,
    defaultValues: toFormDefaults(initial),
  });

  // Narrow watches so this component only re-renders when fields used in render
  // actually change. (Previously useWatch({ control }) without `name` triggered
  // a re-render of the whole form on every keystroke anywhere.)
  const watchedLines = useWatch({ control: form.control, name: 'lines' }) as
    | QuotationLineFormValues[]
    | undefined;
  const watchedTaxRate = useWatch({ control: form.control, name: 'taxRate' }) as number | undefined;
  const watchedDiscount = useWatch({ control: form.control, name: 'discount' }) as number | undefined;
  const watchedFreight = useWatch({ control: form.control, name: 'freight' }) as number | undefined;
  const status: QuotationStatus = initial?.status ?? 'Draft';

  const [selectedCustomerView, setSelectedCustomerView] = useState<{ id: string; code: string; name: string } | null>(
    () =>
      initialSelectedCustomer
        ? {
            id: initialSelectedCustomer.id,
            code: initialSelectedCustomer.code,
            name: initialSelectedCustomer.name,
          }
        : null,
  );
  const [quickAddOpen, setQuickAddOpen] = useState(false);
  const [confirmCloneOpen, setConfirmCloneOpen] = useState(false);
  const [pendingSubmitIntent, setPendingSubmitIntent] = useState<QuotationSubmitIntent | null>(null);
  const [pendingButtonAction, setPendingButtonAction] = useState<QuotationButtonAction | null>(null);
  const lineItemsGridRef = useRef<LineItemsGridHandle>(null);
  const isSubmitBusy = submitting || clone.isPending || pendingSubmitIntent != null || pendingButtonAction != null;

  const GENERAL_INFO_FIELD_ORDER = [
    'quotationDate',
    'deliveryDate',
    'customerId',
    'customerName',
    'deliveryAddress',
    'deliveryRecipient',
    'deliveryPhone',
    'deliveryNote',
    'internalNote',
  ] as const;

  function handleGeneralInfoKeyDown(e: React.KeyboardEvent<HTMLDivElement>) {
    if (e.defaultPrevented) return;
    if (e.key !== 'Enter' || e.ctrlKey || e.altKey) return;
    const target = e.target as HTMLInputElement;
    if (target.type === 'date') return;
    const currentId = target.id;
    const order = GENERAL_INFO_FIELD_ORDER;
    const idx = order.indexOf(currentId as typeof order[number]);
    if (idx === -1) return;
    e.preventDefault();
    if (!e.shiftKey && idx === order.length - 1) {
      lineItemsGridRef.current?.ensureFirstLineAndFocusProductCode();
      return;
    }
    const nextIdx = e.shiftKey ? idx - 1 : idx + 1;
    if (nextIdx < 0 || nextIdx >= order.length) return;
    document.getElementById(order[nextIdx])?.focus();
  }

  function handleFormKeyDown(e: React.KeyboardEvent<HTMLFormElement>) {
    if (e.defaultPrevented) return;
    if (e.key === 's' && e.ctrlKey && !e.shiftKey && !e.altKey) {
      e.preventDefault();
      submitWithIntent(isEdit ? 'save-exit' : 'save-stay');
    }
  }

  function submitWithIntent(intent: QuotationSubmitIntent) {
    if (isSubmitBusy) return;
    setPendingSubmitIntent(intent);
    void form.handleSubmit(
      async (parsed) => {
        try {
          await onSubmit(parsed, intent);
        } finally {
          setPendingSubmitIntent(null);
        }
      },
      () => {
        setPendingSubmitIntent(null);
      },
    )();
  }

  useEffect(() => {
    if (!initial) return;
    form.reset(toFormDefaults(initial));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initial?.id]);

  useEffect(() => {
    if (initialSelectedCustomer) {
      setSelectedCustomerView({
        id: initialSelectedCustomer.id,
        code: initialSelectedCustomer.code,
        name: initialSelectedCustomer.name,
      });
    }
  }, [initialSelectedCustomer?.id]); // eslint-disable-line react-hooks/exhaustive-deps

  function handleSelectCustomer(c: CustomerSearchItem) {
    form.setValue('customerId', c.id, { shouldDirty: true, shouldValidate: true });
    form.setValue('customerName', c.name, { shouldDirty: true });
    setSelectedCustomerView({ id: c.id, code: c.code, name: c.name });

    const cur = form.getValues();
    if (!cur.deliveryAddress?.trim()) {
      form.setValue('deliveryAddress', c.defaultShippingAddress ?? c.companyAddress ?? '', { shouldDirty: true });
    }
    if (!cur.deliveryRecipient?.trim()) {
      form.setValue('deliveryRecipient', c.contactPerson ?? '', { shouldDirty: true });
    }
    if (!cur.deliveryPhone?.trim()) {
      form.setValue('deliveryPhone', c.phoneNumber ?? '', { shouldDirty: true });
    }

    setTimeout(() => document.getElementById('customerName')?.focus(), 0);
  }

  function handleClearCustomer() {
    form.setValue('customerId', '', { shouldDirty: true, shouldValidate: true });
    form.setValue('customerName', '', { shouldDirty: true });
    setSelectedCustomerView(null);
  }

  async function doClone() {
    if (!initial) return;
    try {
      const cloned = await clone.mutateAsync(initial.id);
      toast({ variant: 'success', title: 'Đã clone báo giá', description: cloned.code });
      navigateInner(`/quotations/${cloned.id}`);
    } catch (err) {
      toast({ variant: 'destructive', title: 'Không thể clone', description: getErrorMessage(err) });
    }
  }

  function handleCloneClick() {
    if (form.formState.isDirty) {
      setConfirmCloneOpen(true);
    } else {
      runButtonAction('clone', doClone);
    }
  }

  function runButtonAction(action: QuotationButtonAction, task: () => Promise<void>) {
    if (isSubmitBusy) return;
    setPendingButtonAction(action);
    void task().finally(() => setPendingButtonAction(null));
  }

  const lineLikes: LineLike[] = (watchedLines ?? []).map(toLineLike);
  const header: HeaderLike = {
    taxRate: Number(watchedTaxRate ?? 0) || 0,
    discount: Number(watchedDiscount ?? 0) || 0,
    freight: Number(watchedFreight ?? 0) || 0,
  };

  const onHeaderChange = (patch: Partial<HeaderLike>) => {
    if (patch.discount !== undefined) form.setValue('discount', patch.discount as never, { shouldDirty: true });
    if (patch.freight !== undefined) form.setValue('freight', patch.freight as never, { shouldDirty: true });
    if (patch.taxRate !== undefined) form.setValue('taxRate', patch.taxRate as never, { shouldDirty: true });
  };

  return (
    <div className="space-y-4">
      <div className="sticky top-0 z-30 -mx-4 border-b bg-background/95 px-4 py-3 shadow-sm backdrop-blur md:-mx-3 md:px-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex min-w-0 items-center gap-2">
          <Button variant="ghost" size="icon" asChild aria-label="Quay lại">
            <Link to="/quotations"><ArrowLeft className="h-4 w-4" /></Link>
          </Button>
          <h1 className="truncate text-xl font-bold">{isEdit ? 'Chỉnh sửa báo giá' : 'Thêm báo giá'}</h1>
          {isEdit && <StatusPill status={status} />}
        </div>
          <div className="flex flex-wrap justify-end gap-2">
            <Button type="button" variant="outline" size="sm" asChild>
              <Link to="/quotations">Quay lại danh sách báo giá</Link>
            </Button>
            {isEdit ? (
              <Button
                type="button"
                size="sm"
                onClick={() => submitWithIntent('save-exit')}
                disabled={isSubmitBusy}
                aria-busy={pendingSubmitIntent === 'save-exit' || submitting}
              >
                {(pendingSubmitIntent === 'save-exit' || submitting) && <ButtonLoader className="mr-2" />}
                {pendingSubmitIntent === 'save-exit' || submitting ? 'Đang lưu...' : 'Cập nhật'}
              </Button>
            ) : (
              <>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => submitWithIntent('save-print')}
                  disabled={isSubmitBusy}
                  aria-busy={pendingSubmitIntent === 'save-print'}
                >
                  {pendingSubmitIntent === 'save-print' ? <ButtonLoader className="mr-2" /> : <Printer className="mr-2 h-4 w-4" />}
                  {pendingSubmitIntent === 'save-print' ? 'Đang xử lý...' : 'Lưu và in'}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => submitWithIntent('save-exit')}
                  disabled={isSubmitBusy}
                  aria-busy={pendingSubmitIntent === 'save-exit'}
                >
                  {pendingSubmitIntent === 'save-exit' && <ButtonLoader className="mr-2" />}
                  {pendingSubmitIntent === 'save-exit' ? 'Đang lưu...' : 'Lưu và thoát'}
                </Button>
                <Button
                  type="button"
                  size="sm"
                  onClick={() => submitWithIntent('save-stay')}
                  disabled={isSubmitBusy}
                  aria-busy={pendingSubmitIntent === 'save-stay'}
                >
                  {pendingSubmitIntent === 'save-stay' && <ButtonLoader className="mr-2" />}
                  {pendingSubmitIntent === 'save-stay' ? 'Đang lưu...' : 'Lưu tạm'}
                </Button>
              </>
            )}
            {isEdit && (
              <>
            {status === 'Draft' && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => runButtonAction('send', () => onTransition('Send'))}
                disabled={isSubmitBusy}
                aria-busy={pendingButtonAction === 'send'}
              >
                {pendingButtonAction === 'send' ? <ButtonLoader className="mr-2" /> : <Send className="mr-2 h-4 w-4" />}
                {pendingButtonAction === 'send' ? 'Đang gửi...' : 'Gửi'}
              </Button>
            )}
            {status === 'Sent' && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => runButtonAction('confirm', () => onTransition('Confirm'))}
                disabled={isSubmitBusy}
                aria-busy={pendingButtonAction === 'confirm'}
              >
                {pendingButtonAction === 'confirm' ? <ButtonLoader className="mr-2" /> : <CheckCircle2 className="mr-2 h-4 w-4" />}
                {pendingButtonAction === 'confirm' ? 'Đang xác nhận...' : 'Xác nhận'}
              </Button>
            )}
            {(status === 'Draft' || status === 'Sent' || status === 'Confirmed') && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  if (status === 'Confirmed') {
                    const ok = window.confirm(
                      'Báo giá đã xác nhận — hủy sẽ ghi nhận giảm doanh thu của sale. Cần quyền quotations.cancel_confirmed. Tiếp tục?',
                    );
                    if (!ok) return;
                  }
                  runButtonAction('cancel', () => onTransition('Cancel'));
                }}
                disabled={isSubmitBusy}
                aria-busy={pendingButtonAction === 'cancel'}
              >
                {pendingButtonAction === 'cancel' ? <ButtonLoader className="mr-2" /> : <Ban className="mr-2 h-4 w-4" />}
                {pendingButtonAction === 'cancel' ? 'Đang hủy...' : 'Hủy'}
              </Button>
            )}
            <Can permission="quotations.create">
              <Button
                variant="outline"
                size="sm"
                onClick={handleCloneClick}
                disabled={isSubmitBusy || !initial}
                aria-busy={pendingButtonAction === 'clone' || clone.isPending}
              >
                {pendingButtonAction === 'clone' || clone.isPending ? <ButtonLoader className="mr-2" /> : <Copy className="mr-2 h-4 w-4" />}
                {pendingButtonAction === 'clone' || clone.isPending ? 'Đang clone...' : 'Clone'}
              </Button>
            </Can>
            <Button
              variant="outline"
              size="sm"
              onClick={() => runButtonAction('excel', onDownloadExcel)}
              disabled={isSubmitBusy}
              aria-busy={pendingButtonAction === 'excel'}
            >
              {pendingButtonAction === 'excel' ? <ButtonLoader className="mr-2" /> : <FileSpreadsheet className="mr-2 h-4 w-4" />}
              {pendingButtonAction === 'excel' ? 'Đang xuất...' : 'Excel'}
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => runButtonAction('print', onPrint)}
              disabled={isSubmitBusy}
              aria-busy={pendingButtonAction === 'print'}
            >
              {pendingButtonAction === 'print' ? <ButtonLoader className="mr-2" /> : <Printer className="mr-2 h-4 w-4" />}
              {pendingButtonAction === 'print' ? 'Đang mở PDF...' : 'In'}
            </Button>
              </>
            )}
          </div>
        </div>
      </div>

      {isEdit && initial && !initial.canEdit && (
        <div className="rounded-md border border-amber-300 bg-amber-50 p-3 text-sm text-amber-900">
          {initial.isOwnerDeleted
            ? 'Chủ sở hữu báo giá đã ngừng hoạt động — chỉ có thể clone.'
            : initial.status === 'Cancelled'
            ? 'Báo giá đã huỷ — không thể chỉnh sửa.'
            : `Báo giá đang ở trạng thái ${initial.status} — cấu hình khoá của bạn không cho phép sửa.`}
          {initial.canClone && (
            <Button
              className="ml-3"
              size="sm"
              variant="outline"
              onClick={() => {
                clone.mutate(initial.id, {
                  onSuccess: (cloned) => {
                    toast({ variant: 'success', title: 'Đã clone báo giá', description: cloned.code });
                    navigateInner(`/quotations/${cloned.id}`);
                  },
                  onError: (err) => toast({ variant: 'destructive', title: 'Clone thất bại', description: getErrorMessage(err) }),
                });
              }}
              disabled={clone.isPending}
            >
              {clone.isPending && <ButtonLoader className="mr-2" />}
              {clone.isPending ? 'Đang clone...' : 'Clone'}
            </Button>
          )}
        </div>
      )}

      {/* eslint-disable-next-line jsx-a11y/no-noninteractive-element-interactions -- Form-level Ctrl+S shortcut delegates to the existing submit handler. */}
      <form
        onSubmit={(e) => {
          e.preventDefault();
          submitWithIntent(isEdit ? 'save-exit' : 'save-stay');
        }}
        onKeyDown={handleFormKeyDown}
        className="space-y-4"
      >
        <div className="grid gap-4 lg:grid-cols-[1fr_320px] items-stretch">
          <Card>
            <CardHeader><CardTitle>Thông tin chung</CardTitle></CardHeader>
            <CardContent className="space-y-3" onKeyDown={handleGeneralInfoKeyDown}>
              <div className="form-inline-grid form-cols-2">
                <Label htmlFor="quotationDate" className="field-label required">Ngày báo giá</Label>
                <Input
                  id="quotationDate"
                  type="date"
                  {...form.register('quotationDate')}
                  className="max-w-[200px]"
                />
                <Label htmlFor="deliveryDate" className="field-label">Ngày giao</Label>
                <Input
                  id="deliveryDate"
                  type="date"
                  {...form.register('deliveryDate')}
                  className="max-w-[200px]"
                />
                {form.formState.errors.quotationDate && (
                  <p className="field-message field-message-code text-destructive">
                    {String(form.formState.errors.quotationDate.message)}
                  </p>
                )}
              </div>

              <div className="form-inline-grid form-cols-customer">
                <Label htmlFor="customerId" className="field-label required">Mã KH</Label>
                <CustomerAutocomplete
                  inputId="customerId"
                  inputAriaLabel="Mã khách hàng"
                  placeholder="Nhập mã / tên / MST / địa chỉ / SĐT..."
                  value={selectedCustomerView}
                  errorMessage={
                    form.formState.errors.customerId
                      ? String(form.formState.errors.customerId.message)
                      : undefined
                  }
                  onSelect={handleSelectCustomer}
                  onClear={handleClearCustomer}
                  onAddNewClick={() => setQuickAddOpen(true)}
                />
                <Label htmlFor="customerName" className="field-label">Tên KH</Label>
                <Input
                  id="customerName"
                  {...form.register('customerName', {
                    onBlur: (e) => {
                      if (!e.target.value.trim() && selectedCustomerView) {
                        form.setValue('customerName', selectedCustomerView.name, { shouldDirty: true });
                      }
                    },
                  })}
                />
                {isEdit && initialSelectedCustomer?.status === 'Inactive' && (
                  <p className="field-message field-message-code text-amber-600">Khách hàng đã ngừng sử dụng</p>
                )}
              </div>

              <div className="form-inline-grid">
                <Label htmlFor="deliveryAddress" className="field-label">Địa chỉ giao</Label>
                <Input id="deliveryAddress" {...form.register('deliveryAddress')} />
              </div>

              <div className="form-inline-grid form-cols-2">
                <Label htmlFor="deliveryRecipient" className="field-label">Người nhận</Label>
                <Input id="deliveryRecipient" {...form.register('deliveryRecipient')} />
                <Label htmlFor="deliveryPhone" className="field-label">Điện thoại</Label>
                <Input id="deliveryPhone" {...form.register('deliveryPhone')} />
              </div>

              <div className="form-inline-grid form-cols-2">
                <Label htmlFor="deliveryNote" className="field-label">Ghi chú GH</Label>
                <Input id="deliveryNote" {...form.register('deliveryNote')} />
                <Label htmlFor="internalNote" className="field-label">Ghi chú NB</Label>
                <Input id="internalNote" {...form.register('internalNote')} />
              </div>
            </CardContent>
          </Card>

          <TotalsPanel lines={lineLikes} header={header} onHeaderChange={onHeaderChange} />
        </div>

          <Card>
            <CardContent className="p-0">
              <LineItemsGrid ref={lineItemsGridRef} form={form} />
              {form.formState.errors.lines && (
                <p className="px-6 pb-4 pt-2 text-sm text-destructive">
                  {String((form.formState.errors.lines as { message?: string }).message ?? 'Báo giá chưa hợp lệ.')}
                </p>
              )}
            </CardContent>
          </Card>

          {hasSubmitError && (
            <div className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
              {submitError}
            </div>
          )}

      </form>

      <CustomerQuickAddDialog
        open={quickAddOpen}
        onOpenChange={setQuickAddOpen}
        onCreated={(c) =>
          handleSelectCustomer({
            id: c.id,
            code: c.code,
            name: c.name,
            taxCode: c.taxCode,
            companyAddress: c.companyAddress,
            defaultShippingAddress: c.defaultShippingAddress,
            contactPerson: c.contactPerson,
            phoneNumber: c.phoneNumber,
            status: c.status,
          })
        }
      />

      <ConfirmDialog
        open={confirmCloneOpen}
        onOpenChange={setConfirmCloneOpen}
        title="Có thay đổi chưa lưu"
        description="Bạn có thay đổi chưa lưu — bản clone sẽ không bao gồm các thay đổi này. Tiếp tục?"
        confirmLabel="Clone"
        loading={clone.isPending}
        onConfirm={() => {
          setConfirmCloneOpen(false);
          void doClone();
        }}
      />
    </div>
  );
}

function toFormDefaults(q?: Quotation): QuotationFormValues {
  const today = new Date();
  const tomorrow = new Date(today);
  tomorrow.setDate(today.getDate() + 1);
  const defaultQuotationDate = today.toISOString().slice(0, 10);
  const defaultDeliveryDate = tomorrow.toISOString().slice(0, 10);
  return {
    customerId: q?.customerId ?? '',
    customerName: q?.customerName ?? '',
    quotationDate: q?.quotationDate ?? defaultQuotationDate,
    deliveryAddress: q?.deliveryAddress ?? '',
    deliveryRecipient: q?.deliveryRecipient ?? '',
    deliveryPhone: q?.deliveryPhone ?? '',
    deliveryDate: q?.deliveryDate ?? (q ? '' : defaultDeliveryDate),
    deliveryNote: q?.deliveryNote ?? '',
    taxRate: (q?.taxRate ?? 0) as number,
    discount: (q?.discount ?? 0) as number,
    freight: (q?.freight ?? 0) as number,
    internalNote: q?.internalNote ?? '',
    lines: (q?.lines ?? []).map((l, idx) => ({
      _uiKey: l.id ?? crypto.randomUUID(),
      id: l.id,
      sortOrder: l.sortOrder ?? idx,
      productId: l.productId,
      productCode: l.productCode ?? '',
      productName: l.productName,
      specification: l.specification ?? '',
      unitName: l.unitName,
      pricingMode: l.pricingMode,
      length: l.length ?? '',
      width: l.width ?? '',
      thickness: l.thickness ?? '',
      density: l.density ?? '',
      sheetCount: l.sheetCount ?? '',
      quantity: l.quantity,
      unitPrice: l.unitPrice,
      lineTotal: l.lineTotal,
      unitCost: l.unitCost ?? '',
      lineCost: l.lineCost ?? '',
      lineProfit: l.lineProfit ?? '',
      note: l.note ?? '',
    })) as QuotationLineFormValues[],
  };
}

function toLineLike(line: QuotationLineFormValues): LineLike {
  return {
    pricingMode: line.pricingMode,
    length: toNum(line.length),
    width: toNum(line.width),
    thickness: toNum(line.thickness),
    density: toNum(line.density),
    sheetCount: toNum(line.sheetCount),
    quantity: toNum(line.quantity) ?? 0,
    unitPrice: toNum(line.unitPrice) ?? 0,
    unitCost: toNum(line.unitCost),
  };
}

function toNum(v: unknown): number | undefined {
  if (v === undefined || v === null || v === '') return undefined;
  const n = typeof v === 'number' ? v : Number(v);
  return Number.isFinite(n) ? n : undefined;
}

function toPayload(parsed: QuotationFormParsed): UpsertQuotationRequest {
  return {
    customerId: parsed.customerId,
    customerName: parsed.customerName?.trim() || undefined,
    quotationDate: parsed.quotationDate,
    deliveryAddress: parsed.deliveryAddress,
    deliveryRecipient: parsed.deliveryRecipient,
    deliveryPhone: parsed.deliveryPhone,
    deliveryDate: parsed.deliveryDate,
    deliveryNote: parsed.deliveryNote,
    taxRate: parsed.taxRate,
    discount: parsed.discount,
    freight: parsed.freight,
    internalNote: parsed.internalNote,
    lines: parsed.lines.map<UpsertQuotationLineRequest>((l, idx) => ({
      id: l.id,
      sortOrder: l.sortOrder ?? idx,
      productId: l.productId,
      productCode: l.productCode,
      productName: l.productName,
      specification: l.specification,
      unitName: l.unitName,
      pricingMode: l.pricingMode,
      length: l.length,
      width: l.width,
      thickness: l.thickness,
      density: l.density,
      sheetCount: l.sheetCount,
      quantity: l.quantity,
      unitPrice: l.unitPrice,
      unitCost: l.unitCost,
      note: l.note,
    })),
  };
}

function actionLabel(action: QuotationAction) {
  switch (action) {
    case 'Send': return 'Gửi báo giá';
    case 'Confirm': return 'Xác nhận';
    case 'Cancel': return 'Hủy';
  }
}
