import { Dialog, DialogContent, DialogTitle } from '@/components/ui/dialog';
import { customersApi } from '@/features/customers/api';
import { toast } from '@/lib/use-toast';
import type { CustomerSearchItem } from '@/features/customers/types';
import { CustomerCatalogList } from './customer-catalog-list';

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialQuery: string;
  onSelect: (c: CustomerSearchItem) => void;
}

export function CustomerCatalogDialog({ open, onOpenChange, initialQuery, onSelect }: Props) {
  async function handleSelectId(id: string) {
    try {
      const customer = await customersApi.get(id);
      onSelect({
        id: customer.id,
        code: customer.code,
        name: customer.name,
        taxCode: customer.taxCode,
        companyAddress: customer.companyAddress,
        defaultShippingAddress: customer.defaultShippingAddress,
        contactPerson: customer.contactPerson,
        phoneNumber: customer.phoneNumber,
        status: customer.status,
      });
      onOpenChange(false);
    } catch {
      toast({ variant: 'destructive', title: 'Không thể tải thông tin khách hàng. Vui lòng thử lại.' });
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="flex max-w-4xl overflow-hidden p-0 h-[80vh]" showClose={true}>
        <DialogTitle className="sr-only">Danh mục khách hàng</DialogTitle>
        <div className="flex w-full flex-col min-h-0">
          <div className="border-b px-4 pr-10 py-3 flex-shrink-0">
            <h2 className="text-base font-semibold">Danh mục khách hàng</h2>
          </div>
          <div className="flex-1 overflow-hidden min-h-0">
            <CustomerCatalogList
              initialQuery={initialQuery}
              onSelect={handleSelectId}
            />
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
