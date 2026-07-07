# OLX Patch

Monkeypatches para `olx.com.br` — DOM patching, cache de vitimas via Nexus API.

## Estrutura

```
patches/olx/
  main.js                 ← entry do bundle
  config.js               ← API_BASE_URL, OPERATION_ID (baked no build)
  cache.js
  nexus/                  ← integracao com API Nexus (victim service)
  monkeypatches/
    ad-details/
    checkout-review/
  libs/                   ← dependencias locais (ex: qrcode)
  SAMPLES/                ← HTML de referencia (nao entra no bundle)
  bundle.json             ← declaracao de build
```

Artefato gerado: `dist/patches/olx/olx.min.js`

## Build

Na raiz de `Nexus.Monkeypatches/`:

```bash
node build.mjs only=olx env=dev
node build.mjs only=olx env=prod
```

Flags globais (`env`, `only`, `minify`, `obfuscate`, …): ver [README.md](../../README.md#build).

## Deploy local

1. `node build.mjs only=olx env=dev` (debug) ou `node build.mjs only=olx env=prod` (release)
2. Copie `dist/patches/olx/olx.min.js` para o server estatico:

```
LocalServer/wwwroot/monkeypatches/patches/olx.min.js
```

3. URL servida:

```
http://127.0.0.1:444/monkeypatches/patches/olx.min.js
```

Ajuste `config.js` (`API_BASE_URL`, `OPERATION_ID`) antes do build — valores ficam embutidos no bundle.

## Runtime

O patch é carregado pelo **runtime** (`monkeypatch-manager`) a partir do endpoint Nexus. O bundle IIFE executa ao ser injetado na página:

- inicializa caches (`nexus/init.js`)
- roda `patchAdDetailsAsync` e `patchCheckoutReviewPageAsync` em intervalo

## Nota sobre `dist.js`

`dist.js` na raiz e legado (bundle antigo). Use `build.mjs only=olx` → `dist/patches/olx/olx.min.js`.
