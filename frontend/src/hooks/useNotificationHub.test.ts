import { renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

// Shared mocks — declared before vi.mock calls (hoisted by Vitest)
const mockOn = vi.fn()
const mockStart = vi.fn().mockResolvedValue(undefined)
const mockStop = vi.fn().mockResolvedValue(undefined)
const mockWithUrl = vi.fn().mockReturnThis()
const mockWithAutomaticReconnect = vi.fn().mockReturnThis()
const mockBuild = vi.fn().mockReturnValue({ on: mockOn, start: mockStart, stop: mockStop })
const invalidateQueries = vi.fn()

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn(function () {
    return {
      withUrl: mockWithUrl,
      withAutomaticReconnect: mockWithAutomaticReconnect,
      build: mockBuild,
    }
  }),
}))

vi.mock('@/stores/auth-store', () => ({
  useAuthStore: vi.fn((selector: (s: { accessToken: string | null }) => unknown) =>
    selector({ accessToken: 'test-token' })
  ),
}))

vi.mock('@tanstack/react-query', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@tanstack/react-query')>()
  return {
    ...actual,
    useQueryClient: vi.fn(() => ({ invalidateQueries })),
  }
})

import { useNotificationHub } from './useNotificationHub'

describe('useNotificationHub', () => {
  beforeEach(() => vi.clearAllMocks())
  afterEach(() => vi.restoreAllMocks())

  it('connects to /hubs/notifications on mount', () => {
    renderHook(() => useNotificationHub())
    expect(mockWithUrl).toHaveBeenCalledWith(
      '/hubs/notifications',
      expect.objectContaining({ accessTokenFactory: expect.any(Function) })
    )
    expect(mockStart).toHaveBeenCalled()
  })

  it('registers NewNotification handler', () => {
    renderHook(() => useNotificationHub())
    expect(mockOn).toHaveBeenCalledWith('NewNotification', expect.any(Function))
  })

  it('stops connection on unmount', () => {
    const { unmount } = renderHook(() => useNotificationHub())
    unmount()
    expect(mockStop).toHaveBeenCalled()
  })

  it('invalidates unread-count query on NewNotification', () => {
    renderHook(() => useNotificationHub())

    // Get the handler registered with 'NewNotification' and call it
    const handler = mockOn.mock.calls.find(([event]: [string]) => event === 'NewNotification')?.[1] as (() => void) | undefined
    handler?.()

    expect(invalidateQueries).toHaveBeenCalledWith({
      queryKey: ['notifications', 'unread-count'],
    })
  })
})
