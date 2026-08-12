# 01 — Foundation (Accounts, Auth, Authorization, Journal)

**Status:** em curso (já parcialmente no `Refactor/Nexus.Api`)

## Objetivo

Estabilizar a base sobre a qual o domínio de produto se apoia: identidade de login, papéis técnicos de acesso, JWT, e infraestrutura compartilhada (DB, Journal).

## Já existe (ponto de partida)

- `Accounts/` — create/enable/disable, roles/permissions, password.
- `Authentication/` — sign-in, sign-up admin/user, profile.
- `Authorization/` — `IOperationResult`, roles, request context.
- `Journal/` — append/drain/read (útil para auditoria/ES pragmático depois).
- Persistence Postgres + composition root em `Program.cs`.

## Nesta etapa (fechar)

| Entrega | Notas |
|---------|--------|
| Inventário do que já cobre vs domínio | Mapeamento Account (login) ≠ Membro de domínio (pode coincidir depois) |
| Papéis técnicos alinháveis aos papéis de produto | Admin já existe; Contador/Gateways/Gestor/… entram na etapa 02 |
| Convenção de erro / `IOperationResult` | Reusar; não inventar segundo framework |
| Health / launch local documentado o suficiente pra próxima fatia | Mínimo |

## Fora

- Modelar Conta financeira, Claim, Cobrança.
- Script/Store.
- UI completa.

## Domínio

- [actors-and-mandates.md](../domain/actors-and-mandates.md) — Dono vs Admin; identidade mínima (handle).
- [vision.md](../domain/vision.md) — singleton, cargos de confiança.

## Critérios de pronto

- [ ] Dá pra autenticar Admin e conta comum localmente.
- [ ] Está claro no doc/código o que é **Account de login** vs futuros **Membros/papéis de domínio**.
- [ ] Próxima etapa (02) tem âncora de identidade sem reescrever Auth.
