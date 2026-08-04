import { useState } from 'react';
import { Button } from '@/components/ui/button';
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb';
import { Card, CardContent } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { cn } from '@/lib/utils';
import { getAccessToken } from '../../auth/tokenStore';
import { endpointById, API_GROUPS } from '../../features/api-docs/catalog';
import { buildCurlExample, copyText } from '../../features/api-docs/utils';
import type { ApiEndpoint, ApiDocsView } from '../../features/api-docs/types';
import { AuthBadge } from './AuthBadge';
import { CodeBlock } from './CodeBlock';
import { MethodBadge } from './MethodBadge';

type ApiDocsEndpointDetailProps = {
  endpoint: ApiEndpoint;
  onNavigate?: (view: ApiDocsView) => void;
  embedded?: boolean;
};

export function ApiDocsEndpointDetail({ endpoint, embedded }: ApiDocsEndpointDetailProps) {
  const [copiedPath, setCopiedPath] = useState(false);
  const token = getAccessToken();

  const handleCopyPath = async () => {
    const ok = await copyText(endpoint.path);
    if (ok) {
      setCopiedPath(true);
      window.setTimeout(() => setCopiedPath(false), 2000);
    }
  };

  const curl = buildCurlExample(
    endpoint.method,
    endpoint.path,
    endpoint.requestBody,
    endpoint.auth,
    endpoint.auth === 'jwt' ? token : null,
  );

  const hasParams = Boolean(
    (endpoint.pathParams && endpoint.pathParams.length > 0)
    || (endpoint.queryParams && endpoint.queryParams.length > 0)
    || (endpoint.notes && endpoint.notes.length > 0),
  );

  return (
    <article className={cn(embedded && 'pt-2')}>
      {!embedded ? (
        <header className="mb-4">
          <div className="mb-2.5 flex gap-1.5">
            <MethodBadge method={endpoint.method} />
            <AuthBadge auth={endpoint.auth} />
          </div>
          <h2 className="mb-1 text-[clamp(1.2rem,3vw,1.45rem)] font-bold">{endpoint.title}</h2>
          <p className="m-0 text-[0.9rem] leading-normal text-muted-foreground">
            {endpoint.summary ?? endpoint.description}
          </p>
          {endpoint.whenToUse ? (
            <Card className="mt-3.5 border-primary/20 bg-primary/5">
              <CardContent className="pt-4">
                <h4 className="mb-1.5 text-[0.78rem] font-semibold uppercase tracking-wide text-muted-foreground">
                  Quando usar
                </h4>
                <p className="m-0 text-[0.86rem] leading-relaxed">{endpoint.whenToUse}</p>
              </CardContent>
            </Card>
          ) : null}
        </header>
      ) : null}

      <div className="mb-4 flex flex-wrap items-center gap-1.5 rounded-lg border border-border bg-background/80 px-3 py-2">
        <MethodBadge method={endpoint.method} compact />
        <code className="min-w-0 flex-1 break-all text-[0.8rem] text-primary/80">{endpoint.path}</code>
        <Button
          type="button"
          variant="ghost"
          size="xs"
          className="h-auto shrink-0 px-1.5 py-0.5 text-[0.72rem] text-primary"
          onClick={() => void handleCopyPath()}
        >
          {copiedPath ? 'Copiado' : 'Copiar'}
        </Button>
      </div>

      {hasParams ? (
        <Accordion type="multiple" className="mb-4" defaultValue={['path-params', 'query-params', 'notes'].filter((id) => {
          if (id === 'path-params') return endpoint.pathParams && endpoint.pathParams.length > 0;
          if (id === 'query-params') return endpoint.queryParams && endpoint.queryParams.length > 0;
          if (id === 'notes') return endpoint.notes && endpoint.notes.length > 0;
          return false;
        })}>
          {endpoint.pathParams && endpoint.pathParams.length > 0 ? (
            <AccordionItem value="path-params">
              <AccordionTrigger className="text-[0.78rem] uppercase tracking-wide text-muted-foreground">
                Parâmetros de rota
              </AccordionTrigger>
              <AccordionContent>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Nome</TableHead>
                      <TableHead>Tipo</TableHead>
                      <TableHead>Descrição</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {endpoint.pathParams.map((p) => (
                      <TableRow key={p.name}>
                        <TableCell><code>{p.name}</code></TableCell>
                        <TableCell className="text-primary">{p.type}</TableCell>
                        <TableCell className="text-muted-foreground">{p.description}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </AccordionContent>
            </AccordionItem>
          ) : null}

          {endpoint.queryParams && endpoint.queryParams.length > 0 ? (
            <AccordionItem value="query-params">
              <AccordionTrigger className="text-[0.78rem] uppercase tracking-wide text-muted-foreground">
                Query parameters
              </AccordionTrigger>
              <AccordionContent>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Nome</TableHead>
                      <TableHead>Tipo</TableHead>
                      <TableHead>Descrição</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {endpoint.queryParams.map((p) => (
                      <TableRow key={p.name}>
                        <TableCell><code>{p.name}</code></TableCell>
                        <TableCell className="text-primary">
                          {p.type}{p.required ? ' *' : ''}
                        </TableCell>
                        <TableCell className="text-muted-foreground">{p.description}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </AccordionContent>
            </AccordionItem>
          ) : null}

          {endpoint.notes && endpoint.notes.length > 0 ? (
            <AccordionItem value="notes">
              <AccordionTrigger className="text-[0.78rem] uppercase tracking-wide text-muted-foreground">
                Notas
              </AccordionTrigger>
              <AccordionContent>
                <ul className="my-2 list-disc pl-5 text-[0.86rem] leading-relaxed text-muted-foreground">
                  {endpoint.notes.map((note) => (
                    <li key={note}>{note}</li>
                  ))}
                </ul>
              </AccordionContent>
            </AccordionItem>
          ) : null}
        </Accordion>
      ) : null}

      <div className="mb-4 flex flex-col gap-2.5">
        {endpoint.requestBody ? (
          <CodeBlock code={endpoint.requestBody} label="Request body" />
        ) : null}
        {endpoint.responseBody ? (
          <CodeBlock code={endpoint.responseBody} label="Response" />
        ) : null}
        <CodeBlock
          code={curl}
          label={
            endpoint.auth === 'jwt' && token
              ? 'cURL (com seu token)'
              : endpoint.auth === 'master-token'
                ? 'cURL (token mestre)'
                : 'cURL'
          }
          language="bash"
        />
      </div>
    </article>
  );
}

type ApiDocsEndpointViewProps = {
  endpointId: string;
  onNavigate: (view: ApiDocsView) => void;
};

export function ApiDocsEndpointView({ endpointId, onNavigate }: ApiDocsEndpointViewProps) {
  const endpoint = endpointById.get(endpointId);

  if (!endpoint) {
    return (
      <div className="flex flex-col items-center gap-3 px-4 py-12 text-muted-foreground">
        <p>Endpoint não encontrado.</p>
        <Button type="button" variant="outline" onClick={() => onNavigate({ kind: 'overview' })}>
          Voltar ao início
        </Button>
      </div>
    );
  }

  const groupTitle = API_GROUPS.find((g) => g.id === endpoint.groupId)?.title ?? endpoint.groupId;

  return (
    <div>
      <Breadcrumb className="mb-3">
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink
              className="cursor-pointer"
              onClick={() => onNavigate({ kind: 'overview' })}
            >
              Início
            </BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbLink
              className="cursor-pointer"
              onClick={() => onNavigate({ kind: 'group', id: endpoint.groupId })}
            >
              {groupTitle}
            </BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator />
          <BreadcrumbItem>
            <BreadcrumbPage>{endpoint.title}</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>
      <ApiDocsEndpointDetail endpoint={endpoint} onNavigate={onNavigate} />
    </div>
  );
}
