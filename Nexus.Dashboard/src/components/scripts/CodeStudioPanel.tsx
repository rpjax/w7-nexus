import { lazy, Suspense, useMemo } from 'react';
import type { Monaco } from '@monaco-editor/react';
import type { editor } from 'monaco-editor';
import { Card } from '@/components/ui/card';
import { cn } from '@/lib/utils';

const MonacoEditor = lazy(() => import('@monaco-editor/react'));

function defineScriptsMonacoTheme(monaco: Monaco) {
  monaco.editor.defineTheme('scripts-monaco-theme', {
    base: 'vs-dark',
    inherit: true,
    rules: [],
    colors: {
      'editor.background': '#12141c',
      'editor.lineHighlightBackground': '#1a1d28',
      'editorGutter.background': '#0f1118',
      'editor.selectionBackground': '#e8a87c33',
      'editorLineNumber.foreground': '#6b7690',
      'editorLineNumber.activeForeground': '#b8c4df',
      'editorIndentGuide.activeBackground': '#2a3142',
      'editorBracketMatch.border': '#e8a87c88',
    },
  });
}

type CodeStudioPanelProps = {
  value: string;
  onChange?: (value: string) => void;
  readOnly?: boolean;
  height?: string;
  wordWrap?: boolean;
};

function buildEditorOptions(
  readOnly: boolean,
  wordWrap: boolean,
): editor.IStandaloneEditorConstructionOptions {
  return {
    readOnly,
    domReadOnly: readOnly,
    minimap: { enabled: true, scale: 1 },
    wordWrap: wordWrap ? 'on' : 'off',
    wrappingStrategy: 'advanced',
    fontSize: 13,
    lineHeight: 20,
    fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Consolas, monospace',
    fontLigatures: true,
    lineNumbers: 'on',
    lineNumbersMinChars: 3,
    glyphMargin: true,
    folding: true,
    foldingHighlight: true,
    showFoldingControls: 'mouseover',
    scrollBeyondLastLine: false,
    automaticLayout: true,
    padding: { top: 12, bottom: 12 },
    smoothScrolling: true,
    mouseWheelZoom: true,
    cursorBlinking: 'smooth',
    cursorSmoothCaretAnimation: 'on',
    renderLineHighlight: 'all',
    renderWhitespace: 'selection',
    bracketPairColorization: { enabled: true },
    guides: {
      bracketPairs: true,
      bracketPairsHorizontal: true,
      indentation: true,
      highlightActiveIndentation: true,
    },
    stickyScroll: { enabled: true },
    overviewRulerLanes: 3,
    overviewRulerBorder: false,
    scrollbar: {
      vertical: 'visible',
      horizontal: 'visible',
      verticalScrollbarSize: 12,
      horizontalScrollbarSize: 12,
      useShadows: true,
    },
    find: {
      addExtraSpaceOnTop: true,
      autoFindInSelection: 'never',
      seedSearchStringFromSelection: 'selection',
    },
    contextmenu: true,
    links: true,
    colorDecorators: true,
    occurrencesHighlight: 'singleFile',
    selectionHighlight: true,
    matchBrackets: 'always',
    quickSuggestions: !readOnly,
    suggestOnTriggerCharacters: !readOnly,
    tabCompletion: readOnly ? 'off' : 'on',
    formatOnPaste: !readOnly,
  };
}

export function CodeStudioPanel({
  value,
  onChange,
  readOnly = false,
  height = '100%',
  wordWrap = false,
}: CodeStudioPanelProps) {
  const options = useMemo(
    () => buildEditorOptions(readOnly, wordWrap),
    [readOnly, wordWrap],
  );

  return (
    <Card
      className={cn(
        'overflow-hidden rounded-lg border-border/60 bg-card py-0 ring-0',
        readOnly && 'opacity-95',
      )}
    >
      <Suspense fallback={<div className="px-4 py-8 text-sm text-muted-foreground">Carregando editor…</div>}>
        <MonacoEditor
          height={height}
          defaultLanguage="javascript"
          theme="scripts-monaco-theme"
          beforeMount={defineScriptsMonacoTheme}
          value={value}
          onChange={(next) => onChange?.(next ?? '')}
          options={options}
        />
      </Suspense>
    </Card>
  );
}
