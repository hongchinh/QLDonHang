interface Props {
  canShow: boolean
  onInstall(): void
  onDismiss(): void
}

export function InstallPrompt({ canShow, onInstall, onDismiss }: Props) {
  if (!canShow) return null
  return (
    <div className="fixed top-0 inset-x-0 z-50 flex items-center justify-between gap-4 bg-blue-800 px-4 py-3 text-white shadow-md">
      <span className="text-sm font-medium">Cài đặt QL Đơn Hàng để truy cập nhanh hơn</span>
      <div className="flex shrink-0 gap-2">
        <button
          onClick={onInstall}
          className="rounded bg-white px-3 py-1 text-sm font-semibold text-blue-800 hover:bg-blue-50"
        >
          Cài đặt
        </button>
        <button
          onClick={onDismiss}
          className="rounded px-3 py-1 text-sm text-blue-200 hover:text-white"
        >
          Không, cảm ơn
        </button>
      </div>
    </div>
  )
}
