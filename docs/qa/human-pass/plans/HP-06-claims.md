---
name: HP-06 Claims hops
overview: Passe humano do ledger — claims, hops, revelar, repasse, estorno. Findings só HP-06.md.
isProject: false
---

# HP-06 — Contador (claims)

Contrato: [`README.md`](../../README.md). Output: [`findings/HP-06.md`](../findings/HP-06.md). Prefix: `qa06-`. Glossário: `docs/domain/glossary.md`.

## Qualidade

Você é o Contador: registra a realidade. Pergunte: **a tela me guia no fato (hop) ou parece um CRUD de IDs?** “Claim” na nav é o termo certo para o humano desta org, ou deveria ser “direitos / a receber”?

## Browser

Front + `admin` / `adminadmin`.

## Pré-requisitos

Se não houver claim: gerar o mínimo (operação + conta mundo + cobrança paga + materializar) com prefixo `qa06-`, depois voltar a Claims. Anotar o atalho se a UI não ensinar essa sequência.

## Script (`/dashboard/claims`)

1. Filtros: cobrança, conta, beneficiário — combos vazias, copy.
2. Lista + empty + loading.
3. Clicar um claim: ficha completa? Status ativo/repassado/perdido/estornado/arquivado em PT?
4. Hop: origem/destino iguais, destino vazio, cut mid-path se existir. N destinos se a UI permitir.
5. Repasse: campos obrigatórios.
6. Revelar: sem seleção; com seleção. O que “revelar” muda para a ponta (extrato)? A UI diz?
7. Estorno pela tela de claims vs cobranças — duplicado? Confuso?
8. Relatório controlado / estimativa vs pendente — aparece?

## Lentes

Hop vs saque vs transfer. Bundle. Cut mid-path. Need-to-know: a tela de admin vaza path demais **na copy** (ok ser completo para Contador; o problema é label que a ponta também veria no extrato — anotar se o extrato for visível nesta sessão só como referência rápida, dono do extrato é HP-01).

## Não fazer

Não reconciliação/lost (HP-07). Não shareholders.
