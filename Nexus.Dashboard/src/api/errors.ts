const GENERIC_PROBLEM_TITLES = new Set([
  'bad request',
  'unauthorized',
  'forbidden',
  'not found',
  'unprocessable entity',
  'internal server error',
  'conflict',
]);

export function fallbackForStatus(status: number, fallback: string): string {
  if (fallback !== 'Não foi possível concluir a operação. Tente novamente em instantes.') {
    return fallback;
  }

  switch (status) {
    case 401:
      return 'Sessão expirada ou credenciais inválidas. Entre novamente para continuar.';
    case 403:
      return 'Você não tem permissão para realizar esta ação.';
    case 404:
      return 'O recurso solicitado não foi encontrado.';
    case 422:
      return 'Os dados enviados são inválidos. Revise os campos e tente novamente.';
    case 500:
    case 502:
    case 503:
      return 'O servidor encontrou um problema. Tente novamente em instantes.';
    default:
      return fallback;
  }
}

function isUsefulDetail(value: string): boolean {
  const trimmed = value.trim();
  if (!trimmed) return false;
  return !GENERIC_PROBLEM_TITLES.has(trimmed.toLowerCase());
}

export async function readApiMessage(
  response: Response,
  fallback: string,
): Promise<string> {
  const resolvedFallback = fallbackForStatus(response.status, fallback);

  try {
    const raw = await response.text();
    if (!raw.trim()) return resolvedFallback;

    const json = JSON.parse(raw) as Record<string, unknown>;

    if (Array.isArray(json.errors)) {
      const messages = json.errors
        .map((item) => {
          if (item && typeof item === 'object' && 'message' in item) {
            const msg = (item as { message?: string }).message;
            return msg?.trim() || null;
          }
          return null;
        })
        .filter((msg): msg is string => Boolean(msg));
      if (messages.length > 0) return messages.join('; ');
    }

    if (typeof json.detail === 'string' && isUsefulDetail(json.detail)) return json.detail.trim();
    if (typeof json.title === 'string' && isUsefulDetail(json.title)) return json.title.trim();
  } catch {
    // ignore parse errors
  }

  return resolvedFallback;
}

export async function readJson<T>(response: Response): Promise<T | null> {
  if (response.status === 204) return null;
  const text = await response.text();
  if (!text.trim()) return null;
  return JSON.parse(text) as T;
}
