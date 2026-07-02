import { apiClient } from './client';
import type { GatewayPrefix, KeyPairCredential, SearchRequest, SearchResponse, TokenCredential } from './types';

function apiPath(prefix: GatewayPrefix, segment: string) {
  return `/api/gateways/administrator/${prefix}/${segment}`;
}

export async function searchGatewayCredentials(prefix: GatewayPrefix, payload: SearchRequest) {
  return apiClient.post<SearchResponse<TokenCredential & KeyPairCredential>>(
    apiPath(prefix, 'search'),
    {
      Limit: payload.limit,
      Offset: payload.offset,
      Keyword: payload.keyword ?? null,
      EnabledOnly: payload.enabledOnly ?? undefined,
    },
    { fallbackError: 'Não foi possível carregar as credenciais. Atualize a página e tente novamente.' },
  );
}

export async function addTokenCredential(prefix: GatewayPrefix, body: {
  name: string;
  token: string;
  strawManId?: string | null;
  enabled: boolean;
}) {
  return apiClient.post<void>(apiPath(prefix, 'credentials'), {
    Name: body.name,
    Token: body.token,
    StrawManId: body.strawManId ?? null,
    Enabled: body.enabled,
  }, { fallbackError: 'Não foi possível cadastrar a credencial. Verifique os dados informados.' });
}

export async function addKeyPairCredential(prefix: GatewayPrefix, body: {
  name: string;
  publicKey: string;
  secretKey: string;
  strawManId?: string | null;
  enabled: boolean;
}) {
  return apiClient.post<void>(apiPath(prefix, 'credentials'), {
    Name: body.name,
    PublicKey: body.publicKey,
    SecretKey: body.secretKey,
    StrawManId: body.strawManId ?? null,
    Enabled: body.enabled,
  }, { fallbackError: 'Não foi possível cadastrar a credencial. Verifique os dados informados.' });
}

export async function updateTokenCredential(prefix: GatewayPrefix, body: {
  id: string;
  name: string;
  token: string;
  strawManId?: string | null;
  enabled: boolean;
}) {
  return apiClient.put<void>(apiPath(prefix, 'credentials'), {
    Id: body.id,
    Name: body.name,
    Token: body.token,
    StrawManId: body.strawManId ?? null,
    Enabled: body.enabled,
  }, { fallbackError: 'Não foi possível atualizar a credencial. Verifique os dados informados.' });
}

export async function updateKeyPairCredential(prefix: GatewayPrefix, body: {
  id: string;
  name: string;
  publicKey: string;
  secretKey: string;
  strawManId?: string | null;
  enabled: boolean;
}) {
  return apiClient.put<void>(apiPath(prefix, 'credentials'), {
    Id: body.id,
    Name: body.name,
    PublicKey: body.publicKey,
    SecretKey: body.secretKey,
    StrawManId: body.strawManId ?? null,
    Enabled: body.enabled,
  }, { fallbackError: 'Não foi possível atualizar a credencial. Verifique os dados informados.' });
}

export async function setCredentialEnabled(prefix: GatewayPrefix, id: string, enabled: boolean) {
  return apiClient.patch<void>(apiPath(prefix, 'credentials/enabled'), { id, enabled }, {
    fallbackError: 'Não foi possível alterar o estado da credencial.',
  });
}

export async function deleteCredential(prefix: GatewayPrefix, id: string) {
  return apiClient.delete(`${apiPath(prefix, 'credentials')}?id=${encodeURIComponent(id)}`, {
    fallbackError: 'Não foi possível excluir a credencial. Tente novamente.',
  });
}

export async function searchCredentialsForPicker(prefix: GatewayPrefix, payload: SearchRequest) {
  return apiClient.post<SearchResponse<{ id: string; name: string }>>(
    apiPath(prefix, 'search'),
    {
      Limit: payload.limit,
      Offset: payload.offset,
      Keyword: payload.keyword ?? null,
      EnabledOnly: true,
    },
  );
}
