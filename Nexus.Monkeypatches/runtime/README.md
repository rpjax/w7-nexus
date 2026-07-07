# W7 Runtime

Módulo central: instala `window.w7runtime`, orquestra monkeypatches e expõe API estável para patches e hosts.

Build e estrutura do repositório: [README.md](../README.md).

## Arquivos principais

| Arquivo | Papel |
|---------|-------|
| `env.js` | Versão, nomes de API, endpoints (`RUNTIME_ENDPOINT`, `NEXUS_HUB_URL`) |
| `host.js` | Constantes de host (`CHROME_EXTENSION`, `XSS`, `MITM_INJECTION`) |
| `initializer.js` | Monta `window.w7runtime` conforme o host |
| `runtime.js` | `start()` / `stop()` + monkeypatch-manager |
| `api.js` | API para patches (`getLogger()`, `getState()`, `watchExtension()`, …) |
| `state.js` | Persistência em `localStorage` com prefixo `w7runtime:state:` |
| `logger.js` | Logger namespaced |
| `monkeypatch-manager.js` | Carrega patches declarados por host |

Patches importam `api.js` — não embutem o runtime. O runtime deve estar instalado na página antes do patch executar.

## Hosts

Adaptadores de **entrega** e comportamento específico por canal de instalação:

| Host | Path | Estado |
|------|------|--------|
| `CHROME_EXTENSION` | `hosts/chrome-extension/` | Bridge MAIN ↔ ISOLATED ↔ SW |
| `XSS` | `hosts/xss/delivery/` | Installer, bootstrapper, service worker |
| `MITM_INJECTION` | `hosts/mitm/` | Placeholder |

### Chrome extension

- **Bridge** — protocolo event-driven entre MAIN world, relay ISOLATED e service worker. Documentação completa: [hosts/chrome-extension/bridge/readme.md](hosts/chrome-extension/bridge/readme.md).
- **`installChromeExtensionBridge()`** — chamado pelo initializer quando `host === CHROME_EXTENSION`; API pública em `window.w7runtime.chromeExtension.bridge`.
- **Bootstrap** (em `chrome-extension/core/`) injeta relay, runtime e patches conforme `env.js`.

### XSS

Artefatos em `hosts/xss/delivery/` (`installer.js`, `bootstrapper.js`, `service-worker.js`). Build via `bundle.json` local → `dist/runtime/*.min.js`.

## API para patches (`api.js`)

Funções de conveniência que leem de `window.w7runtime` já instalado:

- `getLogger()`, `getState()`, `createState()`, `deleteState()`
- `watchExtension()`, `unwatchExtension()`

Contratos de nomes de propriedade ficam em `env.js` (`RUNTIME_API`, `STATE_MANAGER_API`, `EXTENSION_WATCHER_API`).
