import './button-loader.css';

interface ButtonLoaderProps {
  className?: string;
}

export function ButtonLoader({ className = '' }: ButtonLoaderProps) {
  return <span className={`button-loader ${className}`.trim()} aria-hidden="true" />;
}
