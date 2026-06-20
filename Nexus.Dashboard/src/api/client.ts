import { getAccessToken } from '../auth/tokenStore';
import { readApiMessage, readJson } from './errors';

type RequestOptions = {
  fallbackError?: string;
  headers?: Record<string, string>;
};

function buildHeaders(initHeaders?: HeadersInit): HeadersInit {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };

  const accessToken = getAccessToken();
  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }

  if (initHeaders) {
    const extra = new Headers(initHeaders);
    extra.forEach((value, key) => {
      headers[key] = value;
    });
  }

  return headers;
}

async function request<T>(
  path: string,
  init: RequestInit,
  options?: RequestOptions,
): Promise<{ ok: true; data: T | null } | { ok: false; error: string; status: number }> {
  const response = await fetch(path, {
    ...init,
    headers: buildHeaders(init.headers),
  });

  if (!response.ok) {
    const fallback = options?.fallbackError ?? 'Não foi possível concluir a operação. Tente novamente em instantes.';
    const error = await readApiMessage(response, fallback);
    return { ok: false, error, status: response.status };
  }

  const data = await readJson<T>(response);
  return { ok: true, data };
}

export const apiClient = {
  get<T>(path: string, options?: RequestOptions) {
    return request<T>(path, { method: 'GET' }, options);
  },

  post<T>(path: string, body: unknown, options?: RequestOptions) {
    return request<T>(path, {
      method: 'POST',
      body: JSON.stringify(body),
      headers: options?.headers,
    }, options);
  },

  put<T>(path: string, body: unknown, options?: RequestOptions) {
    return request<T>(path, { method: 'PUT', body: JSON.stringify(body) }, options);
  },

  patch<T>(path: string, body: unknown, options?: RequestOptions) {
    return request<T>(path, { method: 'PATCH', body: JSON.stringify(body) }, options);
  },

  delete(path: string, options?: RequestOptions) {
    return request<void>(path, { method: 'DELETE' }, options);
  },

  deleteWithBody<T>(path: string, body: unknown, options?: RequestOptions) {
    return request<T>(path, { method: 'DELETE', body: JSON.stringify(body) }, options);
  },
};
