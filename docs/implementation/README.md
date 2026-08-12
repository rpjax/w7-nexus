# Nexus — Implementação (processo)

Documentação do **processo de desenvolvimento e implementação** do domínio de produto em código.

- **Não** é o domínio de negócio — isso vive em [`../domain/`](../domain/).
- **Não** é o blueprint de arquitetura — isso vive em [`../architecture-blueprint.md`](../architecture-blueprint.md).
- Aqui: **o quê construir, em que ordem, o que entra/sai de cada etapa**, critérios de “pronto”, e links de volta ao domínio.

Contexto: alvo de código atual = `Refactor/Nexus.Api` (+ `Refactor/web` quando a fatia exigir UI). Hexagonal / domain-first conforme o blueprint.

## Como usar

1. Trabalhar **uma etapa por vez** (ou fatia vertical mínima dentro da etapa).
2. Antes de codar: ler o doc da etapa + docs de domínio linkados.
3. Ao fechar a etapa: marcar status aqui e no próprio `0N-*.md` (`pendente` → `em curso` → `feito`).
4. Descobertas de domínio no meio do caminho → voltam para `docs/domain/` (não “só no chat”).
5. Adiados de domínio (equipes, API automática de hops, etc.) **não** entram nas etapas v1 salvo decisão explícita.

## Princípios do processo

| Princípio | Significado |
|-----------|-------------|
| **Domínio manda** | Use cases e invariantes vêm de `docs/domain/`; código não inventa regra de negócio. |
| **Fatias verticais** | Preferir caminho ponta a ponta fino (API → handler → persistência → leitura) a “camada inteira”. |
| **Dois livros cedo** | Conta (mundo) e Claim (ledger) nascem separados no modelo; UCs de movimento escrevem nos dois. |
| **Invariante na fronteira** | `soma claims == saldo` (por Conta, por moeda) garantida no use case — não por FK. |
| **Mínimo viável por etapa** | Sem ES “completo” se append-only + projeções simples bastarem; evoluir depois. |
| **Singleton** | Um deploy = uma organização; sem `tenant_id`. |

Ver também: [00-principles.md](./00-principles.md).

## Mapa das etapas

| # | Doc | Foco | Status | Depende de |
|---|-----|------|--------|------------|
| 0 | [00-principles.md](./00-principles.md) | Regras do processo, DoD, fora de escopo v1 | **feito** (meta) | — |
| 1 | [01-foundation.md](./01-foundation.md) | Base existente: Accounts, Auth, Authorization, Journal | **em curso** (já no Refactor) | — |
| 2 | [02-members-roles.md](./02-members-roles.md) | Membros, papéis de domínio, deals, Acionistas | pendente | 1 |
| 3 | [03-operations.md](./03-operations.md) | Operação + operation key; Script/Store mínimos | pendente | 2 |
| 4 | [04-cobranca-paga.md](./04-cobranca-paga.md) | Cobrança, Conta Gateway emissão, webhook Paga | pendente | 3 |
| 5 | [05-contas-livro-mundo.md](./05-contas-livro-mundo.md) | Contas multi-moeda, saldo, transações, quotas (conceito) | pendente | 1 |
| 6 | [06-materializacao-claims.md](./06-materializacao-claims.md) | Materialização → Claims; waterfall; invariante nascimento | pendente | 4, 5 |
| 7 | [07-hops-cuts-repasse.md](./07-hops-cuts-repasse.md) | Hops, cut mid-path, bundle, pago, cut in-place | pendente | 6 |
| 8 | [08-visibilidade-extrato.md](./08-visibilidade-extrato.md) | Estimativa → pendente; relatório controlado; matriz need-to-know | pendente | 6, 7 |
| 9 | [09-attrition-reconciliacao.md](./09-attrition-reconciliacao.md) | Estados Conta/Laranja, write-off, reconciliação, causa | pendente | 5, 6, 7 |

Etapas **5** pode avançar em paralelo com **2–4** (Contas não dependem de Cobrança). Etapas **8** e **9** podem fatiar; **9** não bloqueia um MVP de hops se write-off/reconciliação vierem logo depois.

## Relação com o domínio

| Domínio | Uso na implementação |
|---------|----------------------|
| [domain/README.md](../domain/README.md) | Índice + adiados |
| [domain/money.md](../domain/money.md) | Dois livros, Cobrança, Claim, hops, perdas |
| [domain/actors-and-mandates.md](../domain/actors-and-mandates.md) | Papéis, Laranja, Contador |
| [domain/operations.md](../domain/operations.md) | Operação, Script, Store |
| [domain/visibility.md](../domain/visibility.md) | Extratos |
| [domain/glossary.md](../domain/glossary.md) | Termos canônicos |
| [domain/open-gaps.md](../domain/open-gaps.md) | Decisões G1–G10 (histórico; canônico = money/actors/…) |

## Fora deste folder

- Specs de UX fina / design system → `docs/frontend-*` quando couber.
- Playbooks de infra/deploy → outro lugar (ex. `dockup/`).
- Decisões de produto novas → `docs/domain/`, não aqui.
