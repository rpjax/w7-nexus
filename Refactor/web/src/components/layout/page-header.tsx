import type { ReactNode } from 'react';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';

type PageHeaderProps = {
  title: string;
  description?: ReactNode;
  kicker?: string;
  kickerVariant?: 'default' | 'admin';
  actions?: ReactNode;
  className?: string;
};

export function PageHeader({
  title,
  description,
  kicker,
  kickerVariant = 'default',
  actions,
  className,
}: PageHeaderProps) {
  return (
    <header className={cn('flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between', className)}>
      <div className="min-w-0 space-y-2">
        {kicker ? (
          kickerVariant === 'admin' ? (
            <Badge variant="warning" className="uppercase tracking-wider">
              {kicker}
            </Badge>
          ) : (
            <p className="font-display text-[0.65rem] font-semibold uppercase tracking-[0.22em] text-muted-foreground">
              {kicker}
            </p>
          )
        ) : null}
        <h1 className="break-words font-display text-2xl font-semibold tracking-[-0.03em] md:text-[1.75rem]">
          {title}
        </h1>
        {description ? (
          <p className="max-w-2xl text-sm leading-relaxed text-muted-foreground">{description}</p>
        ) : null}
      </div>
      {actions ? (
        <div className="flex w-full shrink-0 flex-wrap items-center gap-2 sm:w-auto sm:justify-end [&_button]:w-full sm:[&_button]:w-auto">
          {actions}
        </div>
      ) : null}
    </header>
  );
}
