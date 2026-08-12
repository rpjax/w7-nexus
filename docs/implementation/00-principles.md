# 00 — Princípios do processo de implementação

**Status:** feito (meta-documento)

## Objetivo

Fixar *como* implementamos, para as etapas 01–09 não divergirem de estilo nem de escopo.

## Fontes de verdade

| Camada | Doc / código |
|--------|----------------|
| Produto / negócio | `docs/domain/*` |
| Forma do backend | `docs/architecture-blueprint.md` |
| Ordem e fatias | `docs/implementation/*` (este folder) |
| Código alvo | `Refactor/Nexus.Api`, `Refactor/web` |

Conflito produto vs código legado: **domínio + blueprint vencem**; código fora do padrão = dívida, migrar quando a fatia tocar.

## Definition of Done (por etapa)

Uma etapa está **feita** quando:

1. Use cases da etapa existem como **portas + handlers** (um UC = um port + um handler).
2. Invariantes listadas na etapa são **testáveis** (pelo menos testes de aplicação/domínio cobrindo o caminho feliz + 1–2 violações).
3. HTTP contracts (se houver) estão em `Presentation/` e **não** mutam agregados no controller.
4. Doc da etapa atualizado (`Status: feito`) + linha no [README.md](./README.md).
5. Nada de regra de negócio “só no chat” — se descobriu regra, atualizou `docs/domain/`.

## O que **não** é DoD de etapa

- UI polida de dashboard.
- Event sourcing “completo” (projeções sofisticadas, replay UI) — v1 pode ser append-only + estado atual.
- Integração automática banco/gateway (v1 = Contador declara).
- Multi-tenant, equipes, líderes, API automática de hops.

## Fatia vertical mínima

Para cada capacidade nova, preferir:

```text
Port In → Handler → Domain (invariantes) → Port Out (repo)
                ↘ Presentation HTTP (se necessário)
                ↘ Composition (DI)
```

Evitar: “criar todas as pastas Infrastructure do bounded context” sem um UC que as use.

## Dois livros (lembrete operacional)

- **Modelo:** Conta (saldo + TX) sem ownership; Claim no ledger com localização.
- **Use case de caixa:** sempre alimenta livro-mundo **e** ledger no mesmo ato.
- **Use case só-ledger:** permitido (ex. cut in-place) quando não há movimento de caixa.
- **Reconciliação:** única porta para alinhar Nexus ↔ realidade observada.

## Papéis na Application

Roles só em: `Ports/In`, `UseCases`, `Authorization`, `Presentation/Http` — nunca árvore role-first na raiz do domínio.

## Status possíveis (etapas)

`pendente` · `em curso` · `feito` · `adiado` (com motivo)
