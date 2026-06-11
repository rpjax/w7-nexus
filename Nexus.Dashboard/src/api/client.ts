import { readApiMessage, readJson } from './errors';

type RequestOptions = {
  fallbackError?: string;
};

async function request<T>(
  path: string,
  init: RequestInit,
  options?: RequestOptions,
): Promise<{ ok: true; data: T | null } | { ok: false; error: string; status: number }> {
  const response = await fetch(path, {
    headers: { 'Content-Type': 'application/json', ...init.headers },
    ...init,
  });

  if (!response.ok) {
    const fallback = options?.fallbackError ?? 'Falha na requisição.';
    const error = await readApiMessage(response, fallback);
    return { ok: false, error, status: response.status };
  }

  const data = await readJson<T>(response);
  return { ok: true, data };
}

export const apiClient = {
  post<T>(path: string, body: unknown, options?: RequestOptions) {
    return request<T>(path, { method: 'POST', body: JSON.stringify(body) }, options);
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
