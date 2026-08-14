# 01 — Foundation (Accounts, Auth, Authorization, Journal)

**Status:** feito (`Refactor/Nexus.Api`)

## Objetivo

Estabilizar a base sobre a qual o domínio de produto se apoia: **Account = identidade de login** (usuário), autenticação local, Admin semente, e Journal como log operacional de **escritas**.

## Canon desta etapa

- Uma pessoa = um login; sem `tenant_id`. **Não** há entidade Membro paralela — o login *é* a Account.
- `Account.Username` = **usuário** de login: único (`lower(username)`), nunca reusado após troca.
- Desabilitar a conta **não** libera o usuário (a row permanece).
- Onboarding **não** é self-serve: criação de conta comum só via Admin (`ICreateAccountUseCase`).
- Admin semente no deploy se zero Administrators (`NEXUS_SEED_ADMIN_USERNAME` / `NEXUS_SEED_ADMIN_PASSWORD`, com o token de criação já existente como guarda). `POST /api/authentication/sign-up/admin` permanece como bootstrap de emergência.
- Journal **não** é event sourcing. Fatos de mutação de Account vivem no stream Marten (`accounts` identity). Journal fica log de sistema; sem hash-chain, sem log de leitura, sem HTTP de auditoria nesta etapa.

## Já existe no código

| Área | O quê |
|------|--------|
| `Accounts/` | create/enable/disable, grant/revoke role, password, username + `retired_usernames`; stream Marten + `account_secrets` |
| `Authentication/` | sign-in, sign-up **admin** (token), profile; **sem** sign-up público de usuário |
| `Authorization/` | `IOperationResult`; único papel: **Admin** (preset raiz). Sem `OlxOperator` / `StrawMan` / `Operator`. Demais presets = Mandato na etapa 02. |
| `Journal/` | wired em `Program.cs`; **não** é segunda cópia do stream de Account |
| Testes | `Refactor/Nexus.Api.Tests` (username único/reuso, seed, last-admin, sign-in disabled, signup ausente) |

## Ficou para a etapa 02

- Mandato = capacidade × escopo; presets Gestor / Contador / Recrutador / …; atenuação; `conceder_mandato`.
- Onboard com mandato (esta etapa só impede self-serve).
- Attrition `queimado | saiu | traiu` (hoje: `Active` / `Disabled`).
- Deal Recrutador, Acionista %, payout Conta, Laranja completo.
- Tamper-evidence; log de **leituras**; `ler_log_auditoria`.
- `Permissions` no JWT ainda não autorizam nada — não expandir aqui; 02 pode reusar como capacidades.

Grant de role continua in-place (sem atenuação). 02 substitui por mandato-evento.

## Fora

- Modelar Conta financeira, Claim, Cobrança.
- Script/Store.
- UI completa de produto (o web Refactor cobre identidade, Admin e contas desta etapa; mandatos na 02).

## Domínio

- [actors-and-mandates.md](../domain/actors-and-mandates.md) — identidade, username aposentado, Admin semente.
- [auditability.md](../domain/auditability.md) — log ≠ ES.
- [vision.md](../domain/vision.md) — singleton.

## Seed / launch

- Postgres: `ConnectionStrings:AccountsDb` ou `NEXUS_ACCOUNTS_DB_CONNECTION`.
- Seed Admin (idempotente): `NEXUS_SEED_ADMIN_USERNAME` + `NEXUS_SEED_ADMIN_PASSWORD` (ou `Accounts:SeedAdmin:*`), **e** `NEXUS_ADMIN_ACCOUNT_CREATE_TOKEN` / `Accounts:AdministratorCreationToken` configurado.
- JWT local: `Jwt:*` em `appsettings.json`.

## Critérios de pronto

- [x] Dá pra autenticar Admin (semente ou bootstrap) e conta comum localmente.
- [x] Account **é** a identidade; etapa 02 cresce Mandato em cima desta Account, sem segundo login.
- [x] Não existe self-sign-up de usuário.
- [x] Nome de usuário não é reutilizado após rename/disable.
- [x] Journal drena fatos de mutação de Account (escritas).
- [x] Testes da lista da etapa passando em `Refactor.Nexus.Api.Tests`.
