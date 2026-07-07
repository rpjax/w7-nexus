# Extension Framework

Fonte do bootstrap (service worker), bridge e runtime (MAIN world).

## Arquivos

| Arquivo | Uso |
|---------|-----|
| `env.js` | Constantes de build (`RUNTIME_VERSION`, `API_BASE_URL`, `RUNTIME_ENDPOINT`) |
| `bridge/` | Bridge V1 — protocolo event-driven MAIN ↔ ISOLATED ↔ SW ([docs](bridge/README.md)) |
| `bootstrap.js` | SW — monta relay isolated, busca runtime remoto e injeta no MAIN |
| `runtime/` | Bundle MAIN — `runtime.js`, `monkeypatch_manager.js`, `extension_watcher.js`, `state.js`, `cache.js`, `config.js` |
| `logger.js` | Logs de lifecycle |
| `bundler.mjs` | CLI — gera `dist/bootstrap.min.js` e `dist/runtime.min.js` |

## Fluxo

1. SW em `onCommitted`: `installIsolatedBridgeAsync(tabId)` — relay no mundo ISOLATED
2. SW busca `runtime.min.js` no Nexus e injeta no MAIN
3. Runtime `init()`: `installMainWorldBridge()` → `window.w7framework_bridge`
4. Runtime `start()`: `startMonkeyPatchManagerAsync()` → `fetchAsync` / `evalInMainWorldAsync`
5. `watchExtension(id)` → `start_network_observe` no SW; eventos chegam via `addNetworkEventListener`
6. MAIN posta `INVOCATION_REQUEST`; isolado encaminha ao SW (fire-and-forget); SW executa handler e empurra `INVOCATION_RESPONSE` ou `NETWORK_EVENT` via `tabs.sendMessage`

Ver [bridge/README.md](bridge/README.md) para o contrato completo.

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
