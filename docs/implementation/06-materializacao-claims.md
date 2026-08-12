# 06 — Materialização e Claims (ledger)

**Status:** pendente  
**Depende de:** [04-cobranca-paga.md](./04-cobranca-paga.md), [05-contas-livro-mundo.md](./05-contas-livro-mundo.md)

## Objetivo

Fechar o nascimento do **ledger**: Contador materializa líquido X + Conta de aterrissagem → `%` morre → **Claims**. Livro-mundo recebe saldo/TX; ledger recebe claims; invariante de nascimento.

## Escopo

| Entra | Não entra |
|-------|-----------|
| Materialização fim de período (lote UX pode ser N chamadas “1 pagamento”) | Hops / cut mid-path (07) |
| Waterfall → claims (Laranja emissão, Acionistas, Op, Recrutador, residual Org) | Estimativa visível polida (08) |
| Claim: beneficiário, valor, moeda, origem GUID, localização, status `ativo` | Write-off / reconciliação (09) |
| Invariante: `soma claims criados == X` e `soma claims == saldo` na Conta/moeda | |

## Use cases (mínimo)

- Materializar pagamento (Contador/Admin): input X + Conta aterrissagem + Cobrança Paga.
- Listar claims por Conta / por Cobrança / por beneficiário (Contador).

## Regras críticas

- Caixa e ledger no **mesmo** UC.
- X já pós-taxa gateway.
- Conta de aterrissagem pode ≠ Conta de emissão; cut nível-1 já fixado na intenção.
- Sem pool / “poço” persistido — só claims.

## Domínio

- [money.md](../domain/money.md) — Materialização, Claim, Rateio nível 3, Multi-denominação
- [open-gaps.md](../domain/open-gaps.md) — G5, G9, G10

## Critérios de pronto

- [ ] Teste: materializar cria claims que somam X e saldo da Conta/moeda casa.
- [ ] Tentativa de materializar parcial / duas vezes o mesmo pagamento = rejeitada.
- [ ] Residual Org é beneficiário real (não limbo).
