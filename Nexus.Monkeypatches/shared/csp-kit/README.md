Proxy API — spec em `proxy/ENVELOPE.md`

```
<PROXY_URL>?url=https://minha-api.com/api/foo/bar
```

Resposta sempre: `#csp-kit-proxy-envelope` com JSON `{ v, status, contentType, body, error? }`.

Decoder: `internals/proxy-envelope.js`
