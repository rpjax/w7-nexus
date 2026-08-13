# Auditabilidade — DECIDIDO (G12)

Como o Nexus prova o que aconteceu e quem viu/fez o quê. **Duas frentes simultâneas e complementares** — não redundantes. Cada uma cobre um buraco que a outra não enxerga.

## Princípio

O Nexus opera num contexto onde **a estrutura precisa ficar blindada** e **o próprio pessoal de confiança pode ser virado**. Auditabilidade não é "nice to have": é o mecanismo que (a) permite reconstruir a verdade, (b) detecta abuso de mandato / inferência indevida, e (c) resiste a adulteração de história por um insider comprometido.

## As duas frentes

| | **Event Sourcing (ES)** | **Log de Auditoria** |
|---|---|---|
| **Propósito** | **Fonte da verdade** do estado de domínio; o estado é derivado por replay dos eventos | **Prestação de contas / forense**: quem fez — **e quem viu** — o quê, quando, sob qual mandato, com qual contexto |
| **Captura** | Fatos de domínio que **mudam estado** (materialização, hop, cut, split, grant/revogação de mandato, write-off, reconciliação, mudança de estado de Conta/Laranja, geração/pagamento de Cobrança…) | **Toda ação relevante**, incluindo as que **não mudam estado**: logins, **leituras/acessos** (quem abriu qual split/extrato/rota), tentativas **negadas**, contexto (sessão/origem) |
| **Autoridade para** | Comportamento do sistema; reconstrução de estado; reconciliação | Investigação humana; detecção de anomalia; responsabilização |
| **Consumidor** | O próprio sistema | Auditor humano (Admin) |
| **Mutabilidade** | Append-only, imutável, **tamper-evident** (hash-chain — mesma integridade do log) | Append-only, imutável, **tamper-evident** |
| **Forma** | Por agregado (ledger, mandato, operação, conta…) | Fluxo transversal, org-wide |

## Por que não são redundantes — o caso das LEITURAS

O trabalho **mais importante** do log de auditoria aqui é registrar **leituras**, não escritas.

O maior risco residual que o domínio **aceitou** (ver [visibility.md](./visibility.md) e G3): um cargo de confiança (Contador, Gestor) que, ao **ver** muitos splits/rotas ao longo do tempo, **infere** a estrutura. Isso é um ataque de **leitura** — e o **event sourcing nunca o captura**, porque ler não muda estado. O log de acesso é a **única** ferramenta que detecta o padrão (ex.: "por que o Contador abriu o split de 40 ops distintas esta semana?").

Regra: **toda leitura de dado sensível** (split, extrato de terceiro, rotas, carteira, catálogo de contas, log) **gera entrada no log de auditoria**, mesmo que não gere evento de ES.

## Acoplamento — mesmo padrão do dois-livros

ES e log são **modelos separados** (propósitos diferentes), **acoplados na fronteira de use case**:

- Uma ação que muda estado escreve **o fato no ES** e **a entrada no log**, ligados por um **id de correlação**. O investigador liga "X executou esta ação (log) → que produziu estes fatos (ES)".
- Uma ação que só lê escreve **apenas no log**.
- Um fato puramente derivado/interno (replay) **não** gera log de auditoria.

Invariante de porta: **nenhuma ação sensível ocorre sem deixar rastro** na frente apropriada (leitura → log; escrita → ES + log).

## O log de auditoria é a joia da coroa (risco crítico)

O log é o **único lugar que vê tudo**: todas as ações, de todos os mandatos, cruzando todos os compartimentos. **Quem lê o log de auditoria consegue montar o organograma completo** — exatamente o que o produto existe para impedir. Portanto ele tem duas defesas que nenhum outro dado tem:

1. **Leitura minimamente permitida** — praticamente **só Admin** (capacidade `ler_log_auditoria`, escopo org). **Não** é um relatório distribuível. Nem cargos de confiança (Contador/Gestor) leem o log por padrão.
2. **Tamper-evidence (encadeamento por hash)** — cada entrada encadeia o hash da anterior; qualquer edição/remoção da história **quebra a cadeia** e é detectável. Objetivo: **ninguém — nem o Admin, nem um insider comprometido — reescreve a história em silêncio**. O ES também é append-only e deve ter a mesma integridade.

## Retenção vs footprint (refino consciente)

Tensão real: log/ES que vivem para sempre são ricos para auditoria mas viram **passivo** se a máquina for apreendida. Decisão de **retenção** (quanto tempo, qual granularidade, o que pode ser purgado/compactado preservando a cadeia de integridade) fica como **refino explícito** — não bloqueia o núcleo, mas está registrado para não ser esquecido.

## Capacidades relacionadas

- `ler_log_auditoria` (escopo org) — praticamente só Admin.
- Toda outra capacidade que **lê** dado sensível implica emissão de entrada de log ao ser exercida.

## Estado

- **Decidido:** duas frentes (ES verdade-do-estado + Log accountability incl. leituras), acopladas por correlação; log = joia da coroa (leitura Admin + tamper-evidence); ambos append-only/imutáveis.
- **Refino adiado (não bloqueia):** política de retenção/footprint; formato exato de tamper-evidence; catálogo fino de "o que conta como leitura sensível" (começa do óbvio: split, extrato de terceiro, rotas, carteira, contas, log).
