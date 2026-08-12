# 08 — Visibilidade e extrato (estimativa → pendente)

**Status:** pendente  
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

- [visibility.md](../domain/visibility.md)
- [money.md](../domain/money.md) — estado; G10
- [open-gaps.md](../domain/open-gaps.md) — G8/G10

## Critérios de pronto

- [ ] Nenhum endpoint de baixa confiança expõe saldo/claim “atual” mudando com hops.
- [ ] Contador vê claims/hops reais.
- [ ] Reveal não lista Contas intermediárias nem identidades de Laranjas do path.
