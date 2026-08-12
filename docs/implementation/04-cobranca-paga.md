# 04 — Cobrança e webhook Paga

**Status:** pendente  
**Depende de:** [03-operations.md](./03-operations.md), Contas de Gateway de emissão (pode stubear até [05](./05-contas-livro-mundo.md) fechar)

## Objetivo

Nascimento e lifecycle da Cobrança até **Paga** — ainda **sem** materialização (Claims).

## Escopo

| Entra | Não entra |
|-------|-----------|
| API: Operação + Operador + valor → Cobrança | Materialização / Claims (06) |
| Conta de Gateway de **emissão** (fixa cut nível-1) | Hops |
| Intenção de split em % (snapshot na geração) | Execução de pagamento pelo Nexus |
| Aberta → Paga (webhook) | Attrition completo |
| Terminais: Expirada / Cancelada / Falhou | |

## Use cases (mínimo)

- Gerar Cobrança (API + fallback painel se quiser depois).
- Receber webhook Paga.
- Consultar Cobrança (Admin/Contador/Operador — visões mínimas).

## Domínio

- [money.md](../domain/money.md) — Cobrança, dois pontos de vista da Conta Gateway, split intenção
- [glossary.md](../domain/glossary.md) — Cobrança, Cut nível-1

## Critérios de pronto

- [ ] Split intenção imutável na base após geração.
- [ ] Cut nível-1 amarrado ao Laranja da **conta de emissão**, independente de aterrissagem futura.
- [ ] Paga é fato externo; não cria Claims ainda.
