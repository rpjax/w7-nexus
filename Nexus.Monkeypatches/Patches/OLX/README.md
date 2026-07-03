# OLX Patch

Monkeypatches para `olx.com.br` — DOM patching, cache de vitimas via Nexus API.

## Estrutura

```
Patches/OLX/
  main.js                 ← entry do bundle
  config.js               ← API_BASE_URL, OPERATION_ID (baked no build)
  cache.js
  nexus/                  ← integracao com API Nexus (victim service)
  monkeypatches/
    ad_details/
    checkout_review/
  libs/                   ← dependencias locais (ex: qrcode)
  SAMPLES/                ← HTML de referencia (nao entra no bundle)
  bundler.mjs             ← CLI de build
  dist/
    olx.min.js            ← artefato gerado
```

## Build

```bash
node bundler.mjs
node bundler.mjs --help
node bundler.mjs --no-minify --sourcemap
```

### Flags

| Flag | Default | Descricao |
|------|---------|-----------|
| `--out-dir <path>` | `dist` | Pasta de saida |
| `--format iife\|esm` | `iife` | Formato esbuild |
| `--target <target>` | `es2022` | Target esbuild |
| `--minify` / `--no-minify` | minify on | Minificacao |
| `--obfuscate` | off | Pos-processo com `javascript-obfuscator` |
| `--sourcemap` | off | Gera source map |

## Deploy local

1. `node bundler.mjs`
2. Copie `dist/olx.min.js` para o server estatico:

```
LocalServer/wwwroot/monkeypatches/patches/olx.min.js
```

3. URL servida:

```
http://127.0.0.1:444/monkeypatches/patches/olx.min.js
```

Ajuste `config.js` (`API_BASE_URL`, `OPERATION_ID`) antes do build — valores ficam embutidos no bundle.

## Runtime

O patch e carregado pelo **Extension Framework** (`monkeypatch_manager`) a partir do endpoint Nexus. O bundle IIFE executa ao ser injetado na pagina:

- inicializa caches (`nexus/init.js`)
- roda `patchAdDetailsAsync` e `patchCheckoutReviewPageAsync` em intervalo

## Nota sobre `dist.js`

`dist.js` na raiz e legado (bundle antigo). Use `bundler.mjs` → `dist/olx.min.js`.
