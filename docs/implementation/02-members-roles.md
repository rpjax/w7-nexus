# 02 — Membros, papéis de domínio, deals, Acionistas

**Status:** pendente  
**Depende de:** [01-foundation.md](./01-foundation.md)

## Objetivo

Representar **quem existe na organização** e **mandatos de produto** (não só ACL técnica): Operador, Recrutador, Laranja, Gateways, Contador, Gestor de Operações, Acionista (beneficiário).

## Escopo

| Entra | Não entra |
|-------|-----------|
| Membro + handle único | Equipes / Líder de equipe |
| Concessão de papéis de domínio | Árvore de sub-recrutadores |
| Deal de agenciamento (`operador_pct` + `recrutador_pct` ≤ 100%) | Override de % por Operação |
| Lista de Acionistas (nível 2, %) | Portal rico de Acionista |
| Cadastro mínimo de Laranja (sem Contas ainda — ou stub) | Attrition completo (etapa 09) |

## Use cases (mínimo)

- Conceder / revogar papéis de domínio (Admin).
- Criar/atualizar deal Recrutador↔Operador (validar invariante ≤ 100%).
- CRUD mínimo Acionistas (Admin).
- Queries: “minha carteira” (Recrutador) — pode ser stub de leitura.

## Domínio

- [actors-and-mandates.md](../domain/actors-and-mandates.md)
- [money.md](../domain/money.md) — Rateio nível 3 / autor único do deal
- [glossary.md](../domain/glossary.md) — Handle, Agenciamento, Acionista

## Critérios de pronto

- [ ] Deal inválido (>100%) falha na borda do UC.
- [ ] Handle único no deploy (singleton).
- [ ] Papéis de produto distintos de “só permission string” improvisada — modelo explícito.
