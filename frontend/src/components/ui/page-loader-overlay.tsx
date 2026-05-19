import './page-loader-overlay.css';

interface PageLoaderOverlayProps {
  open: boolean;
  title?: string;
  description?: string;
}

export function PageLoaderOverlay({
  open,
  title = 'Đang xử lý dữ liệu...',
  description,
}: PageLoaderOverlayProps) {
  if (!open) return null;

  return (
    <div className="page-loader-overlay" role="status" aria-live="polite" aria-busy="true">
      <div className="page-loader-panel">
        <span className="page-loader-spinner" aria-hidden="true" />
        <div className="page-loader-copy">
          <div className="page-loader-title">{title}</div>
          {description && <div className="page-loader-description">{description}</div>}
        </div>
      </div>
    </div>
  );
}
