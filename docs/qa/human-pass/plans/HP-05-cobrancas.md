---
name: HP-05 Cobranças
overview: Passe humano do pedido de pagamento até materializar/estornar. Findings só HP-05.md.
isProject: false
---

# HP-05 — Cobranças

Contrato: [`README.md`](../../README.md). Output: [`findings/HP-05.md`](../findings/HP-05.md). Prefix: `qa05-`.

## Qualidade

Você é quem pede dinheiro no trilho. Pergunte: **Paga vs Materializada** está claro? O waterfall/split aparece ou a cobrança parece um boleto genérico?

## Browser

Front + `admin` / `adminadmin`.

## Pré-requisitos (criar você mesmo se faltar)

- Uma operação (pode ser `qa05-op`).
- Uma conta mundo de aterrissagem se a materialização exigir (abrir em Livro-mundo `qa05-gw` **sem** lost/freeze se for só para o fluxo feliz).

## Script (`/dashboard/charges`)

1. Lista: loading, empty, colunas, status badge.
2. Gerar sem operação → erro didático?
3. Gerar cobrança válida (valor, líquido, op, operador se pedido).
4. Marcar paga. A confirmação explica que isso **não** é materializar?
5. Materializar: sem conta de aterrissagem; depois com conta. Copy “aterrissagem”.
6. Estorno: diálogo + causa. Perigoso demais / pouco aviso?
7. Outros botões de status visíveis: percorrer os que não destruam o seed; anotar os que estão disabled sem explicação.

## Lentes

Glossário: Cobrança, Paga, Materialização, Conta de Gateway vs aterrissagem. A UI mistura os dois?

## Não fazer

Não desenhar hops (HP-06) além do que a própria tela de cobranças oferece (ex. atalho). Não lost de conta mundo alheia.
