# Human-pass — planos de fix (paralelo)

Fonte: [conclusions.md](./conclusions.md). Cada plano é **dono exclusivo** de ficheiros. Onda única: **FX-01 … FX-09 em paralelo**. Sem fila.

Agentes: seguir o plano, marcar os IDs no próprio ficheiro do plano (`- [ ]` → `- [x]`), não editar `conclusions.md`.

## Matriz

| Plano | Qualidade | Ficheiros donos | Findings |
|-------|-----------|-----------------|----------|
| [fix-01-chrome.md](./plans/fix-01-chrome.md) | Chrome / novato | `App.tsx`, `AppShell`, `HomePage`, `AuthPage`, `ProfilePage` | HP-01-001/004–008, HP-02-005/007, HP-06-008 (só nav), HP-08-001/005/007/009, parte chrome de «Conta» |
| [fix-02-extrato.md](./plans/fix-02-extrato.md) | Extrato | `StatementPage.tsx` | HP-01-002, empty desta página (HP-08-004) |
| [fix-03-recrutador.md](./plans/fix-03-recrutador.md) | Recrutador | `CarteiraPage.tsx`, `DealsPage.tsx` | HP-02-001–004, HP-02-006 |
| [fix-04-membros.md](./plans/fix-04-membros.md) | Identidades | `AccountsPage.tsx` + Mandates attrition/erros | HP-03-* (exceto nav «Contas») |
| [fix-05-operacoes.md](./plans/fix-05-operacoes.md) | Ops | `OperationsPage.tsx` + Store list | HP-04-* |
| [fix-06-cobrancas.md](./plans/fix-06-cobrancas.md) | Cobranças | `ChargesPage.tsx` + mark-paid / create | HP-05-* |
| [fix-07-claims.md](./plans/fix-07-claims.md) | Contador | `ClaimsPage.tsx` + hop | HP-06-* menos nav |
| [fix-08-livro-mundo.md](./plans/fix-08-livro-mundo.md) | Gateways | `WorldAccountsPage.tsx` + guards de emissão/lost | HP-07-* menos nav «Conta» |
| [fix-09-acionistas.md](./plans/fix-09-acionistas.md) | Acionistas | `ShareholdersPage.tsx` + mensagem de soma | HP-08-002/003/006/010 |

## Dono único (não cruzar)

| Ficheiro | Dono |
|----------|------|
| `Refactor/web/src/App.tsx`, `layouts/AppShell.tsx`, `pages/HomePage.tsx`, `AuthPage.tsx`, `ProfilePage.tsx` | FX-01 |
| `pages/StatementPage.tsx` | FX-02 |
| `pages/CarteiraPage.tsx`, `DealsPage.tsx` | FX-03 |
| `pages/AccountsPage.tsx`; `Mandates/**` (attrition, error messages de mandato) | FX-04 |
| `pages/OperationsPage.tsx`; `Operations/**` Store list | FX-05 |
| `pages/ChargesPage.tsx`; `Charging/**` mark-paid / create charge | FX-06 |
| `pages/ClaimsPage.tsx`; `Ledger/**` hop (perda, causa) | FX-07 |
| `pages/WorldAccountsPage.tsx`; `WorldAccounts/**` emissão/lost | FX-08 |
| `pages/ShareholdersPage.tsx`; mensagem soma acionistas (Mandates **só** se for handler de stake **não** tocado por FX-04 — preferir copy no cliente) | FX-09 |
| `components/data/data-table.tsx` | **ninguém** nesta onda (empty via prop) |
| `components/ui/*` | leitura |
| `feedback/*` | leitura |

## Regras

- PT-BR, glossário: Conta = livro-mundo; login = usuário/membro/identidade.
- Não inventar use case novo além do que o finding pede (ex. hop com causa de perda se o domínio já tiver causa).
- Não desabilitar Admin seed em testes manuais.
- Blueprint hexagonal se tocar `Nexus.Api`.
