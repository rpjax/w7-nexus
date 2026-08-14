# Planos de ataque aos gaps (paralelo)

**Pergunta:** dá para atacar todos os gaps em paralelo?  
**Resposta:** **sim**, se cada plano for dono de uma fatia de código **sem overlap**. Authz HTTP (E) e eixos de emissão na UI (D) **não** são planos separados — cairiam nos mesmos controllers/páginas que a ponta. Journal **não** é paralelo ao invariante de caixa: serializa depois.

Inventário: [`gaps.md`](./gaps.md). Planos Cursor: `C:\Users\rodri\.cursor\plans\gap_*.plan.md`.  
**Não** reabre etapas 01–09 nem altera os status `feito` neste README.

## Matriz

| Plano (ficheiro em `C:\Users\rodri\.cursor\plans\`) | GAP-xx | Paralelo com | Não sobrepor ficheiros |
|------------------------------------------------------|--------|--------------|-------------------------|
| `gap_01_ponta_need_to_know_a7c31e90.plan.md` | 01, 02, 03, 04, 05, 06, 07, 08 (N destinos), 10, 11, 13, 15, 22, 24 | B; F (23+26); attrition se não tocar grants/`MemberMandate` | `App.tsx`, `AppShell`, páginas web listadas, `ChargeView`/`ChargingGuards`/rails, statement grouping, grant Mandates, `WorldAccountsPage` |
| `gap_02_invariantes_dinheiro_b8d42f01.plan.md` | 16, 19 | A; attrition; F (23+26) | `RecordWorldAccountObservationHandler`, UC Archive + `LedgerAdministratorController` (verbo novo) |
| `gap_03_journal_g12_c9e53012.plan.md` | 12 (núcleo), 17, 28 | Attrition e F **depois** de B (e de A nos handlers de query) | `LedgerJournal`, handlers 06–09, `*Journal.cs` Charging/World, `Capabilities.cs`, HTTP Journal, notas 01/08 |
| `gap_04_attrition_mandatos_d0f64123.plan.md` | 14 | A/B/F se A não editar `RecordMemberAttrition` nem `MemberMandate` | `MemberMandate`, `RecordMemberAttritionHandler`, drop/re-parent |
| `gap_05_blueprint_drift_e1a75234.plan.md` | 23, 26; **27 só onda 3** | 23+26 com A/B | `OperationsServiceCollectionExtensions`, JWT/Accounts permissions; testes GAP-27 |
| `gap_06_adiado_v1_f2b86345.plan.md` | 09, 18, 20, 21, 25; GAP-08 path designer; GAP-12 tamper | Sempre (não há código) | — |

## Onda vs serial

```text
Onda 1 (verdadeiro paralelo):  A  |  B  |  attrition*  |  F(23,26)
Onda 2:                        Journal C  (depois de B; rebase em queries da A)
Onda 3:                        GAP-27 no plano F (testes 11/16/14 já nas fatias)
Adiado:                        plano G — não implementar
```

\*Attrition: serializar **só** `MandatesServiceCollectionExtensions.cs` se A e 14 registarem UCs ao mesmo tempo. A **não** deve editar `MemberMandate.cs`.

### Ficheiros que forçam fila

| Ficheiro | Ordem |
|----------|--------|
| `WorldAccountCommandHandlers.cs` (observation) | **B** depois **C** (C só append Journal) |
| `LedgerAdministratorController.cs` | B adiciona archive; C **não** precisa deste controller se o log tiver Presentation própria. Se C adicionar rota no mesmo controller → **depois de B** |
| Handlers hop/lost/reconcile/reverse | **C depois de B** (B não os deve editar; se editar, C espera) |
| `GetMyStatementHandler.cs` / carteira query | **A** (matriz) depois **C** (uma linha Journal) **ou** C espera A |
| `MandatesServiceCollectionExtensions.cs` | A (grant) depois attrition, ou um único merge |
| `Capabilities.cs` | só **C** (`ler_log_auditoria`) |
| `WorldAccountsPage.tsx` | só **A** |
| `docs/implementation/01-foundation.md` e `08-*.md` | só **C** (nota G12); README etapas **intocado** |

## Checklist GAP-xx

| Id | Plano | Notas |
|----|-------|--------|
| 01 | A | Nav por capacidade |
| 02 | A | UI carteira |
| 03 | A | UI Operador + API autenticada |
| 04 | A | Emissão UI (cluster D merged) |
| 05 | A | Descongelar / reabrir emissão UI |
| 06 | A | Contador telas + HTTP já capability |
| 07 | A | Gestor nav/ops |
| 08 | A + G | N destinos = A; path designer = adiado |
| 09 | G | Adiado v1 |
| 10 | A | Extras UI; permissions UI espera F/26 |
| 11 | A | ChargeView (não B) |
| 12 | C + G | Núcleo C; tamper G |
| 13 | A | Grant aninhado HTTP (cluster E merged) |
| 14 | attrition | Cascata/re-parent mandatos |
| 15 | A | Queries por preset |
| 16 | B | Observation vs claims |
| 17 | C | Journal escritas |
| 18 | G | PSP/webhook produto |
| 19 | B | UC arquivar |
| 20 | G | Ciclo quota |
| 21 | G | Absorção Org |
| 22 | A | Presentation/policies confiança |
| 23 | F | Composition |
| 24 | A | Rails `gerir_gateways` |
| 25 | G | Script/Store SQL |
| 26 | F | JWT permissions |
| 27 | F onda 3 | Testes 11/16/14 |
| 28 | C | Nota G12 parcial; status 01–09 intactos |

Clusters sugeridos E e D: **fundidos em A**. Cluster C: **depois de B**. Cluster G: **won't implement now**.
