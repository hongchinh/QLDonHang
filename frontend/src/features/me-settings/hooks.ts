import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { meSettingsApi, type HandoverTemplateType } from './api';
import { meSettingsKeys } from './keys';

export function useMySettings() {
  return useQuery({
    queryKey: meSettingsKeys.all,
    queryFn: () => meSettingsApi.getMine(),
  });
}

export function useUploadTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => meSettingsApi.uploadTemplate(file),
    onSuccess: () => qc.invalidateQueries({ queryKey: meSettingsKeys.all }),
  });
}

export function useDeleteTemplate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => meSettingsApi.deleteTemplate(),
    onSuccess: () => qc.invalidateQueries({ queryKey: meSettingsKeys.all }),
  });
}

export function useUploadHandoverTemplate(type: HandoverTemplateType) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => meSettingsApi.uploadHandoverTemplate(file, type),
    onSuccess: () => qc.invalidateQueries({ queryKey: meSettingsKeys.all }),
  });
}

export function useDeleteHandoverTemplate(type: HandoverTemplateType) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => meSettingsApi.deleteHandoverTemplate(type),
    onSuccess: () => qc.invalidateQueries({ queryKey: meSettingsKeys.all }),
  });
}
