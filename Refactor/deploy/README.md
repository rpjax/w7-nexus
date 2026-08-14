# Refactor — deploy

Docker deploy do **Nexus Refactor** (`Refactor/Nexus.Api` + `Refactor/web`) via [@rodrigopjax/dockup](https://github.com/rpjax/npm-dockup).

**Não** use o `dockup/` da raiz do monorepo — esse sobe o legado (`Nexus.Api` + `Nexus.Dashboard`).

## Layout

| Peça | Papel |
|------|--------|
| `Refactor/deploy/` | Onde você roda o dockup (`dockup.json`, `out/`) |
| `--root ..` | Raiz do **Refactor** — base dos `context` de build |
| `Nexus.Api`, `web` | Contexts dentro de `Refactor/` |
| `../Nexus.ReverseProxy` | Traefik compartilhado (fora do Refactor, um nível acima) |

`--root` **não** é a pasta `deploy/`. É a pasta a partir da qual o dockup resolve `container.context`.

## Install (once)

```bash
npm install -g @rodrigopjax/dockup
```

## Setup

```bash
cd Refactor/deploy
cp dockup.example.json dockup.json
```

Edite `dockup.json` — namespace, hosts e env dos containers (connection string, seed Admin, token de criação). `dockup.json` está no `.gitignore`.

```bash
dockup validate --root ..
```

Artefatos: `out/<env>/docker-compose.yml` e `out/<env>/.env`.

No boot da API: tabelas `accounts`, `retired_usernames`, `journal_*` e, se não houver Admin, seed do usuário configurado.

## Dev local (sem push)

Builda as imagens na máquina e gera o compose **sem** enviar nada pro Docker Hub / registry:

```bash
cd Refactor/deploy
dockup deploy --env dev --skip-push --root ..
```

Sobe o stack com as imagens locais:

```bash
cd out/dev
docker compose up -d
```

URLs (dev): **https://nexus.websete.localhost:9143** (o browser chama `/api` no mesmo host; `https://api.nexus.websete.localhost:9143` continua no Traefik).
Portas `9180`/`9143` evitam conflito com outros stacks. Certificado local é self-signed.

Rebuild só de um serviço:

```bash
dockup deploy --env dev --skip-push --only backend --root ..
cd out/dev && docker compose up -d backend
```

Parar:

```bash
cd Refactor/deploy/out/dev
docker compose down
```

## Prod

Build + push das imagens + gera o compose:

```bash
cd Refactor/deploy
dockup deploy --env prod --root ..
```

Leva o artefato pra VPS e sobe puxando as imagens do registry:

```bash
scp -r out/prod/ user@vps:/opt/nexus-refactor
ssh user@vps
cd /opt/nexus-refactor
docker compose pull
docker compose up -d
```

Só regenerar compose (sem build/push):

```bash
dockup deploy --env prod --generate-only --root ..
```

## Flags úteis

| Flag | Efeito |
|------|--------|
| `--skip-push` | Não faz `docker push` — use no **dev local** |
| `--skip-build` | Não faz `docker build` |
| `--generate-only` | Só gera `out/<env>/` (equivale a skip build + skip push) |
| `--only <id>` | Só um container (`backend`, `frontend`, `webserver`) |
| `--dry-run` | Mostra os comandos Docker sem executar |

## Credenciais locais (dev)

Só valem com `ASPNETCORE_ENVIRONMENT=Development` no container. **Não usar em produção.**

| | |
|--|--|
| Usuário | o de `NEXUS_SEED_ADMIN_USERNAME` / `Accounts:SeedAdmin:Username` |
| Senha | a de `NEXUS_SEED_ADMIN_PASSWORD` |
| Chave mestra (bootstrap extra) | `NEXUS_ADMIN_ACCOUNT_CREATE_TOKEN` |

O seed **não** recria Admin se já existir um.

## O que testar (etapa 01)

- Entrar com o Admin semente
- Criar conta comum (só identidade) e outra Admin (chave mestra)
- Trocar o usuário — o nome antigo não pode ser reusado
- Desabilitar conta — o usuário continua ocupado; sign-in da conta desabilitada falha
- Não dá para desabilitar/revogar o último Admin
- Sem self-sign-up de usuário; aba “Bootstrap admin” é emergência

Mandatos (Operador, Recrutador, …) ainda não existem — só o preset Admin.

Docs do dockup: [github.com/rpjax/npm-dockup](https://github.com/rpjax/npm-dockup)
