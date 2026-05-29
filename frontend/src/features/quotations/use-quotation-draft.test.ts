import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { useForm } from 'react-hook-form';
import {
  readQuotationDraft,
  writeQuotationDraft,
  deleteQuotationDraft,
  useQuotationDraft,
} from './use-quotation-draft';
import type { QuotationFormValues } from './schema';

const MINIMAL_VALUES: QuotationFormValues = {
  customerId: 'cust-uuid',
  customerName: 'Test KH',
  quotationDate: '2026-05-29',
  deliveryAddress: '',
  deliveryRecipient: '',
  deliveryPhone: '',
  transportVehicleNumber: '',
  deliveryDate: '2026-05-30',
  deliveryNote: '',
  taxRate: 0,
  discount: 0,
  freight: 0,
  advancePayment: 0,
  internalNote: '',
  lines: [
    {
      _uiKey: 'key1',
      sortOrder: 0,
      productName: 'Sản phẩm A',
      unitName: 'Cái',
      pricingMode: 'PerUnit',
      quantity: 1,
      unitPrice: 100000,
    },
  ],
};

// ── Pure function tests ──────────────────────────────────────────────────────

describe('readQuotationDraft', () => {
  beforeEach(() => localStorage.clear());

  it('returns null when no draft exists', () => {
    expect(readQuotationDraft('user1')).toBeNull();
  });

  it('returns null for empty userId', () => {
    expect(readQuotationDraft('')).toBeNull();
  });

  it('returns null when JSON is malformed', () => {
    localStorage.setItem('quotation_draft_user1', '{invalid json');
    expect(readQuotationDraft('user1')).toBeNull();
  });

  it('returns null when savedAt is missing', () => {
    localStorage.setItem('quotation_draft_user1', JSON.stringify({ values: MINIMAL_VALUES }));
    expect(readQuotationDraft('user1')).toBeNull();
  });

  it('returns null when values is missing', () => {
    localStorage.setItem('quotation_draft_user1', JSON.stringify({ savedAt: new Date().toISOString() }));
    expect(readQuotationDraft('user1')).toBeNull();
  });

  it('returns parsed draft when valid data exists', () => {
    const savedAt = new Date().toISOString();
    localStorage.setItem(
      'quotation_draft_user1',
      JSON.stringify({ savedAt, values: MINIMAL_VALUES, selectedCustomer: null }),
    );
    const result = readQuotationDraft('user1');
    expect(result).not.toBeNull();
    expect(result?.savedAt).toBe(savedAt);
    expect(result?.values.customerId).toBe('cust-uuid');
    expect(result?.selectedCustomer).toBeNull();
  });

  it('returns draft including selectedCustomer when present', () => {
    const savedAt = new Date().toISOString();
    const customer = { id: 'cust-uuid', code: 'KH001', name: 'Test KH' };
    localStorage.setItem(
      'quotation_draft_user1',
      JSON.stringify({ savedAt, values: MINIMAL_VALUES, selectedCustomer: customer }),
    );
    expect(readQuotationDraft('user1')?.selectedCustomer).toEqual(customer);
  });
});

describe('writeQuotationDraft', () => {
  beforeEach(() => localStorage.clear());

  it('writes JSON with savedAt, values, selectedCustomer to localStorage', () => {
    writeQuotationDraft('user1', MINIMAL_VALUES, null);
    const raw = localStorage.getItem('quotation_draft_user1');
    expect(raw).not.toBeNull();
    const parsed = JSON.parse(raw!);
    expect(parsed.values.customerId).toBe('cust-uuid');
    expect(parsed.selectedCustomer).toBeNull();
    expect(typeof parsed.savedAt).toBe('string');
  });

  it('overwrites existing draft on second write', () => {
    writeQuotationDraft('user1', MINIMAL_VALUES, null);
    writeQuotationDraft('user1', { ...MINIMAL_VALUES, customerName: 'Updated' }, null);
    const parsed = JSON.parse(localStorage.getItem('quotation_draft_user1')!);
    expect(parsed.values.customerName).toBe('Updated');
  });

  it('uses user-scoped key (different users have separate drafts)', () => {
    writeQuotationDraft('user1', MINIMAL_VALUES, null);
    expect(localStorage.getItem('quotation_draft_user2')).toBeNull();
  });
});

describe('deleteQuotationDraft', () => {
  beforeEach(() => localStorage.clear());

  it('removes draft from localStorage', () => {
    writeQuotationDraft('user1', MINIMAL_VALUES, null);
    deleteQuotationDraft('user1');
    expect(localStorage.getItem('quotation_draft_user1')).toBeNull();
  });

  it('is a no-op when no draft exists', () => {
    expect(() => deleteQuotationDraft('user1')).not.toThrow();
  });
});

// ── Hook tests ───────────────────────────────────────────────────────────────

describe('useQuotationDraft', () => {
  beforeEach(() => {
    localStorage.clear();
    // shouldAdvanceTime lets waitFor's internal polling timers fire via real wall-clock time
    // while still allowing vi.advanceTimersByTime() to control our debounce timer.
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  function renderDraftHook(opts?: {
    isEdit?: boolean;
    initialHasDraft?: boolean;
    initialSavedAt?: Date | null;
  }) {
    return renderHook(() => {
      const form = useForm<QuotationFormValues>({ defaultValues: MINIMAL_VALUES });
      const draft = useQuotationDraft({
        form,
        userId: 'user1',
        isEdit: opts?.isEdit ?? false,
        getSelectedCustomer: () => null,
        initialHasDraft: opts?.initialHasDraft ?? false,
        initialSavedAt: opts?.initialSavedAt ?? null,
      });
      return { form, draft };
    });
  }

  it('initializes hasDraft=false when initialHasDraft=false', () => {
    const { result } = renderDraftHook();
    expect(result.current.draft.hasDraft).toBe(false);
    expect(result.current.draft.draftSavedAt).toBeNull();
  });

  it('initializes hasDraft=true and draftSavedAt when initialHasDraft=true', () => {
    const savedAt = new Date();
    const { result } = renderDraftHook({ initialHasDraft: true, initialSavedAt: savedAt });
    expect(result.current.draft.hasDraft).toBe(true);
    expect(result.current.draft.draftSavedAt).toEqual(savedAt);
  });

  it('clearDraft removes from localStorage and resets state', () => {
    writeQuotationDraft('user1', MINIMAL_VALUES, null);
    const { result } = renderDraftHook({ initialHasDraft: true, initialSavedAt: new Date() });

    act(() => { result.current.draft.clearDraft(); });

    expect(result.current.draft.hasDraft).toBe(false);
    expect(result.current.draft.draftSavedAt).toBeNull();
    expect(localStorage.getItem('quotation_draft_user1')).toBeNull();
  });

  it('does not write to localStorage when isEdit=true', () => {
    const { result } = renderDraftHook({ isEdit: true });

    act(() => {
      result.current.form.setValue('customerName', 'Changed', { shouldDirty: true });
    });
    act(() => { vi.advanceTimersByTime(2000); });

    expect(localStorage.getItem('quotation_draft_user1')).toBeNull();
  });

  it('does not write before debounce expires', () => {
    const { result } = renderDraftHook();

    act(() => {
      result.current.form.setValue('customerName', 'New Name', { shouldDirty: true });
    });
    act(() => { vi.advanceTimersByTime(1000); }); // < 1500ms threshold

    expect(localStorage.getItem('quotation_draft_user1')).toBeNull();
  });

  it('writes to localStorage and updates hasDraft after debounce', async () => {
    const { result } = renderDraftHook();

    act(() => {
      result.current.form.setValue('customerName', 'New Name', { shouldDirty: true });
    });
    act(() => { vi.advanceTimersByTime(1600); }); // > 1500ms debounce

    await waitFor(() => {
      expect(localStorage.getItem('quotation_draft_user1')).not.toBeNull();
    });
    expect(result.current.draft.hasDraft).toBe(true);
    expect(result.current.draft.draftSavedAt).not.toBeNull();
  });

  it('debounces rapid changes — only writes once after the final change', async () => {
    const { result } = renderDraftHook();

    act(() => { result.current.form.setValue('customerName', 'A', { shouldDirty: true }); });
    act(() => { vi.advanceTimersByTime(800); }); // timer reset by next change
    act(() => { result.current.form.setValue('customerName', 'AB', { shouldDirty: true }); });
    act(() => { vi.advanceTimersByTime(800); }); // timer reset again

    // 800ms since last change — not yet written
    expect(localStorage.getItem('quotation_draft_user1')).toBeNull();

    act(() => { vi.advanceTimersByTime(800); }); // 1600ms since last change

    await waitFor(() => {
      expect(localStorage.getItem('quotation_draft_user1')).not.toBeNull();
    });
  });

  it('does not write after form.reset() because isDirty becomes false', async () => {
    const { result } = renderDraftHook();

    // User types something (isDirty=true)
    act(() => { result.current.form.setValue('customerName', 'X', { shouldDirty: true }); });
    // Then reset simulates "Xóa nháp" (isDirty=false)
    act(() => { result.current.form.reset(MINIMAL_VALUES); });
    act(() => { vi.advanceTimersByTime(1600); });

    expect(localStorage.getItem('quotation_draft_user1')).toBeNull();
  });
});
