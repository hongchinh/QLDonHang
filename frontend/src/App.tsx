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
import { MyQuotationSettingsPage } from '@/pages/settings/my-quotation-settings-page';
import { SettingsHubPage } from '@/pages/settings/settings-hub-page';
import { UserSettingsPage } from '@/pages/admin/user-settings-page';
import { UsersListPage } from '@/pages/admin/users-list-page';
import { BulkTransferPage } from '@/pages/admin/bulk-transfer-page';
import { AdminDashboardPage } from '@/pages/admin/admin-dashboard-page';
import { RolesMatrixPage } from '@/pages/admin/roles-matrix-page';
import { RevenuePage } from '@/pages/reports/revenue-page';
import { SalesPerformancePage } from '@/pages/reports/sales-performance-page';
import { SalesRevenuePage } from '@/pages/reports/sales-revenue-page';
import { ForbiddenPage, NotFoundPage } from '@/pages/error-pages';

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
                <Route
                  path="settings/my-quotation-settings"
                  element={<MyQuotationSettingsPage />}
                />
                <Route
                  path="admin/users"
                  element={
                    <ProtectedRoute permission="user_settings.manage">
                      <UsersListPage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="admin/user-settings/:userId"
                  element={
                    <ProtectedRoute permission="user_settings.manage">
                      <UserSettingsPage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="admin/users/:userId/transfer-quotations"
                  element={
                    <ProtectedRoute permission="quotations.transfer_any">
                      <BulkTransferPage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="admin/roles"
                  element={
                    <ProtectedRoute permission="roles.view">
                      <RolesMatrixPage />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="admin/dashboard"
                  element={
                    <ProtectedRoute permission="quotations.view_all">
                      <AdminDashboardPage />
                    </ProtectedRoute>
                  }
                />
                <Route path="reports">
                  <Route index element={<Navigate to="revenue" replace />} />
                  <Route
                    path="revenue"
                    element={
                      <ProtectedRoute permission="reports.revenue">
                        <RevenuePage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="sales-performance"
                    element={
                      <ProtectedRoute permission="quotations.view_all">
                        <SalesPerformancePage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="sales-revenue"
                    element={
                      <ProtectedRoute permission="reports.revenue">
                        <SalesRevenuePage />
                      </ProtectedRoute>
                    }
                  />
                </Route>
                <Route path="settings" element={<SettingsHubPage />} />
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
