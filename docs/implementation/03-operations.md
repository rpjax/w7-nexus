# 03 — Operações, Script e Store (mínimo)

**Status:** feito  
**Depende de:** [02-members-roles.md](./02-members-roles.md)

## Objetivo

Operação como fronteira de isolamento + **operation key 1:1** ligando Script/Store sem engolir entidades. Divisão interna = mandato, **sem Equipe**.

## Escopo

| Entra | Não entra |
|-------|-----------|
| Ciclo Rascunho ⇄ Ativa ⇄ Pausada → Encerrada (guardas G13) | Entidade Equipe |
| Assign de Operadores à Operação | SQL/analytics genérico no Store |
| Gestor: `gerir_operacao` no escopo | Detalhe fino de canais/releases de Script |
| Script: entidade + key + delivery mínimo (ou stub) | Execução do script pelo Nexus |
| Store: object store keyed (CRUD mínimo) | Mistura Store ↔ dinheiro |
| Cut de gestão da op (config de cima; linha 3) — pode stubear até 06 | |

## Use cases (mínimo)

- Transições de ciclo (com guardas; Encerrada é terminal).
- Assign/unassign Operador.
- Registrar/resolver Script por key (mínimo viável).
- CRUD objeto Store no escopo da key.
- Pausada: Script para de resolver; Store read-only; sem Cobrança nova.

## Domínio

- [operations.md](../domain/operations.md)
- [visibility.md](../domain/visibility.md) — Gestor vê split das ops sob mandato

## Critérios de pronto

- [x] Pausada/Encerrada bloqueiam **nova** Cobrança; Encerrada não reabre.
- [x] Operation key **1:1** com a op; nunca reusada.
- [x] Objects de keys diferentes não se misturam.
- [x] Operação não é container pai obrigatório de Script/Store no modelo.

## Entrega (Refactor)

- BC `Operations/` (Domain/Application/Infrastructure/Presentation/Composition). `Operation` event-sourced (ciclo, assign, cut); Script/Store continuam SQL keyed.
- Mandates: `OperationSpecific` desbloqueado; `IOperationDirectory` + tip×gestão.
- HTTP Admin + edge (resolve Script / Store read).
- UI Admin: lista/detalhe, assign, Script, Store; fine-tune Specific em Contas.
