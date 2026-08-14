# Inventário de gaps — domínio vs implementação

**Status:** rascunho de inventário (não é etapa de código)  
**Data:** 14 de agosto de 2026  
**Escopo:** comparação de `docs/domain/*` + `docs/implementation` 01–09 contra o código em `Refactor/Nexus.Api` e `Refactor/web`. Sem implementação de produto neste doc.

O frontend **não** está 100% completo. `Refactor/web` cobre a **UI mínima das etapas 01–09**, não um produto acabado.

---

## Como ler

Cada item usa um id `GAP-xx` e uma severidade:

| Severidade | Significado |
|------------|-------------|
| **bloqueia produto** | Sem isso, um ator do domínio não consegue cumprir o papel no hub (API e/ou UI). |
| **dívida** | Código existe de forma incompleta, frouxa ou divergente do domínio/blueprint; não impede um Admin de exercitar o fluxo mínimo. |
| **adiado v1** | Decidido fora do fechamento de domínio / DoD das etapas; não esquecido. |
| **UI** | API (ou domínio) existe; a tela mínima da etapa não cobre, ou o produto pediria mais. |

Quatro naturezas (não misturar):

1. **Adiado de propósito (domínio v1)** — fila #13 em [domain/README.md](../domain/README.md); etapas 00–09 repetem o mesmo recorte.
2. **UI mínima da etapa vs UI de produto** — DoD de implementação **não** exige dashboard polido nem telas por papel.
3. **Falta real vs docs** — UC, invariante ou HTTP descritos e ausentes (ou incompletos) no código.
4. **Gap independente** — qualidade, authz, Journal, blueprint, testes/docs — não necessariamente um “esqueci a etapa”.

Itens marcados **a verificar** não foram fechados só pela leitura estática.

---

## O que o web cobre hoje (mínimo 01–09)

**Existe**

- Login / perfil (usuário e senha).
- Extrato autenticado (estimativa → pendente / perda).
- **Admin:** Contas (mandatos + attrition de membro); Livro-mundo (quota, observação, congelar saldo, lost, reconciliar, exposição); Operações (ciclo, assign, Script/Store, cut, bind de trilho de emissão); Cobranças (Paga / materializar / reverse / transições); Claims (hop / cut / repasse / reveal / reverse); Deals; Acionistas.

**Não está completo**

- Papéis não-Admin (Operador / Recrutador / Laranja / Acionista / Gateways / Contador / Gestor) quase só veem **perfil + extrato**. Não há UI para `GET /api/mandates/me/carteira`.
- Livro-mundo mostra `emissionStatus` mas **não** oferece controle de emissão `ok | bloqueada` (só freeze de saldo / lost).
- Contador vs Admin: a nav é **tudo-ou-nada Admin**; não há telas filtradas por mandato.
- **Fora de v1 de produto (intencional):** dashboard polido; path designer rico de hops; portal rico de Acionista; UI de Journal/replay; webhook real (Paga é botão no painel).

---

## Lista completa

### A. Frontend / produto UI

#### GAP-01 — Nav e rotas só Admin vs restante dos papéis
- **Severidade:** bloqueia produto (para papéis de confiança e ponta usarem o hub sem ser Admin)
- **Docs:** [visibility.md](../domain/visibility.md) (matriz); [vision.md](../domain/vision.md) (revelação progressiva); etapa [02](./02-members-roles.md)
- **Código:** `App.tsx` envolve contas, livro-mundo, ops, cobranças, claims, deals e acionistas em `RequireAdministrator`. `AppShell` filtra `adminOnly`.
- **Falta:** nav e rotas por capacidade/preset (Contador, Gestor, Gateways, Recrutador, Operador, Laranja, Acionista).

#### GAP-02 — Sem UI de carteira
- **Severidade:** UI
- **Docs:** [visibility.md](../domain/visibility.md) (Recrutador vê carteira); etapa 02 (Me API)
- **Código:** `GET /api/mandates/me/carteira` (`MandatesMeController` + `GetMyCarteiraHandler`)
- **Falta:** página / cliente HTTP em `Refactor/web`. Recrutador autenticado não vê downline no produto.

#### GAP-03 — Sem UI de cobranças do Operador
- **Severidade:** UI (API parcial existe)
- **Docs:** [visibility.md](../domain/visibility.md) — Operador vê as próprias cobranças; [04](./04-cobranca-paga.md)
- **Código:** `GET/POST /api/charging/authenticated`; listagem filtra pelo `requesterId` se não for Admin
- **Falta:** tela autenticada; Operador só gera cobrança se alguém chamar a API. Painel Admin chama `POST /authenticated` por cima.

#### GAP-04 — Emissão ok\|bloqueada só no backend
- **Severidade:** UI (API existe)
- **Docs:** [open-gaps.md](../domain/open-gaps.md) G4; [09](./09-attrition-reconciliacao.md); [money.md](../domain/money.md)
- **Código:** `PUT /api/world-accounts/administrator/{id}` aceita `emissionStatus`; agregado `SetEmissionStatus`; quota recusa `EmissionBlocked`
- **Falta:** botão/controle na página Livro-mundo (mostra o status; freeze só manda `balanceStatus: Frozen`)

#### GAP-05 — Descongelar saldo / reabrir emissão
- **Severidade:** UI
- **Docs:** dois eixos independentes (G4)
- **Código:** `configure` aceita `balanceStatus: Accessible` e `emissionStatus: Ok`
- **Falta:** ações na UI (só freeze → lost)

#### GAP-06 — Contador: API financeira vs tela Admin
- **Severidade:** bloqueia produto (Contador sem JWT Admin)
- **Docs:** G3; [08](./08-visibilidade-extrato.md) / [07](./07-hops-cuts-repasse.md) — Contador/Admin
- **Código:** `LedgerGuards` autoriza `registrar_movimento_financeiro` **ou** Admin; `WorldAccountAccessAdapter` autoriza `gerir_gateways` **ou** Admin
- **Falta:** UI e rotas para Contador/Gateways. HTTP de **rails de emissão** (`ChargingGuards.AuthorizeAdminAsync`) é **só Admin** — Gateways não bindam trilho via API (dívida acoplada: ver GAP-24).

#### GAP-07 — Gestor de Operações: API filtrada vs UI Admin
- **Severidade:** UI (API parcial)
- **Docs:** G3/G11; [03](./03-operations.md)
- **Código:** listagem de ops filtra por `CanManageOperationAsync` (`gerir_operacao` + escopo)
- **Falta:** rota/nav para Gestor; grant de mandato/preset na UI continua atrás de Admin.

#### GAP-08 — Hop 1→N e path designer
- **Severidade:** UI (mínimo da etapa 07 existe)
- **Docs:** [07](./07-hops-cuts-repasse.md) — UI path designer rica **fora**; hop canônico 1→N **entra**
- **Código:** `POST /hops` aceita lista de destinos; `ClaimsPage` tem **um** par conta/valor
- **Falta:** N destinos na UI; desenho de rota. Não bloqueia o UC se o Contador repetir hops ou usar API.

#### GAP-09 — Fora de v1 de produto (UI)
- **Severidade:** adiado v1
- **Docs:** [00-principles.md](./00-principles.md) DoD; [domain/README.md](../domain/README.md) adiados
- **Código:** dashboard `HomePage` é resumo de sessão; Paga = `markChargePaid`; sem Journal HTTP
- **Falta (proposital):** dashboard polido; portal rico de Acionista; Journal/replay UI; webhook PSP real

#### GAP-10 — Extra UI vs APIs (mínimo)
- **Severidade:** UI
- **Docs:** etapas 01–05 (HTTP extra)
- **Código vs web:**
  - `PUT .../world-accounts/{id}/label` — sem tela dedicada
  - `GET .../edge/scripts/{operationKey}` — cliente em `operations.ts`, sem página de ponta
  - `GET/PUT/DELETE` Store e POST Script — cobertos na página Operações (Admin)
  - Grant/revoke **permissions** de Account (`/api/accounts/administrator/permissions`) — HTTP existe; UI de Contas não chama
  - `GET /api/ledger/administrator/claims/{id}` — listagem agregada na UI, sem ficha única
- **Falta:** o acima, se o produto quiser além do mínimo da etapa.

---

### B. Visão, mandatos e need-to-know

#### GAP-11 — `ChargeView` vaza split e Laranja para o Operador
- **Severidade:** bloqueia produto (need-to-know)
- **Docs:** [visibility.md](../domain/visibility.md) — Operador não vê split dos outros / path
- **Código:** `ListChargesHandler.ToView` devolve `SplitIntent` e `OrangeMemberId` no `GET /api/charging/authenticated`
- **Falta:** contrato de leitura estreito para ponta (sem intenção waterfall completa nem id do Laranja)

#### GAP-12 — `ler_log_auditoria` e HTTP do Journal
- **Severidade:** dívida (G12 núcleo) / **adiado v1** no refino (tamper-evidence, retenção)
- **Docs:** [auditability.md](../domain/auditability.md); etapa [01](./01-foundation.md) (Journal sem HTTP nesta etapa — nunca voltou)
- **Código:** capacidade `ler_log_auditoria` + `GET /api/journal/administrator/entries` (envelope, sem payload cru); Journal nas mutações 06–09 + observation/archive + charging/world. Sem hash-chain; sem `CorrelationId` ES↔log
- **Falta (adiado G):** tamper-evidence, retenção, UI Journal, correlação fina. Núcleo HTTP/capacidade/escritas: **parcialmente fechado**.

#### GAP-13 — Conceder mandato só via Admin HTTP
- **Severidade:** dívida
- **Docs:** G11 — `conceder_mandato` com atenuação; autoridade aninha
- **Código:** domínio atenua se o grantor não for Admin; **handlers** de preset/capability usam `MandateAdministratorGuards` (role Admin)
- **Falta:** porta autenticada para Gestor/Recrutador com `conceder_mandato` no escopo (sem ser Administrator)

#### GAP-14 — Attrition de membro: sinal vs cascata / re-parent
- **Severidade:** dívida
- **Docs:** G4/G11 — queimado/traiu suspende **cascata**; saída voluntária **re-parent**; [09](./09-attrition-reconciliacao.md) entrega “só sinaliza; sem cascata de Contas”
- **Código:** `RecordMemberAttrition` grava status+causa no `MemberMandate`; **não** chama `DropGrantsIssuedBy` / re-parent; Contas não são auto-burn (alinhado à etapa 09)
- **Falta:** cascata de **mandatos** (domínio) vs Contas (etapa 09 adiou de propósito). Re-parent **não** implementado.

#### GAP-15 — Matriz de visão por preset incompleta nas queries
- **Severidade:** dívida
- **Docs:** [08](./08-visibilidade-extrato.md) — query Operador/Recrutador/Acionista/Laranja; dois regimes G13
- **Código:** um `GET .../statement` agrupa claims do `beneficiaryId` do login (serve os quatro se tiverem claim). Contador vê claims reais só nas rotas administrator
- **Falta:** views distintas (Laranja: bruto nas contas dele; Recrutador: fatia + agenciados; Gestor: split das ops sob mandato). Recrutador **não** vê estimativa dos agenciados no statement (só claims em que ele é beneficiário). **a verificar** se isso é aceitável como “mínimo 08”

---

### C. Dinheiro / dois livros / invariantes

#### GAP-16 — Observação de caixa sem ledger
- **Severidade:** bloqueia produto (invariante G6/G9) se usada com claims ativos
- **Docs:** [money.md](../domain/money.md) — caixa nunca anda sozinho; reconciliação = porta estreita; etapa [05](./05-contas-livro-mundo.md) permitiu observação como seed
- **Código:** `POST .../observations` credita/debita `WorldAccount` **sem** tocar Claims. UI Admin usa isso no Livro-mundo
- **Falta:** recusar observação quando há claims na Conta/moeda, **ou** exigir reconciliação; não deixar Admin furar a invariante `soma claims ativos == saldo`

#### GAP-17 — Journal nas escritas novas do ledger
- **Severidade:** dívida
- **Docs:** G12 — escrita → ES + log; [08](./08-visibilidade-extrato.md) cita Journal só em statement/reveal
- **Código:** fatos Journal em materialize/hop/repasse/lost/reconcile/reverse/archive + charging/world `*Journal.cs`. Statement/reveal continuam.
- **Falta:** — núcleo fechado; tamper-evidence continua GAP-12 adiado.

#### GAP-18 — Webhook Paga vs PSP
- **Severidade:** adiado v1 (integração) + **dívida** operacional
- **Docs:** [04](./04-cobranca-paga.md) — webhook entra; PSP real (`IPaymentIssuer` no-op) não entra
- **Código:** `POST /api/charging/webhooks/paid` + secret; `NoOpPaymentIssuer`; UI `mark-paid` (Admin)
- **Falta:** emissor real; UI de ponta não dispara Paga (proposital)

#### GAP-19 — Status `arquivado` do Claim sem UC
- **Severidade:** dívida
- **Docs:** glossário / G13 — `arquivado` terminal
- **Código:** `Claim.Archive` + evento `ClaimArchived` no Marten; **nenhum** handler/HTTP
- **Falta:** UC que case arquivo com livro-mundo (quando fizer sentido)

#### GAP-20 — Quota: ciclo calendário vs rolling
- **Severidade:** adiado v1
- **Docs:** domain README fila #13; [05](./05-contas-livro-mundo.md)
- **Código:** `quotaRemaining` por moeda; consumo na emissão
- **Falta:** reset de ciclo (não era DoD)

#### GAP-21 — Absorção central de perda pela Org
- **Severidade:** adiado v1
- **Docs:** G4 — Org não absorve na v1; [09](./09-attrition-reconciliacao.md)
- **Código:** write-off mecânico nos claims da Conta
- **Falta:** feature futura (não gap de etapa)

---

### D. Authz, HTTP e arquitetura

#### GAP-22 — Authz por capacidade vs pastas `Administrator`
- **Severidade:** dívida
- **Docs:** blueprint (roles só em Ports/UseCases/Authorization/Http); G11
- **Código:** vários BCs já checam capability **dentro** de handlers `Administrator/*`; Presentation ainda é árvore Admin. Charging **rails** = Admin puro. Mandates writes = Admin puro. Ledger writes = Admin **ou** Contador
- **Falta:** alinhar Presentation/policies com a matriz (Contador/Gateways/Gestor) sem exigir role `Administrator`

#### GAP-23 — Cross-domain `Infrastructure` no Composition
- **Severidade:** dívida (blueprint)
- **Docs:** [architecture-blueprint.md](../architecture-blueprint.md) — sem referência Infrastructure entre BCs
- **Código:** `OperationsServiceCollectionExtensions` faz `using ... Mandates.Infrastructure.Operations` para registrar adapter. Outros BCs adaptam via **ports** (melhor). `Program.cs` / `EventStoreServiceCollectionExtensions` conhecem vários `*.Infrastructure.Persistence`
- **Falta:** Composition raiz (já parcialmente) em vez de BC A importar Infrastructure de B

#### GAP-24 — Bind de trilho de emissão só Admin
- **Severidade:** dívida / bloqueia Gateways
- **Docs:** [actors](../domain/actors-and-mandates.md) — Gateways gerem contas/%/quotas; [04](./04-cobranca-paga.md)
- **Código:** `RailCommandHandlers` + queries de rails usam `AuthorizeAdminAsync`
- **Falta:** `gerir_gateways` (e talvez Gestor no escopo da op) nas mesmas portas

#### GAP-25 — Script/Store SQL vs ES-by-default
- **Severidade:** adiado / exceção documentada
- **Docs:** [00-principles.md](./00-principles.md) — corpo Script/Store é exceção
- **Código:** `PostgresScriptArtifactRepository` / `PostgresStoreObjectRepository`; Operation em Marten
- **Falta:** nada obrigatório na v1

#### GAP-26 — JWT `permissions` não autorizam
- **Severidade:** dívida
- **Docs:** etapa 01 — “Permissions no JWT ainda não autorizam nada”
- **Código:** grant/revoke permissions HTTP; guards usam role Admin + snapshot de mandato
- **Falta:** ou ligar JWT a capacidades, ou remover o canal morto (UI já não usa)

---

### E. Testes / docs

#### GAP-27 — Drift testes × fatias novas
- **Severidade:** dívida — **fechado** (onda 3 do plano F; regressões 11/16/14 + Composition/JWT já no CI)
- **Docs:** DoD etapa 00 — testes do caminho feliz + violações
- **Código:** `ChargeUseCaseTests` (ChargeView Operador sem split/Laranja; GetCharge; Contador vê split); `WorldAccountTests` (observação seed-only, inclusive por moeda); `MandateAttritionTests` (cascata burned/betrayed + re-parent `left`); `CompositionLayoutTests`; `JwtPermissionAuthorizationTests`. Quarentena `_quarantine/` fora do compile.
- **Falta:** — cobertura das fatias A/B/14/F no `Refactor/Nexus.Api.Tests`.

#### GAP-28 — Etapas 01–09 `feito` vs G12 incompleto
- **Severidade:** dívida de processo (não reescrever o mapa aqui)
- **Docs:** README implementação marca 01–09 **feito**; G12 pede log de leituras + tamper-evidence
- **Código:** ES Marten nos agregados combinados; Journal cobre Accounts/Mandates/Operations + ledger 06–09 + charging/world. HTTP `ler_log_auditoria` existe. Notas G12 parciais em 01/08.
- **Falta:** tamper-evidence ainda adiado. Status 01–09 permanece **feito**.

---

## Walkthrough domínio × código

### Visão ([vision.md](../domain/vision.md))
Singleton, need-to-know, cargos de confiança ≠ Admin. **No código:** um deploy, sem `tenant_id`. **Gap:** o hub **não se revela** por mandato (GAP-01); Admin é o único “produto”.

### Atores e mandatos
Presets, capacidades, atenuação, deals, acionistas, carteira API: **existem**. Concessão aninhada e attrition em cascata: **domínio sim, HTTP/UC incompletos** (GAP-13, GAP-14). Dono fora do produto: ok.

### Operações
Ciclo, key 1:1, assign, Script/Store mínimo, cut de gestão: **etapa 03 feita** (Admin + edge). Gestor na UI: GAP-07.

### Dinheiro
Dois livros, emissão, Paga, materialização, waterfall, hops, cut in-place, redenominação, repasse, lost/write-off, reconciliação, estorno: **UCs Admin/Contador existem**. Buracos: observação solta (GAP-16), emissão na UI (GAP-04), webhook/PSP (GAP-18), `arquivado` (GAP-19).

### Visibilidade
Statement autenticado sem hops/contas: **alinhado G10**. Reveal + relatório mínimo: **existe**. Matriz completa e queries por papel: **não** (GAP-11, GAP-15). Contador vê o real **só se tiver HTTP Admin/capability**, não tela.

### Auditabilidade
ES plug-in (Marten) atrás de repositórios: **alinhado ao blueprint**. Log ≠ ES: Journal existe. G12 completo (leituras, hash-chain, correlação, `ler_log_auditoria`): **não** (GAP-12, GAP-17).

### Glossário — conceitos com UC esperado

| Conceito | UC no código? |
|----------|----------------|
| Cobrança / Paga / materialização / hop / repasse / reveal | Sim (Admin + alguns authenticated) |
| Reconciliação / write-off lost / estorno | Sim |
| Carteira | Query sim; UI não |
| Conceder mandato (não-Admin) | Domínio sim; HTTP não |
| Ler log auditoria | Não |
| Arquivar claim | Método de agregado; sem UC |
| Observação de saldo | Sim — **conflita** com invariante se houver claims |

### G1–G13 — decisão vs código

| Gap | Decisão de domínio | No código? |
|-----|--------------------|------------|
| G1 | Sem átomo; materialização + claims | Sim (06) |
| G2 | Cut proporcional + quotas | Sim (07 + 05); UI de path pobre |
| G8/G10 visibilidade | Estimativa congelada → reveal | Sim (08); leak em ChargeView |
| G3 | Contador/Gestor ≠ Admin | Capability no ledger/ops; UI e várias APIs ainda Admin |
| G4 | Dois eixos Conta; Laranja attrition | Eixos no agregado; emissão UI falta; membro só sinaliza |
| G5 | Sem pool; residual Org | Sim (06) |
| G6 | Reconciliação estreita; invariante | Reconcile/lost sim; **observations** furam |
| G7 | Singleton | Sim |
| G9 | Dois livros; caixa nunca só | Quase; GAP-16 |
| G10 multi-moeda | Sem FX; hop declara | Sim |
| G11 | Mandato × escopo; sem Equipe | Presets/escopo sim; delegação HTTP Admin-only |
| G12 | ES + log + tamper-evidence | ES sim; log parcial; tamper não |
| G13 | Endurecimento (máquinas de estado, etc.) | Maioria nos agregados 06–09; `arquivado` e cascata attrition frouxos |

---

## Não-gaps (etapas 01–09 `feito` que existem)

Resumo — o mapa de etapas **não** está mentindo sobre o núcleo Admin/API:

| Etapa | O que de fato está no Refactor |
|-------|--------------------------------|
| 00 | Processo / DoD / adiados |
| 01 | Account, auth local, seed Admin, Journal de escritas de identidade, username aposentado |
| 02 | Mandates (presets, capabilities, deals ≤100%, acionistas, carteira API, atenuação) |
| 03 | Operation ES + Script/Store SQL keyed + edge |
| 04 | Charge ES, trilho=`WorldAccount`, mark-paid + webhook secret, transições |
| 05 | WorldAccount multi-moeda, quota, eixos de estado, TX |
| 06 | MaterializeCharge, waterfall, Residual Org, invariante nascimento |
| 07 | Hop 1→N, cut path/in-place, repasse |
| 08 | BirthAmount, reveal, statement autenticado |
| 09 | Lost+write-off, reconcile, reverse, exposure, attrition de membro (sinal) |

Isso é **mínimo vertical Admin + alguns authenticated**, não produto need-to-know.

---

## Próximas fatias sugeridas (sem novo mapa de etapas)

Ordem prática se o objetivo for “o domínio sobrevive ao uso real”, não cosmética:

1. **Fechar invariante** — observação de caixa vs claims (GAP-16); testes.
2. **Need-to-know na Cobrança autenticada** — enxugar `ChargeView` (GAP-11); UI Operador (GAP-03).
3. **Authz + UI de confiança** — Contador/Gateways nas rotas que a API já permite; emissão bloqueada na UI; bind de trilho com `gerir_gateways` (GAP-01, 04, 06, 24).
4. **Carteira Recrutador** — tela em cima da API (GAP-02).
5. **G12 mínimo** — Journal nas escritas ledger/charging/world; leituras de rotas/contas; HTTP Admin de consulta (sem hash-chain ainda = refino adiado).
6. **Attrition de mandato** — cascata / re-parent (GAP-14) quando doer.
7. Cosmético / adiados: dashboard, path designer, PSP, ciclo de quota, portal Acionista.

Não abrir etapa “10” neste folder até decidir se G12 e need-to-know são processo novo ou dívida das etapas já marcadas `feito`.
