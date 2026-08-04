import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { API_GROUPS, endpointsByGroup } from '../../features/api-docs/catalog';
import { MethodBadge } from './MethodBadge';
import { AuthBadge } from './AuthBadge';
import type { ApiDocsView } from '../../features/api-docs/types';

type ApiDocsGroupViewProps = {
  groupId: string;
  onNavigate: (view: ApiDocsView) => void;
};

export function ApiDocsGroupView({ groupId, onNavigate }: ApiDocsGroupViewProps) {
  const group = API_GROUPS.find((g) => g.id === groupId);
  const endpoints = endpointsByGroup(groupId);

  if (!group) {
    return (
      <div className="flex flex-col items-center gap-3 px-4 py-12 text-muted-foreground">
        <p>Domínio não encontrado.</p>
      </div>
    );
  }

  return (
    <div>
      <header className="mb-4">
        <h2 className="mb-2 text-[clamp(1.2rem,3vw,1.45rem)] font-bold">{group.title}</h2>
        <p className="mb-2.5 max-w-[65ch] text-[0.9rem] leading-relaxed text-muted-foreground">
          {group.intro}
        </p>
        <Badge variant="secondary" className="text-xs">
          {endpoints.length} endpoints documentados
        </Badge>
      </header>

      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Método</TableHead>
            <TableHead>Endpoint</TableHead>
            <TableHead className="hidden sm:table-cell">Auth</TableHead>
            <TableHead className="hidden md:table-cell">Resumo</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {endpoints.map((endpoint) => (
            <TableRow key={endpoint.id}>
              <TableCell>
                <MethodBadge method={endpoint.method} compact />
              </TableCell>
              <TableCell>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="h-auto flex-col items-start gap-0.5 px-1 py-0.5 font-normal"
                  onClick={() => onNavigate({ kind: 'endpoint', id: endpoint.id })}
                >
                  <span className="text-[0.88rem] font-semibold">{endpoint.title}</span>
                  <code className="break-all text-[0.72rem] text-primary/80">{endpoint.path}</code>
                </Button>
              </TableCell>
              <TableCell className="hidden sm:table-cell">
                <AuthBadge auth={endpoint.auth} />
              </TableCell>
              <TableCell className="hidden max-w-xs md:table-cell">
                <span className="line-clamp-2 text-[0.8rem] text-muted-foreground">
                  {endpoint.summary ?? endpoint.description}
                </span>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
