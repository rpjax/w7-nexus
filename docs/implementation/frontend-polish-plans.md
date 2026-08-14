# Planos de polish UX (paralelo)

**Pergunta:** dá para atacar o frontend cru em paralelo?  
**Resposta:** **sim**, se cada plano for dono das **páginas** sem overlap. Clientes `api/**` e primitivos `components/ui/**` já existentes são **leitura**; extração de um `EntityPicker` partilhado **não** entra nesta onda (cada página monta `Select` local a partir de listas HTTP que já existem).

Inventário de dor: `Refactor/web` — Inputs crus, UUID colado, hop como muro de campos, causa/kind/moeda em texto livre, pouca hierarquia, estados vazios/erro/loading raros, Início ainda “tudo Admin”.  
Planos Cursor: `C:\Users\rodri\.cursor\plans\ui_*.plan.md`.  
**Não** reabre etapas 01–09, **não** edita `etapa_09` Cursor, **não** implementa GAP-G / `gap_06` (PSP/webhook produto, path designer, portal Acionista rico, Journal tamper, ciclo quota, absorção Org).

## Princípios de polish

- Hierarquia clara (lista → detalhe; ações destrutivas atrás de confirmação).
- Menos UUID cru: picker/`Select` onde a API **já** lista (contas, operações, livro-mundo, cobranças).
- Catálogos em `Select`: kind de Conta, moeda usual (BRL/USD/USDT), causa de attrition do ledger, `burned|left|betrayed`.
- Loading / vazio / erro visíveis (não só `toast`).
- Labels em português, alinhadas à cópia atual (Livro-mundo, Claims, Carteira, Extrato…).
- **Não** inventar design system: shadcn + tokens em `Refactor/web` ([frontend-standards.md](../frontend-standards.md), [frontend-patterns.md](../frontend-patterns.md)). `DataTable` onde a coleção for tabular e o plano for dono da página.

## Fora de escopo

- Domínio GAP-G / adiados v1 (`gap_06_adiado_v1_*.plan.md`).
- Use cases novos no backend, salvo contrato **já existente e não usado** na UI (ex. `listExposure` já está na página do livro-mundo).
- Mudar regras de observação, invariante de caixa, grants, JWT permissions.
- `docs/implementation/09-attrition-reconciliacao.md` e status `feito` das etapas.

## Matriz

| Plano (ficheiro em `C:\Users\rodri\.cursor\plans\`) | Páginas / ficheiros donos | Paralelo com |
|------------------------------------------------------|---------------------------|--------------|
| `ui_01_chrome_nav_home_a1f82c10.plan.md` | `App.tsx`, `AppShell`, Início, Auth, Perfil, chrome partilhado | 02–06 |
| `ui_02_livro_mundo_b2e93d21.plan.md` | `WorldAccountsPage` (+ exposição na mesma tela) | 01, 03–06 |
| `ui_03_cobrancas_c3fa4e32.plan.md` | `ChargesPage` | 01–02, 04–06 |
| `ui_04_claims_hops_d4055f43.plan.md` | `ClaimsPage` (hops, reveal, reverse) | 01–03, 05–06 |
| `ui_05_operacoes_e5166054.plan.md` | `OperationsPage` | 01–04, 06 |
| `ui_06_identidades_me_f6277165.plan.md` | Contas, Deals, Acionistas, Extrato, Carteira | 01–05 |

Onda única: **01 | 02 | 03 | 04 | 05 | 06**. Sem fila.

### Ficheiros que forçam dono único

| Ficheiro | Dono |
|----------|------|
| `App.tsx`, `layouts/AppShell.tsx`, `auth/MandateContext.tsx` (só agrupamento de nav se preciso), `pages/HomePage.tsx`, `pages/AuthPage.tsx`, `pages/ProfilePage.tsx`, `index.css`, `page-header.tsx`, `StatusBadge.tsx`, `AuthLoadingCard.tsx`, `NexusBackground.tsx`, `brand/*` | **01** |
| `pages/WorldAccountsPage.tsx` | **02** |
| `pages/ChargesPage.tsx` | **03** |
| `pages/ClaimsPage.tsx` | **04** |
| `pages/OperationsPage.tsx` | **05** |
| `pages/AccountsPage.tsx`, `DealsPage.tsx`, `ShareholdersPage.tsx`, `StatementPage.tsx`, `CarteiraPage.tsx`, `components/data/*` | **06** |
| `api/**` | **leitura** para todos; só o dono da página pode acrescentar um helper **local na página**. Sem PR de cliente HTTP partilhado nesta onda. |
| `components/ui/*` | **leitura**; não estender kit salvo falha de primitive já existente (`Select` já está). |

`reverseCharge` / `listExposure` / rails: **não** duplicar UI entre 02/03/04/05. Reverse de cobrança fica em **03** (ficha da charge) e **04** (ledger); cada um só no seu ecrã. Exposição só em **02**. Bind de trilho só em **05**.

## O que fica cru de propósito

- Path designer / hops visuais de produto (GAP-08 G).
- Webhook PSP / conciliação automática (GAP-18 G).
- Portal Acionista rico (GAP-21-ish / adiados).
- Journal tamper / UI de auditoria completa (GAP-12 G).
- UUID na ficha técnica (cópia / `font-mono` truncado) — some o **paste como único fluxo**, não a identidade.
- Script/Store JSON em Operações: editor mínimo, não IDE.
- Densidade admin em Contas/Livro-mundo/Claims: o trabalho é usável, não “consumer app”.
