import type { ReactNode } from 'react';
import { RefreshCw, Search } from 'lucide-react';
import { PageHeader, type BreadcrumbItemDef } from '@/components/layout/page-header';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardFooter, CardHeader } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { cn } from '@/lib/utils';

type ListPageLayoutProps = {
  title: string;
  description?: string;
  kicker?: string;
  kickerVariant?: 'default' | 'admin';
  breadcrumbs?: BreadcrumbItemDef[];
  searchId: string;
  searchLabel: string;
  searchPlaceholder: string;
  searchValue: string;
  onSearchChange: (value: string) => void;
  onSearch: () => void;
  onRefresh: () => void;
  totalLabel?: string;
  createAction?: ReactNode;
  toolbarExtra?: ReactNode;
  footer?: ReactNode;
  isLoading?: boolean;
  error?: string | null;
  isEmpty?: boolean;
  emptyTitle?: string;
  emptyMessage?: string;
  children: ReactNode;
  className?: string;
};

export function ListPageLayout({
  title,
  description,
  kicker,
  kickerVariant,
  breadcrumbs,
  searchId,
  searchLabel,
  searchPlaceholder,
  searchValue,
  onSearchChange,
  onSearch,
  onRefresh,
  totalLabel,
  createAction,
  toolbarExtra,
  footer,
  isLoading,
  error,
  isEmpty,
  emptyTitle = 'Nenhum registro encontrado',
  emptyMessage = 'Ajuste os filtros ou tente novamente.',
  children,
  className,
}: ListPageLayoutProps) {
  return (
    <div className={cn('space-y-6', className)}>
      <PageHeader
        title={title}
        description={description}
        kicker={kicker}
        kickerVariant={kickerVariant}
        breadcrumbs={breadcrumbs}
        actions={createAction}
      />

      <Card>
        <CardHeader className="gap-4">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end">
            <div className="min-w-0 flex-1 space-y-2">
              <Label htmlFor={searchId}>{searchLabel}</Label>
              <Input
                id={searchId}
                value={searchValue}
                onChange={(e) => onSearchChange(e.target.value)}
                placeholder={searchPlaceholder}
                onKeyDown={(e) => { if (e.key === 'Enter') onSearch(); }}
              />
            </div>
            <div className="flex shrink-0 items-center gap-2">
              <Button type="button" variant="outline" size="icon" aria-label="Buscar" onClick={onSearch}>
                <Search className="size-4" />
              </Button>
              <Button type="button" variant="outline" size="icon" aria-label="Atualizar" onClick={onRefresh}>
                <RefreshCw className="size-4" />
              </Button>
              {toolbarExtra}
            </div>
          </div>
          {totalLabel ? <p className="text-sm text-muted-foreground">{totalLabel}</p> : null}
        </CardHeader>

        <Separator />

        <CardContent className="pt-6">
          {error ? (
            <Alert variant="destructive">
              <AlertTitle>Erro ao carregar</AlertTitle>
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          ) : isLoading ? (
            <div className="space-y-3">
              <Skeleton className="h-16 w-full" />
              <Skeleton className="h-16 w-full" />
              <Skeleton className="h-16 w-full" />
            </div>
          ) : isEmpty ? (
            <Alert>
              <AlertTitle>{emptyTitle}</AlertTitle>
              <AlertDescription>{emptyMessage}</AlertDescription>
            </Alert>
          ) : (
            children
          )}
        </CardContent>

        {footer && !isLoading && !error ? (
          <CardFooter className="justify-between">{footer}</CardFooter>
        ) : null}
      </Card>
    </div>
  );
}
