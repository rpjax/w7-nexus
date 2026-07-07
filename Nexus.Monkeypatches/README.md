# Nexus Monkeypatches

Runtime unificado (`window.w7runtime`), extensão Chrome, patches por site e artefatos de entrega (XSS, MITM). O **host** (`CHROME_EXTENSION`, `XSS`, `MITM_INJECTION`) é definido na entrega, não detectado em runtime.

## Estrutura

```
Nexus.Monkeypatches/
  build.mjs              CLI unificada de build
  dist/                  artefatos gerados (gitignored)
  tools/bundler.mjs      engine esbuild + generator

  runtime/               env, host, api, initializer, monkeypatch-manager, hosts/
  chrome-extension/      bootstrap (core) + shells MV3
  patches/               bundles por site (ex.: olx)
  shared/                libs e helpers reutilizáveis
  nexus/                 cliente SignalR / telemetria
```

Documentação local com mais detalhe:

| Path | Conteúdo |
|------|----------|
| [runtime/README.md](runtime/README.md) | Módulo runtime, hosts, API para patches |
| [runtime/hosts/chrome-extension/bridge/readme.md](runtime/hosts/chrome-extension/bridge/readme.md) | Contrato da bridge V1 (protocolo, invocações) |
| [patches/olx/README.md](patches/olx/README.md) | Patch OLX — config, deploy, comportamento |

## Build

Um comando na raiz descobre todos os `bundle.json` e escreve em `dist/`:

```bash
node build.mjs env=dev          # tudo, sourcemaps, sem minify
node build.mjs env=prod         # minify + obfuscate
node build.mjs only=runtime     # bundle isolado
node build.mjs help=true
```

### `bundle.json`

Cada módulo declara bundles ao lado dos sources. Paths de **`entry`** / **`module`** são relativos ao diretório do manifest; paths de **`outfile`** / **`outfiles`** são relativos a **`dist/`**.

```json
{
  "bundles": [
    {
      "name": "runtime",
      "entry": "./runtime.js",
      "outfile": "runtime/runtime.min.js"
    }
  ]
}
```

| Campo | Obrigatório | Default | Uso |
|-------|-------------|---------|-----|
| `name` | sim | — | Filtro CLI `only=<name>` |
| `entry` | sim* | — | Entry esbuild |
| `outfile` | sim** | — | Arquivo de saída em `dist/` |
| `outfiles` | sim** | — | Mesmo build, N destinos em `dist/` |
| `type` | não | `esbuild` | `esbuild` ou `generator` |
| `module` | se generator | — | Módulo com função geradora |
| `export` | se generator | — | Nome da export (ex. `buildServiceWorkerSource`) |
| `format` | não | `iife` | `iife` ou `esm` |

\* `generator` não usa `entry`. \*\* `outfile` ou `outfiles`.

### Manifests ativos

| Manifest | Bundle(s) | Saída em `dist/` |
|----------|-----------|-----------------|
| `runtime/bundle.json` | `runtime` | `runtime/runtime.min.js` |
| `chrome-extension/core/bundle.json` | `bootstrap` | `chrome-extension/core/bootstrap.min.js` |
| `patches/olx/bundle.json` | `olx` | `patches/olx/olx.min.js` |
| `runtime/hosts/xss/delivery/bundle.json` | `xss-installer`, `xss-bootstrapper`, `xss-service-worker` | `runtime/installer.min.js`, etc. |

### Flags globais (`key=value`)

| Flag | Default | Descrição |
|------|---------|-----------|
| `env=dev\|prod` | — | Preset de minify, obfuscate e sourcemap |
| `only=<name>` | — | Build de um bundle |
| `format=iife\|esm` | `iife` | Formato esbuild |
| `target=<target>` | `es2022` | Target esbuild |
| `minify=true\|false` | `true` | Minificação |
| `obfuscate=true\|false` | `false` | Pós-processo com `javascript-obfuscator` |
| `obfuscation=standard\|max` | `standard` | Intensidade da ofuscação |
| `sourcemap=true\|false` | `false` | Source maps |

Flags explícitas sobrescrevem o preset de `env`.

## Chrome extension

`chrome-extension/core/bootstrap.js` é o service worker compartilhado. **Shells** (`monkey-lab`, `tester`) são manifests MV3 mínimos que apontam para `bootstrap.min.js`.

### Atualizar e testar

1. Edite `runtime/env.js` (ou `patches/<site>/config.js` para patches)
2. `node build.mjs env=dev`
3. Copie `dist/chrome-extension/core/bootstrap.min.js` para a pasta da shell (`chrome-extension/shells/<shell>/`), se necessário
4. Recarregue em `chrome://extensions`

Verificação no console da página:

```js
window.w7runtime.host === "CHROME_EXTENSION"
window.w7runtime.chromeExtension.bridge
```

Bridge: [runtime/hosts/chrome-extension/bridge/readme.md](runtime/hosts/chrome-extension/bridge/readme.md).

## Shared

Código reutilizável por runtime, extensão e patches:

| Path | Uso |
|------|-----|
| `shared/libs/` | Bibliotecas vendor (SignalR, xhook, etc.) |
| `shared/helpers/` | Utilitários JS genéricos |

## Nexus

Cliente SignalR para o backend (`NEXUS_HUB_URL` em `runtime/env.js`):

| Arquivo | Uso |
|---------|-----|
| `nexus/nexus.js` | Hub connection, uplink state, reconnect |
| `nexus/telemetry.js` | Eventos de navegação (WIP) |

Depende de `shared/libs/signalr.min.js` e `runtime/env.js`.
