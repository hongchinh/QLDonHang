import { apiGet, apiPost } from '@/lib/api-client';

export interface NotificationItem {
  id: string;
  type: string;
  title: string;
  body?: string | null;
  link?: string | null;
  isRead: boolean;
  createdAt: string;
}

export const notificationsApi = {
  list: (unreadOnly = false, limit = 10) =>
    apiGet<NotificationItem[]>('/notifications', { unreadOnly, limit }),
  unreadCount: () => apiGet<number>('/notifications/unread-count'),
  markRead: (id: string) => apiPost<void>(`/notifications/${id}/read`),
  markAllRead: () => apiPost<void>('/notifications/mark-all-read'),
};
