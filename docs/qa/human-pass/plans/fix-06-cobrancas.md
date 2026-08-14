---
name: FX-06 Cobranças
overview: Marcar paga 500 vs sucesso; operador opcional vs regra; pedagogia Paga/Materializada.
isProject: false
---

# FX-06 — Cobranças

Índice: [`fix-plans.md`](../fix-plans.md).

## Pode tocar

- `Refactor/web/src/pages/ChargesPage.tsx`
- `Refactor/Nexus.Api/Charging/**` — mark-paid (não 500 se o evento persistiu); create charge (Admin sem operator vs UI)
- Testes `ChargeUseCaseTests` / equivalentes se a regra mudar

## Proibido

ClaimsPage (estorno canónico na ficha da cobrança aqui; FX-07 só o botão perigoso do filtro).

## Checklist

- [x] **HP-05-002** Mark-paid: se persistiu → 2xx + toast sucesso. Nunca 500 + lista Paga. Investigar NRE/journal após append.
- [x] **HP-05-001** Ou emitir sem ser Operator (Admin) **ou** campo obrigatório + copy PT (não «opcional»). Toast `assigned` em PT.
- [x] **HP-05-003** Paga ≠ Materializada visualmente; diálogo paga: «ainda não cria direitos/claims». Split em PT (Laranja, Agência…).
- [x] **HP-05-004** Gerar sem operação: texto, não só botão morto. Status do combo em PT. Toast Operação Ativa com acentos.
- [x] **HP-05-005** Aterrissagem: placeholder certo; pré-selecionar trilho se houver; líquido vs bruto.
- [x] **HP-05-006** Estorno: causa do glossário; repetir valor; efeito nos dois livros.
- [x] **HP-05-007** Cancelar/Expirar/Falhou com diálogo; Paga explica por que o trio some.
- [x] **HP-05-008** Ficha: nomes (op, operador), não só IDs.

## DoD

Marcar paga numa cobrança de teste: um único sinal de sucesso, status Paga.
