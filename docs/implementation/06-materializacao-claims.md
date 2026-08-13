# 06 — Materialização e Claims (ledger)

**Status:** pendente  
**Depende de:** [04-cobranca-paga.md](./04-cobranca-paga.md), [05-contas-livro-mundo.md](./05-contas-livro-mundo.md)

## Objetivo

Nascimento do **ledger**: Contador materializa líquido X + Conta de aterrissagem → `%` morre → **Claims**. Livro-mundo recebe saldo/TX; invariante de nascimento; Residual da Org recebe resto e arredondamento.

## Escopo

| Entra | Não entra |
|-------|-----------|
| Materialização fim de período (lote UX = N chamadas “1 pagamento”) | Hops / cut mid-path (07) |
| Waterfall → claims (Laranja, Acionistas, Gestão da Op, Op, Recrutador, **Residual Org**) | Estimativa visível polida (08) |
| Claim: beneficiário, valor, moeda, origem GUID, localização, status `ativo` | Write-off / reconciliação (09) |
| Invariante: `soma claims criados == X` e `soma claims ativos == saldo` na Conta/moeda | Materialização parcial / multi-aterrissagem |

## Use cases (mínimo)

- Materializar pagamento (Contador/Admin): input X + Conta aterrissagem + Cobrança Paga. Idempotente; no máximo uma vez.
- Listar claims por Conta / por Cobrança / por beneficiário (Contador).

## Regras críticas

- Caixa e ledger no **mesmo** UC.
- X já pós-taxa gateway. Aterrissagem = **uma** Conta/moeda.
- Cut nível-1 já fixado na intenção (emissão ≠ aterrissagem).
- Arredondamento **na materialização** → Residual da Org (ou última linha não-zero). Sem pool persistido.

## Domínio

- [money.md](../domain/money.md) — Materialização, Claim, Waterfall, Multi-denominação, G13
- [open-gaps.md](../domain/open-gaps.md) — G5, G9, G10, G13

## Critérios de pronto

- [ ] Materializar cria claims que somam X e saldo da Conta/moeda casa (só `ativo` na invariante).
- [ ] Re-materializar o mesmo pagamento é rejeitado (exceto compensatório explícito).
- [ ] Residual da Org recebe resto do deal `< 100%` e o centavo de arredondamento.
