import type { ReactNode } from 'react';

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
    <div className="olx-active-filters" aria-label="Filtros ativos">
      {filters.map((filter) => (
        <button
          key={filter.id}
          type="button"
          className="olx-active-filter"
          onClick={() => onClearFilter(filter.id)}
          title="Remover filtro"
        >
          <span>{filter.label}</span>
          <span aria-hidden="true">×</span>
        </button>
      ))}
      {onClearAll && filters.length > 1 ? (
        <button type="button" className="btn btn-ghost btn-small" onClick={onClearAll}>
          Limpar tudo
        </button>
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
};

export function OlxPickerField({ label, value, placeholder, onPick, onClear }: OlxPickerFieldProps) {
  return (
    <div className="olx-picker-field">
      <span className="olx-picker-field__label">{label}</span>
      <button type="button" className="olx-picker-field__button" onClick={onPick}>
        <span className={value ? 'olx-picker-field__value' : 'olx-picker-field__placeholder muted'}>
          {value ?? placeholder}
        </span>
        <span className="olx-picker-field__chevron" aria-hidden="true">▾</span>
      </button>
      {value && onClear ? (
        <button type="button" className="olx-picker-field__clear btn btn-ghost btn-small" onClick={onClear}>
          Limpar
        </button>
      ) : null}
    </div>
  );
}

export function OlxFilterPanel({ children, filters, onClearFilter, onClearAll }: OlxScopeFilterBarProps) {
  return (
    <div className="olx-filter-panel">
      <div className="olx-filter-panel__grid">{children}</div>
      <OlxActiveFilters filters={filters} onClearFilter={onClearFilter} onClearAll={onClearAll} />
    </div>
  );
}
