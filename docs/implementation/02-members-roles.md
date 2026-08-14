# 02 — Membros, papéis de domínio, deals, Acionistas

**Status:** feito (`Refactor/Nexus.Api` · `Refactor/web`)  
**Depende de:** [01-foundation.md](./01-foundation.md)

## Objetivo

Representar **quem existe na organização** e **mandatos de produto**: Operador, Recrutador, Laranja, Gateways, Contador, Gestor de Operações, Acionista. **Mandato = (capacidade × escopo)**; papéis batizados = **presets**.

## Canon (G11–G13)

- Membro com login: Operador, Laranja, staff, **Acionista (read-only)**. Admin semente no deploy.
- Presets + fine-tune por exceção; mandato efetivo = **união**; atenuação contínua.
- Deal de agenciamento: `operador_pct + recrutador_pct ≤ 100%`; resto = **Residual da Organização**. Recrutador-raiz (Org/Admin, `pct=0`).
- **Sem entidade Equipe.** Divisão interna emerge de Operação × Carteira.
- Capacidades distintas: `conceder_mandato` / `recrutar` / `conceder_recrutamento` / `onboard`.
- Exclusão ponta × gestão na mesma op; username aposentado (nunca reusado).

## Já existe no código

| Área | O quê |
|------|--------|
| `Mandates/` | BC hexagonal: `MemberMandate`, `AgencyDeal`, `ShareholderStake` event-sourced (Marten) |
| Catalog | `PresetCatalog` + `Capabilities` (anti-atomização) |
| Admin API | `/api/mandates/administrator/*` (presets, capabilities, deals, shareholders) |
| Me API | `/api/mandates/me/carteira` |
| Guards | Atenuação; Admin bypass; Operator exige deal; Specific op scope bloqueado até 03 |
| Web | Contas (mandatos), Deals, Acionistas |
| Testes | domínio + use cases em `Refactor.Nexus.Api.Tests` |

## Escopo

| Entra | Não entra |
|-------|-----------|
| Membro + username único | Entidade Equipe / líder de equipe |
| Conceder/revogar mandato (presets + atenuação) | Árvore de **dinheiro** (pirâmide) |
| Deal de agenciamento (`≤ 100%`) | Override de % por Operação |
| Lista de Acionistas (nível 2, %) + login read-only | Portal rico de Acionista |
| Cadastro mínimo de Laranja (preset) | Attrition completo (etapa 09) |
| Escopo Operação `None`/`All` | Grants `Specific` (etapa 03) |

## Domínio

- [actors-and-mandates.md](../domain/actors-and-mandates.md)
- [money.md](../domain/money.md) — Waterfall / nível 3 / Residual da Org
- [glossary.md](../domain/glossary.md) — Mandato, Preset, Agenciamento, Recrutador-raiz

## Critérios de pronto

- [x] Deal `> 100%` falha ao salvar.
- [x] Username único no deploy; username aposentado não reusado (etapa 01).
- [x] Presets mapeáveis; Admin = raiz. Sem entidade Equipe.
- [x] Atenuação + poda em cascata testadas.
- [x] Operator exige AgencyDeal ativo; raiz = Admin com `recrutador_pct = 0`.
- [x] Soma Acionistas `≤ 100%`.
