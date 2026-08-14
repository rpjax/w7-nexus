---
name: HP-08 Chrome acionistas
overview: Passe humano do chrome, acionistas, consistência transversal. Findings só HP-08.md. Merge NÃO é este agente.
isProject: false
---

# HP-08 — Admin transversal (chrome + acionistas)

Contrato: [`README.md`](../../README.md). Output: [`findings/HP-08.md`](../findings/HP-08.md). Prefix: `qa08-`.

## Qualidade

Você julga o **produto como um só**. Não re-executa materializar/hop. Pergunte: **a casa está consistente?** Nav mente? Empty states falam línguas diferentes? Acionistas parece um Excel perdido?

## Browser

Front + `admin` / `adminadmin`. Estreitar viewport para mobile.

## Script

### Shell

- Grupos **Eu / Dinheiro / Pessoas**: ordem, labels, item ativo ao navegar.
- Logo, avatar, papéis (“Administrator” vs “Admin”).
- Sair.
- Menu hambúrguer: abre, fecha, scroll do body, destinos.

### Acionistas (`/dashboard/shareholders`)

- Total %. Upsert `qa08-*` (criar membro em Contas se precisar). Remover. Ultrapassar 100% se a UI deixar — o que acontece?
- Copy Acionista vs Admin vs Dono.

### Consistência (passe **leve**)

Abrir cada rota da nav (~15s): loading skeleton? empty? título vs item de nav (Claims vs Claim, Deals vs agenciamento, Livro-mundo vs Contas)? Toasts iguais?

### Bordas

- URL `/dashboard/nao-existe` → para onde vai?
- `/auth` autenticado → redireciona?

## Não fazer

Não preencher `conclusions.md` (merge é passo depois dos oito findings). Não lost/repass em massa.
