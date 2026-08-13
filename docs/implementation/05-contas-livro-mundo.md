# 05 — Contas (livro-mundo)

**Status:** pendente  
**Depende de:** [01-foundation.md](./01-foundation.md) (paralelo possível com 02–04)

## Objetivo

Modelar o **livro-mundo**: Conta burra, multi-moeda, saldo numérico + transações. Sem ownership na Conta.

## Escopo

| Entra | Não entra |
|-------|-----------|
| Conta (gateway / banco / crypto) | Claim / hop (06–07) |
| Saldo **por moeda** | Conversão FX pelo Nexus |
| Transações (±) como registro observado | Ledger semântico |
| Owner Laranja em Conta de Gateway | Attrition completo (09) — estados mínimos ok |
| Contas de payout (destino de **repasse**, fora do escopo-org) | Quota ciclo fino (calendário vs rolling) |
| Quotas por (Conta × moeda) | |
| Eixos de estado Conta: emissão / saldo (mesmo sem fluxos 09) | |

## Use cases (mínimo)

- CRUD Conta (Gateways / Admin).
- Ajustar saldo **só** via UCs que depois serão compostos (nesta etapa: seed/reconciliação stub ou “registrar observação” cuidadoso — preferir não ter “set saldo livre” permanente).
- Consultar saldo por moeda + histórico de TX.
- Configurar quota básica.

## Invariantes (ainda parciais)

- Conta não guarda “de quem é”.
- Saldo só muda por transação registrada.
- Quando Claims existirem (06+): `soma claims == saldo` por Conta/moeda — esta etapa prepara o lado mundo.

## Domínio

- [money.md](../domain/money.md) — Dois livros, Conta, quotas, attrition (estados)
- [glossary.md](../domain/glossary.md) — Conta, Saldo, Livro-mundo, Estado de Conta

## Critérios de pronto

- [ ] Conta multi-moeda sem inventar “uma conta = uma moeda”.
- [ ] Nenhum campo de ownership/beneficiário na entidade Conta.
- [ ] Pronto para materialização creditar saldo + TX na mesma Conta de aterrissagem.
