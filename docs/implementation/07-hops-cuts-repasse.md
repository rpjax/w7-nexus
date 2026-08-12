# 07 — Hops, cuts mid-path e repasse

**Status:** pendente  
**Depende de:** [06-materializacao-claims.md](./06-materializacao-claims.md)

## Objetivo

Movimentação pós-materialização: hop escreve **dois livros**; cut proporcional no bundle; marcar pago; cut in-place (só ledger) quando não há TX de caixa.

## Escopo

| Entra | Não entra |
|-------|-----------|
| Hop A→B (destino líquido declarado) | API automática banco/gateway |
| Bundle efêmero (subconjunto de claims) | Persistência de “batch” |
| Cut mid-path % proporcional + 1× por Laranja no fluxo | Absorção central de perda pela Org |
| Hop que redenomina (valor declarado; sem FX Nexus) | UI path designer rica |
| Repasse: localização final + status `pago` | Relatório controlado ao beneficiário (08) |
| Cut in-place (ledger sozinho) | |

## Use cases (mínimo)

- Registrar hop (Contador/Admin) — claims + saldos.
- Registrar hop com cut de Laranja.
- Marcar claim(s) pagos / hop de repasse.
- (Opcional na mesma etapa) cut in-place sem movimento de caixa.

## Invariantes

- Caixa nunca sozinho.
- Após hop (mesma moeda): soma claims por Conta/moeda == saldo.
- Cut rateia em **todos** os claims do bundle (sem escolha manual de quem paga).
- Escopo de “1× por Laranja”: decidir na implementação e **documentar em domain** se ainda ambíguo (sugestão: por GUID de Cobrança de origem nos claims do bundle — confirmar).

## Domínio

- [money.md](../domain/money.md) — Hops, Cut mid-path, Quotas, Multi-denominação
- [glossary.md](../domain/glossary.md) — Bundle, Hop, Cut mid-path

## Critérios de pronto

- [ ] Exemplo 100 / cut 10% → claims 20→18, 50→45… + claim Laranja 10.
- [ ] Hop com perda (100→95) encolhe claims proporcionalmente.
- [ ] Redenominação: consome moeda origem, nasce destino com valor declarado.
