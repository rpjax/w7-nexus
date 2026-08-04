import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationNext,
  PaginationPrevious,
} from '@/components/ui/pagination';
import { cn } from '@/lib/utils';

type ListPaginationProps = {
  currentPage: number;
  totalPages: number;
  onPrev: () => void;
  onNext: () => void;
  disabled?: boolean;
  className?: string;
};

export function ListPagination({
  currentPage,
  totalPages,
  onPrev,
  onNext,
  disabled = false,
  className,
}: ListPaginationProps) {
  const prevDisabled = disabled || currentPage <= 1;
  const nextDisabled = disabled || currentPage >= totalPages;

  return (
    <div className={cn('flex w-full items-center justify-between gap-3', className)}>
      <Pagination className="mx-0 w-auto justify-start">
        <PaginationContent>
          <PaginationItem>
            <PaginationPrevious
              href="#"
              text="Anterior"
              className={prevDisabled ? 'pointer-events-none opacity-50' : undefined}
              onClick={(e) => {
                e.preventDefault();
                if (!prevDisabled) onPrev();
              }}
            />
          </PaginationItem>
          <PaginationItem>
            <span className="px-3 text-sm text-muted-foreground">
              Página {currentPage} de {totalPages}
            </span>
          </PaginationItem>
          <PaginationItem>
            <PaginationNext
              href="#"
              text="Próxima"
              className={nextDisabled ? 'pointer-events-none opacity-50' : undefined}
              onClick={(e) => {
                e.preventDefault();
                if (!nextDisabled) onNext();
              }}
            />
          </PaginationItem>
        </PaginationContent>
      </Pagination>
    </div>
  );
}
