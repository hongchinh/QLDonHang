import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';

export function NotFoundPage() {
  return (
    <div className="flex h-[60vh] flex-col items-center justify-center gap-3">
      <h1 className="text-4xl font-bold">404</h1>
      <p className="text-muted-foreground">Không tìm thấy trang.</p>
      <Button asChild>
        <Link to="/">Về trang chủ</Link>
      </Button>
    </div>
  );
}

export function ForbiddenPage() {
  return (
    <div className="flex h-[60vh] flex-col items-center justify-center gap-3">
      <h1 className="text-4xl font-bold">403</h1>
      <p className="text-muted-foreground">Bạn không có quyền truy cập chức năng này.</p>
      <Button asChild variant="outline">
        <Link to="/">Quay lại</Link>
      </Button>
    </div>
  );
}
