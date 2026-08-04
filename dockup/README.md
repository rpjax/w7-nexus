# Nexus deploy

Docker deploy for Nexus via [@rodrigopjax/dockup](https://github.com/rpjax/npm-dockup).

## Install (once)

```bash
npm install -g @rodrigopjax/dockup
```

## Setup

```bash
cd dockup
cp nexus.dockup.example.json nexus.dockup.json
```

Edit `nexus.dockup.json` — namespace, hosts, and container env. App secrets stay in each project (`appsettings.*`, `.env.*`).

## Run

Always from `dockup/` with `--root ..` (repo root where `Nexus.Api`, `Nexus.Dashboard`, etc. live):

```bash
cd dockup
dockup validate --root ..
dockup deploy --env prod --root ..
dockup deploy --env dev --only backend --root ..
dockup deploy --env prod --generate-only --root ..
```

## VPS

```bash
scp -r out/prod/ user@vps:/opt/nexus
ssh user@vps
cd /opt/nexus
docker compose pull
docker compose up -d
```

Full dockup docs: [github.com/rpjax/npm-dockup](https://github.com/rpjax/npm-dockup)
