import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { queryClient } from '@/lib/query-client';
import { ProtectedRoute } from '@/routes/protected-route';
import { AuthInit } from '@/routes/auth-init';
import { ErrorBoundary } from '@/components/error-boundary';
import { Toaster } from '@/components/ui/toaster';
import { AppLayout } from '@/components/layout/app-layout';
import { LoginPage } from '@/pages/login-page';
import { DashboardPage } from '@/pages/dashboard-page';
import { CustomerListPage } from '@/pages/customers/customer-list-page';
import { CustomerFormPage } from '@/pages/customers/customer-form-page';
import { ProductListPage } from '@/pages/products/product-list-page';
import { ProductFormPage } from '@/pages/products/product-form-page';
import { QuotationListPage } from '@/pages/quotations/quotation-list-page';
import { QuotationFormPage } from '@/pages/quotations/quotation-form-page';
import { ForbiddenPage, NotFoundPage } from '@/pages/error-pages';

const PLACEHOLDER = (title: string) => (
  <div className="space-y-2">
    <h1 className="text-2xl font-bold">{title}</h1>
    <p className="text-sm text-muted-foreground">
      Module này chưa được hiện thực — tham khảo pattern của module Khách hàng để mở rộng.
    </p>
  </div>
);

export function App() {
  return (
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <AuthInit>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route path="/403" element={<ForbiddenPage />} />

              <Route element={<ProtectedRoute><AppLayout /></ProtectedRoute>}>
                <Route index element={<DashboardPage />} />

                <Route path="customers">
                  <Route
                    index
                    element={
                      <ProtectedRoute permission="customers.view">
                        <CustomerListPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="new"
                    element={
                      <ProtectedRoute permission="customers.create">
                        <CustomerFormPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path=":id"
                    element={
                      <ProtectedRoute permission="customers.update">
                        <CustomerFormPage />
                      </ProtectedRoute>
                    }
                  />
                </Route>

                <Route path="products">
                  <Route
                    index
                    element={
                      <ProtectedRoute permission="products.view">
                        <ProductListPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="new"
                    element={
                      <ProtectedRoute permission="products.create">
                        <ProductFormPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path=":id"
                    element={
                      <ProtectedRoute permission="products.update">
                        <ProductFormPage />
                      </ProtectedRoute>
                    }
                  />
                </Route>
                <Route path="quotations">
                  <Route
                    index
                    element={
                      <ProtectedRoute permission="quotations.view">
                        <QuotationListPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="new"
                    element={
                      <ProtectedRoute permission="quotations.create">
                        <QuotationFormPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path=":id"
                    element={
                      <ProtectedRoute permission="quotations.update">
                        <QuotationFormPage />
                      </ProtectedRoute>
                    }
                  />
                </Route>
                <Route path="orders" element={<ProtectedRoute permission="orders.view">{PLACEHOLDER('Đơn hàng')}</ProtectedRoute>} />
                <Route path="deliveries" element={<ProtectedRoute permission="orders.deliver">{PLACEHOLDER('Bàn giao')}</ProtectedRoute>} />
                <Route path="payments" element={<ProtectedRoute permission="orders.pay">{PLACEHOLDER('Thanh toán & Công nợ')}</ProtectedRoute>} />
                <Route path="reports" element={<ProtectedRoute permission="reports.revenue">{PLACEHOLDER('Báo cáo')}</ProtectedRoute>} />
                <Route path="settings" element={<ProtectedRoute requireRole="ADMIN">{PLACEHOLDER('Cấu hình hệ thống')}</ProtectedRoute>} />
              </Route>

              <Route path="/404" element={<NotFoundPage />} />
              <Route path="*" element={<Navigate to="/404" replace />} />
            </Routes>
          </AuthInit>
        </BrowserRouter>
        <Toaster />
        <ReactQueryDevtools initialIsOpen={false} />
      </QueryClientProvider>
    </ErrorBoundary>
  );
}
