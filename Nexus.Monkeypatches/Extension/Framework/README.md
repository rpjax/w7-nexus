# Extension Framework

Fonte do bootstrap (service worker), bridge e runtime (MAIN world).

## Arquivos

| Arquivo | Uso |
|---------|-----|
| `env.js` | Constantes de build (`RUNTIME_VERSION`, `API_BASE_URL`, `RUNTIME_ENDPOINT`) |
| `bridge.js` | Ponte de privilégios: relay (ISOLATED), `__w7_pageBridge` (MAIN), dispatcher (SW) |
| `bootstrap.js` | SW — monta ponte na aba e injeta runtime remoto |
| `monkeypatch_manager.js` | Fetch + inject de patches via page bridge |
| `runtime.js` | Lifecycle (`window.w7runtime`) |
| `logger.js` | Logs de lifecycle |
| `bundler.mjs` | CLI — gera `dist/bootstrap.min.js` e `dist/runtime.min.js` |

## Fluxo

1. SW em `onCommitted`: `mountIsolatedWorldRelay` + `mountPageBridge` (`window.__w7_pageBridge`)
2. SW busca `runtime.min.js` no Nexus e injeta no MAIN
3. Runtime chama `startMonkeyPatchManager()` → `fetchRemote` + `injectScript`
4. Relay encaminha ao SW → `dispatchPrivilegedRequest` (bypass CSP)

## Build

```bash
node bundler.mjs
```

| Artefato | Destino |
|----------|---------|
| `bootstrap.min.js` | cada shell |
| `runtime.min.js` | `wwwroot/monkeypatches/framework/` |
