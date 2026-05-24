import { useEffect } from 'react'
import { HubConnectionBuilder } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@/stores/auth-store'
import { notificationsKeys } from '@/features/notifications/hooks'

export function useNotificationHub() {
  const queryClient = useQueryClient()
  const accessToken = useAuthStore((s) => s.accessToken)

  useEffect(() => {
    if (!accessToken) return

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => useAuthStore.getState().accessToken ?? '',
      })
      .withAutomaticReconnect()
      .build()

    connection.on('NewNotification', () => {
      queryClient.invalidateQueries({ queryKey: notificationsKeys.unreadCount })
    })

    connection.start().catch((err) => {
      console.error('SignalR connection failed:', err)
    })

    return () => {
      connection.stop()
    }
  }, [accessToken, queryClient])
}
