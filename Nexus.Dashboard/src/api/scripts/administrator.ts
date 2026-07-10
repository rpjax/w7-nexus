import { apiClient } from '../client';
import type {
  CreateScriptPayload,
  PublishReleasePayload,
  PublishReleaseResponse,
  ReleaseDetail,
  ReleaseListResponse,
  ReleaseSourceCode,
  DeleteReleaseResponse,
  ScriptDetail,
  SearchScriptsResponse,
  UpdateScriptPayload,
} from './types';

export async function searchScripts(params: {
  limit?: number;
  offset?: number;
  keyword?: string | null;
}) {
  const query = new URLSearchParams();
  query.set('Limit', String(params.limit ?? 20));
  query.set('Offset', String(params.offset ?? 0));
  if (params.keyword?.trim()) query.set('Keyword', params.keyword.trim());

  return apiClient.get<SearchScriptsResponse>(
    `/api/scripts/administrator?${query.toString()}`,
    { fallbackError: 'Não foi possível carregar os scripts.' },
  );
}

export async function createScript(payload: CreateScriptPayload) {
  return apiClient.post<{ id: string }>('/api/scripts/administrator', {
    Name: payload.name,
    HostPatterns: payload.hostPatterns ?? [],
    Priority: payload.priority ?? 0,
    Description: payload.description ?? null,
  }, { fallbackError: 'Não foi possível criar o script.' });
}

export async function getScript(scriptId: string) {
  return apiClient.get<ScriptDetail>(
    `/api/scripts/administrator/${scriptId}`,
    { fallbackError: 'Não foi possível carregar o script.' },
  );
}

export async function updateScript(scriptId: string, payload: UpdateScriptPayload) {
  return apiClient.patch<ScriptDetail>(
    `/api/scripts/administrator/${scriptId}`,
    {
      Priority: payload.priority,
      Description: payload.description,
      HostPatterns: payload.hostPatterns,
    },
    { fallbackError: 'Não foi possível atualizar o script.' },
  );
}

export async function listReleases(scriptId: string) {
  return apiClient.get<ReleaseListResponse>(
    `/api/scripts/administrator/${scriptId}/releases`,
    { fallbackError: 'Não foi possível carregar os releases.' },
  );
}

export async function getRelease(scriptId: string, releaseId: string) {
  return apiClient.get<ReleaseDetail>(
    `/api/scripts/administrator/${scriptId}/releases/${releaseId}`,
    { fallbackError: 'Não foi possível carregar o release.' },
  );
}

export async function getReleaseSourceCode(scriptId: string, releaseId: string) {
  return apiClient.get<ReleaseSourceCode>(
    `/api/scripts/administrator/${scriptId}/releases/${releaseId}/source-code`,
    { fallbackError: 'Não foi possível carregar o código-fonte.' },
  );
}

export async function publishRelease(scriptId: string, payload: PublishReleasePayload) {
  return apiClient.post<PublishReleaseResponse>(
    `/api/scripts/administrator/${scriptId}/releases`,
    {
      SourceCode: payload.sourceCode,
      Major: payload.major,
      Minor: payload.minor,
      Patch: payload.patch,
    },
    { fallbackError: 'Não foi possível publicar o release.' },
  );
}

export async function promoteRelease(
  scriptId: string,
  channelRouteValue: string,
  releaseId: string,
) {
  return apiClient.post<boolean>(
    `/api/scripts/administrator/${scriptId}/channels/${encodeURIComponent(channelRouteValue)}/promote`,
    { ReleaseId: releaseId },
    { fallbackError: 'Não foi possível promover o release.' },
  );
}

export async function addCustomChannel(scriptId: string, customName: string) {
  return apiClient.post<boolean>(
    `/api/scripts/administrator/${scriptId}/channels`,
    { CustomName: customName },
    { fallbackError: 'Não foi possível adicionar o canal.' },
  );
}

export async function deprecateRelease(scriptId: string, releaseId: string) {
  return apiClient.post<boolean>(
    `/api/scripts/administrator/${scriptId}/releases/${releaseId}/deprecate`,
    {},
    { fallbackError: 'Não foi possível deprecar o release.' },
  );
}

export async function restoreRelease(scriptId: string, releaseId: string) {
  return apiClient.post<boolean>(
    `/api/scripts/administrator/${scriptId}/releases/${releaseId}/restore`,
    {},
    { fallbackError: 'Não foi possível restaurar o release.' },
  );
}

export async function deleteRelease(scriptId: string, releaseId: string) {
  return apiClient.delete<DeleteReleaseResponse>(
    `/api/scripts/administrator/${scriptId}/releases/${releaseId}`,
    { fallbackError: 'Não foi possível excluir o release.' },
  );
}
