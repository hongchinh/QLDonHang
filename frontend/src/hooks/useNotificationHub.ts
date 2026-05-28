import { useEffect } from 'react'
import { HubConnectionBuilder } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@/stores/auth-store'

export function useNotificationHub() {
  const queryClient = useQueryClient()
  const accessToken = useAuthStore((s) => s.accessToken)

  useEffect(() => {
    if (!accessToken) return

    const apiBase = import.meta.env.VITE_API_BASE_URL ?? import.meta.env.VITE_API_BASE ?? ''
    const hubUrl = apiBase
      ? apiBase.replace(/\/api$/, '') + '/hubs/notifications'
      : '/hubs/notifications'

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => useAuthStore.getState().accessToken ?? '',
      })
      .withAutomaticReconnect()
      .build()

    connection.on('NewNotification', () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
    })

    connection.start().catch((err) => {
      console.error('SignalR connection failed:', err)
    })

    return () => {
      connection.stop()
    }
  }, [accessToken, queryClient])
}
