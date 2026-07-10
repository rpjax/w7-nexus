import { useState } from 'react';
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

  return (
    <article className={`api-endpoint${embedded ? ' api-endpoint--embedded' : ''}`}>
      {!embedded ? (
        <header className="api-endpoint__header">
          <div className="api-endpoint__method-row">
            <MethodBadge method={endpoint.method} />
            <AuthBadge auth={endpoint.auth} />
          </div>
          <h2 className="api-endpoint__title">{endpoint.title}</h2>
          <p className="api-endpoint__desc">{endpoint.summary ?? endpoint.description}</p>
          {endpoint.whenToUse ? (
            <div className="api-callout api-callout--why api-endpoint__when">
              <h4>Quando usar</h4>
              <p>{endpoint.whenToUse}</p>
            </div>
          ) : null}
        </header>
      ) : null}

      <div className="api-endpoint__path-bar">
        <MethodBadge method={endpoint.method} compact />
        <code className="api-endpoint__path">{endpoint.path}</code>
        <button type="button" className="api-endpoint__copy" onClick={() => void handleCopyPath()}>
          {copiedPath ? 'Copiado' : 'Copiar'}
        </button>
      </div>

      {endpoint.pathParams && endpoint.pathParams.length > 0 ? (
        <section className="api-endpoint__section">
          <h4>Parâmetros de rota</h4>
          <div className="api-param-table">
            {endpoint.pathParams.map((p) => (
              <div key={p.name} className="api-param-row">
                <code>{p.name}</code>
                <span className="api-param-type">{p.type}</span>
                <span className="muted">{p.description}</span>
              </div>
            ))}
          </div>
        </section>
      ) : null}

      {endpoint.queryParams && endpoint.queryParams.length > 0 ? (
        <section className="api-endpoint__section">
          <h4>Query parameters</h4>
          <div className="api-param-table">
            {endpoint.queryParams.map((p) => (
              <div key={p.name} className="api-param-row">
                <code>{p.name}</code>
                <span className="api-param-type">{p.type}{p.required ? ' *' : ''}</span>
                <span className="muted">{p.description}</span>
              </div>
            ))}
          </div>
        </section>
      ) : null}

      <div className="api-endpoint__examples">
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

      {endpoint.notes && endpoint.notes.length > 0 ? (
        <section className="api-endpoint__section">
          <h4>Notas</h4>
          <ul className="api-docs-list">
            {endpoint.notes.map((note) => (
              <li key={note}>{note}</li>
            ))}
          </ul>
        </section>
      ) : null}
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
      <div className="api-docs-empty">
        <p>Endpoint não encontrado.</p>
        <button type="button" className="btn" onClick={() => onNavigate({ kind: 'overview' })}>
          Voltar ao início
        </button>
      </div>
    );
  }

  const groupTitle = API_GROUPS.find((g) => g.id === endpoint.groupId)?.title ?? endpoint.groupId;

  return (
    <div className="api-docs-endpoint-view">
      <button
        type="button"
        className="api-docs-back-link muted small"
        onClick={() => onNavigate({ kind: 'group', id: endpoint.groupId })}
      >
        ← Voltar para {groupTitle}
      </button>
      <ApiDocsEndpointDetail endpoint={endpoint} onNavigate={onNavigate} />
    </div>
  );
}
