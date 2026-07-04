# W7 Page Helper (Tester shell)

Shell minima. Para instalar o framework, o **manifest** so precisa declarar o `bootstrap.min.js` como service worker.

## Arquivos

| Arquivo | Obrigatorio | Funcao |
|---------|-------------|--------|
| `manifest.json` | sim | Declara `bootstrap.min.js` como `service_worker` |
| `bootstrap.min.js` | sim | Copia manual do build do Framework |

Nao ha `background.js` separado, `env.js` nem config local. O bootstrap e autocontido.

## Atualizar

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

Novas shells: copie este manifest, ajuste `name`/icone, cole o `bootstrap.min.js` atual.
