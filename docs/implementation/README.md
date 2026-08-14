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
5. Adiados de domínio (ciclo fino de quota, API automática de hops, retenção de log) **não** entram nas etapas v1 salvo decisão explícita. **Não** há etapa “equipes” — G11.

## Princípios do processo

| Princípio | Significado |
|-----------|-------------|
| **Domínio manda** | Use cases e invariantes vêm de `docs/domain/`; código não inventa regra de negócio. |
| **Fatias verticais** | Preferir caminho ponta a ponta fino (API → handler → persistência → leitura) a “camada inteira”. |
| **Dois livros cedo** | Conta (mundo) e Claim (ledger) nascem separados no modelo; UCs de movimento escrevem nos dois. |
| **Invariante na fronteira** | `soma claims **ativos** == saldo` (por Conta, por moeda) garantida no use case — não por FK. |
| **Mínimo viável por etapa** | Sem ES “completo” se append-only + projeções simples bastarem; evoluir depois. |
| **Singleton** | Um deploy = uma organização; sem `tenant_id`. |

Ver também: [00-principles.md](./00-principles.md).

## Mapa das etapas

| # | Doc | Foco | Status | Depende de |
|---|-----|------|--------|------------|
| 0 | [00-principles.md](./00-principles.md) | Regras do processo, DoD, fora de escopo v1 | **feito** (meta) | — |
| 1 | [01-foundation.md](./01-foundation.md) | Base: Account = identidade, Auth, Authorization, Journal (escritas) | **feito** | — |
| 2 | [02-members-roles.md](./02-members-roles.md) | Membros, papéis de domínio, deals, Acionistas | **feito** | 1 |
| 3 | [03-operations.md](./03-operations.md) | Operação + operation key; Script/Store mínimos | **feito** | 2 |
| 4 | [04-cobranca-paga.md](./04-cobranca-paga.md) | Cobrança, Conta Gateway emissão, webhook Paga | **feito** | 3 |
| 5 | [05-contas-livro-mundo.md](./05-contas-livro-mundo.md) | Contas multi-moeda, saldo, transações, quotas (conceito) | **feito** | 1 |
| 6 | [06-materializacao-claims.md](./06-materializacao-claims.md) | Materialização → Claims; waterfall; invariante nascimento | **feito** | 4, 5 |
| 7 | [07-hops-cuts-repasse.md](./07-hops-cuts-repasse.md) | Hops 1→N, cut mid-path, bundle, `repassado`, cut in-place | **feito** | 6 |
| 8 | [08-visibilidade-extrato.md](./08-visibilidade-extrato.md) | Estimativa → pendente; relatório controlado; matriz need-to-know | **feito** | 6, 7 |
| 9 | [09-attrition-reconciliacao.md](./09-attrition-reconciliacao.md) | Estados Conta/Laranja, write-off, reconciliação, causa | **feito** | 5, 6, 7 |

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
| [domain/open-gaps.md](../domain/open-gaps.md) | Decisões G1–G13 (histórico; canônico = money/actors/…) |
| [domain/auditability.md](../domain/auditability.md) | ES + log (G12) |

## Gaps

Inventário domínio × código (Refactor API + web), independente do mapa de etapas: [gaps.md](./gaps.md) (14 ago 2026). Não altera os status `feito` acima. Planos paralelos por fatia: [gap-attack-plans.md](./gap-attack-plans.md). Polish UX `Refactor/web` (sem domínio novo): [frontend-polish-plans.md](./frontend-polish-plans.md).

## Fora deste folder

- Passe humano coordenado (browser): [`docs/qa/human-pass/README.md`](../qa/human-pass/README.md). Fixes paralelos: [`docs/qa/human-pass/fix-plans.md`](../qa/human-pass/fix-plans.md).
- Specs de UX fina / design system → `docs/frontend-*` quando couber.
- Playbooks de infra/deploy → outro lugar (ex. `dockup/`).
- Decisões de produto novas → `docs/domain/`, não aqui.
