# Dinheiro

## Ideia central

O hub registra **Cobranças** com uma **intenção de split em %**. Quando a grana existe de verdade, o Contador **materializa** um **valor líquido concreto**: o `%` morre e nascem **Claims** (valores concretos, **em uma moeda**, com dono). Daí em diante o domínio só fala em **valor + moeda**, movidos por **hops** entre **Contas**, com rastreabilidade total via **event sourcing**. O Nexus **nunca converte moeda** — só registra a realidade ("X de Y").

Filosofia: **a realidade manda**; o Contador declara o que aconteceu; o Nexus organiza e mantém tudo consistente. O Nexus **não executa** movimento no mundo — ele **é o ledger** do Contador (não sincroniza com nenhum ledger externo).

## Os dois livros (G9)

O modelo separa o que o mundo real separa:

| Livro | O que modela | O que **não** modela |
|-------|--------------|----------------------|
| **Livro-mundo (Contas)** | Contas com **saldo fungível (número)** + transações (± saldo). Registro do que o Contador observou na realidade | Ownership, cuts, "de quem é", Cobrança, semântica |
| **Ledger (semântica)** | Toda rastreabilidade: **Claims**, materialização, cuts, "pago", ajustes, linhagem/eventos | O saldo bruto da Conta |

| Camada | Relação entre os dois |
|--------|------------------------|
| **Modelo** | **Desconectados** — sem FK. O saldo não referencia Claim; o Claim referencia Conta só como **localização**, não como posse rígida. |
| **Use case** | **Acoplados** — **caixa nunca anda sozinho** (todo movimento de caixa alimenta o ledger no mesmo ato); **o ledger pode andar sozinho** quando não há movimento de caixa (ex.: cut in-place). |

**Invariante de consistência (inviolável):** dentro do Nexus o estado é **sempre** íntegro — por Conta **e por moeda** `soma dos claims naquela moeda == saldo naquela moeda` (**escopo-org**), claim ≥ 0, todo claim com beneficiário real (sem limbo "desconhecido"). Garantida na **fronteira de use case**, não por relacionamento de modelo. Divergência não vive dentro do Nexus — vive entre o Nexus e a **realidade**, e só entra pela porta de **reconciliação** (ver Perdas e reconciliação).

**Escopo-org:** o Nexus modela só a grana **da organização**. O dinheiro **pessoal** do dono de uma conta é invisível/irrelevante — o book-keeping deixa tudo-da-org accounted for; se o Laranja misturou grana pessoal numa conta dedicada, problema dele.

## Cobrança

### Como nasce

| Canal | Uso |
|-------|-----|
| **API** (principal) | Script chama Nexus: **Operação + Operador + valor** |
| **Painel** | Fallback (sem uso prático real) |

Resto (Conta de Gateway, %s, recrutador, acionistas…) **computado** por regras/config. Amarra direto: Operação, Operador, valor (bruto declarado), **Conta de Gateway de emissão**.

### Conta de Gateway: dois pontos de vista

A mesma entidade do mundo real (a conta no gateway, de um Laranja) tem **duas qualidades** distintas — mesma coisa, preocupações diferentes:

| Ponto de vista | Papel | Consequência |
|----------------|-------|--------------|
| **Gerador de pagamento** (emissão) | A Cobrança é emitida por esta conta | A grana **passou pelo Laranja de emissão** → o **cut nível-1 dele é devido**; config da conta-enquanto-gerador; **independe de onde o líquido aterrisse** |
| **Detentor de saldo** | A conta pode receber/segurar saldo | É uma Conta comum do livro-mundo; pode ou não ser a **Conta de aterrissagem** do líquido |

O cut nível-1 do Laranja de emissão é **fixado na emissão**. Se o gateway forwarda o líquido pra outra Conta (crypto de outro Laranja), aquilo é aterrissagem/hop — **não** apaga o cut de emissão.

### Lifecycle

```
Aberta  →  Paga (webhook)  →  Materializada (líquido X, Conta de aterrissagem)  →  hops…  →  repasse
```

Terminais sem dinheiro: Expirada / Cancelada / Falhou. (Cobrança Aberta numa conta com emissão bloqueada não morre automático — ver Attrition.)

## Split — intenção em % (só até materializar)

Na geração, a Cobrança carrega uma **intenção de split**: participantes + `%`, resolvida por uma **ordem waterfall fixa** (cada corte come do remanescente — por construção nunca passa de 100%):

1. Cut(s) de **Laranja** (nível 1) sobre o líquido
2. **Acionistas** (nível 2) sobre o remanescente
3. **Nível 3** (Operador + Recrutador + residual da Org) sobre o que resta

A intenção em `%` **só existe até a materialização**. Não há pool nem bucket armazenado — é uma fórmula que, aplicada ao líquido X, produz **claims concretos** e então **morre**. Ordem waterfall = invariante de significado (não configurável). Ver [glossary.md](./glossary.md) (Split intenção, Base do nível 3).

## Materialização

Materializar **não** é o Nexus inventar grana. É o Contador dizer:

> O webhook já disse **Paga**. No mundo real, o líquido utilizável deste pagamento é **X**, e ele aterrissou na **Conta Y**.

- Acontece no **fim de período** (dia/semana/ciclo da org), em **lote**, **um pagamento por vez**.
- **Inteiro** — não existe materialização parcial (pagamento não entra parcial).
- **X já é líquido pós-taxa de gateway** (o que o Contador considera utilizável). Sem segundo desconto automático.
- Declara a **Conta de aterrissagem** — pode ser Conta de Gateway, carteira crypto ou conta de banco. É a **localização de nascimento** dos claims.
- Aplica o waterfall sobre X → nascem os **Claims** (`%` morre). **Invariante de nascimento:** `soma dos claims criados == X`.

## Claim

Unidade de trabalho do ledger:

```
Claim = { beneficiário, valor, moeda, origem (GUID da Cobrança), localização (Conta atual), status }
```

- **Beneficiário ≠ localização** (eixos ortogonais): beneficiário é uma *parte* (Operador, Laranja, Acionista, Recrutador, ou a **própria Organização** para o residual); localização é a *Conta* onde a grana está agora.
- **Moeda:** o claim tem denominação (BRL, USDT, BTC…) = a moeda em que está no momento. Muda só num hop que redenomina (ver Multi-denominação).
- **Status:** `ativo → pago`; mais `perdido` (write-off) e exposição quando a Conta é queimada.
- Todo valor tem dono desde o nascimento (sem grana órfã). Ownership **não** é propriedade da Conta — é o Claim, no ledger.

### Multi-denominação (cripto) — DECIDIDO (G10)

O Nexus dá suporte a saldo em **qualquer moeda** (real, cripto…) e **nunca converte** — só registra a realidade "X de Y".

- **Conta é multi-moeda:** o saldo é **por denominação** (um conjunto de saldos, um por moeda). Não forçamos "uma conta = uma moeda" — carteira cripto multi-ativo é real. A invariante é **por Conta, por moeda**.
- **Hop que redenomina** (ex.: banco BRL → carteira USDT): o claim é **consumido na moeda de origem e renasce na moeda do destino**, com o valor que o **Contador declara** no destino. Sem taxa de câmbio calculada pelo Nexus.
- **Perda/cut só existem dentro da mesma moeda.** Atravessando fronteira de moeda não há "encolheu 5" — não há régua comum; é redenominação declarada. O cut proporcional e o encolhimento no hop se aplicam **por moeda**.

## Hops e repasse

Cada hop é um **fato simples** registrado pelo Contador — e, pela invariante de use case, escreve nos **dois livros**:

- **Livro-mundo:** −valor na Conta A, +valor na Conta B (destino **já líquido**: taxas/perdas embutidas no que ele digitou; sem motor de FX).
- **Ledger:** os Claims que se moveram são **relocalizados** de A para B (e encolhem se houve perda).

**Repasse final:** hop (ou série) que deixa o Claim na Conta de quem deve receber e marca o Claim como **pago**.

Um hop pode mover **um subconjunto de claims** (o *bundle* — o Contador declara "movi estes / tudo o que está nesta Conta"). O bundle é **efêmero, por-hop** — não é entidade persistida nem "batch".

## Cut de Laranja no path (mid-hop)

Quando a grana **passa** por um Laranja novo no path, o Contador (ou Admin) registra o cut:

- É **N % do valor em trânsito** no hop — não valor fixo, não "Contador escolhe quem paga".
- Rateia **proporcionalmente** sobre **todos os claims do bundle**. Nasce um Claim do Laranja na Conta dele.
- **Exemplo:** passam 100; cut 10% → Laranja recebe claim de 10; os claims que formavam os 100 encolhem 10% cada (20→18, 50→45…).

| Quem | Poder |
|------|-------|
| **Contador** | Registra hop + cut (função do cargo) |
| **Admin** | Idem |
| **1× por Laranja** | Se o Laranja já recebeu cut neste fluxo, **não** cobra de novo no mid-path — só usa as Contas dele |

**Sem teto artificial** no path. Limites reais vêm das **quotas de Conta**.

## Quotas / limites de Conta

Contas (gateway, banco, crypto) têm **limites operacionais** no mundo real (banco bloqueia volume suspeito). O Nexus modela **quotas** por ciclo:

| Uso | Para quê |
|-----|----------|
| **Emissão de Cobrança** | Quais Contas de Gateway ainda podem emitir sem estourar quota |
| **Hops / circulação** | Quais rotas/Contas ainda têm margem antes de estourar quota |

Quota = **capacidade da Conta**, não teto de % no path. Detalhe de ciclo (calendário vs rolling) e quem configura (Gateways/Admin): refinar depois; **conceito** decidido.

## Rateio do nível 3 (sem pool)

**Base do nível 3** = valor *calculado* na materialização = remanescente do líquido X após Laranja(s) + Acionistas. É base de cálculo, **não** objeto persistido.

| Linha da intenção | Base | Autor |
|-------------------|------|-------|
| Operador | `operador_pct` da base nível 3 | deal de agenciamento |
| Recrutador | `recrutador_pct` da base nível 3 | deal de agenciamento |
| Residual da Organização | resto | — |

- `operador_pct + recrutador_pct ≤ 100%` — invariante de **autor único** (o deal define os dois), validada ao salvar o deal. Sem multi-autor → sem risco de >100%.
- **`demais` configurável (líderes de equipe) = adiado** com equipes. Quando voltar: **não** virar bucket multi-autor — usar waterfall (deal primeiro; líderes do remanescente).

## Attrition — queima de Laranja/Conta

Queimar é o evento **mais comum**, não exceção. É a morte de **um dos dois pontos de vista** da Conta (gerador ↔ detentor). Modelado por event sourcing.

- **Estado da Conta = dois eixos independentes:** `emissão: ok|bloqueada`, `saldo: acessível|congelado|perdido`.
- **Estado do Laranja:** `ativo → queimado | saiu (voluntário) | traiu`. Queimar Laranja **sinaliza** as contas dele como suspeitas; **não** hard-burn automático (Contador decide conta a conta). Uma Conta pode morrer sem o Laranja cair.
- **Cobranças Abertas em conta com emissão bloqueada:** não morrem automático; param de nascer novas; existentes seguem até Paga ou expiram; cancelamento é manual.
- **Re-atribuição:** não se reescreve a conta de emissão de uma Cobrança já enviada; atualiza-se o **conjunto de contas de emissão disponíveis da Operação**.
- **Claim preso:** localizado em Conta com saldo congelado/perdido → exposição consultável (soma dos claims ali). Vira `perdido` via write-off; se recuperado, hop normal.
- **Incidência da perda: mecânica** — cai sobre quem tinha claim naquela Conta. Org **não** absorve na v1 (absorção central = feature futura, mecânica a mais on top).

## Perdas e reconciliação (G6)

Duas perdas **distintas**:

| Mecanismo | Quando | Como |
|-----------|--------|------|
| **Encolhimento no hop** | Taxa/FX na movimentação | Implícito: destino já líquido (saiu 100, chegou 95); claims do bundle encolhem proporcional |
| **Write-off** | Grana some **sem destino** (presa/apreendida/erro/traição) | Explícito: claim → `perdido` + **causa**; evento ES |

Taxa de gateway → já embutida no líquido X. "Dust"/arredondamento → dissolve na reconciliação (o Contador digita valores reais).

**Reconciliação = use-case oficial e estreito** (não bypass de validação). A divergência vive só entre **Nexus e a realidade** (banco/gateway/carteira que o Contador observa). A reconciliação importa a realidade e produz **estado válido**, respeitando as invariantes:

- **Falta** (real < Nexus): baixa rateada **proporcional nos claims da Conta** (default) ou atribuída, com causa.
- **Sobra** (real > Nexus): atribuída a **beneficiário real** (ex.: residual da Org) — nunca fica "desconhecida".

**Causa** (intel), anexada a todo evento de attrition/write-off/reconciliação: `bloqueio_bancario | apreensao | traicao | saida_voluntaria | erro_operacional | desconhecido`. Reputação por Laranja/conta/gateway = **replay de eventos**, não subsistema de analytics.

**Reversibilidade:** correção de fato errado = **evento compensatório** (append-only, ES); nunca delete/mutação. É event sourcing + contabilidade textbook.

## Rastreio (linhagem via Claims + event sourcing)

- O **GUID da Cobrança** de origem é carregado em cada Claim.
- Linhagem se recompõe por **divisão/junção de claims** (transferência parcial, cut mid-path) — **não** existe "máquina de fork de GUID" separada.
- **Não** rastreamos "origem física misturada na conta do banco". Rastreamos **de quem é o R$, em qual Conta, e a história dos eventos**.

Melhor dos dois mundos: estado **simples no presente** (claims por Conta), auditoria **rica no passado** (ES).

## Contador

- Cargo de confiança; visão ampla do financeiro (ver [visibility.md](./visibility.md)).
- Declara líquido X, registra hops (incl. cut mid-path), repasses, write-offs e reconciliações — **book-keeping da realidade**.
- Usa **quotas** das Contas para escolher rotas/hops viáveis.
- **Admin** tem os mesmos poderes de registro financeiro. Nexus **não executa** o movimento no mundo.

## O que morreu neste modelo

- **Átomo** de saldo / "não mesclar fisicamente".
- **Ownership como propriedade da entidade Conta** (agora é Claim no ledger).
- **"Poço P" / pool / batch** pós-materialização.
- **"Fork de GUID" como máquina separada** (agora linhagem = divisão/junção de claims).
- `%` viajando ao longo dos hops.
- Materialização parcial; tesouraria/saldo fora de Conta.
- Drift tolerado entre os livros (agora: invariante inviolável + porta de reconciliação).
- Conversão de moeda pelo Nexus (nunca converte — só registra "X de Y").
- "Uma conta = uma moeda" como invariante forçada (Conta é multi-moeda).

## Estado

- **Todos os gaps de dinheiro resolvidos:** G1, G2, G5, G6, G8, G9, **G10 (multi-moeda + visibilidade de estimativa)**. Ver [open-gaps.md](./open-gaps.md).
- Visibilidade do beneficiário (estimativa congelada → flag do Contador → pendente/a receber, com relatório controlado): ver [visibility.md](./visibility.md).
- Refinos de propósito adiados (não bloqueiam o núcleo): ciclo/config de quota, `demais` configurável (com equipes), integração API de hops (v1 = book-keeping manual).
