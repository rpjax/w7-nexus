# Nexus Deploy

Declarative infrastructure in JSON. Run all commands from this directory (`deploy/`).

Root keys in `nexus.config.json` are environment names; each env defines `namespace`, `network`, and `containers[]`.

## Setup

```bash
cd deploy
cp nexus.config.example.json nexus.config.json
```

Edit `dev.containers` / `prod.containers` — each entry describes a container (ports, env, volumes, build context).

App secrets stay in each project (`appsettings.*`, `.env.*`). TLS terminates only at `webserver`.

## Run

```bash
cd deploy
node deploy.mjs env=prod
node deploy.mjs env=dev only=backend
node deploy.mjs help
```

The script runs these phases in order:

1. **Preflight** — working directory, Docker daemon, Hub credentials
2. **Config** — load and validate `nexus.config.json`
3. **Build** — `docker build --build-arg ENV=<env>`
4. **Push** — `docker push <namespace>/<image>:<env>`
5. **Generate** — `out/<env>/docker-compose.yml` + `.env`
6. **Validate** — `docker compose config`

On failure, the log shows the phase, command, exit code, and the last lines of output.

## Config schema

```json
{
  "dev": {
    "namespace": "alottafagina",
    "network": "nexus-dev",
    "containers": [ ... ]
  },
  "prod": {
    "namespace": "alottafagina",
    "network": "nexus",
    "containers": [ ... ]
  }
}
```

| Field | Compose |
|-------|---------|
| `namespace` | Docker Hub namespace for images |
| `network` | Docker network name for the stack |
| `ports[]` | `host:container` on the VPS |
| `expose[]` | internal Docker network only (documentation) |
| `env[]` | `environment:` |
| `volumes[]` with `name` | named volume |
| `volumes[]` with `host` | bind mount |
| `dependsOn[]` | `depends_on:` |

## VPS

```bash
scp -r out/prod/ user@vps:/opt/nexus
ssh user@vps
cd /opt/nexus
docker compose pull
docker compose up -d
```

`dev` uses ports `8080`/`8443` on webserver so it does not bind host `80`/`443`.

## Project env files

| Project | Per-env config |
|---------|----------------|
| API | `appsettings.json`, `appsettings.Development.json` |
| Frontend | `.env.development`, `.env.production` |
| Webserver | routing via `env[]` on the `webserver` container in JSON |
