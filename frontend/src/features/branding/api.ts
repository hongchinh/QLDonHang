import api, { apiGet, type ApiResponse } from '@/lib/api-client';

export interface BrandingMeta {
  hasLogoFull: boolean;
  hasLogoMark: boolean;
  updatedAt: string;
}

export const brandingApi = {
  getMeta: () => apiGet<BrandingMeta>('/settings/branding'),

  upload: async (logoFull?: File, logoMark?: File): Promise<BrandingMeta> => {
    const form = new FormData();
    if (logoFull) form.append('logoFull', logoFull);
    if (logoMark) form.append('logoMark', logoMark);
    const res = await api.put<ApiResponse<BrandingMeta>>('/settings/branding', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    if (!res.data.success || !res.data.data) {
      throw new Error(res.data.error?.message ?? 'Upload thất bại');
    }
    return res.data.data;
  },
};

// URL helper: used as <img src>. `v` is cache-bust based on updatedAt.
export function logoUrl(variant: 'full' | 'mark', version: string): string {
  const base = import.meta.env.VITE_API_BASE_URL ?? import.meta.env.VITE_API_BASE ?? '/api';
  const v = encodeURIComponent(version);
  return `${base}/settings/branding/logo?variant=${variant}&v=${v}`;
}
