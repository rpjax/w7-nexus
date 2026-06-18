import { IconButton } from './IconButton';

type PaginationBarProps = {
  currentPage: number;
  totalPages: number;
  onPrev: () => void;
  onNext: () => void;
  disabled?: boolean;
  className?: string;
};

export function PaginationBar({
  currentPage,
  totalPages,
  onPrev,
  onNext,
  disabled = false,
  className = '',
}: PaginationBarProps) {
  return (
    <div className={`pagination ${className}`.trim()}>
      <div className="pagination-icon-actions">
        <IconButton
          icon="chevron-left"
          label="Página anterior"
          disabled={disabled || currentPage <= 1}
          onClick={onPrev}
        />
      </div>
      <span className="muted">Página {currentPage} de {totalPages}</span>
      <div className="pagination-icon-actions">
        <IconButton
          icon="chevron-right"
          label="Próxima página"
          disabled={disabled || currentPage >= totalPages}
          onClick={onNext}
        />
      </div>
    </div>
  );
}

type ToolbarSearchActionsProps = {
  onSearch: () => void;
  onRefresh: () => void;
  disabled?: boolean;
};

export function ToolbarSearchActions({ onSearch, onRefresh, disabled = false }: ToolbarSearchActionsProps) {
  return (
    <div className="toolbar-icon-actions">
      <IconButton icon="search" label="Buscar" disabled={disabled} onClick={onSearch} />
      <IconButton icon="refresh" label="Atualizar lista" disabled={disabled} onClick={onRefresh} />
    </div>
  );
}
