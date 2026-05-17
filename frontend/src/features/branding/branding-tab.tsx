import { useEffect, useRef, useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { toast } from '@/lib/use-toast';
import { getErrorMessage } from '@/lib/api-client';
import { logoUrl } from './api';
import { useBrandingMeta, useUploadBranding } from './hooks';

const MAX_BYTES = 2 * 1024 * 1024;
const ALLOWED_MIMES = ['image/png', 'image/jpeg', 'image/svg+xml'];

interface UploadCardProps {
  title: string;
  description: string;
  hint: string;
  hasExisting: boolean;
  existingUrl: string | null;
  file: File | null;
  onPick: (file: File | null) => void;
}

function UploadCard({ title, description, hint, hasExisting, existingUrl, file, onPick }: UploadCardProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!file) {
      setPreviewUrl(null);
      return;
    }
    const url = URL.createObjectURL(file);
    setPreviewUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [file]);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const picked = e.target.files?.[0] ?? null;
    if (!picked) {
      onPick(null);
      return;
    }
    if (picked.size > MAX_BYTES) {
      toast({ title: 'File quá lớn', description: 'Tối đa 2MB.', variant: 'destructive' });
      e.target.value = '';
      return;
    }
    if (!ALLOWED_MIMES.includes(picked.type)) {
      toast({
        title: 'Định dạng không hỗ trợ',
        description: 'Chỉ chấp nhận PNG, JPEG hoặc SVG.',
        variant: 'destructive',
      });
      e.target.value = '';
      return;
    }
    onPick(picked);
  };

  const showImage = previewUrl ?? (hasExisting ? existingUrl : null);

  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex h-24 items-center justify-center rounded-md border bg-muted/30">
          {showImage ? (
            <img src={showImage} alt={title} className="max-h-full max-w-full object-contain" />
          ) : (
            <span className="text-sm text-muted-foreground">Chưa có logo</span>
          )}
        </div>
        <p className="text-xs text-muted-foreground">{hint}</p>
        <div className="flex items-center gap-2">
          <Button type="button" variant="outline" onClick={() => inputRef.current?.click()}>
            Chọn file
          </Button>
          {file && (
            <span className="truncate text-xs text-muted-foreground">{file.name}</span>
          )}
        </div>
        <input
          ref={inputRef}
          type="file"
          accept="image/png,image/jpeg,image/svg+xml"
          className="hidden"
          onChange={handleFileChange}
        />
      </CardContent>
    </Card>
  );
}

export function BrandingTab() {
  const { data: meta } = useBrandingMeta();
  const upload = useUploadBranding();
  const [logoFull, setLogoFull] = useState<File | null>(null);
  const [logoMark, setLogoMark] = useState<File | null>(null);

  const version = meta?.updatedAt ?? '';
  const fullExistingUrl = meta?.hasLogoFull ? logoUrl('full', version) : null;
  const markExistingUrl = meta?.hasLogoMark ? logoUrl('mark', version) : null;

  const handleSubmit = async () => {
    if (!logoFull && !logoMark) {
      toast({ title: 'Chưa chọn file', description: 'Chọn ít nhất 1 logo để tải lên.', variant: 'destructive' });
      return;
    }
    try {
      await upload.mutateAsync({
        logoFull: logoFull ?? undefined,
        logoMark: logoMark ?? undefined,
      });
      toast({ title: 'Cập nhật logo thành công' });
      setLogoFull(null);
      setLogoMark(null);
    } catch (err) {
      toast({ title: 'Cập nhật thất bại', description: getErrorMessage(err), variant: 'destructive' });
    }
  };

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <UploadCard
          title="Logo ngang"
          description="Dùng khi sidebar mở rộng. Khuyến nghị 240×64px, PNG/JPG/SVG, ≤ 2MB."
          hint="Logo này hiện ở góc trái header khi sidebar mở rộng."
          hasExisting={meta?.hasLogoFull ?? false}
          existingUrl={fullExistingUrl}
          file={logoFull}
          onPick={setLogoFull}
        />
        <UploadCard
          title="Logo vuông"
          description="Dùng khi sidebar thu gọn / mobile / favicon. Khuyến nghị 64×64px, ưu tiên SVG, ≤ 2MB."
          hint="Logo này hiện ở góc trái header khi sidebar thu gọn hoặc trên màn hình nhỏ."
          hasExisting={meta?.hasLogoMark ?? false}
          existingUrl={markExistingUrl}
          file={logoMark}
          onPick={setLogoMark}
        />
      </div>
      <div className="flex justify-end">
        <Button onClick={handleSubmit} disabled={upload.isPending || (!logoFull && !logoMark)}>
          {upload.isPending ? 'Đang lưu…' : 'Lưu thay đổi'}
        </Button>
      </div>
    </div>
  );
}
