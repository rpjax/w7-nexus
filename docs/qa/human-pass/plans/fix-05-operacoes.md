---
name: FX-05 Operações Store
overview: Store 500, confirmação Encerrar, empty de trilho/operador, copy de cut/chave.
isProject: false
---

# FX-05 — Operações

Índice: [`fix-plans.md`](../fix-plans.md).

## Pode tocar

- `Refactor/web/src/pages/OperationsPage.tsx`
- `Refactor/Nexus.Api/Operations/**` necessário para **list Store 500** (`PostgresStoreObjectRepository` / `ListStoreObjectsHandler`)
- Teste mínimo do list se houver infra de teste

## Proibido

WorldAccountsPage, ChargesPage, Mandates.

## Checklist

- [x] **HP-04-001** List Store não 500; depois de salvar, a lista mostra o objeto (não empty + erro). Remover visível quando houver item.
- [x] **HP-04-002** Associar: filtrar elegíveis **ou** empty com pré-req + link Deals/Membros. Toast PT (mapear na página se a API ainda falar AgencyDeal).
- [x] **HP-04-003** Empty trilhos: próximo passo livro-mundo / conta de emissão.
- [x] **HP-04-004** Encerrar: diálogo irreversível Cancelar / Confirmar.
- [x] **HP-04-005** `op_…` / UUID fora da cara; copiar em avançado.
- [x] **HP-04-006** «Percentual de gestão»; erro 0–100 ou vazio, com acentos.
- [x] **HP-04-007** Mobile: detalhe em fluxo que não compete com Encerrar (lista some ou ecrã próprio simples).
- [x] **HP-04-008** Script Resolver: copy que ensina; não só eco «edge».

## DoD

Abrir detalhe de `qa04-frente` ou operação nova: Store lista sem 500. Encerrar pede confirmação.
