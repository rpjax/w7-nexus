# CSP-KIT Proxy Envelope (v1)

Formato fixo e agnóstico de infra. **Todo proxy** (`google/`, `cloudflare/`, self-hosted) deve devolver este envelope.

## Wire format

Resposta HTTP com `Content-Type: text/html; charset=utf-8` contendo **apenas** o fragmento:

```html
<div id="csp-kit-proxy-envelope">{"v":1,"status":200,"contentType":"text/plain","body":"UE9ORyE="}</div>
```

| Constante | Valor |
|-----------|-------|
| Element ID | `csp-kit-proxy-envelope` |
| Versão (`v`) | `1` |

O conteúdo do elemento é JSON UTF-8 (texto puro, sem HTML interno).

## Schema JSON

```json
{
  "v": 1,
  "status": 200,
  "contentType": "text/plain",
  "body": "<base64>",
  "error": "opcional — mensagem de erro do proxy"
}
```

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `v` | number | sim | Versão do protocolo (sempre `1`) |
| `status` | number | sim | Status HTTP upstream ou do proxy |
| `contentType` | string | sim | MIME type do body decodificado |
| `body` | string | sim | Payload sempre em **base64** (UTF-8) |
| `error` | string | não | Erro no nível do proxy (não upstream) |

## Decodificação (cliente)

1. Buscar `#csp-kit-proxy-envelope`
2. `JSON.parse(element.textContent)`
3. Validar `v === 1`
4. `atob(body)` → string UTF-8

Implementação: `internals/proxy-envelope.js`

## Rotas internas

```
GET ?path=/___internal__/ping
```

Resposta: envelope com `body` base64 de `PONG!`, `contentType: text/plain`, `status: 200`.

## Request (proxy)

```
GET  ?url=https://api.exemplo.com/path
POST ?url=...  ou body JSON { "url", "method", "headers", "body" }
```
