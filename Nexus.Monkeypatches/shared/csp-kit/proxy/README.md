# CSP-KIT Proxy

Proxy HTTP para contornar `connect-src` em páginas com CSP restritiva.

**Spec do envelope:** [ENVELOPE.md](./ENVELOPE.md)

Todo proxy implementa a mesma resposta:

```html
<div id="csp-kit-proxy-envelope">{"v":1,"status":200,"contentType":"text/plain","body":"..."}</div>
```

Decoder cliente: `internals/proxy-envelope.js`

## Implementações

| Path | Plataforma | Redirect? |
|------|------------|-----------|
| `google/index.gs` | Google Apps Script (HtmlService) | Não |
| `cloudflare/worker.js` | Cloudflare Worker | Não |

## Request

```
GET  ?url=https://api.exemplo.com/path
GET  ?path=/___internal__/ping
POST body JSON { "url", "method", "headers", "body" }
```

## Deploy GAS

1. Cole `google/index.gs` no Apps Script
2. **Implantar → Aplicativo da Web**
3. Executar como: **Eu** · Quem tem acesso: **Qualquer pessoa**
4. Opcional: `ALLOWED_HOSTS` nas propriedades do script
