# 08 — Visibilidade e extrato (estimativa → pendente)

**Status:** feito  
**Depende de:** [06-materializacao-claims.md](./06-materializacao-claims.md), [07-hops-cuts-repasse.md](./07-hops-cuts-repasse.md)

## Objetivo

Extratos need-to-know: beneficiário **não** vê valor ao vivo (vaza path). Fluxo estimativa congelada → flag Contador → pendente/a receber + relatório controlado.

## Escopo

| Entra | Não entra |
|-------|-----------|
| Estimativa = valor no nascimento do claim (congelada) | Atualização incremental por hop |
| Flag “visível” / hop final (Contador/Admin) | Organograma / hops para ponta |
| Relatório controlado de alto nível no reveal | Portal rico Acionista |
| Matriz de visão por papel (APIs de query) | Inferência zero (risco residual aceito) |

## Use cases (mínimo)

- Query extrato Operador / Recrutador / Acionista / Laranja.
- Marcar claim visível ( Contador ).
- Anexar/gerar relatório controlado no reveal (conteúdo mínimo: diferença estimativa vs liberado + causa genérica).

## Domínio

- [visibility.md](../domain/visibility.md) — dois regimes; G10/G13
- [auditability.md](../domain/auditability.md) — leituras no log
- [open-gaps.md](../domain/open-gaps.md) — G8/G10/G12

## Critérios de pronto

- [x] Nenhum endpoint de baixa confiança expõe saldo/claim “atual” mudando com hops.
- [x] Contador vê claims/hops reais.
- [x] Reveal não lista Contas intermediárias nem identidades de Laranjas do path.

## Entrega (Refactor)

- Claim guarda `BirthAmount`/`BirthCurrency` (imune a hops); filhos de split herdam o nascimento do pai.
- `RevealClaim` snapshota valor/moeda atuais + causa genérica (`ClaimRevealed`).
- `GET /api/ledger/authenticated/statement` agrupa por cobrança: `estimate` | `pending` | `loss` — sem Conta, hop ou valor ao vivo.
- Contador: `POST .../claims/{id}/reveal`; listagem 06/07 inalterada (amount/local reais).
- UI Extrato (autenticado) + revelar na página Claims. Journal `Ledger.StatementRead` / `Ledger.ClaimRevealed`.

**Nota G12 (parcial, GAP-28):** Journal também cobre mutações 06–09 e algumas leituras sensíveis (carteira, contas, rotas, claims Admin). HTTP de leitura do log existe (`ler_log_auditoria`). Hash-chain / UI de Journal / correlação fina **não**. O status desta etapa permanece **feito**.

