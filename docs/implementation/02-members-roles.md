# 02 — Membros, papéis de domínio, deals, Acionistas

**Status:** pendente  
**Depende de:** [01-foundation.md](./01-foundation.md)

## Objetivo

Representar **quem existe na organização** e **mandatos de produto**: Operador, Recrutador, Laranja, Gateways, Contador, Gestor de Operações, Acionista. **Mandato = (capacidade × escopo)**; papéis batizados = **presets**.

## Canon (G11–G13)

- Membro com login: Operador, Laranja, staff, **Acionista (read-only)**. Admin semente no deploy.
- Presets + fine-tune por exceção; mandato efetivo = **união**; atenuação contínua.
- Deal de agenciamento: `operador_pct + recrutador_pct ≤ 100%`; resto = **Residual da Organização**. Recrutador-raiz (Org/Admin, `pct=0`).
- **Sem entidade Equipe.** Divisão interna emerge de Operação × Carteira.
- Capacidades distintas: `conceder_mandato` / `recrutar` / `conceder_recrutamento` / `onboard`.
- Exclusão ponta × gestão na mesma op; handle aposentado (nunca reusado).

## Escopo

| Entra | Não entra |
|-------|-----------|
| Membro + handle único | Entidade Equipe / líder de equipe |
| Conceder/revogar mandato (presets + atenuação) | Árvore de **dinheiro** (pirâmide) |
| Deal de agenciamento (`≤ 100%`) | Override de % por Operação |
| Lista de Acionistas (nível 2, %) + login read-only | Portal rico de Acionista |
| Cadastro mínimo de Laranja | Attrition completo (etapa 09) |

## Use cases (mínimo)

- Conceder / revogar mandato (preset ou fine-tune); poda em cascata ao estreitar.
- Criar/atualizar deal Recrutador↔Operador (validar `≤ 100%`).
- CRUD mínimo Acionistas (Admin).
- Queries: “minha carteira” (Recrutador) — pode ser stub de leitura.

## Domínio

- [actors-and-mandates.md](../domain/actors-and-mandates.md)
- [money.md](../domain/money.md) — Waterfall / nível 3 / Residual da Org
- [glossary.md](../domain/glossary.md) — Mandato, Preset, Agenciamento, Recrutador-raiz

## Critérios de pronto

- [ ] Deal `> 100%` falha ao salvar.
- [ ] Handle único no deploy; handle aposentado não reusado.
- [ ] Presets mapeáveis; Admin = raiz. Sem entidade Equipe.
