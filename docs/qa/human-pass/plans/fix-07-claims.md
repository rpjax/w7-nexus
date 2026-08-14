---
name: FX-07 Claims hops
overview: Aviso de perda no hop, ficha, status PT, estorno sem filtro alheio.
isProject: false
---

# FX-07 — Claims / hops

Índice: [`fix-plans.md`](../fix-plans.md).

## Pode tocar

- `Refactor/web/src/pages/ClaimsPage.tsx`
- `Refactor/Nexus.Api/Ledger/**` — RegisterHop (aviso/causa se perda > 0); mensagens PT
- Testes de hop se a regra de UI exigir causa

## Proibido

AppShell (nav Direitos = FX-01). ChargesPage (estorno da ficha = FX-06). WorldAccountsPage.

## Checklist

- [x] **HP-06-001** Diálogo do hop: mostrar perda (origem − destinos); se perda > 0, avisar e pedir causa **ou** deixar resto na origem — alinhado ao domínio (não silenciar perda).
- [x] **HP-06-002** Bloquear origem=destino e 0 destinos **antes** de Confirmar.
- [x] **HP-06-003** Status PT (ativo, visível separado). Filtros Paga/Aberta/Materializada. Kind Banco não `Bank`.
- [x] **HP-06-004** Ficha/drawer: beneficiário, Conta, montante, status, visível, cobrança, hops.
- [x] **HP-06-005** Relatório controlado visível ao revelar; toast PT; o que muda no extrato.
- [x] **HP-06-006** Repasse com fluxo próprio (payout, status repassado), não botão órfão.
- [x] **HP-06-007** Estornar **não** usa o filtro como alvo implícito. Seleção explícita ou só FX-06. Pedir causa.
- [x] **HP-06-008 (página)** Tabela: username do beneficiário; Residual com rótulo humano; empty sem «ledger».
- [x] **HP-06-009** Combos PT; filtrar sem ser o único caminho obscuro.
- [x] **HP-06-010** Copy bundle + cut proporcional, não «cut in-place».
- [x] **HP-06-011** Empty: 4 passos clicáveis (op, trilho, paga, materializar).

## DoD

Hop com destino &lt; bundle não confirma sem o Contador ver a perda.
