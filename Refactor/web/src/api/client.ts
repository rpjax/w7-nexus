import { getAccessToken } from '@/auth/accessToken';
import { readApiMessage, readJson } from './errors';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '';

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

export type ApiResult<T> =
  | { ok: true; data: T | null }
  | { ok: false; error: string; status: number };

async function request<T>(
  path: string,
  init: RequestInit,
  options?: RequestOptions,
): Promise<ApiResult<T>> {
  let response: Response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      ...init,
      headers: buildHeaders(init.headers),
    });
  } catch {
    return {
      ok: false,
      error: 'Não foi possível conectar à API. Verifique a rede e se o serviço está no ar.',
      status: 0,
    };
  }

  if (!response.ok) {
    const fallback = options?.fallbackError ?? 'Não foi possível concluir a operação. Tente novamente em instantes.';
    const error = await readApiMessage(response, fallback);
    return { ok: false, error, status: response.status };
  }

  try {
    const data = await readJson<T>(response);
    return { ok: true, data };
  } catch {
    return { ok: false, error: 'A resposta do servidor não pôde ser lida.', status: response.status };
  }
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
    return request<T>(path, {
      method: 'PUT',
      body: JSON.stringify(body),
      headers: options?.headers,
    }, options);
  },

  delete<T>(path: string, body?: unknown, options?: RequestOptions) {
    return request<T>(path, {
      method: 'DELETE',
      body: body === undefined ? undefined : JSON.stringify(body),
      headers: options?.headers,
    }, options);
  },
};
