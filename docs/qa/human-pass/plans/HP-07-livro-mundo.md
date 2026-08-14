---
name: HP-07 Livro-mundo
overview: Passe humano nas contas do mundo — saldo, freeze, lost, reconcile, exposição. Findings só HP-07.md.
isProject: false
---

# HP-07 — Gateways / livro-mundo

Contrato: [`README.md`](../../README.md). Output: [`findings/HP-07.md`](../findings/HP-07.md). Prefix: `qa07-`.

## Qualidade

Você olha o dinheiro **no mundo** (banco/gateway). Medo de clicar Lost. Pergunte: **os dois eixos (emissão vs saldo) estão óbvios?** “Livro-mundo” é nome de produto ou de implementador?

## Browser

Front + `admin` / `adminadmin`.

## Script (`/dashboard/world-accounts`)

1. Header, lista, empty, kinds (Gateway/Banco/Crypto/Payout) — labels PT.
2. Abrir `qa07-caixa` (Gateway com laranja/cut/quota se a tela pedir). Validar campos vazios.
3. Selecionar: transações, exposição, formulários à direita. Densidade: dá para trabalhar?
4. Observar crédito e débito com memo. Números: moeda explícita? Sem FX surpresa?
5. Quota.
6. Congelar saldo → voltar acessível. Copy do diálogo.
7. Emissão ok / bloqueada.
8. Renomear rótulo.
9. **Lost só em `qa07-*`**: causa em linguagem humana (`bloqueio_bancario` vs “Bloqueio bancário”).
10. Reconciliar na mesma conta de teste (falta/sobra se a UI permitir). A confirmação explica invariante (claims vs saldo) em humano?
11. Exposição: tabela compreensível (“claim preso”)?

## Lentes

Conta mundo vs Account de login (a palavra “Conta” nas duas navs). Laranja. Quota vs teto.

## Não fazer

Não desligar Admin. Não lost em contas sem prefixo `qa07-`.
