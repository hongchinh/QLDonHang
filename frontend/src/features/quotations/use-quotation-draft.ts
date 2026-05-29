import { useCallback, useEffect, useRef, useState } from 'react';
import type { UseFormReturn } from 'react-hook-form';
import type { QuotationFormValues } from './schema';

export interface QuotationDraftCustomer {
  id: string;
  code: string;
  name: string;
}

export interface QuotationDraftStorage {
  savedAt: string;
  values: QuotationFormValues;
  selectedCustomer: QuotationDraftCustomer | null;
}

const DRAFT_KEY_PREFIX = 'quotation_draft_';
const DEBOUNCE_MS = 1500;

function draftKey(userId: string): string {
  return `${DRAFT_KEY_PREFIX}${userId}`;
}

export function readQuotationDraft(userId: string): QuotationDraftStorage | null {
  if (!userId) return null;
  try {
    const raw = localStorage.getItem(draftKey(userId));
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<QuotationDraftStorage>;
    if (!parsed.values || !parsed.savedAt) return null;
    return parsed as QuotationDraftStorage;
  } catch {
    return null;
  }
}

export function writeQuotationDraft(
  userId: string,
  values: QuotationFormValues,
  selectedCustomer: QuotationDraftCustomer | null,
): void {
  try {
    const storage: QuotationDraftStorage = {
      savedAt: new Date().toISOString(),
      values,
      selectedCustomer,
    };
    localStorage.setItem(draftKey(userId), JSON.stringify(storage));
  } catch {
    // ignore QuotaExceededError
  }
}

export function deleteQuotationDraft(userId: string): void {
  localStorage.removeItem(draftKey(userId));
}

export interface UseQuotationDraftOptions {
  form: UseFormReturn<QuotationFormValues>;
  userId: string;
  isEdit: boolean;
  getSelectedCustomer: () => QuotationDraftCustomer | null;
  initialHasDraft: boolean;
  initialSavedAt: Date | null;
}

export interface UseQuotationDraftResult {
  hasDraft: boolean;
  draftSavedAt: Date | null;
  clearDraft: () => void;
}

export function useQuotationDraft({
  form,
  userId,
  isEdit,
  getSelectedCustomer,
  initialHasDraft,
  initialSavedAt,
}: UseQuotationDraftOptions): UseQuotationDraftResult {
  const [hasDraft, setHasDraft] = useState(initialHasDraft);
  const [draftSavedAt, setDraftSavedAt] = useState<Date | null>(initialSavedAt);

  const userIdRef = useRef(userId);
  // Updated every render so the timeout closure always captures the latest customer
  // without re-creating the subscription (stable ref pattern).
  const getSelectedCustomerRef = useRef(getSelectedCustomer);
  getSelectedCustomerRef.current = getSelectedCustomer;
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  // RHF's formState Proxy only tracks properties accessed during render.
  // Read isDirty here so RHF subscribes to it; then mirror into a ref so
  // the watch closure always sees the latest value without needing re-renders.
  const isDirtyRef = useRef(false);
  isDirtyRef.current = form.formState.isDirty;

  useEffect(() => {
    if (isEdit) return;

    const subscription = form.watch((values) => {
      if (debounceTimerRef.current) clearTimeout(debounceTimerRef.current);
      debounceTimerRef.current = setTimeout(() => {
        // isDirty is false after form.reset() (e.g. "Xóa nháp") — skip write to
        // prevent the reset triggering a new draft with default/empty values.
        if (!isDirtyRef.current) return;
        writeQuotationDraft(
          userIdRef.current,
          // form.watch callback typing is Partial<DeepPartial<T>>, but the form
          // is always fully initialized before the user can trigger a change.
          values as QuotationFormValues,
          getSelectedCustomerRef.current(),
        );
        setHasDraft(true);
        setDraftSavedAt(new Date());
      }, DEBOUNCE_MS);
    });

    return () => {
      subscription.unsubscribe();
      if (debounceTimerRef.current) clearTimeout(debounceTimerRef.current);
    };
  }, [form, isEdit]);

  const clearDraft = useCallback(() => {
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
      debounceTimerRef.current = null;
    }
    deleteQuotationDraft(userIdRef.current);
    setHasDraft(false);
    setDraftSavedAt(null);
  }, []);

  return { hasDraft, draftSavedAt, clearDraft };
}
