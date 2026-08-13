# Dinheiro

## Ideia central

O hub registra **Cobranças** com uma **intenção de split em %**. Quando a grana existe de verdade, o Contador **materializa** um **valor líquido concreto**: o `%` morre e nascem **Claims** (valores concretos, **em uma moeda**, com dono). Daí em diante o domínio só fala em **valor + moeda**, movidos por **hops** entre **Contas**, com rastreabilidade total via **event sourcing**. O Nexus **nunca converte moeda** — só registra a realidade ("X de Y").

Filosofia: **a realidade manda**; o Contador declara o que aconteceu; o Nexus organiza e mantém tudo consistente. O Nexus **não executa** movimento no mundo — ele **é o ledger** do Contador (não sincroniza com nenhum ledger externo).

## Os dois livros (G9)

O modelo separa o que o mundo real separa:

| Livro | O que modela | O que **não** modela |
|-------|--------------|----------------------|
| **Livro-mundo (Contas)** | Contas com **saldo fungível (número)** + transações (± saldo). Registro do que o Contador observou na realidade | Ownership, cuts, "de quem é", Cobrança, semântica |
| **Ledger (semântica)** | Toda rastreabilidade: **Claims**, materialização, cuts, `repassado`, ajustes, linhagem/eventos | O saldo bruto da Conta |

| Camada | Relação entre os dois |
|--------|------------------------|
| **Modelo** | **Desconectados** — sem FK. O saldo não referencia Claim; o Claim referencia Conta só como **localização**, não como posse rígida. |
| **Use case** | **Acoplados** — **caixa nunca anda sozinho** (todo movimento de caixa alimenta o ledger no mesmo ato); **o ledger pode andar sozinho** quando não há movimento de caixa (ex.: cut in-place). |

**Invariante de consistência (inviolável):** dentro do Nexus o estado é **sempre** íntegro — por Conta **e por moeda** `soma dos claims **ativos** naquela moeda == saldo naquela moeda` (**escopo-org**), claim ≥ 0, todo claim com beneficiário real (sem limbo "desconhecido"). Garantida na **fronteira de use case**, não por relacionamento de modelo. Divergência não vive dentro do Nexus — vive entre o Nexus e a **realidade**, e só entra pela porta de **reconciliação** (ver Perdas e reconciliação).

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
- **Status (máquina de estados canônica):**
  - `ativo` — vivo e localizado numa Conta; **é o único status que conta na invariante** `soma claims == saldo`.
  - `repassado` — a grana saiu do escopo-org (repasse final ao beneficiário). Terminal. **Sai da soma**; o saldo da Conta de origem é decrementado **no mesmo use case**.
  - `perdido` — write-off (grana sumiu sem destino). Terminal. **Sai da soma**; saldo decrementado no mesmo use case.
  - `estornado` — revertido por chargeback/reembolso (ver Edge cases). Terminal. Sai da soma; saldo decrementado.
  - **Transições sempre casadas com o livro-mundo** — nenhum claim sai de `ativo` sem a transação de caixa correspondente no mesmo use case (mantém a invariante sempre íntegra).
  - **`exposição` NÃO é status** — é uma *query* (soma de claims `ativo` em Conta com `saldo: congelado/perdido`). Ver [glossary.md](./glossary.md).
- **Claim que encolhe a exatamente 0** → vira `arquivado` (terminal, fora da soma). Encolhimento **nunca** produz valor negativo (clamp em 0).
- Todo valor tem dono desde o nascimento (sem grana órfã). Ownership **não** é propriedade da Conta — é o Claim, no ledger.

### Multi-denominação (cripto) — DECIDIDO (G10)

O Nexus dá suporte a saldo em **qualquer moeda** (real, cripto…) e **nunca converte** — só registra a realidade "X de Y".

- **Conta é multi-moeda:** o saldo é **por denominação** (um conjunto de saldos, um por moeda). Não forçamos "uma conta = uma moeda" — carteira cripto multi-ativo é real. A invariante é **por Conta, por moeda**.
- **Hop que redenomina** (ex.: banco BRL → carteira USDT): o claim é **consumido na moeda de origem e renasce na moeda do destino**, com o valor que o **Contador declara** no destino. Sem taxa de câmbio calculada pelo Nexus.
- **Perda/cut só existem dentro da mesma moeda.** Atravessando fronteira de moeda não há "encolheu 5" — não há régua comum; é redenominação declarada. O cut proporcional e o encolhimento no hop se aplicam **por moeda**.

## Hops e repasse

### Forma canônica do Hop (uma origem → um ou mais destinos)

Um hop é um fato registrado pelo Contador. Modelo formal, **não binário**:

```
Hop = {
  origem: Conta,
  bundle: [claims ativos na origem que este hop toca],   // efêmero, por-hop, não persistido
  destinos: [ { conta, valor, moeda } ],                 // 1 ou mais
  perda?: valor implícito = (valor do bundle) − (soma dos destinos), por moeda
}
```

- **Livro-mundo:** `−(valor movido)` na origem; `+valor` em cada Conta de destino. A **perda** (taxa/FX) é o que saiu e não chegou a destino nenhum — não é lançada como destino.
- **Ledger:** os claims do bundle são **relocalizados/repartidos** para os destinos, **proporcionalmente ao valor de cada claim no bundle** (a mesma proporção é a chave de rateio para perda e para cut). Encolhem quando há perda.
- **Validações (use case rejeita se violar):** `valor do hop > 0`; `soma(destinos) ≤ valor do bundle` **por moeda**; o bundle e os destinos batem por moeda; sem produzir claim negativo.

### Repasse final

O **repasse** é um hop cujo destino é a Conta do beneficiário **fora do escopo-org** (payout). Efeito: `−valor` na última Conta org (livro-mundo) e o claim correspondente vira `repassado` (terminal, sai da soma). O claim **não permanece** numa Conta org "marcado pago" — ele **sai**.

### Bundle

O Contador declara o bundle ("movi estes / tudo nesta Conta nesta moeda"). É **efêmero, por-hop** — não é entidade persistida nem "batch". Um bundle é **por moeda**; um hop que toca várias moedas é decomposto em sub-movimentos por moeda no livro-mundo.

### Hop que redenomina (troca de moeda)

Quando o destino está em **moeda diferente** da origem (ex.: banco BRL → carteira USDT): cada claim do bundle é **consumido na moeda de origem e renasce na moeda de destino**. O valor total de destino é o que o **Contador declara** (sem câmbio calculado pelo Nexus) e é repartido entre os claims **proporcionalmente ao valor de origem de cada um** — essa proporção é apenas **chave de distribuição**, *não* uma conversão de câmbio. Atravessar moeda não gera "perda em número" (não há régua comum).

## Cut de Laranja no path (mid-hop)

Quando a grana **passa** por um Laranja novo no path, o Contador (ou Admin) registra o cut. Ele é **um caso do hop canônico**: o valor em trânsito é repartido entre o destino "normal" e a Conta do Laranja que corta.

- É **N % do valor em trânsito** no hop — não valor fixo, não "Contador escolhe quem paga".
- Rateia **proporcionalmente** sobre **todos os claims do bundle** (mesma chave de rateio do hop). **Nasce um Claim do Laranja** (beneficiário = o Laranja) na Conta dele.
- **Exemplo:** passam 100; cut 10% → nasce claim de 10 do Laranja na Conta dele; os claims que formavam os 100 encolhem 10% cada (20→18, 50→45…).
- **Um claim de cut por moeda** presente no bundle (bundle multi-moeda → um claim de cut por moeda).

**Duas variantes (pela existência de movimento de caixa):**

| Variante | Livro-mundo | Ledger |
|----------|-------------|--------|
| **Cut com transferência** (grana vai pra outra Conta do Laranja) | Conta do Laranja é um **destino** do hop (+valor) | claims encolhem; nasce claim do Laranja **na Conta de destino dele** |
| **Cut in-place** (grana fica na mesma Conta, só muda de dono) | **nenhuma** transação (número igual) | claims encolhem; nasce claim do Laranja **na mesma Conta** — é o caso "ledger anda sozinho" |

### Governança e "1× por Laranja"

| Quem | Poder |
|------|-------|
| **Contador** | Registra hop + cut (função do cargo) |
| **Admin** | Idem |

- **"Fluxo" = a linhagem de uma Cobrança** (o GUID de origem e seus claims-filhos por divisão/junção). "1× por Laranja no fluxo" = um mesmo Laranja **não** cobra dois cuts sobre claims que descendem da **mesma Cobrança**; nas passagens seguintes, só usa as Contas dele sem novo cut.
- O **cut nível-1 de emissão** (Laranja dono da Conta de Gateway de emissão) **conta** como o cut daquele Laranja para aquele fluxo: se o mesmo Laranja reaparece mid-path no mesmo fluxo, **não** cobra de novo.
- Enforcement: como um bundle pode misturar claims de vários fluxos (GUIDs), a regra "1×" é avaliada **por (Laranja × GUID de origem)**, varrendo a linhagem dos claims do bundle.

**Sem teto artificial** no path. Limites reais vêm das **quotas de Conta**.

## Quotas / limites de Conta

Contas (gateway, banco, crypto) têm **limites operacionais** no mundo real (banco bloqueia volume suspeito). O Nexus modela **quotas** por ciclo:

| Uso | Para quê |
|-----|----------|
| **Emissão de Cobrança** | Quais Contas de Gateway ainda podem emitir sem estourar quota |
| **Hops / circulação** | Quais rotas/Contas ainda têm margem antes de estourar quota |

Quota = **capacidade da Conta**, não teto de % no path. **Quota é por (Conta × moeda)** — como o Nexus não converte, não existe teto agregado cross-moeda. Detalhe de ciclo (calendário vs rolling) e quem configura (Gateways/Admin): refinar depois; **conceito** decidido.

**Seleção da Conta de Gateway de emissão:** o Nexus escolhe automaticamente entre as Contas de Gateway com quota disponível para a Operação; a chamada de API pode opcionalmente **forçar** uma Conta específica (override). Se nenhuma Conta tem quota, a emissão é rejeitada.

## Waterfall completo do split (ordem canônica)

O split é **intenção em %** resolvida numa **ordem waterfall fixa** — cada linha come do **remanescente** da anterior, então por construção nunca passa de 100%. Aplicado sobre o líquido **X** na materialização; vira claims concretos e morre.

| # | Linha | Incide sobre | Autor / config | Obrigatória? |
|---|-------|--------------|----------------|--------------|
| 1 | **Cut(s) de Laranja (nível 1)** | líquido X | Gateways/Admin (config da conta-gerador) | sim (o trilho tem dono) |
| 2 | **Acionistas (nível 2)** | remanescente pós-1 | Admin (lista global) | se houver acionistas |
| 3 | **Gestão da Operação** (cut fixo de gestor) | remanescente pós-2 | concedido **de cima** (Admin/mandato superior) | **opcional** |
| 4 | **Agenciamento (nível 3)**: Operador + Recrutador | remanescente pós-3 (a "base do nível 3") | deal de agenciamento | sim |
| 5 | **Residual da Organização** | o que sobra | — | sim (recebe o resto) |

**Base do nível 3** = remanescente após Laranja(s) + Acionistas + Gestão da Op. É valor **calculado**, não objeto persistido. **Sem pool.**

### Regras do nível 3 (agenciamento)

- `operador_pct + recrutador_pct ≤ 100%` da base do nível 3 — invariante de **autor único** (o deal define os dois), validada ao salvar o deal. Sem multi-autor competindo → sem risco de >100%.
- Sobra (se `< 100%`) → **Residual da Organização**.

### Cut de Gestão da Operação (linha 3) — DECIDIDO (G11)

- É o "demais" que estava adiado, agora definido: **cut fixo** que remunera quem tem mandato de gestão sobre a op (overhead do cargo).
- **Flat e não-empilhável:** um único cut de gestão por op, sobre o resultado da op — **nunca** % em cima da fatia de cada subordinado (isso seria pirâmide; ver [actors-and-mandates.md](./actors-and-mandates.md) — "autoridade aninha; dinheiro não").
- **Posição no waterfall (linha 3, antes do agenciamento):** protege a org (overhead sai antes) mas **dilui** proporcionalmente operador/recrutador, que incidem sobre a base já líquida de gestão. Isso é intencional e transparente: o deal do recrutador é sempre "% da base do nível 3", e a base já considera o overhead.
- Config: **de cima** (Admin ou mandato superior com a capacidade); autoridade aninha, mas o cut de gestão é **um só por op** (não um por nível de delegação).

### Linhagem de recrutamento e dinheiro

- `recrutador_pct` remunera **apenas** o recrutador **direto** do operador. Linhagem multi-nível pode existir (ver [actors-and-mandates.md](./actors-and-mandates.md)), mas **não empilha**: sub-recrutados não pagam cut pra cima da cadeia.

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

Taxa de gateway → já embutida no líquido X.

> **Arredondamento NÃO é reconciliação.** O resíduo de arredondamento do split em % (ex.: X=100 com três cortes de 33,33% = 99,99) é resolvido **na hora da materialização**, alocando o centavo residual ao **Residual da Organização** (ou, se residual=0, à última linha não-zero do waterfall). **Nunca** há drift interno entre use cases. "Dust dissolve na reconciliação" vale **só** para divergência real-vs-Nexus, jamais para arredondamento de split.

**Reconciliação = use-case oficial e estreito** (não bypass de validação). A divergência vive só entre **Nexus e a realidade** e é sempre avaliada **por (Conta × moeda)**. O Contador informa o **saldo real org-scoped** que observou (não o extrato bruto do banco — grana pessoal do dono fica fora, ver Escopo-org); o Nexus booka a diferença produzindo **estado válido**:

- **Falta** (real < Nexus): baixa rateada **proporcional nos claims `ativo` daquela Conta/moeda** (default) ou atribuída a um claim específico, com causa.
- **Sobra** (real > Nexus): atribuída a **beneficiário real** — cria/credita claim do **Residual da Organização** naquela Conta/moeda (mesmo que seja uma moeda em que a Conta ainda não tinha claim). Nunca fica "desconhecida".

**Causa** (intel), anexada a todo evento de attrition/write-off/reconciliação/estorno: `bloqueio_bancario | apreensao | traicao | saida_voluntaria | erro_operacional | estorno | desconhecido`. Reputação por Laranja/conta/gateway = **replay de eventos**, não subsistema de analytics.

**Reversibilidade:** correção de fato errado = **evento compensatório** (append-only, ES); nunca delete/mutação. É event sourcing + contabilidade textbook.

## Regras precisas de implementação e edge cases (endurecimento G13)

Estes pontos foram fechados numa revisão adversarial (engenheiro implementando só a partir dos docs). Todos preservam a invariante `soma claims ativos == saldo` por (Conta × moeda).

- **Unicidade da materialização:** cada Cobrança materializa **no máximo uma vez**. Transição `Paga → Materializada` é idempotente; re-materializar exige evento compensatório explícito (não silencioso).
- **Materialização por pagamento é inteira e mono-aterrissagem:** um pagamento aterrissa numa **única Conta/moeda**. Settlement fisicamente dividido (parte BRL, parte cripto) é modelado como o pagamento aterrissando numa Conta e depois um hop — **não** como materialização parcial.
- **Caps do waterfall:** cada linha ∈ [0, 100%] da sua base. As linhas "obrigatórias" (Agenciamento e Residual da Org) **podem receber 0** se as linhas acima consumirem tudo — "obrigatória" significa "sempre existe como linha", não "recebe > 0". Cut de Laranja e Gestão da Op também são capados por construção (waterfall).
- **`saldo: perdido` (eixo Conta) e write-off são o mesmo use case:** marcar uma Conta como `saldo: perdido` **dispara o write-off** de todos os claims `ativo` nela (→ `perdido` + causa) e zera o saldo, no mesmo ato. Não existe janela `saldo=0` com claims `ativo` pendurados. `saldo: congelado` **não** mexe em claims (só exposição).
- **Estorno / chargeback / reembolso (pós-Paga):** evento **compensatório** distinto de write-off. Reverte os claims descendentes daquela Cobrança (→ `estornado`) e decrementa o livro-mundo; causa = `estorno`. (Write-off é "sumiu sem destino"; estorno é "voltou pro cliente/gateway" — destino conhecido.)
- **Pagamento tardio após terminal** (`Expirada`/`Cancelada`) : a Cobrança pode ser **re-materializada** via seu GUID por evento compensatório (reabre para materialização), preservando o vínculo Operação/Operador/split. Não vira "Sobra órfã" na reconciliação.
- **Correção de materialização com downstream já movido:** não se reescreve o passado. Corrige-se por **eventos compensatórios na ponta atual** da linhagem (com nova causa); se os claims já foram repartidos/repassados, a correção se propaga pela linhagem viva, nunca ressuscitando claims terminais.
- **Enforcement da invariante:** todo use case financeiro **re-verifica** a invariante ao commitar; violação **aborta** o use case (nada é persistido). A invariante é de fronteira de use case, não FK de modelo.
- **`valor` bruto declarado na emissão** é só referência; o split incide sobre o **líquido X** da materialização. Não há validação `X ≤ bruto` (o mundo real diverge); divergências grandes são sinal para o Contador, não erro de sistema.

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
- Refinos de propósito adiados (não bloqueiam o núcleo): ciclo/config de quota, integração API de hops (v1 = book-keeping manual). Cut de gestão da op = linha 3 do waterfall (G11), não “equipes”.
