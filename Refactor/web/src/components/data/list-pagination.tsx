import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

type ListPaginationProps = {
  page: number;
  pageSize: number;
  total: number;
  onPageChange: (page: number) => void;
  disabled?: boolean;
  className?: string;
};

export function ListPagination({
  page,
  pageSize,
  total,
  onPageChange,
  disabled = false,
  className,
}: ListPaginationProps) {
  const totalPages = Math.max(1, Math.ceil(total / Math.max(pageSize, 1)));
  const currentPage = Math.min(Math.max(page, 1), totalPages);
  const from = total === 0 ? 0 : (currentPage - 1) * pageSize + 1;
  const to = Math.min(currentPage * pageSize, total);
  const prevDisabled = disabled || currentPage <= 1;
  const nextDisabled = disabled || currentPage >= totalPages || total === 0;

  return (
    <div className={cn('flex flex-wrap items-center justify-between gap-2', className)}>
      <p className="text-xs tabular-nums text-muted-foreground">
        {total === 0 ? 'Nenhum registro' : `${from}–${to} de ${total}`}
      </p>
      <div className="flex items-center gap-1">
        <span className="px-1.5 text-xs tabular-nums text-muted-foreground">
          {currentPage}/{totalPages}
        </span>
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="size-7"
          disabled={prevDisabled}
          aria-label="Página anterior"
          onClick={() => onPageChange(currentPage - 1)}
        >
          <ChevronLeft />
        </Button>
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="size-7"
          disabled={nextDisabled}
          aria-label="Próxima página"
          onClick={() => onPageChange(currentPage + 1)}
        >
          <ChevronRight />
        </Button>
      </div>
    </div>
  );
}
