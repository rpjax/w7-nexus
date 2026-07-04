# Extension Framework

Fonte do bootstrap (service worker), bridge e runtime (MAIN world).

## Arquivos

| Arquivo | Uso |
|---------|-----|
| `env.js` | Constantes de build (`RUNTIME_VERSION`, `API_BASE_URL`, `RUNTIME_ENDPOINT`) |
| `bridge.js` | RPC tipado (`eval:request` / `eval:response`), relay (ISOLATED), `evalAsync` (MAIN), compile no SW |
| `bootstrap.js` | SW — monta ponte na aba e injeta runtime remoto |
| `monkeypatch_manager.js` | Features de runtime: fetch/inject privilegiados via `evalAsync` |
| `runtime.js` | Lifecycle (`window.w7runtime`) |
| `logger.js` | Logs de lifecycle |
| `bundler.mjs` | CLI — gera `dist/bootstrap.min.js` e `dist/runtime.min.js` |

## Fluxo

1. SW em `onCommitted`: `mountIsolatedWorldRelay` + `mountPageBridge` (`window.__w7_pageBridge.evalAsync`)
2. SW busca `runtime.min.js` no Nexus e injeta no MAIN
3. Runtime chama `startMonkeyPatchManagerAsync()` → `fetchRemoteAsync` / `injectScriptAsync` via `evalAsync({ source, args })`
4. MAIN posta `eval:request` (flag + CompletionSource); isolado encaminha ao SW; SW `runPrivilegedRequestAsync`; isolado posta `eval:response`

## Build

```bash
node bundler.mjs env=dev
node bundler.mjs env=prod
node bundler.mjs help=true
```

### Flags (`key=value`)

| Flag | Default | Descricao |
|------|---------|-----------|
| `env=dev\|prod` | — | Preset: `dev` desliga minify/obfuscate e liga sourcemap; `prod` minifica, ofusca (`max`) e desliga sourcemap |
| `out-dir=<path>` | `dist` | Pasta de saida |
| `only=bootstrap\|runtime` | todos | Artefato unico |
| `format=iife\|esm` | `iife` | Formato esbuild |
| `target=<target>` | `es2022` | Target esbuild |
| `minify=true\|false` | `true` | Minificacao |
| `obfuscate=true\|false` | `false` | Pos-processo com `javascript-obfuscator` |
| `obfuscation=standard\|max` | `standard` | Intensidade da ofuscacao |
| `sourcemap=true\|false` | `false` | Gera source map |

Flags explicitas sobrescrevem o preset de `env`. Ex.: `env=prod obfuscate=false`.

| Artefato | Destino |
|----------|---------|
| `bootstrap.min.js` | cada shell |
| `runtime.min.js` | `wwwroot/monkeypatches/framework/` |
