---
name: FX-08 Livro-mundo
overview: Gateway só com Laranja; esconder emissão fora de Gateway; Lost morto; reconciliação didática.
isProject: false
---

# FX-08 — Livro-mundo

Índice: [`fix-plans.md`](../fix-plans.md).

## Pode tocar

- `Refactor/web/src/pages/WorldAccountsPage.tsx`
- `Refactor/Nexus.Api/WorldAccounts/**` e guards de emissão se a API já recusa e a UI mente
- `Ledger` **só** se ListExposure empty copy exigir — preferir UI

## Proibido

AppShell. Charges/Claims. AccountsPage.

## Checklist

- [x] **HP-07-001** Combo Laranja: só quem atua como Laranja; Abrir Gateway disabled sem Laranja; toast produto; label não «Conta» para login.
- [x] **HP-07-002** Eixo emissão só em Gateway; Banco/Crypto/Payout sem Ok/Bloqueada.
- [x] **HP-07-003** Reconciliar: copy humana; se não há claims, apontar observação crédito/débito em vez de invariante crua.
- [x] **HP-07-004** Já Perdido: desabilitar Perdido, observação, congelar conforme domínio.
- [x] **HP-07-005** Congelar com confirmação.
- [x] **HP-07-006** Empty exposição: direitos presos ≠ saldo observado. PT-BR «registrada».
- [x] **HP-07-007 (página)** Cards/labels: Conta = livro-mundo; não chamar login de Conta.
- [x] **HP-07-008** Transações Crédito/Débito; causa humana; memo próprio no lost.
- [x] **HP-07-009** Estado Abrir ≠ detalhe (quota não vaza); cut com %; Payout em PT.
- [x] **HP-07-010** Lost copy humana; esconder emissão se Perdido.
- [x] **HP-07-011** Toasts/empty PT-BR com acentos.

## DoD

Não Lost em contas sem prefixo de teste. Gateway não abre com login comum.
