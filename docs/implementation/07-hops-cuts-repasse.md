# 07 — Hops, cuts mid-path e repasse

**Status:** pendente  
**Depende de:** [06-materializacao-claims.md](./06-materializacao-claims.md)

## Objetivo

Hop canônico **origem → N destinos**; cut proporcional no bundle; cut in-place (só ledger); repasse tira o claim do escopo-org (`repassado`).

## Escopo

| Entra | Não entra |
|-------|-----------|
| Hop 1→N (destino já líquido; perda = origem − Σ destinos) | API automática banco/gateway |
| Bundle efêmero por-hop, por moeda | Persistência de “batch” |
| Cut mid-path % proporcional; 1× por (Laranja × GUID de origem) | Absorção central de perda pela Org |
| Cut com transferência vs **in-place** | UI path designer rica |
| Hop que redenomina (valor declarado; proporção = chave, não câmbio) | Relatório controlado (08) |
| Repasse: destino fora do escopo-org; claim → `repassado` (sai da soma) | Status `pago` que permanece na Conta org |

## Use cases (mínimo)

- Registrar hop (Contador/Admin) — claims + saldos; re-verifica invariante ao commit.
- Registrar hop com cut de Laranja (transferência ou in-place).
- Repasse final (claim `repassado`).

## Invariantes

- Caixa nunca sozinho (exceto cut in-place: ledger sozinho, número da Conta igual).
- Após hop (mesma moeda): `soma claims ativos == saldo` por Conta/moeda.
- Cut rateia em **todos** os claims do bundle.
- 1× por Laranja **no fluxo** = por (Laranja × GUID); cut de emissão conta.

## Domínio

- [money.md](../domain/money.md) — Hops, Cut mid-path, Quotas, Multi-denominação, G13
- [glossary.md](../domain/glossary.md) — Bundle, Hop, Status do Claim

## Critérios de pronto

- [ ] Exemplo 100 / cut 10% → claims 20→18, 50→45… + claim Laranja 10.
- [ ] Hop com perda (100→95) encolhe claims proporcionalmente.
- [ ] Redenominação: consome moeda origem, nasce destino com valor declarado.
- [ ] Repasse remove claim da soma; saldo da Conta org decrementa no mesmo UC.
