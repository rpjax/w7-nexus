# Glossário

Termos canônicos do domínio de produto. Se a conversa usar sinônimo, atualizar aqui ou abandonar o sinônimo.

| Termo | Significado | Não confundir com |
|-------|-------------|-------------------|
| **Nexus** | Hub centralizador de gestão da organização (no RPG); **software sob medida** para **uma** org (um Nexus = uma org), não produto de prateleira | O script na ponta / runtime |
| **Organização** | O “todo” — a quadrilha; **singleton por deploy** (raiz conceitual implícita das regras globais) | Uma Operação isolada; tenant de multi-org (não existe) |
| **Operação** | Fronteira de isolamento: missão/frente com identidade, gente, lucro local e recursos | Organização; store (recurso) |
| **Membro** | Identidade reconhecida pelo hub | Papel |
| **Papel / Mandato** | O que o sistema revela e autoriza | Classe de confiança |
| **Ponta** | Classe de baixa confiança depositada; tipicamente Operadores | Mandato interno |
| **Mandato interno** | Confiança localizada numa competência (“internal staff” na conversa) | Um role chamado InternalStaff |
| **Admin** | Papel no hub: administra o sistema com **acesso irrestrito** | Dono (fora do produto); Acionista |
| **Dono** | Fora do domínio de produto: quem tem infra/código/deploy no mundo do RPG. O Nexus não modela isso | Admin; Acionista |
| **Operador** | Papel de ponta; login = **handle/nickname único**; payout + vínculo com Recrutador; sem identidade civil no hub | Recrutador; Laranja |
| **Handle** | Nickname único usado como login do Membro na ponta (ao menos Operador) | ID interno; nome civil (não guardado) |
| **Recrutador** (sargento) | Traz/agencia Operadores; vê só downline direta | Líder de equipe da Operação (pode diferir) |
| **Gateways** | Papel de mandato interno: Laranjas, contas de gateway, % de trilho, **quotas** das Contas | Contador; Admin |
| **Contador** | Papel de mandato interno: book-keeping pós-Paga (saque, hops, repasse); registra a realidade no hub; Nexus **guia**, não executa | Gateways; Admin |
| **Finanças** | Sinônimo informal de **Contador** na prosa — preferir Contador no glossário | Contador |
| **Acionista** | Beneficiário do nível 2 (fatia global); **não** é role de gestão | Admin; Dono |
| **Laranja** | Quem empresta o trilho de recebimento (credenciais/gateway) e recebe o cut nível-1 (sobre o líquido X, na materialização); visão só do próprio a receber | Operador; Acionista |
| **Cobrança** | Pedido de pagamento: Operação + Operador + valor + Conta de Gateway; GUID estável; split em % até materializar | Saldo; Hop |
| **Materialização** | No fim de período, o Contador declara — por pagamento, em lote — o **líquido X** que de fato chegou (pós-taxa gateway) **e a Conta de aterrissagem**; % → R$ concretos (nascem os Claims); pagamento inteiro (não parcial) | Hop; Paga (webhook) |
| **Conta de aterrissagem** | Conta onde o líquido do pagamento **chegou de fato** (gateway, crypto ou banco); localização de nascimento dos Claims; pode diferir da Conta de Gateway de emissão | Conta de Gateway de emissão |
| **Livro-mundo** | Um dos dois livros do Nexus: Contas com **saldo fungível (número)** + transações; registro do que o Contador observou na realidade | Ledger; semântica |
| **Ledger** | O outro livro: toda a semântica — Claims, cuts, materialização, “pago”, ajustes, linhagem/eventos. O Nexus **é** o ledger do Contador (não sincroniza com externo) | Livro-mundo; saldo bruto |
| **Claim** | Unidade do ledger: `{ beneficiário, valor, moeda, origem (GUID), localização (Conta), status }`; nasce na materialização quando o % morre | Ownership-em-Conta (morto); % de split |
| **Moeda / Denominação** | Unidade de um valor (BRL, USDT, BTC…). O Nexus **nunca converte** — só registra "X de Y" | Câmbio/FX calculado pelo Nexus |
| **Conta** | Entidade do livro-mundo (gateway, banco, crypto…): **saldo por moeda + transações**, fungível, **multi-moeda**; **não** modela “de quem é” | Tesouraria abstrata; portadora de ownership; conta mono-moeda forçada |
| **Conta de Gateway** | Tipo de Conta: credenciais/gateway; **um** Laranja owner; alvo de **emissão** da Cobrança (≠ necessariamente aterrissagem) | Conta banco/crypto de hop |
| **Saldo** | Número na Conta **por moeda** (livro-mundo), **escopo-org**. **Invariante:** por Conta e por moeda `soma dos claims naquela moeda == saldo naquela moeda`; grana **pessoal** do dono é invisível ao Nexus | Saldo real do banco (inclui grana pessoal); saldo único cross-moeda |
| **Hop** | Fato do Contador: move R$ entre Contas (livro-mundo ±) **e relocaliza claims** (ledger); destino já líquido; cut mid-path = **% proporcional** sobre claims do bundle | Materialização do pagamento |
| **Cut mid-path** | N % do valor em trânsito no hop; rateio **proporcional** em todos os claims do bundle; nasce claim do Laranja na Conta dele | Cut fixo escolhido à mão; “só fulano paga” |
| **Quota (Conta)** | Limite operacional por ciclo (mês etc.) em Conta gateway/banco/crypto; guia emissão de Cobrança e hops | Teto artificial de % no path |
| **Estimativa** | O que o beneficiário de baixa confiança vê antes da liberação: fatia estimada, base = **materialização inicial**, **congelada** (não acompanha hops — evitar vazamento de path) | Valor "atual" atualizando ao vivo (morto — vaza rota) |
| **Claim visível (flag)** | Marca que o Contador/Admin põe quando o claim chega no hop final (pendente só de repasse); muda a exibição de *estimativa* → *pendente/a receber* | Visibilidade automática por hop |
| **Pendente / a receber** | Estado exibido pós-flag: valor concreto (na moeda de aterrissagem) que o beneficiário vai receber | Estimativa congelada |
| **Relatório controlado** | Descritivo de alto nível no reveal — por que recebeu menos/perdeu — sem hops, Contas ou identidades | Extrato hop-by-hop |
| **Ajuste de rota** | Diferença entre estimativa e valor liberado, explicada **uma vez** no relatório controlado; sem detalhe de hops/Laranjas | Atualização incremental ao vivo |
| **GUID (origem do Claim)** | Identidade do pagamento de origem, carregada em cada Claim; linhagem se recompõe por **divisão/junção de claims** + event sourcing | “Fork de GUID” como máquina à parte (morta) |
| **Saque** | Movimentação em lote a partir do gateway ( Contas ); mesma lógica de hop depois do saldo | — |
| **Bundle (do hop)** | Conjunto de claims que um hop toca (o Contador declara "movi estes / tudo nesta Conta"); base do cut proporcional; ** efêmero, por-hop** — não é entidade persistida | Lote/batch armazenado; FK rígida para Cobrança |
| **Reconciliação** | **Use-case oficial e estreito** que traz o Nexus em linha com a realidade observada (banco/gateway/carteira); produz **estado válido** (write-off da falta / atribui a sobra a beneficiário real), **respeitando** as invariantes | Bypass de validação; suspensão de regra; segurar estado inconsistente |
| **Invariante de consistência** | Dentro do Nexus, estado é **sempre** íntegro: `soma claims == saldo` por Conta, claim ≥ 0, todo claim com beneficiário real. Garantida na **fronteira de use case** (não por FK) | Drift tolerado; "soft check" |
| **Evento compensatório** | Correção de fato financeiro errado = **novo evento** (append-only, ES); nunca delete/mutação | Editar/apagar o fato original |
| **Write-off** | Evento que baixa claim(s) a `perdido` com **causa**, quando a grana some sem destino (presa/apreendida/erro) | Encolhimento no hop (perda implícita, com destino) |
| **Cut nível-1 (emissão)** | Corte do Laranja **de emissão** devido por a grana ter passado pela conta geradora; config da **Conta de Gateway enquanto gerador**; independe da Conta de aterrissagem | Cut do dono da Conta de aterrissagem |
| **Attrition** | Queima/falha de Laranja ou Conta (evento comum, não exceção); modelada por event sourcing | Exclusão/desaparecimento silencioso do cadastro |
| **Estado de Conta** | **Dois eixos independentes**: `emissão: ok\|bloqueada` e `saldo: acessível\|congelado\|perdido` | Enum único de status |
| **Estado de Laranja** | `ativo → queimado \| saiu (voluntário) \| traiu`; queimar sinaliza (não hard-burn) as contas dele | Estado da Conta (nível diferente) |
| **Causa** | Categoria anexada a eventos de attrition/write-off: `bloqueio_bancario\|apreensao\|traicao\|saida_voluntaria\|erro_operacional\|desconhecido`; base da intel/reputação | Módulo de analytics separado |
| **Claim preso (exposição)** | Claim localizado em Conta com saldo congelado/perdido; query = soma dos claims ali | Perda já realizada (só após write-off) |
| **Participação / Corte** | No plano %: direito na materialização; mid-path: cut concreto no hop | — |
| **Base do nível 3** | Valor **calculado** na materialização (remanescente pós Laranja+Acionista) sobre o qual as % do nível 3 incidem; **não** é bucket armazenado | "Poço P" como objeto/pool persistido |
| **Split (intenção)** | Conjunto ordenado de % (waterfall) que só existe até a materialização; vira claims concretos e morre | Pool; % que persiste no ledger |
| **Script** | Entidade independente: artefato de ponta versionado/entregue pelo Nexus; **não** executado pelo hub; ligado a Operação só via **operation key** | Operação; Store |
| **Operation key** | Chave de agrupamento em Script e objetos do Store; filtra e impede mistura entre frentes | ID interno da Operação (pode coincidir ou mapear) |
| **Store** | Object store desacoplado (CRUD + pesquisa); objetos keyed por operation key | Cobrança; “filho” embutido da Operação |
| **Gestor de Operações** | Mandato: ciclo de vida e configs das Operações (sem Admin irrestrito) | Admin; Contador |
| **Need-to-know** | Default: só o necessário ao mandato estreito; **cargos de confiança** ampliam escopo de propósito | “Ninguém de confiança sabe nada”; opacidade absoluta |
| **Cargo de confiança** | Mandato amplo por necessidade real (Contador, Gestor de Operações, Admin) | Papel estreito da ponta |
| **Agenciamento** | Vínculo **global** Recrutador→Operador na Organização (deal nível 3); assign a Operação é separado | Atribuição a uma Operação |

## Sinônimos aceitos na prosa

- Recrutador = sargento (mesmo conceito).
- Mandato interno ≈ “internal staff” (só na prosa; não virar nome de papel).
- Contador ≈ “finanças” / “o cara da grana” (prosa); termo canônico = **Contador**.

## Termos proibidos como conceito raiz (por enquanto)

- “InternalStaff” como papel único.
- “Dono” como papel ou entidade do hub.
- “Acionista” como role de gestão / mandato de visão ampla.
- “Átomo” de saldo / rastreio físico sem mescla — morto; usar Livro-mundo (Conta fungível) + Ledger (Claim).
- **“Ownership” como propriedade da entidade Conta** — morto (G9); ownership vive como **Claim no ledger**, não no saldo.
- **“Fork de GUID” como máquina separada** — morto (G9); linhagem = divisão/junção de Claims + event sourcing.
- “Tesouraria” com saldo fora de Conta — não existe; toda Conta é do livro-mundo.
- “Pirâmide” como regra fixa de % — a metáfora é livre; a regra é **ordem canônica fixa + quem/% configuráveis**.
- **"Poço P" / pool / bucket** de nível 3 como objeto armazenado — morto (G5); é **base calculada** na materialização, e depois só **claims**.
- **"Batch" pós-materialização** — não existe; só claims que se movem independentes (o "bundle" do hop é efêmero).
- Ordem de split configurável por operação — rejeitado.
