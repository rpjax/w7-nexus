---
name: FX-01 Chrome novato
overview: Nav, Início, Auth, Perfil, 404 — copy humana e «Conta» só no chrome. Paralelo com FX-02–09.
isProject: false
---

# FX-01 — Chrome / novato

Índice: [`docs/qa/human-pass/fix-plans.md`](../fix-plans.md). Conclusões: [`conclusions.md`](../conclusions.md).

## Pode tocar

- `Refactor/web/src/App.tsx` (rota 404 no shell)
- `Refactor/web/src/layouts/AppShell.tsx`
- `Refactor/web/src/pages/HomePage.tsx`
- `Refactor/web/src/pages/AuthPage.tsx`
- `Refactor/web/src/pages/ProfilePage.tsx`

## Proibido

Qualquer outra `pages/*`. Backend. `data-table`. Findings/conclusions.

## Checklist (marcar no plano)

- [x] **HP-01-001** Atalhos do Início em PT humano; jargão só em subtítulo se preciso. Não falar operation key / webhook Paga / etapa.
- [x] **HP-01-003 (chrome)** Kickers «Resumo da conta» / perfil CONTA → Sessão / Identidade / Usuário. Nav Pessoas **Contas** → **Membros** (ou Identidades).
- [x] **HP-02-005** Carteira + Deals no grupo **Pessoas** (não Eu).
- [x] **HP-02-007** Nav carteira: **Minha gente** (ou Downline) — não soa saldo.
- [x] **HP-06-008 (nav)** **Claims** → **Direitos** (ou «Claims (a receber)»); H1 fica com FX-07.
- [x] **HP-02-002 (nav)** **Deals** → **Agenciamento**.
- [x] **HP-08-005** `document.title` por rota; nav e intenção alinhadas (H1 das outras páginas não editar).
- [x] **HP-08-007** Início Admin: kicker não «ADMINISTRAÇÃO» no grupo Eu.
- [x] **HP-08-001** Catch-all: página 404 no shell com o path, não sumir no Início.
- [x] **HP-08-009** Overlay mobile: fechar fácil (área / botão).
- [x] **HP-01-004** Bootstrap admin atrás de «Problemas para entrar?»; sem pré-preencher senha/chave.
- [x] **HP-01-005** Um sítio de erro no login; «Usuário» com acento (toast via `reportError` já unificado — não duplicar se o form basta).
- [x] **HP-01-006** Auth sem «token de acesso» no painel.
- [x] **HP-01-007** Perfil: um aviso de nome; senha «encerra outras sessões»; diálogo se mudar usuário.
- [x] **HP-01-008** Sair sem deixar `?redirect=` técnico visível se possível (navigate limpo).

## DoD

Login/logout, nav, Início e Perfil usáveis por novato. Sem overlap com FX-02 (extrato).
