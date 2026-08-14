---
name: HP-02 Recrutador
overview: Passe humano — carteira e deals. Qualidade sargento/recrutador. Findings só HP-02.md.
isProject: false
---

# HP-02 — Recrutador

Contrato: [`docs/qa/human-pass/README.md`](../../README.md). Output: [`findings/HP-02.md`](../findings/HP-02.md). Prefix: `qa02-`.

## Qualidade

Você traz gente e vê a downline. Não opera hops. Pergunte: **consigo cuidar da minha carteira sem parecer um ledger?** “Deal” e “agenciamento” são palavras que um sargento usaria?

## Browser

`https://nexus.websete.localhost:9143` → `admin` / `adminadmin`.

## Script

### Carteira (`/dashboard/carteira`)

- Ler header e empty.
- Lista: o que é cada linha (pessoa? mandato? %)? Copiar ID — o sucesso é claro? O ID é o que o recrutador precisa no dia a dia?
- Conceder preset se o botão existir: o destino (combo de contas) é usável? Sem contas, o empty ensina o próximo passo?
- “Carteira” no glossário é downline, não saldo bancário — a UI trai isso?

### Deals (`/dashboard/deals`)

- Título “Deals de agenciamento”: humano ou inglês de escritório?
- Criar deal `qa02-*` (campos obrigatórios vazios primeiro).
- Salvar; encerrar com confirmação. Cancelar o diálogo uma vez.
- Relação deal ↔ carteira: a UI explica ou são dois mundos?

### Nav

- Itens em **Eu** vs **Pessoas**: recrutador acharia Carteira em Eu e Deals em Pessoas?

## Não fazer

Não varrer Operações/Claims. Não encerrar deals que não sejam `qa02-*` se houver dados de outros agentes — criar o seu.
