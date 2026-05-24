import { apiDelete, apiPost } from '@/lib/api-client'

export const pushApi = {
  subscribe: (endpoint: string, p256dh: string, auth: string) =>
    apiPost<void>('/push/subscribe', { endpoint, p256dh, auth }),

  unsubscribe: (endpoint: string) =>
    apiDelete<void>('/push/subscribe', { data: { endpoint } }),
}
