import type { ReactNode } from 'react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

export type OlxScopeFilter = {
  id: string;
  label: string;
};

type OlxScopeFilterBarProps = {
  children: ReactNode;
  filters: OlxScopeFilter[];
  onClearFilter: (id: string) => void;
  onClearAll?: () => void;
};

export function OlxActiveFilters({ filters, onClearFilter, onClearAll }: Omit<OlxScopeFilterBarProps, 'children'>) {
  if (filters.length === 0) return null;

  return (
    <div className="flex flex-wrap items-center gap-2" aria-label="Filtros ativos">
      {filters.map((filter) => (
        <button
          key={filter.id}
          type="button"
          className="inline-flex items-center gap-1.5 rounded-full border border-border bg-muted/50 px-2.5 py-1 text-xs transition-colors hover:bg-muted"
          onClick={() => onClearFilter(filter.id)}
          title="Remover filtro"
        >
          <span>{filter.label}</span>
          <span aria-hidden="true">×</span>
        </button>
      ))}
      {onClearAll && filters.length > 1 ? (
        <Button type="button" variant="ghost" size="xs" onClick={onClearAll}>
          Limpar tudo
        </Button>
      ) : null}
    </div>
  );
}

type OlxPickerFieldProps = {
  label: string;
  value: string | null;
  placeholder: string;
  onPick: () => void;
  onClear?: () => void;
  fullWidth?: boolean;
};

export function OlxPickerField({ label, value, placeholder, onPick, onClear, fullWidth }: OlxPickerFieldProps) {
  return (
    <div className={cn('flex flex-col gap-1.5', fullWidth && 'w-full')}>
      {label ? <span className="text-xs font-medium text-muted-foreground">{label}</span> : null}
      <button
        type="button"
        className={cn(
          'flex w-full items-center justify-between gap-2 rounded-lg border border-input bg-background/60 px-3 py-2 text-left text-sm transition-colors hover:bg-muted/50',
          fullWidth && 'w-full',
        )}
        onClick={onPick}
      >
        <span className={cn(value ? 'text-foreground' : 'text-muted-foreground')}>
          {value ?? placeholder}
        </span>
        <span className="text-muted-foreground" aria-hidden="true">▾</span>
      </button>
      {value && onClear ? (
        <Button type="button" variant="ghost" size="xs" className="self-start" onClick={onClear}>
          Limpar
        </Button>
      ) : null}
    </div>
  );
}

export function OlxFilterPanel({ children, filters, onClearFilter, onClearAll }: OlxScopeFilterBarProps) {
  return (
    <div className="rounded-xl border border-border bg-card/40 p-3.5">
      <div className="grid grid-cols-1 items-end gap-3 md:grid-cols-[1fr_1fr_auto] lg:grid-cols-[1fr_1fr_1fr_auto]">
        {children}
      </div>
      <div className="mt-3">
        <OlxActiveFilters filters={filters} onClearFilter={onClearFilter} onClearAll={onClearAll} />
      </div>
    </div>
  );
}

export function OlxHubStrip({
  items,
  variant = 'default',
}: {
  items: { label: string; value: number | string }[];
  variant?: 'default' | 'admin';
}) {
  return (
    <div
      className="mb-4 grid grid-cols-2 gap-3 sm:max-w-md"
      aria-label={variant === 'admin' ? 'Resumo OLX admin' : 'Resumo OLX'}
    >
      {items.map((item) => (
        <div
          key={item.label}
          className={cn(
            'rounded-xl border border-border bg-card/60 px-3.5 py-3',
            variant === 'admin' && 'border-primary/20 bg-primary/5',
          )}
        >
          <span className="block text-xs text-muted-foreground">{item.label}</span>
          <strong className="text-xl font-bold">{item.value}</strong>
        </div>
      ))}
    </div>
  );
}
