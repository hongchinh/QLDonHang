import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notificationsApi } from './api';

export const notificationsKeys = {
  list: (unreadOnly: boolean) => ['notifications', 'list', unreadOnly] as const,
  unreadCount: ['notifications', 'unread-count'] as const,
};

export function useUnreadCount() {
  return useQuery({
    queryKey: notificationsKeys.unreadCount,
    queryFn: () => notificationsApi.unreadCount(),
    refetchInterval: 60_000,
    refetchIntervalInBackground: false,
    staleTime: 0,
    refetchOnWindowFocus: true,
  });
}

export function useNotifications(unreadOnly: boolean, enabled: boolean) {
  return useQuery({
    queryKey: notificationsKeys.list(unreadOnly),
    queryFn: () => notificationsApi.list(unreadOnly),
    enabled,
    staleTime: 30_000,
  });
}

export function useMarkRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['notifications'] });
    },
  });
}

export function useMarkAllRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => notificationsApi.markAllRead(),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['notifications'] });
    },
  });
}
