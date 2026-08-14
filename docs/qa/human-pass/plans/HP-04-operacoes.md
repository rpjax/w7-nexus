---
name: HP-04 Operações
overview: Passe humano na tela Operações (ciclo, cut, trilhos, script, store). Findings só HP-04.md.
isProject: false
---

# HP-04 — Gestor de operações

Contrato: [`README.md`](../../README.md). Output: [`findings/HP-04.md`](../findings/HP-04.md). Prefix: `qa04-`.

## Qualidade

Você monta frentes de trabalho. Não é Contador. Pergunte: **consigo criar uma operação e deixá-la usável sem um manual?** Script/Store/Trilho são didáticos ou siglas de infra?

## Browser

Front + `admin` / `adminadmin`.

## Script (`/dashboard/operations`)

1. Empty + criar operação `qa04-frente` (campos vazios primeiro).
2. Selecionar na lista: o detalhe compete com a lista? Mobile?
3. Percorrer **todos** os status disponíveis; ler o rótulo de cada um. Algum é inglês cru / estado interno?
4. Cut de gestão: salvar número, limpar (null). O que “cut” significa na tela?
5. Associar operador (precisa de membro — criar `qa04-op` em Contas se a combo estiver vazia, ou usar conta existente que não seja de outro prefixo destrutivo). Remover operador. Confirmação?
6. Trilhos: ligar / desligar. Se não houver trilho, a UI diz o que falta (livro-mundo / emissão)?
7. Script: registrar e resolver. O resultado resolvido é legível?
8. Store: salvar objeto, remover. Pesquisa se existir.

## Lentes

Operation key vs id interno — vaza para o humano? “Operação” vs “missão” vs “produto”.

## Não fazer

Não materializar cobranças (HP-05). Não hops (HP-06).
