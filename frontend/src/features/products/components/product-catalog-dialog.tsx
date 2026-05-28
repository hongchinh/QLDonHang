import { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogTitle } from '@/components/ui/dialog';
import { ProductCatalogList } from './product-catalog-list';
import { ProductCatalogDetail } from './product-catalog-detail';
import type { ProductSuggestion } from '@/features/products/types';

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialQuery?: string;
  onSelect: (product: ProductSuggestion) => void;
}

export function ProductCatalogDialog({ open, onOpenChange, initialQuery, onSelect }: Props) {
  const [query, setQuery] = useState(initialQuery ?? '');
  const [groupId, setGroupId] = useState<string | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setQuery(initialQuery ?? '');
      setGroupId(undefined);
      setPage(1);
      setSelectedId(null);
    }
  }, [open, initialQuery]);

  function handleSelect(product: ProductSuggestion) {
    onSelect(product);
    onOpenChange(false);
  }

  function handleQueryChange(q: string) {
    setQuery(q);
    setPage(1);
    setSelectedId(null);
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="flex max-w-5xl overflow-hidden p-0 gap-0 h-[85vh]"
        showClose={true}
      >
        <DialogTitle className="sr-only">Danh mục hàng hóa</DialogTitle>
        {/* Left panel — 55% */}
        <div className="flex w-[55%] flex-col min-h-0">
          <div className="border-b px-4 py-3 flex-shrink-0">
            <h2 className="text-base font-semibold">Danh mục hàng hóa</h2>
          </div>
          <div className="flex-1 overflow-hidden min-h-0">
            <ProductCatalogList
              query={query}
              onQueryChange={handleQueryChange}
              groupId={groupId}
              onGroupChange={(id) => { setGroupId(id); setPage(1); setSelectedId(null); }}
              page={page}
              onPageChange={(p) => { setPage(p); setSelectedId(null); }}
              selectedId={selectedId}
              onSelectId={setSelectedId}
              onSelect={handleSelect}
            />
          </div>
        </div>
        {/* Divider */}
        <div className="w-px bg-border flex-shrink-0" />
        {/* Right panel — 45% */}
        <div className="flex w-[45%] flex-col overflow-hidden min-h-0">
          <div className="border-b px-4 py-3 flex-shrink-0 flex items-center justify-between">
            <h2 className="text-sm font-medium text-muted-foreground">Chi tiết sản phẩm</h2>
          </div>
          <div className="flex-1 overflow-hidden min-h-0">
            <ProductCatalogDetail
              productId={selectedId}
              onSelect={handleSelect}
            />
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
