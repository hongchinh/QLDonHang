export function SkipToContent() {
  return (
    <a
      href="#main-content"
      className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-[100] focus:rounded focus:bg-header-bg focus:px-4 focus:py-2 focus:text-header-fg"
    >
      Bỏ qua tới nội dung chính
    </a>
  );
}
