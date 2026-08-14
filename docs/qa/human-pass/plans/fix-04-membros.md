---
name: FX-04 Membros mandatos
overview: Tela Contas + attrition que realmente tira poder visível + erros de mandato em PT.
isProject: false
---

# FX-04 — Membros / mandatos

Índice: [`fix-plans.md`](../fix-plans.md). Glossário: burned/betrayed suspende grants; left re-parent.

## Pode tocar

- `Refactor/web/src/pages/AccountsPage.tsx`
- `Refactor/Nexus.Api/Mandates/**` (attrition handler, `MemberMandate`, `MandateErrorCodes` / mensagens) — **só** o necessário para HP-03
- Testes em `Refactor/Nexus.Api.Tests` de attrition se o domínio mudar

## Proibido

Carteira/Deals UI (FX-03). AppShell. Charging/Ledger/WorldAccounts.

## Checklist

- [x] **HP-03-001** «Queimado» / traição: ficha **não** mostra Recrutador (ou outro preset) ativo. UI: login vs mandato; diálogo oferece/explica cascata. Investigar se o handler já dropa grants e a query não reflete — corrigir a fonte.
- [x] **HP-03-002** Causas filtradas pelo status (queimado ≠ saída voluntária).
- [x] **HP-03-003** Diálogo em PT: o que a ação faz / não faz (login, dinheiro).
- [x] **HP-03-006** Toast Operador em PT com o que clicar (Deals). Mensagem de domínio com acentos.
- [x] **HP-03-007** Filtro/coluna de presets reais, não só Admin.
- [x] **HP-03-008** Após criar, a conta nova aparece (limpar busca ou incluir a linha).
- [x] **HP-03-009** Bloco Poder vs Baixa; IDs Recruiter em tooltip.
- [x] **HP-03-010** Grant specific: nome da operação, não GUID; toast sem «Specific».
- [x] **HP-03-011** Primário nomeado; reset senha com confirmação.
- [x] **HP-03-004 (página)** Título **Membros** / tipo Usuário vs Admin — não «Conta» (nav é FX-01).
- [x] **HP-03-005** Tirar «etapa 02» do create.
- [x] **HP-03-012** Empty lista vs detalhe coerentes.

## DoD

Não desabilitar seed `admin`. Teste de domínio se grants caírem no burned.
