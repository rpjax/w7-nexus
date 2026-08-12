# 03 — Operações, Script e Store (mínimo)

**Status:** pendente  
**Depende de:** [02-members-roles.md](./02-members-roles.md)

## Objetivo

Operação como fronteira de isolamento + **operation key** ligando Script/Store sem engolir entidades.

## Escopo

| Entra | Não entra |
|-------|-----------|
| Ciclo Rascunho → Ativa → Pausada → Encerrada | Equipes |
| Assign de Operadores à Operação | SQL/analytics genérico no Store |
| Gestor de Operações: mandato sobre ops assignadas | Detalhe fino de canais/releases de Script |
| Script: entidade + key + delivery mínimo (ou stub) | Execução do script pelo Nexus |
| Store: object store keyed (CRUD mínimo) | Mistura Store ↔ dinheiro |

## Use cases (mínimo)

- Criar/atualizar ciclo de vida da Operação (Admin / Gestor).
- Assign/unassign Operador.
- Registrar/resolver Script por key (mínimo viável).
- CRUD objeto Store no escopo da key.

## Domínio

- [operations.md](../domain/operations.md)
- [visibility.md](../domain/visibility.md) — Gestor vê split das ops sob mandato (split em si vem depois)

## Critérios de pronto

- [ ] Pausada bloqueia **nova** Cobrança (contrato com etapa 04; pode ser flag/teste de integração depois).
- [ ] Objects de keys diferentes não se misturam.
- [ ] Operação não é container pai obrigatório de Script/Store no modelo.
