# 04 — Cobrança e webhook Paga

**Status:** feito  
**Depende de:** [03-operations.md](./03-operations.md), Contas de Gateway de emissão ([05](./05-contas-livro-mundo.md))

## Objetivo

Nascimento e lifecycle da Cobrança até **Paga** — materialização (Claims) é [06](./06-materializacao-claims.md).

## Escopo

| Entra | Não entra |
|-------|-----------|
| API: Operação + Operador + valor → Cobrança | Materialização / Claims (06) |
| Conta de Gateway de **emissão** (`WorldAccount`) | Hops; execução de pagamento pelo Nexus |
| Intenção de split em % (snapshot; waterfall 5 linhas) | Attrition completo |
| Seleção da Conta: auto + override no conjunto da Op | PSP real (`IPaymentIssuer` no-op) |
| Aberta → Paga (webhook) | |
| Terminais: Expirada / Cancelada / Falhou | |

## Use cases (mínimo)

- Gerar Cobrança (API + painel Admin).
- Receber webhook Paga (secret).
- Consultar Cobrança (Admin / Operador nas próprias).

## Domínio

- [money.md](../domain/money.md) — Cobrança, dois pontos de vista da Conta Gateway, split intenção
- [glossary.md](../domain/glossary.md) — Cobrança, Cut nível-1

## Critérios de pronto

- [x] Split intenção imutável na base após geração (waterfall: Laranja → Acionistas → Gestão da Op → agenciamento → Residual Org).
- [x] Cut nível-1 amarrado ao Laranja da **conta de emissão**, independente de aterrissagem futura.
- [x] Paga é fato externo; não cria Claims ainda (materialização = etapa 06: `Paga → Materializada`).
- [x] Sem quota disponível → emissão rejeitada; override de Conta só se estiver no conjunto da Op.

## Entrega (Refactor)

- BC `Charging/`: `Charge` event-sourced (Marten, schema compartilhado `nexus_es`); emissão contra `WorldAccount` (etapa 05) — o campo `EmissionRailId` no evento da Cobrança guarda o id da Conta.
- HTTP `/api/charging/administrator`, `/authenticated`, `/webhooks/paid`.
- UI Admin: Cobranças + bind de Conta de Gateway na página Operações.
- Journal **não** é o stream da Cobrança.
