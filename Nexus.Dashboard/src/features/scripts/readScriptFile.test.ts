import { describe, expect, it } from 'vitest';
import {
  countSourceLines,
  formatScriptFileSize,
  getSourceCodeByteSize,
  readScriptFile,
  validateScriptFile,
} from './readScriptFile';

function createFile(name: string, content: string, sizeOverride?: number) {
  const file = new File([content], name, { type: 'text/javascript' });
  if (sizeOverride !== undefined) {
    Object.defineProperty(file, 'size', { value: sizeOverride });
  }
  return file;
}

describe('readScriptFile', () => {
  it('formats file sizes', () => {
    expect(formatScriptFileSize(512)).toBe('512 B');
    expect(formatScriptFileSize(2048)).toBe('2.0 KB');
  });

  it('counts lines', () => {
    expect(countSourceLines('a\nb\n')).toBe(3);
    expect(countSourceLines('')).toBe(0);
  });

  it('rejects unsupported extensions', () => {
    expect(validateScriptFile(createFile('bundle.ts', 'code'))).not.toBeNull();
    expect(validateScriptFile(createFile('bundle.js', 'code'))).toBeNull();
  });

  it('counts utf-8 byte size', () => {
    expect(getSourceCodeByteSize('console.log')).toBe(11);
    expect(getSourceCodeByteSize('ação')).toBeGreaterThan(4);
  });

  it('reads javascript bundles', async () => {
    const result = await readScriptFile(createFile('runtime.js', "console.log('ok');"));
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.fileName).toBe('runtime.js');
      expect(result.content).toContain('console.log');
      expect(result.lineCount).toBe(1);
    }
  });
});
