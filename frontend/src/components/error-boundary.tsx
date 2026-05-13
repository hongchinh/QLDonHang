import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Button } from '@/components/ui/button';

interface Props {
  children: ReactNode;
}

interface State {
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Unhandled UI error:', error, info);
  }

  reset = () => this.setState({ error: null });

  render() {
    if (this.state.error) {
      return (
        <div className="flex min-h-screen flex-col items-center justify-center gap-3 p-6 text-center">
          <h1 className="text-2xl font-bold">Đã có lỗi xảy ra</h1>
          <p className="max-w-md text-sm text-muted-foreground">
            {this.state.error.message || 'Lỗi không xác định.'}
          </p>
          <div className="flex gap-2">
            <Button onClick={this.reset}>Thử lại</Button>
            <Button variant="outline" onClick={() => window.location.assign('/')}>
              Về trang chủ
            </Button>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}
