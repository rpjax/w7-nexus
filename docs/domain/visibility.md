# Visões e extratos (need-to-know)

O que cada ator **vê**. Princípio: **mínimo ao mandato** — com exceção explícita de **cargos de confiança** (ver [vision.md](./vision.md)).

## Princípio

- **Ponta / mandato estreito:** só a fatia da própria competência — sem montar organograma.
- **Cargo de confiança:** visão ampla **no domínio do cargo** (financeiro ou ops), porque o mundo real exige braço direito.
- **Admin:** irrestrito.
- Inferência estatística ao longo do tempo (ex.: Contador contar usernames únicos em splits) é **risco residual aceito** de cargo de confiança — o hub não entrega catálogo/organograma de bandeja, e o **log de auditoria** (ver abaixo) é o que permite *detectar* a inferência quando ela acontece.

### Visão = escopo do Mandato (G11)

O que um ator vê é **derivado do escopo do seu Mandato** (ver [actors-and-mandates.md](./actors-and-mandates.md) — capacidades × escopo), não de um rótulo fixo:

- Mandato de **Operação** sobre a op X → vê tudo dentro de X (no que suas capacidades permitirem).
- Mandato de **Carteira** → vê só a fatia dele (seus recrutados diretos), onde quer que estejam.
- **Autoridade aninha, mas visão nunca sobe nem vaza lateral:** você vê pra baixo/dentro do teu escopo; nunca quem está acima nem os pares.
- Cada **capacidade de leitura** exercida gera entrada no **log de auditoria**.

## Matriz — DECIDIDA (fila #11 + G3)

| Ator | Camada | Vê | Não vê (de propósito) |
|------|--------|-----|------------------------|
| **Laranja** | Estreito | Bruto nas contas dele; conta de gateway; **parte dele** | Operação, vendedor, outros cuts, path, outros Laranjas |
| **Operador** | Estreito | Próprias cobranças (com Operação); **estimativa congelada** (base = materialização inicial); ao ser liberado: **pendente/a receber** + relatório controlado | Split dos outros; Acionistas; hops; **valor atualizando ao vivo** (vaza o path) |
| **Recrutador** | Estreito | Carteira; deal; fatia **dele** + agenciados (mesma regra: estimativa → pendente) | Acionistas; path; outros recrutadores; lista global de ops |
| **Acionista** | Estreito | **Sua** %; estimativa congelada → pendente/a receber quando liberado | Outros acionistas; ponta; estrutura; valor ao vivo |
| **Gateways** | Estreito | Laranjas, Contas de Gateway, % de trilho, **quotas** das Contas | Hops completos; recrutamento; split nível 3 |
| **Contador** | **Confiança (financeiro)** | Pagas, materialização, split, Contas/**claims**, hops, rotas, **quotas/margem**, repasse, reconciliação | Organograma/recrutamento de bandeja; catálogo operacional completo |
| **Gestor de Operações** | **Confiança (ops)** | Ops **sob mandato**: ciclo, assign, configs, **split completo dessas ops** | Hub inteiro; ops de outro gestor; Admin powers; hops/execução de repasse (domínio do Contador) — salvo overlap de papéis |
| **Admin** | Irrestrito | Tudo | — |

## Contador (G3) — detalhe

- **Cargo de confiança.** Sem visão financeira ampla o book-keeping não funciona no mundo real.
- Vê o **split inteiro** da cobrança/fluxo que está liquidando (quem/% / valores / destinos de payout ligados ao split).
- Vê **rotas possíveis** (contas de liquidação, caminhos) para planejar hops.
- **Não** é o mesmo que “sabe como a quadrilha está organizada”: o produto **não** lhe dá a tela de organograma, lista completa de membros por papel, nem grafo de recrutamento.
- Risco residual: ao processar muitos splits, ele *pode inferir* volumes. Aceito; mitigação = poucos Contadores, alta confiança, não fingir opacidade impossível.

## Gestor de Operações (G3) — detalhe

- **Cargo de confiança** para **delegar** trabalho operacional — braço direito de ops, **≠ Admin**.
- Split completo **das Operações sob o mandato dele** (precisa disso para gerir a frente).
- Não herda: criar Admins, ver todas as ops sem assign, irrestrito no hub, substituir Contador no path físico (a menos que também tenha o papel Contador).

## Estimativa → Pendente (G8 + G10) — DECIDIDO

**Correção de rumo importante:** a versão anterior deste doc dizia que o beneficiário via o "valor atual" **atualizando** ao longo dos hops. Isso é um **vazamento de canal lateral** — o *ritmo/padrão* das atualizações deixa inferir o caminho da grana, mesmo sem mostrar hops. Foi substituído pelo modelo abaixo.

**Duas fases do que o beneficiário de baixa confiança vê:**

1. **Estimativa (congelada).** Assim que há materialização, ele vê uma estimativa da sua fatia, com base na **materialização inicial** (o claim no nascimento). **Não** acompanha os hops — fica parada, aconteça o que acontecer na rota. (Base = materialização inicial, não o bruto — o bruto vazaria a estrutura de %.)
2. **Pendente / a receber.** Quando o Contador (ou Admin) determina que a grana chegou no **hop final**, pendente só do repasse, ele marca o claim como **visível**. A semântica exibida **muda**: de *estimativa* para *pendente/a receber* — valor concreto, na moeda em que aterrissou.

**No reveal**, o beneficiário vê um **relatório controlado** — o suficiente pra entender por que recebeu ligeiramente menos (ou perdeu tudo): um descritivo de alto nível, **nunca hop-by-hop**, sem Contas intermediárias, sem identidade de Laranjas, sem organograma. Prestação de contas que constrói confiança sem revelar o path.

Cargos de confiança (Contador, Gestor, Admin) **não** passam por isso — veem o real direto.

## Log de auditoria — quem lê (G12)

O **log de auditoria** é o único lugar que vê **todas** as ações de todos os mandatos — quem lê o log monta o organograma inteiro. Por isso:

- Leitura do log = capacidade `ler_log_auditoria`, escopo org — **praticamente só Admin**. **Não** é relatório distribuível; nem Contador nem Gestor leem por padrão.
- É **tamper-evident** (encadeado por hash): nem Admin reescreve a história em silêncio.
- Toda **leitura de dado sensível** (split, extrato de terceiro, rotas, carteira, contas) **gera** entrada de log — é assim que a inferência de estrutura (risco residual aceito) fica **detectável**.

Detalhe completo: [auditability.md](./auditability.md).

## Regras precisas de visibilidade (endurecimento G13)

### Dois regimes de visão (a matriz é atalho, não a fonte da verdade)

Existem **dois regimes** e o doc antes só nomeava um:

1. **Visão de beneficiário (ponta):** fixa e cega, por *tipo de participação* (Operador/Laranja/Acionista/Recrutador-na-fatia-dele). Não deriva de mandato de gestão. É o regime da tabela de "estreitos".
2. **Visão de gestão:** **derivada do escopo do Mandato** (união dos escopos — ver [actors-and-mandates.md](./actors-and-mandates.md)). É o regime de Gestor/Contador/Admin/custom.

**A matriz acima é o atalho dos presets padrão.** Para um Membro com mandato custom (capacidade×escopo fora dos presets) ou com **múltiplos** mandatos, a visão é a **união** dos escopos das capacidades de leitura que ele detém — a matriz não é consultada como lei, é ilustração.

### Gestor: "split completo" vs grafo de recrutamento

- O Gestor vê o **split completo das ops sob mandato dele** — incluindo, **dentro dessas ops**, quem é o Operador e qual Recrutador recebe o `recrutador_pct` daquele operador. Isso é need-to-know **dentro do escopo dele**.
- O que continua vetado (mesmo pra ele) é o **grafo de recrutamento GLOBAL** da Org (quem recruta quem fora das ops dele; a árvore inteira). "Não entregar organograma de bandeja" = o mapa org-wide, não a fatia da própria op.

### Escopo de Contador e Gateways

- **Contador** e **Gateways** são, por padrão, **org-wide** no seu domínio (financeiro / trilhos). É o **risco residual aceito** (inferência ao longo do tempo — detectável via log). O modelo capacidade×escopo **permite** estreitá-los (ex.: um Contador por conjunto de ops), mas o default é org.

### Estimativa / pendente — precisão

- **Granularidade:** a estimativa é **por-claim** (por Cobrança/beneficiário), não um agregado difuso. "Materialização inicial" = o valor do claim no nascimento (materialização é única; "inicial" reforça "no nascimento, antes de qualquer cut/hop").
- **Troca de moeda no reveal:** se o pendente aterrissa em moeda diferente da estimativa (ex.: estimou BRL, recebe USDT), o relatório controlado **declara a mudança de denominação** ("liquidado em USDT") — não a apresenta como "recebeu menos" (não há régua comum; ver [money.md](./money.md)).
- **Reveal de perda:** se um claim de baixa-confiança vira `perdido`/`estornado` **antes** do hop final, o Contador (ou Admin) dispara o **relatório controlado da perda** — o beneficiário não fica preso na estimativa congelada para sempre.

### Log — quem lê (preciso)

- `ler_log_auditoria` (escopo org): **somente Admin** na v1 ("praticamente" = sem outro leitor padrão; qualquer exceção seria um mandato custom explícito, auditado).

## Notas

- A estimativa é congelada na materialização inicial e **não** reflete cuts/hops até o Contador liberar.
- Acionista = extrato mínimo de beneficiário (mesma lógica estimativa → pendente).
