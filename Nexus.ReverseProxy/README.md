# Nexus.ReverseProxy (webserver)

Traefik edge proxy. Terminates TLS and routes by host via env vars set in `nexus.deploy.json`:

| Variable | Purpose |
|----------|---------|
| `FRONTEND_HOST` | Public host for the frontend |
| `BACKEND_HOST` | Public host for the backend |
| `FRONTEND_UPSTREAM` | Internal URL (e.g. `http://frontend:80`) |
| `BACKEND_UPSTREAM` | Internal URL (e.g. `http://backend:8080`) |
| `LETSENCRYPT_EMAIL` | ACME account email (substituted into `traefik.yml` at startup) |

At container start, `docker-entrypoint.sh` runs `envsubst` on `traefik.yml.template` and `dynamic/routes.yml.template`.

## Build

```bash
cd deploy
node deploy.mjs env=prod only=webserver
```
