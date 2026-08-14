---
name: FX-09 Acionistas
overview: Toast com acentos, diálogo nomeia quem sai, copy Acionista ≠ Admin.
isProject: false
---

# FX-09 — Acionistas

Índice: [`fix-plans.md`](../fix-plans.md).

## Pode tocar

- `Refactor/web/src/pages/ShareholdersPage.tsx`
- Mensagem de soma > 100%: **preferir** string no cliente via `reportError` se a API vier sem acento; **ou** só o handler de stake **se FX-04 não o estiver a editar**. Não tocar `MemberMandate` / attrition.

## Proibido

AppShell, AccountsPage, WorldAccountsPage.

## Checklist

- [x] **HP-08-002** Toast soma ≤ 100% com acentos (PT-BR).
- [x] **HP-08-003** Diálogo remover: username + %.
- [x] **HP-08-006 (página)** Coluna «Usuário», não «Conta».
- [x] **HP-08-010** Copy: fatia residual, sem poder de gestão; não «nível 2» solto; não parecer dono da casa.
- [x] **HP-08-004 (esta página)** Empty no padrão da casa.

## DoD

Não estourar 100% sem mensagem legível. Remover não é anónimo.
