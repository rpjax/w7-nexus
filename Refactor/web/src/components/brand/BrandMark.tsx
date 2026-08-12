import { cn } from '@/lib/utils';

type BrandMarkProps = {
  className?: string;
  size?: 'sm' | 'md' | 'lg';
  showWordmark?: boolean;
};

type BrandLockupProps = {
  className?: string;
  /** `hero` for auth brand plane; `compact` for shell chrome. */
  size?: 'hero' | 'compact';
  subtitle?: string;
};

const sizeMap = {
  sm: { mark: 'size-7', word: 'text-base', gap: 'gap-2' },
  md: { mark: 'size-9', word: 'text-xl', gap: 'gap-2.5' },
  lg: { mark: 'size-11', word: 'text-3xl', gap: 'gap-3' },
} as const;

export function BrandGlyph({ className }: { className?: string }) {
  return (
    <span
      className={cn(
        'relative inline-flex shrink-0 items-center justify-center rounded-xl bg-primary/15 ring-1 ring-primary/35',
        className,
      )}
      aria-hidden="true"
    >
      <span className="absolute inset-[18%] rounded-md bg-gradient-to-br from-primary via-primary to-brand-violet opacity-90" />
      <span className="absolute inset-[32%] rounded-sm bg-background/80" />
      <span className="absolute left-[42%] top-[28%] h-[44%] w-[16%] rounded-full bg-primary" />
    </span>
  );
}

/** Ícone + wordmark compacto. */
export function BrandMark({
  className,
  size = 'md',
  showWordmark = true,
}: BrandMarkProps) {
  const s = sizeMap[size];

  return (
    <div className={cn('inline-flex items-center', s.gap, className)}>
      <BrandGlyph className={s.mark} />
      {showWordmark ? (
        <span className={cn('font-display font-semibold tracking-tight text-foreground', s.word)}>
          Nexus
        </span>
      ) : (
        <span className="sr-only">Nexus</span>
      )}
    </div>
  );
}

export function BrandEyebrow({ className }: { className?: string }) {
  return (
    <p
      className={cn(
        'font-display text-[0.7rem] font-semibold uppercase tracking-[0.22em] text-primary',
        className,
      )}
    >
      Websete
    </p>
  );
}

/** Lockup canônico — brand como sinal de herói (auth) ou chrome (shell). */
export function BrandLockup({
  className,
  size = 'compact',
  subtitle,
}: BrandLockupProps) {
  if (size === 'hero') {
    return (
      <div className={cn('space-y-5', className)}>
        <div className="flex items-center gap-3">
          <BrandGlyph className="size-11" />
          <BrandEyebrow />
        </div>
        <h1 className="font-display text-5xl font-semibold tracking-[-0.04em] text-foreground md:text-6xl">
          Nexus
        </h1>
        {subtitle ? (
          <p className="max-w-sm text-base leading-relaxed text-muted-foreground">
            {subtitle}
          </p>
        ) : null}
      </div>
    );
  }

  return (
    <div className={cn('flex min-w-0 items-center gap-2.5', className)}>
      <BrandGlyph className="size-7" />
      <div className="min-w-0">
        <p className="font-display text-[0.65rem] font-semibold uppercase tracking-[0.24em] text-primary">
          Websete Nexus
        </p>
        {subtitle ? (
          <p className="truncate text-sm text-muted-foreground">{subtitle}</p>
        ) : null}
      </div>
    </div>
  );
}
