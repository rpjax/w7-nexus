# 09 — Attrition, write-off e reconciliação

**Status:** feito  
**Depende de:** [05-contas-livro-mundo.md](./05-contas-livro-mundo.md), [06-materializacao-claims.md](./06-materializacao-claims.md), [07-hops-cuts-repasse.md](./07-hops-cuts-repasse.md)

## Objetivo

Estados de Conta/Laranja/Operador, claim preso, write-off com causa, reconciliação como **única** porta Nexus ↔ realidade, eventos compensatórios (incl. estorno).

## Escopo

| Entra | Não entra |
|-------|-----------|
| Eixos Conta: emissão ok\|bloqueada; saldo acessível\|congelado\|perdido | Absorção central de perda pela Org (feature futura) |
| Estado Laranja/Operador: ativo → queimado \| saiu \| traiu | Módulo analytics separado |
| `saldo: perdido` **dispara write-off** de todos os `ativo` no mesmo UC | Reescrever conta de emissão de Cobrança já enviada |
| Write-off → claim `perdido` + causa | Delete/mutate fato passado |
| Reconciliação por (Conta × moeda): falta proporcional (default) / sobra → **Residual da Org** | Bypass de invariantes |
| Estorno/chargeback → `estornado` | |
| Query exposição (claims `ativo` em Conta congelada/perdida) | |

## Use cases (mínimo)

- Atualizar estado Conta / Laranja / Operador.
- Write-off (implícito em `saldo: perdido`).
- Reconciliar Conta/moeda com saldo observado org-scoped.
- Evento compensatório (correção; estorno).

## Domínio

- [money.md](../domain/money.md) — Attrition, Perdas e reconciliação, G13
- [actors-and-mandates.md](../domain/actors-and-mandates.md) — Laranja / Operador attrition
- [glossary.md](../domain/glossary.md) — Causa, Reconciliação, Write-off, Estorno, Residual da Organização

## Critérios de pronto

- [x] Pós-write-off e pós-reconciliação: `soma claims ativos == saldo`.
- [x] Sobra cria/credita Residual da Org — nunca “desconhecida”.
- [x] Incidência mecânica: perda cai em quem tinha claim `ativo` na Conta.
- [x] Causa obrigatória nos eventos relevantes (incl. `estorno`).

## Entrega (Refactor)

- `MarkAccountLost`: `Lost` + write-off de claims `ativo` + débito até zerar; configure recusa `BalanceStatus=Lost`.
- `ReconcileAccount`: falta proporcional (ou claim indicado) / sobra → claim Residual Org; causa obrigatória.
- `ReverseCharge`: claims `ativo` da Cobrança → `Reversed`; débito nas Contas; `ChargeReversed`.
- `GET .../exposure`: claims ativos em Contas Frozen/Lost.
- Mandato: `RecordMemberAttrition` (`burned|left|betrayed`) só sinaliza; sem cascata de Contas.
- UI: Livro-mundo (congelar/perdido/reconciliar/exposição), Cobranças/Claims (estorno), Contas (attrition).
