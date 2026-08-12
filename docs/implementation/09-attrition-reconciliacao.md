# 09 — Attrition, write-off e reconciliação

**Status:** pendente  
**Depende de:** [05-contas-livro-mundo.md](./05-contas-livro-mundo.md), [06-materializacao-claims.md](./06-materializacao-claims.md), [07-hops-cuts-repasse.md](./07-hops-cuts-repasse.md)

## Objetivo

Fechar o ciclo “mundo real falha”: estados de Conta/Laranja, claim preso, write-off com causa, reconciliação como **única** porta Nexus ↔ realidade, eventos compensatórios.

## Escopo

| Entra | Não entra |
|-------|-----------|
| Eixos Conta: emissão ok\|bloqueada; saldo acessível\|congelado\|perdido | Absorção central de perda pela Org (feature futura) |
| Estado Laranja: ativo → queimado \| saiu \| traiu (sinaliza, não hard-burn) | Módulo analytics separado |
| Cobranças Abertas não morrem automático se emissão bloqueada | Reescrever conta de emissão de Cobrança já enviada |
| Write-off → claim `perdido` + causa | Delete/mutate fato passado |
| Reconciliação: falta (rateio default) / sobra (beneficiário real) | Bypass de invariantes |
| Query exposição (claims presos) | |

## Use cases (mínimo)

- Atualizar estado Conta / Laranja.
- Write-off claims (com causa).
- Reconciliar Conta/moeda com saldo observado.
- Evento compensatório (corrigir fato errado).

## Domínio

- [money.md](../domain/money.md) — Attrition, Perdas e reconciliação
- [actors-and-mandates.md](../domain/actors-and-mandates.md) — Laranja / attrition
- [glossary.md](../domain/glossary.md) — Causa, Reconciliação, Write-off, Claim preso
- [open-gaps.md](../domain/open-gaps.md) — G4, G6

## Critérios de pronto

- [ ] Pós-write-off e pós-reconciliação: invariante `soma claims == saldo` mantida.
- [ ] Sobra nunca fica “desconhecida”.
- [ ] Incidência mecânica: perda cai em quem tinha claim na Conta.
- [ ] Causa obrigatória nos eventos de attrition/write-off/reconciliação relevantes.
