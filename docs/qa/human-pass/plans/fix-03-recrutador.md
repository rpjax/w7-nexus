---
name: FX-03 Recrutador
overview: Carteira e Deals — próximo passo, % válidos, copy humana. Sem Mandates domain (toasts mapear na página).
isProject: false
---

# FX-03 — Recrutador (carteira + agenciamento)

Índice: [`fix-plans.md`](../fix-plans.md).

## Pode tocar

- `Refactor/web/src/pages/CarteiraPage.tsx`
- `Refactor/web/src/pages/DealsPage.tsx`

## Proibido

`AppShell` (nav é FX-01). `AccountsPage`. `Mandates/**` (FX-04). Mapear toast `AgencyDeal` / `pct=0` **nesta UI** para PT + CTA Deals.

## Checklist

- [x] **HP-02-001** Empty da carteira: próximo clique = agenciar (link `/dashboard/deals`). Conceder Operador: toast humano se falhar.
- [x] **HP-02-002** H1/subtítulo humanos («Agenciamento»); fórmula em «Como funciona», não no hero.
- [x] **HP-02-003** Bloquear salvar sem % 0–100 e soma ≤ 100; diálogo nunca «(% / %)».
- [x] **HP-02-004** Linha da carteira: pessoa + fatia + atalho ao deal. Combo «Usuário» / «Pessoa», não «Conta». Copiar ID ou equivalente visível se ainda faltar.
- [x] **HP-02-006** Diálogos/toasts: «Salvar vínculo», «Encerrar agenciamento», sem «Preset concedido» cru.

## DoD

Recrutador cria deal com % válidos e acha a downline ligada ao agenciamento.
