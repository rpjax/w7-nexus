---
name: HP-01 Novato identidade
overview: Passe humano no browser — auth, início, perfil, extrato. Qualidade novato. Findings só em findings/HP-01.md.
isProject: false
---

# HP-01 — Novato / identidade

Ler primeiro: [`docs/qa/human-pass/README.md`](../../README.md). Escrever **somente** [`docs/qa/human-pass/findings/HP-01.md`](../findings/HP-01.md). **Não** editar `conclusions.md` nem findings de outros.

## Qualidade

Você é alguém que acabou de ganhar acesso. Não sabe o que é Claim. Pergunte o tempo todo: **é fácil de entender?** **o vocabulário é humano?** **a tela me segura ou me abandona?**

## Browser

1. Abrir `https://nexus.websete.localhost:9143` (não o host `api.*`).
2. Usar o navegador do Cursor como humano: clicar, digitar, ler, errar de propósito.
3. Login seed: `admin` / `adminadmin` **depois** de testar falhas.

## Script (nessa ordem)

### Auth (`/auth`)

- Ler título, subtítulo, abas Entrar vs Bootstrap.
- Submeter vazio; senha errada (`admin` / `errado`).
- O erro aparece onde? Toast + formulário? Texto compreensível?
- Entrar certo. Se cair em `/auth?redirect=%2F`, o redirect funciona?
- A aba bootstrap pede chave mestra — parece emergência ou cadastro normal? Isso é didático?

### Início (`/dashboard`)

- O “Olá” e os atalhos batem com o que a nav mostra?
- Algum atalho é inglês cru, interno, ou “etapa 0N”?
- Empty/erro de perfil: se falhar, o humano entende o que fazer?

### Perfil

- Labels: usuário vs senha vs “username”.
- Tentar senha curta; cancelar se houver diálogo.
- **Não** trocar o usuário `admin`. Se testar troca, crie outra conta em Contas (`qa01-tmp`) e **não** use o seed — ou pule a troca de username do seed e anote “não testei troca no seed de propósito”.

### Extrato

- Sem linhas: a mensagem ensina o que é extrato neste produto?
- Com linhas: colunas (origem, valor, status) — “estimativa / pendente” aparece? Faz sentido sem ser Contador?
- IDs crus assustam ou ajudam?

### Sessão

- Sair. A tela de auth é o destino? Token some (precisa entrar de novo)?

## Lentes

Copy da nav **Eu / Extrato / Perfil**. Confusão Conta (login) vs dinheiro. Qualquer P0 de login.

## Fim

Marcar o checklist no findings. Se uma tela estiver quebrada (branco, loop, Failed to fetch), P0 com URL e o que o toast disse.
