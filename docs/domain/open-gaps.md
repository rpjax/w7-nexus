# Gaps críticos — domínio precisa sobreviver ao mundo real

Este doc existe para **não perder nenhum gap** enquanto discutimos um a um. Regra: nada sai daqui até virar decisão registrada no doc correspondente (`money.md`, `actors-and-mandates.md`, etc.) **e** o gap for marcado como `resolvido` aqui, com link.

Cada gap tem: **o que quebra**, **cenário concreto** que expõe a quebra, e **status**.

## Como usar

- Vamos um a um, na ordem de severidade abaixo (ou na ordem que você escolher).
- Ao resolver um gap, atualizamos o doc de domínio afetado **e** marcamos aqui como `resolvido — ver <doc>#<seção>`.
- Se decidirmos adiar de propósito (não é o mesmo que ignorar), marcar `adiado — motivo`.
- Novos gaps encontrados no meio da discussão entram aqui também, não se perdem no chat.

## Status possíveis

`aberto` · `em discussão` · `resolvido` · `adiado`

---

## Lista de gaps

### G1 — Átomo vs fungibilidade — RESOLVIDO (modelo trocado)

**Decisão:** abandonar átomo. Fluxo:

1. Webhook **Paga** = grana já existe no mundo.
2. Contador **materializa** valor líquido **X** (pagamento inteiro; sem parcial).
3. % do split aplicam-se sobre X → **R$ concretos**; depois só **ownership em Conta**.
4. Creditar **saldo** na Conta Gateway → hops (destino já líquido) → repasse.
5. Laranja mid-path = **cut % proporcional** no hop (independente), não % sobre B antigo.
6. **GUID** do pagamento + forks (parcial / Laranja mid-path) + **event sourcing**.
7. Sem tesouraria abstrata: ownership **sempre** em Conta.

**Status:** resolvido — ver [money.md](./money.md)

---

### G2 — Laranja mid-path / cut proporcional / quotas — RESOLVIDO

**Decisão:**
- Cut mid-path = **N % do valor em trânsito** no hop; **rateio proporcional** em todos os ownerships daquele valor (não escolha manual de quem paga).
- **Contador** registra; **Admin** também pode.
- **1× por Laranja** no fluxo/GUID.
- **Sem teto** artificial no path; limites via **quotas** de Conta (gateway/banco/crypto) por ciclo — emissão de Cobrança e hops respeitam margem.
- Beneficiários: ver G8.

**Status:** resolvido — ver [money.md](./money.md) (Laranja mid-path, Quotas)

---

### G8 — Extrato vs ajuste mid-path — RESOLVIDO (refinado por G10)

**Decisão original:** beneficiários veem prestação de contas básica sem hops nem identidades.

**⚠ Refinado pelo G10:** a ideia de mostrar "valor atual/pago" **atualizando** ao longo da rota foi **descartada** (vaza o path por canal lateral). Modelo vigente: **estimativa congelada** (base materialização) → flag do Contador → **pendente/a receber** + **relatório controlado**. Ver G10.

**Status:** resolvido (via G10) — ver [visibility.md](./visibility.md) (Estimativa → Pendente)

---

### G3 — Contador / cargos de confiança vs compartimentalização — RESOLVIDO

**Onde:** `visibility.md`, `vision.md`, `actors-and-mandates.md`.

**Decisão:**
- **Contador** = cargo de confiança com **visão ampla do financeiro**: split **completo** + rotas/destinos para hops. Sem isso o papel não funciona no mundo real.
- Isso **não** equivale a entregar organograma, lista de todos os operadores/ops, nem grafo de recrutamento — o hub não dá isso de bandeja. Risco residual de *inferência* ao longo do tempo: **aceito**.
- **Gestor de Operações** = cargo de confiança para **delegar** ops; vê split completo **das ops sob mandato**; **≠ Admin**.
- Need-to-know na visão foi **ajustado**: default estreito na ponta; cargos de confiança ampliam escopo de propósito (hierarquia prática do mundo real).

**Status:** resolvido — ver [visibility.md](./visibility.md), [vision.md](./vision.md) (hierarquia prática)

---

### G4 — Attrition (queima) de Laranja/Conta — RESOLVIDO

**O que quebrava:** o domínio assumia que Laranjas e Contas só crescem, nunca falham — sendo attrition o evento **mais comum** nesse tipo de org.

**Sacada:** attrition = morte de **um dos dois pontos de vista** da Conta (gerador de pagamento ↔ detentor de saldo). Não exige framework novo.

**Decisões:**
- **Estado de Conta = dois eixos independentes:** `emissão: ok|bloqueada`, `saldo: acessível|congelado|perdido`. (Escolhido sobre enum único.)
- **Estado de Laranja:** `ativo → queimado | saiu (voluntário) | traiu`. Queimar Laranja **sinaliza** contas como suspeitas; **não** hard-burn automático (Contador decide conta a conta). Conta pode morrer sem o Laranja cair.
- **Cobranças Abertas em conta com emissão bloqueada:** não morrem automático; param de nascer novas; existentes seguem até Paga ou expiram; cancelamento manual (reality-first).
- **Re-atribuição:** não reescreve conta de emissão de Cobrança já enviada; atualiza o **conjunto de contas de emissão disponíveis da Operação**.
- **Claim preso:** localizado em Conta com saldo congelado/perdido; exposição = soma dos claims ali; vira `perdido` via write-off (G6); se recuperado, hop normal.
- **Incidência da perda: MECÂNICA** — cai sobre quem tinha claim naquela Conta. Org **não** absorve na v1 (absorção central = feature futura, mecânica a mais on top). Prioridade: modelo simples e fiel à realidade.
- **Causa (intel):** todo evento de attrition/write-off carrega `causa ∈ { bloqueio_bancario, apreensao, traicao, saida_voluntaria, erro_operacional, desconhecido }`. Reputação por Laranja/conta/gateway = replay de eventos (sem subsistema de analytics).

**Status:** resolvido — ver [actors-and-mandates.md](./actors-and-mandates.md) (Laranja § Attrition), [glossary.md](./glossary.md). Estados de Conta a consolidar no `money.md` na task #6.

---

### G5 — "Pool" do nível 3 / invariante ≤ 100% — RESOLVIDO (dissolvido: não há pool)

**Reenquadramento (o gap era um falso problema herdado):** não existe **pool nem bucket** de nível 3. O split é **intenção em %** resolvida por **ordem waterfall** (já fixada); na materialização vira **claims concretos** e o `%` morre. Depois: sem pool, sem batch — só claims que se movem independentes por hops/paths. Rastreabilidade via ledger paralelo.

**Decisão:**
- **Base do nível 3** = valor *calculado* (remanescente pós Laranja+Acionista sobre o líquido X), **não** objeto armazenado.
- `operador_pct + recrutador_pct ≤ 100%` é invariante de **autor único** — o **deal de agenciamento** define os dois números; validada no ato de salvar o deal (fail-fast). Sem multi-autor competindo → 130% não pode ocorrer.
- Waterfall não passa de 100% por construção; não há "dono de invariante" cross-mandato a inventar.
- **`demais` configurável (líderes)** = adiado com equipes. **Princípio gravado:** quando voltar, não criar bucket multi-autor — usar waterfall (deal primeiro; líderes do remanescente).

**Status:** resolvido — ver [money.md](./money.md) (Rateio do nível 3), [glossary.md](./glossary.md) (Base do nível 3 / Split intenção).

---

### G6 — Write-off, perda e reconciliação — RESOLVIDO

**Invariante inviolável:** dentro do Nexus o estado é **sempre** consistente — por Conta `soma dos claims == saldo` (**escopo-org**), claim ≥ 0, todo claim com beneficiário real (sem limbo "desconhecido"). Garantida na **fronteira de use case**, não por FK (compatível com G9).

**Escopo-org do saldo:** o Nexus modela só a grana **da organização**. Dinheiro pessoal do dono da conta é **invisível/irrelevante** — o book-keeping deixa tudo-da-org accounted for; se o Laranja misturou grana pessoal numa conta dedicada, problema dele.

**Duas perdas distintas:**
| Mecanismo | Quando | Como |
|-----------|--------|------|
| **Encolhimento no hop** | Taxa/FX na movimentação | Implícito: destino já líquido (saiu 100, chegou 95); claims do bundle encolhem proporcional |
| **Write-off** | Grana some **sem destino** (presa/apreendida/erro) | Explícito: claim → `perdido` + **causa**; evento ES |

- Taxa de gateway → já embutida no **líquido X** da materialização.
- "Dust"/arredondamento → dissolve na reconciliação (Contador digita valores reais).

**Reconciliação = UC oficial e estreito** (não bypass de validação): a divergência vive só entre **Nexus e a realidade** (banco/carteira que o Contador observa). A reconciliação importa a realidade e produz **estado válido**, respeitando invariantes:
- **Falta** (real < Nexus): baixa rateada **proporcional nos claims da Conta** (default) ou atribuída, com causa.
- **Sobra** (real > Nexus): atribuída a **beneficiário real** (ex.: residual da Org) — nunca fica "desconhecida".

**Reversibilidade:** correção de fato errado = **evento compensatório** (append-only, ES); nunca delete/mutação. → é ES + contabilidade textbook.

**Status:** resolvido — ver [glossary.md](./glossary.md) (Invariante de consistência, Reconciliação, Evento compensatório) e [money.md](./money.md) (Estado). Corpo detalhado no `money.md` na task #6.

---

### G7 — Organização: singleton — RESOLVIDO

**Decisão:** **um Nexus = uma Organização** (singleton por deploy). Nexus é **software sob medida** para uma org específica (websete/Grupo Thal), **não** produto de prateleira / SaaS / multi-tenant.

- "Global" = global à org; handle único = único no deploy; Acionista global sem ambiguidade.
- Segundo "capítulo" = **outro deploy** (isolamento melhor, raio de explosão menor — coerente com a blindagem).
- `tenant_id` / abstração multi-org = **anti-requisito** (YAGNI + segurança). `Organização` = raiz conceitual implícita onde penduram regras globais.

**Status:** resolvido — ver [vision.md](./vision.md) (Natureza do produto).

---

### G10 — Multi-moeda (cripto) + visibilidade de estimativa — RESOLVIDO

**Parte A — multi-denominação:** o Nexus suporta saldo em qualquer moeda (real/cripto) e **nunca converte** — só registra a realidade "X de Y".
- **Conta é multi-moeda** (saldo por denominação). **Não** forçar "uma conta = uma moeda" (carteira cripto multi-ativo é real — não inventar invariante que o mundo não tem).
- **Invariante** passa a ser **por Conta, por moeda:** `soma dos claims naquela moeda == saldo naquela moeda`.
- **Claim** ganha `moeda`. Hop que **redenomina** (BRL→cripto): claim consumido na moeda de origem, renasce na do destino com valor **declarado** pelo Contador; sem FX calculado.
- **Perda/cut só dentro da mesma moeda** — atravessar fronteira de moeda não tem "encolheu 5" (sem régua comum).

**Parte B — estimativa vs pendente (corrige G8):** a versão anterior mostrava "valor atual" **atualizando** pelos hops → **vazamento de canal lateral** (o ritmo das atualizações revela o path). Substituído por:
- **Estimativa congelada** (base = **materialização inicial**, não o bruto) — não acompanha hops.
- Contador/Admin marca o claim **visível** ao chegar no hop final (pendente só de repasse) → semântica muda de *estimativa* para **pendente/a receber** (valor concreto, na moeda de aterrissagem).
- No reveal, **relatório controlado** de alto nível (por que menos / perdeu tudo) — **nunca hop-by-hop**, sem Contas/identidades.
- Cargos de confiança não passam por isso (veem o real direto).

**Status:** resolvido — ver [money.md](./money.md) (Multi-denominação), [visibility.md](./visibility.md) (Estimativa → Pendente), [glossary.md](./glossary.md).

---

### G9 — Dois livros: mundo (fungível) vs ledger (semântica) — RESOLVIDO

**Diagnóstico:** G1 “resolveu” o átomo trocando por **ownership em Conta**. Isso ainda tratava saldo como objeto não-fungível (“este R$ é de X”). No mundo real a Conta guarda **um número + lista de transações**; o contexto (“esses 50 são do pagamento Y / devem ao Operador Z”) **não mora no saldo** — mora no **ledger do Contador**.

**Decisão — dois livros, ambos DENTRO do Nexus:**

> O Nexus **é** o ledger do Contador — não espelha nem sincroniza um ledger externo/físico. Os dois livros abaixo são projeções internas, ambas alimentadas pelo Contador. Ele não mantém planilha paralela: o Nexus é a ferramenta de trabalho dele.

| Livro | O que modela | Não modela |
|-------|----------------|------------|
| **Livro-mundo (Contas)** | Contas, saldo fungível (**número**), transações (± saldo) — registro do que o Contador **observou** na realidade | Ownership, cuts, “de quem é”, Cobrança, semântica |
| **Ledger (semântica)** | Toda rastreabilidade: Claims, cuts, materialização, “pago”, ajustes de rota, linhagem/eventos | Não é o saldo da Conta |

**Desconexão = só no modelo:** sem FK / relacionamento entre saldo da Conta e Claim. O saldo **não sabe** e **não tenta** modelar semântica.

**Acoplamento = no use case (invariante precisa):** **caixa nunca anda sozinho** (todo movimento de caixa alimenta o ledger no mesmo ato); **o ledger pode andar sozinho** quando não há movimento de caixa (ex.: cut in-place — Laranja ganha pedaço sem transferência bancária). Proibido mover saldo sem alimentar o ledger.

**Consistência (ver G6):** dentro do Nexus os dois livros são **sempre** consistentes — `soma dos claims numa Conta == saldo` (invariante inviolável, garantida na fronteira de use case, não por FK). Divergência **não** vive dentro do Nexus; vive entre o Nexus e a **realidade**, e só entra pela porta estreita do UC de **reconciliação** (G6), que produz estado válido.

**Forma do ledger — CLAIM-cêntrico + event sourcing:**

```
Claim = { beneficiário, valor, moeda, origem (GUID Cobrança), localização (Conta atual), status }
```

- **Claim** = unidade de trabalho do ledger; nasce na **materialização**, quando o `%` morre e vira R$ concreto.
- **Beneficiário ≠ localização** (eixos ortogonais): beneficiário é uma *parte* (Operador, Laranja, Acionista, Recrutador, **ou a própria Organização** para o residual); localização é a *Conta* onde a grana dele está agora.
- **Claim com localização** (decidido): o claim sabe em qual Conta está; o hop **relocaliza** o claim. Responde G4 (“quanto está preso na Conta queimada”). **Não é o átomo:** a Conta continua número burro (sem FK), mas `soma claims == saldo` é **invariante** garantida no use case (ver G6) — não drift tolerado.
- **Invariante de nascimento:** na materialização, `soma dos claims criados = X` (todo R$ tem dono desde o segundo zero; sem grana órfã).
- **Status mínimo:** `ativo → pago`; mais `perdido` (write-off, G6) e estado de exposição em Conta queimada (G4). Sem “liquidando/em trânsito” a menos que doa.

**Materialização (refinada com o fluxo real do Contador):**
- Acontece no **fim de período** (dia/semana/ciclo da Org), em **lote**, **um pagamento por vez**.
- O Contador olha o saldo real e declara, por pagamento, o **líquido que de fato chegou** — já pós-taxa de gateway.
- Declara também **em qual Conta** o líquido aterrissou — Conta de Gateway, **carteira crypto** ou **conta de banco** (alguns gateways mandam direto pra crypto). Essa é a **localização de nascimento** dos claims.
- Daí em diante: lógica normal de hops até o ponto de repasse; o Contador desenha o path com o Nexus como ferramenta.

**Direção do ato do Contador:** **caixa-primeiro**, com o Nexus como o próprio ledger dele. O sistema mantém a lista de hops/destinos/repasses e o auxilia a desenhar o path.

**O que morre com o G9:** ownership como propriedade da **entidade Conta**; GUID+fork como máquina separada (linhagem vira **divisão/junção de claims** em eventos); “rastreio físico da origem misturada na conta”.

**Aresta RESOLVIDA:** líquido que aterrissa numa Conta ≠ Conta de Gateway de emissão. Decisão: a Conta de Gateway tem **dois pontos de vista** — *gerador de pagamento* e *detentor de saldo*. O **cut nível-1** é do Laranja **de emissão** (a grana passou pela conta geradora), config da conta-enquanto-gerador, **independente da aterrissagem**. Aterrissagem em outra Conta = hop/localização, não apaga o cut de emissão. Ver [money.md](./money.md) (Conta de Gateway: dois pontos de vista).

**Impacto em decisões já tomadas:**
- G1 (ownership em Conta) — **substituído** por Claim no ledger.
- G2 cut proporcional mid-path — operação do **ledger** sobre os claims do bundle (+ TX no livro-mundo quando houver movimento).
- G8 extrato — visão do **ledger** (estado do claim).
- GUID/rastreio — linhagem de claims + event sourcing no **ledger**, não no saldo.

**Status:** resolvido — consolidar corpo do [money.md](./money.md) na reescrita (task #6).

---

## Ordem sugerida de ataque

1. ~~G3~~ ~~G8~~ ~~G1~~ ~~G2~~ ~~G9~~ ~~G5~~ ~~G4~~ ~~G6~~ ~~G7~~ ~~G10~~ **todos resolvidos**. 🎉
2. `money.md` reescrito no modelo de dois livros; docs de resumo reconciliados; auditoria de consistência cruzada feita.

Quotas: conceito ainda vale nas Contas do livro-mundo; detalhe de ciclo depois.
