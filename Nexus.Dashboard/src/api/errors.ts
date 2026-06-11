export async function readApiMessage(response: Response, fallback: string): Promise<string> {
  try {
    const raw = await response.text();
    if (!raw.trim()) return fallback;

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

    if (typeof json.detail === 'string' && json.detail.trim()) return json.detail;
    if (typeof json.title === 'string' && json.title.trim()) return json.title;
  } catch {
    // ignore parse errors
  }

  return fallback;
}

export async function readJson<T>(response: Response): Promise<T | null> {
  if (response.status === 204) return null;
  const text = await response.text();
  if (!text.trim()) return null;
  return JSON.parse(text) as T;
}
