import type { ButtonHTMLAttributes, ReactNode } from 'react';

export type IconName =
  | 'plus'
  | 'trash'
  | 'x'
  | 'copy'
  | 'search'
  | 'refresh'
  | 'chevron-left'
  | 'chevron-right'
  | 'percent'
  | 'link'
  | 'check';

const ICONS: Record<IconName, ReactNode> = {
  plus: (
    <>
      <path d="M12 5v14M5 12h14" />
    </>
  ),
  trash: (
    <>
      <path d="M3 6h18" />
      <path d="M8 6V4h8v2" />
      <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6" />
      <path d="M10 11v6M14 11v6" />
    </>
  ),
  x: (
    <>
      <path d="M18 6 6 18M6 6l12 12" />
    </>
  ),
  copy: (
    <>
      <rect x="9" y="9" width="13" height="13" rx="2" />
      <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
    </>
  ),
  search: (
    <>
      <circle cx="11" cy="11" r="8" />
      <path d="m21 21-4.3-4.3" />
    </>
  ),
  refresh: (
    <>
      <path d="M21 12a9 9 0 1 1-2.64-6.36" />
      <path d="M21 3v6h-6" />
    </>
  ),
  'chevron-left': (
    <path d="m15 18-6-6 6-6" />
  ),
  'chevron-right': (
    <path d="m9 18 6-6-6-6" />
  ),
  percent: (
    <>
      <circle cx="19" cy="5" r="2" />
      <circle cx="5" cy="19" r="2" />
      <path d="m21 3-14 14" />
    </>
  ),
  link: (
    <>
      <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71" />
      <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71" />
    </>
  ),
  check: (
    <path d="M20 6 9 17l-5-5" />
  ),
};

type IconProps = {
  name: IconName;
  className?: string;
};

export function Icon({ name, className = 'ui-icon' }: IconProps) {
  return (
    <svg
      className={className}
      viewBox="0 0 24 24"
      aria-hidden="true"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      {ICONS[name]}
    </svg>
  );
}

type IconButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  icon: IconName;
  label: string;
  variant?: 'ghost' | 'primary' | 'danger';
  size?: 'sm' | 'md';
};

export function IconButton({
  icon,
  label,
  variant = 'ghost',
  size = 'sm',
  className = '',
  type = 'button',
  title,
  ...rest
}: IconButtonProps) {
  const classes = [
    'icon-btn',
    variant === 'ghost' ? 'icon-btn-ghost' : `icon-btn-${variant}`,
    size === 'sm' ? 'icon-btn-sm' : '',
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button
      type={type}
      className={classes}
      aria-label={label}
      title={title ?? label}
      {...rest}
    >
      <Icon name={icon} />
    </button>
  );
}
