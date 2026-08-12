import {
  type ColumnDef,
  flexRender,
  getCoreRowModel,
  useReactTable,
} from '@tanstack/react-table';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';

type DataTableProps<TData, TValue> = {
  columns: ColumnDef<TData, TValue>[];
  data: TData[];
  className?: string;
  density?: 'compact' | 'default';
  selectedRowId?: string | null;
  onRowClick?: (row: TData) => void;
  getRowId?: (row: TData) => string;
  emptyMessage?: string;
  loading?: boolean;
  loadingRows?: number;
};

export function DataTable<TData, TValue>({
  columns,
  data,
  className,
  density = 'compact',
  selectedRowId,
  onRowClick,
  getRowId,
  emptyMessage = 'Sem resultados.',
  loading = false,
  loadingRows = 8,
}: DataTableProps<TData, TValue>) {
  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getRowId: getRowId ? (row) => getRowId(row) : undefined,
  });

  const compact = density === 'compact';

  return (
    <div className={cn('relative min-h-0 overflow-auto', className)}>
      <Table>
        <TableHeader className="sticky top-0 z-10 bg-card/95 backdrop-blur-sm">
          {table.getHeaderGroups().map((headerGroup) => (
            <TableRow key={headerGroup.id} className="hover:bg-transparent">
              {headerGroup.headers.map((header) => (
                <TableHead
                  key={header.id}
                  className={cn(
                    compact && 'h-8 px-2.5 text-[0.65rem] font-semibold uppercase tracking-[0.08em]',
                  )}
                >
                  {header.isPlaceholder
                    ? null
                    : flexRender(header.column.columnDef.header, header.getContext())}
                </TableHead>
              ))}
            </TableRow>
          ))}
        </TableHeader>
        <TableBody>
          {loading ? (
            Array.from({ length: loadingRows }).map((_, index) => (
              <TableRow key={`skeleton-${index}`} className="hover:bg-transparent">
                {columns.map((_, colIndex) => (
                  <TableCell key={colIndex} className={cn(compact && 'px-2.5 py-1.5')}>
                    <Skeleton className="h-3.5 w-full max-w-[9rem]" />
                  </TableCell>
                ))}
              </TableRow>
            ))
          ) : table.getRowModel().rows.length ? (
            table.getRowModel().rows.map((row) => {
              const id = getRowId?.(row.original);
              const selected = selectedRowId != null && id === selectedRowId;
              return (
                <TableRow
                  key={row.id}
                  data-state={selected ? 'selected' : undefined}
                  className={cn(
                    onRowClick && 'cursor-pointer',
                    selected && 'bg-primary/12 ring-1 ring-inset ring-primary/25',
                  )}
                  onClick={onRowClick ? () => onRowClick(row.original) : undefined}
                >
                  {row.getVisibleCells().map((cell) => (
                    <TableCell
                      key={cell.id}
                      className={cn(compact && 'px-2.5 py-1.5 text-[0.8125rem]')}
                    >
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              );
            })
          ) : (
            <TableRow className="hover:bg-transparent">
              <TableCell
                colSpan={columns.length}
                className="h-28 text-center text-muted-foreground"
              >
                {emptyMessage}
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );
}
