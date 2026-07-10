export const SCRIPT_FILE_ACCEPT = '.js,.mjs,.cjs';
export const MAX_SCRIPT_FILE_BYTES = 5 * 1024 * 1024;

const ALLOWED_EXTENSIONS = new Set(['js', 'mjs', 'cjs']);

export type ScriptFileReadResult =
  | {
      ok: true;
      content: string;
      fileName: string;
      sizeBytes: number;
      lineCount: number;
    }
  | { ok: false; message: string };

export function formatScriptFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(bytes >= 10240 ? 0 : 1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function countSourceLines(source: string): number {
  if (!source) return 0;
  return source.split('\n').length;
}

export function getSourceCodeByteSize(source: string): number {
  return new TextEncoder().encode(source).length;
}

function getExtension(fileName: string): string | null {
  const dot = fileName.lastIndexOf('.');
  if (dot <= 0 || dot === fileName.length - 1) return null;
  return fileName.slice(dot + 1).toLowerCase();
}

export function validateScriptFile(file: File): string | null {
  const extension = getExtension(file.name);
  if (!extension || !ALLOWED_EXTENSIONS.has(extension)) {
    return 'Use um bundle JavaScript (.js, .mjs ou .cjs).';
  }

  if (file.size === 0) {
    return 'O arquivo está vazio.';
  }

  if (file.size > MAX_SCRIPT_FILE_BYTES) {
    return `O arquivo excede ${formatScriptFileSize(MAX_SCRIPT_FILE_BYTES)}.`;
  }

  return null;
}

export async function readScriptFile(file: File): Promise<ScriptFileReadResult> {
  const validationError = validateScriptFile(file);
  if (validationError) {
    return { ok: false, message: validationError };
  }

  try {
    const content = await file.text();
    if (!content.trim()) {
      return { ok: false, message: 'O arquivo não contém código JavaScript.' };
    }

    return {
      ok: true,
      content,
      fileName: file.name,
      sizeBytes: file.size,
      lineCount: countSourceLines(content),
    };
  } catch {
    return { ok: false, message: 'Não foi possível ler o arquivo.' };
  }
}
