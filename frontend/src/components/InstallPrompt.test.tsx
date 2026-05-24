import { render, screen, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { InstallPrompt } from './InstallPrompt'

describe('InstallPrompt', () => {
  it('không render khi canShow = false', () => {
    const { container } = render(
      <InstallPrompt canShow={false} onInstall={vi.fn()} onDismiss={vi.fn()} />
    )
    expect(container.firstChild).toBeNull()
  })

  it('render banner khi canShow = true', () => {
    render(<InstallPrompt canShow={true} onInstall={vi.fn()} onDismiss={vi.fn()} />)
    expect(screen.getByRole('button', { name: /cài đặt/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /không/i })).toBeInTheDocument()
  })

  it('gọi onInstall khi click Cài đặt', () => {
    const onInstall = vi.fn()
    render(<InstallPrompt canShow={true} onInstall={onInstall} onDismiss={vi.fn()} />)
    fireEvent.click(screen.getByRole('button', { name: /cài đặt/i }))
    expect(onInstall).toHaveBeenCalledOnce()
  })

  it('gọi onDismiss khi click Không', () => {
    const onDismiss = vi.fn()
    render(<InstallPrompt canShow={true} onInstall={vi.fn()} onDismiss={onDismiss} />)
    fireEvent.click(screen.getByRole('button', { name: /không/i }))
    expect(onDismiss).toHaveBeenCalledOnce()
  })
})
