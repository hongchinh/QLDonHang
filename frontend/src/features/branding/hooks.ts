import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { brandingApi, type BrandingMeta } from './api';

const PLACEHOLDER: BrandingMeta = {
  hasLogoFull: false,
  hasLogoMark: false,
  updatedAt: '',
};

export const brandingKeys = {
  meta: ['branding-meta'] as const,
};

export function useBrandingMeta() {
  return useQuery({
    queryKey: brandingKeys.meta,
    queryFn: () => brandingApi.getMeta(),
    staleTime: 5 * 60_000,
    placeholderData: PLACEHOLDER,
  });
}

export function useUploadBranding() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ logoFull, logoMark }: { logoFull?: File; logoMark?: File }) =>
      brandingApi.upload(logoFull, logoMark),
    onSuccess: () => qc.invalidateQueries({ queryKey: brandingKeys.meta }),
  });
}
