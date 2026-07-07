# W7 MonkeyLab (Tester shell)

Shell mínima de **desenvolvimento e testes** do framework W7. Não é o produto final — é o laboratório para injetar o runtime, consumir a API na página e validar ferramentas (ex.: `extensionWatcher` para monitorar rede de extensões).

## Identidade

| Campo | Valor |
|-------|-------|
| Nome | W7 MonkeyLab |
| `short_name` | MonkeyLab |
| Ícone | `logo.svg` → PNGs em `icons/` (16, 48, 128) |
| API na página | `window.w7framework` (ex.: `extensionWatcher.startWatching(id)`) |

## Arquivos

| Arquivo | Obrigatório | Função |
|---------|-------------|--------|
| `manifest.json` | sim | Identidade, ícones, permissões, `bootstrap.min.js` como service worker |
| `bootstrap.min.js` | sim | Cópia manual do build do Framework |
| `logo.svg` | sim | Arte-fonte do ícone |
| `icons/icon-*.png` | sim | Ícones gerados para o Chrome (16, 48, 128) |

Não há `background.js` separado, `env.js` nem config local. O bootstrap é autocontido.

## Regenerar ícones

A partir de `logo.svg` (requer Node + sharp):

```bash
node -e "
const sharp = require('sharp');
const fs = require('fs');
const svg = fs.readFileSync('logo.svg');
for (const size of [16, 48, 128]) {
  sharp(svg, { density: 300 }).resize(size, size).png().toFile('icons/icon-' + size + '.png');
}
"
```

## Atualizar runtime

1. Edite `Extension/Framework/env.js`
2. `node ../../Framework/bundler.mjs env=dev`
3. Copie `Framework/dist/bootstrap.min.js` → esta pasta
4. Copie `Framework/dist/runtime.min.js` → `Nexus/wwwroot/monkeypatches/framework/`

## Manifest (contrato da shell)

```json
{
  "background": { "service_worker": "bootstrap.min.js" },
  "permissions": ["scripting", "webNavigation"],
  "host_permissions": ["<all_urls>"]
}
```

Novas shells: copie este manifest, ajuste `name`/ícone, cole o `bootstrap.min.js` atual.
