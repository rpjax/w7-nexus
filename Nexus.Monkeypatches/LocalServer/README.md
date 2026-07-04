# LocalServer

Servidor Node estático mínimo para desenvolvimento local do runtime Extension.

## Setup

1. Build do runtime:

```bash
node ../Extension/Framework/bundler.mjs env=dev
```

2. Copie os artefatos para o wwwroot local:

```
Extension/Framework/dist/runtime.min.js
  → wwwroot/monkeypatches/framework/runtime.min.js

Patches/OLX/dist/olx.min.js
  → wwwroot/monkeypatches/patches/olx.min.js
```

3. Aponte `Extension/Framework/env.js` para o server local:

```js
export const API_BASE_URL = "http://127.0.0.1:444";
```

4. Rebuild do bootstrap (URL fica baked no bundle) e copie `bootstrap.min.js` pro shell.

## Rodar

```bash
node server.mjs
node server.mjs --port 444 --host 127.0.0.1
node server.mjs --help
```

## URLs

| Recurso | URL |
|---------|-----|
| Runtime | `http://127.0.0.1:444/monkeypatches/framework/runtime.min.js` |
| Patch (por origin) | `http://127.0.0.1:444/monkeypatches?origin=https://www.olx.com.br` |

O endpoint `/monkeypatches?origin=...` resolve o bundle de patch pelo `origin` da pagina (ex: OLX → `patches/olx.min.js`).

CORS e headers de private network access incluídos para páginas de terceiros.
