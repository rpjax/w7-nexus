# Atores e mandatos

## Ideia central

No Nexus existem **Membros** (identidades no hub). O que cada um vê e faz vem de **Papéis / Mandatos**, não de um rótulo vago “staff”.

O sistema **se revela** conforme mandatos são atribuídos.

## Conceitos

### Organização

O “todo” no RPG — a quadrilha. Regras globais (ex.: acionistas across operations) pertencem a este nível.

### Membro

Qualquer identidade que o hub reconhece. Pode ser quase opaca (ponta) ou mais explícita (mandato interno).

### Papel / Mandato

Conjunto de permissões e de fatias de visão. Papéis podem se sobrepor no começo do RPG; a premissa de produto continua sendo gestão fina, não um único super-role. O modelo formal está em **Mandato: capacidades × escopo** (abaixo).

### Classes de confiança

Ver [vision.md](./vision.md). Resumo:

- **Ponta** — Operadores; “nem preciso saber quem são” no sentido civil / pessoal.
- **Mandato interno** — confiança depositada para uma competência (Finanças, Admin, Recrutamento, …).

## Mandato: capacidades × escopo — DECIDIDO (G11)

A divisão interna de uma Operação **não** é modelada como "equipes". A dor real é **delegar gestão com escopo variável** ("gerir tudo" / "gerir uma op" / "gerir só o meu contexto"). Isso é um **Mandato escopado**, não uma estrutura de containers.

### Os dois eixos de escopo

| Eixo | Escopo possível | Cobre |
|------|-----------------|-------|
| **Operação** | nenhuma / algumas / **todas** as ops | "gerir tudo" e "gerir uma op" |
| **Carteira** | meus **recrutados diretos** | "gerir só o meu contexto" |

Um conceito, escopos diferentes. Um mesmo Membro pode ter os dois (o "Gestor de Operações **e** Recrutador"). A **divisão interna da op emerge sozinha** na interseção Operação × Carteira — o gestor da op vê a op inteira; cada recrutador vê a **fatia dele** dentro dela. **Não existe entidade "Equipe".**

### Granularidade: capacidades × escopo (fino, mas domado)

O Mandato realiza a filosofia de **gestão fina de acesso** (ver [vision.md](./vision.md)) sem virar risco:

- **Capacidade** = a menor unidade de "pode fazer X" (ver split, configurar split, assign operador, criar cobrança, onboard, conceder recrutamento, gerir gateways, registrar hop, ler log de auditoria…).
- **Escopo** em cada capacidade (org / conjunto de ops / carteira / uma op específica).
- **Mandato = conjunto de (capacidade × escopo).**
- **Presets nomeados** — os papéis batizados (Gestor de Operações, Recrutador, Contador, Gateways) são **bundles prontos** de capacidades. **Caminho normal:** concede-se um preset, não permissão a permissão. Fine-tune só **por exceção**. **Admin** = preset raiz (todas as capacidades, escopo org).

### As três amarras inegociáveis

1. **Atenuação** — nunca se concede capacidade/escopo que não se tem. Sub-mandato ⊆ guarda-chuva do concedente. A árvore de delegação só **estreita** pra baixo. Admin = raiz.
2. **Auditabilidade** — todo grant é evento (quem/quando/sob qual mandato); "o que X pode e por quê" é reconstruível via ES (ver [auditability.md](./auditability.md)).
3. **Anti-atomização prematura** — só se quebra uma capacidade quando uma delegação real exigir. Começa das capacidades que os presets atuais já pedem; nada de catálogo inventado.

### Autoridade aninha; dinheiro NÃO aninha

Chave que separa gestão saudável de pirâmide:

- **Autoridade PODE aninhar** (delegação atenuante multi-nível) — porque cada um só enxerga pra baixo/dentro (need-to-know intacto).
- **Dinheiro é FLAT:** (a) recrutador ↔ recrutado **direto** = cut nível-1 (`recrutador_pct`); (b) gestão de op = **cut fixo** opcional sobre o resultado da op (linha no waterfall, decidida de cima). **Nunca** % empilhado sobre a fatia de cada subordinado. Ver [money.md](./money.md).

### Revogação de mandato — por causa (reusa G4)

| Causa | Comportamento |
|-------|---------------|
| **Queimado / traiu** | **Suspende em cascata** tudo sob o mandato (a rede dele é suspeita até reconfirmação de um superior) |
| **Saída voluntária** | **Re-parent**: sub-mandatos sobem para o concedente (ex.: Admin); operação segue |

**Nada é retroativo:** claims/cuts já materializados são imutáveis; revogar mandato ou re-agenciar operador só muda **daqui pra frente**.

## Regras precisas de mandato e identidade (endurecimento G13)

Fechadas numa revisão adversarial (implementação só a partir dos docs).

### Composição de mandatos (um Membro, vários mandatos)

- **Mandato efetivo = UNIÃO** de todos os (capacidade × escopo) concedidos ao Membro. Nunca interseção.
- **Visão = união dos escopos.** Se um mandato dá op-A e outro dá op-B, ele vê A e B.
- A matriz de [visibility.md](./visibility.md) é **atalho por preset**, não a fonte da verdade — a fonte é o mandato efetivo (união).

### Capacidades de concessão (a meta-camada que faltava)

Três capacidades **distintas**, todas escopadas e sujeitas à atenuação:

| Capacidade | O que autoriza |
|------------|----------------|
| `conceder_mandato` | Sub-delegar gestão (dar a outro um mandato ⊆ o seu). É o que torna "autoridade aninha" implementável. |
| `recrutar` | Trazer um Operador para a **própria carteira** (agenciar diretamente). |
| `conceder_recrutamento` | Dar a capacidade `recrutar` a um recrutado (habilita a linhagem multi-nível, vetada/auditada). |

`onboard` (preparar a operação/pessoa no hub) é **distinta** das três acima.

### Atenuação é invariante CONTÍNUA (não só no ato de conceder)

- Validada em **dois momentos**: ao conceder (grant-time) **e** sempre que o mandato do concedente muda (on-parent-change).
- **Estreitar** o mandato do concedente (ex.: Admin tira a op-C de um Gestor que geria {A,B,C}) dispara **poda em cascata**: todo sub-mandato sobre C é reduzido para ⊆ o novo guarda-chuva. Forward-only (coerente com "nada retroativo"). Isso cobre o caso que **não** é revogação total nem saída — é redução de escopo.

### Agenciamento ≠ Mandato na revogação (duas hierarquias)

O `recrutador_pct` + a posse da carteira são **agenciamento** (vínculo global), *separado* do mandato de gestão. Na saída de um Recrutador:

| Causa | Mandato | Agenciamento (carteira + `recrutador_pct` futuro) |
|-------|---------|----------------------------------------------------|
| **Queimado / traiu** | suspende em cascata | carteira **suspensa**; `recrutador_pct` das cobranças **futuras** dos operadores dele → **Residual da Organização** até re-agenciamento |
| **Saída voluntária** | re-parent | carteira **re-agenciada** ao concedente (ou a quem o Admin indicar); `recrutador_pct` futuro segue o novo dono |

Operadores nunca ficam órfãos sem regra: futuro cai na Org até re-agenciar. **Retroativo nunca** (claims materializados são imutáveis).

### Raiz da linhagem (bootstrap do recrutamento)

- Todo Operador tem um Recrutador (obrigatório). A **raiz** é a própria **Organização/Admin como recrutador-sistema**, com `recrutador_pct = 0` (a fatia reverte ao Residual da Org). Isso resolve "quem recruta o primeiro operador" e o operador agenciado diretamente pela cúpula.

### Conflito de interesse ponta × gestão (evita furar a estimativa)

- Um Membro **pode** acumular papéis, **mas não pode ter mandato de gestão sobre uma Operação onde ele mesmo é beneficiário de ponta** (Operador/Laranja/Recrutador com fatia ali). Senão ele veria em tempo real o próprio dinheiro, furando a proteção "estimativa congelada" (ver [visibility.md](./visibility.md)). Regra de exclusão validada na concessão do mandato.

### Bootstrap do sistema

- No deploy nasce **um Admin semente** (preset raiz). Admins são iguais entre si (todos irrestritos); criar/!revogar Admin é capacidade de Admin. Primeira Operação e primeiro Recrutador descendem daí.

## Dono vs Admin vs Acionista — DECIDIDO (fila #2)

Três coisas **diferentes**. Não colapsar no modelo de produto.

| Conceito | Existe no Nexus? | O que é |
|----------|------------------|---------|
| **Dono** | **Não** | Pessoa no mundo real do RPG que criou o sistema e tem infra, código, deploy etc. O hub **não modela** “Dono” e isso **não faz diferença** para o domínio de produto. |
| **Admin** | **Sim — papel** | Administra o sistema. **Acesso irrestrito** no hub. Pode ser a mesma pessoa que é Dono no mundo, ou outra; o produto só conhece Admin. |
| **Acionista** | **Sim — não como role tradicional** | **Beneficiário** do nível 2 do split: facilita dizer “fulano leva X% do lucro do sistema”. Não é mandato de gestão. Quem configura a lista (na prática o Admin) também pode (e em geral vai) constar como Acionista. |

### Implicações

- **Need-to-know vale para todos, menos Admin** — Admin é a exceção explícita (vê/administra tudo). A blindagem da estrutura é contra a ponta e mandatos localizados, não contra Admin.
- **Acionista = Membro com login mínimo de leitura** (decidido em G13, resolvendo a ambiguidade antiga): o conceito raiz continua sendo **beneficiário** (participação = identificador + %), **sem** mandato de gestão; mas ele **tem** login read-only para ver a própria participação/fatia (é o que a matriz de [visibility.md](./visibility.md) já lhe dá). Não é login "rico" — é extrato mínimo de beneficiário.
- Não criar papel `Dono` no hub “por simetria”.

## Papéis e beneficiários

### Nomeados e claros o suficiente

| Nome | Tipo | Classe | Notas |
|------|------|--------|--------|
| **Admin** | Papel | Mandato interno | Acesso **irrestrito**; administrar o sistema |
| **Operador** | Papel | Ponta | Login = username único; visão mínima; payout + vínculo com Recrutador |
| **Laranja** | Papel fino + trilho | Própria | Credenciais/gateway; vê só o próprio a receber |
| **Recrutador** (“sargento”) | Papel | Mandato interno | Competência **concedida**; carteira direta; vínculo global |
| **Gateways** | Papel | Mandato interno | Laranjas, contas de gateway, % do trilho (nível 1 inicial) |
| **Contador** | Papel | Mandato interno | Book-keeping do dinheiro pós-Paga: saques, hops, repasse; sync com o mundo real |
| **Gestor de Operações** | Papel | Mandato interno | Ciclo de vida das ops, assign de gente, configs locais; não é Admin irrestrito |
| **Acionista** | Beneficiário | — | Fatia global nível 2; não é role de gestão |

### Pendentes (nomear/escopar quando a conversa clarear)

Nenhum papel candidato em aberto. **Líder de equipe / Equipe** — **não existe** (G11): divisão interna = mandato (capacidade × escopo). Não inventar papel só por simetria.

Regra de condução: **não inventar papel** só por simetria. Nomear quando o mandato tiver responsabilidade clara.

## Recrutamento e visão — DECIDIDO (fila #5)

### Competência de recrutar (não é automático)

- Uma pessoa **pode** acumular Recrutador + Operador.
- **Recrutar** só existe se alguém com a devida competência **permitir** (concessão de mandato/capacidade). Sem isso, vira bagunça de “todo mundo recruta”.
- Quem concede essa permissão no dia a dia: **Admin** com certeza; outros mandatos só se forem nomeados depois com essa competência explícita.

### Downline / linhagem — REFINADO (G11)

O texto antigo dizia "sem árvore de sub-recrutadores". A regra real, mais precisa:

- **A linhagem de recrutamento PODE ter vários níveis** — um recrutado, **se receber a competência** (concedida e auditada), pode recrutar outro. Recrutar é vetado de perto de propósito ("poucos e competentes").
- **O que NÃO existe é árvore de dinheiro nem de visão.** A linhagem é **inerte**:
  - **Dinheiro:** estritamente **nível-1** — cada recrutador ganha só sobre quem **ele** trouxe direto. Sub-recrutados **não** herdam cut pra cima. Sem empilhamento (não é pirâmide: é linear, um degrau).
  - **Visão:** só **downline direto** (um nível). Não vê o que o sub-recrutado faz.

### Visão

- Recrutador vê só quem ele mesmo trouxe/agencia (abaixo direto).
- Não vê outros recrutadores, nem acima, nem organograma lateral.

### Vínculo Recrutador→Operador — DECIDIDO: global (opção A)

- Agenciamento é da **Organização**: um Recrutador “dona” o Operador (carteira + deal de nível 3: `operador_pct` + `recrutador_pct` sobre a **base calculada** do nível 3; **sem pool**).
- **Assign a Operações é separado** — a Operação só aloca quem já existe.
- O corte do recrutador no nível 3 segue o Operador onde ele gerar resultado (nas ops em que estiver assigned).
- Override de % por Operação: **não** no modelo atual (só se um dia doer de verdade).

## Identidade mínima do Operador — DECIDIDO (fila #4)

### O que o hub guarda (mínimo)

| Campo | Obrigatório | Notas |
|-------|-------------|--------|
| **ID interno** | sim | Âncora estável do sistema |
| **Username** | sim | **Único**; é o **login** no Nexus (na UI: usuário) |
| **Meio de payout** | sim | Como recebe a parte do nível 3 |
| **Vínculo com Recrutador** | sim | Agenciamento **global** na Org (quem trouxe / agencia) |

### O que não guarda

Nome civil, documentos e demais identidade “do mundo” — a ponta permanece opaca nesse sentido.

### Login

- Sim: o Operador entra no hub com o **username único**.
- Visão: **mínimo privilégio** — ver [visibility.md](./visibility.md). Extrato: próprias cobranças (com Operação), própria fatia, status grosso.

### Implicação

Username é ao mesmo tempo **identidade de produto** e **credencial de acesso**. Colisões de nome são regra de negócio (unicidade na Organização / no hub).

### Ciclo de vida / attrition do Operador — DECIDIDO (G13)

Simetria com Laranja/Conta: o Operador também falha no mundo real.

- **Estado:** `ativo → queimado | saiu (voluntário) | traiu` (mesma família de `causa` do G4).
- **Cobranças Abertas** do Operador queimado: param de nascer novas; as existentes seguem até Paga/expiram (igual attrition de Conta).
- **Claims** já materializados do Operador são **imutáveis** — continuam a receber repasse normalmente (a menos que write-off por causa real). Queimar o Operador **não** apaga o que ele já tem a receber.
- **Username é aposentado, nunca reusado:** após saída/queima o username fica **reservado permanentemente** (integridade do event sourcing — o mesmo username nunca aponta para duas pessoas na história).

### Classes de identidade — quem é Membro (com login)

| Ator | É Membro (login)? | Notas |
|------|-------------------|-------|
| **Operador** | Sim (ponta) | login = username; visão cega mínima |
| **Laranja** | **Sim** (ponta) | login mínimo; vê só o próprio a receber; credenciais de gateway |
| **Recrutador / Gestor / Contador / Gateways / Admin** | Sim (mandato interno) | login + capacidades por preset/mandato |
| **Acionista** | **Sim — Membro com login mínimo de leitura** | vê só a própria participação e fatia (ver [visibility.md](./visibility.md)); **não** tem mandato de gestão. (Resolve a contradição antiga "login é extra/futuro": na v1 o Acionista tem login read-only, porque a matriz de visão já lhe dá um extrato.) |

## Laranja — DECIDIDO (fila #3)

### Para quê existe

Disponibiliza **dados/credenciais** para a organização **receber pagamentos** (via **gateways**).  
Amarração: **Cobrança → Conta de Gateway (emissão) → um Laranja** (nunca Laranja direto na Cobrança). O cut nível-1 é do Laranja **de emissão** — ver [money.md](./money.md) (Conta de Gateway: dois pontos de vista).

É a identidade do **trilho de recebimento** + beneficiário do nível 1 do split.

### Visão (need-to-know extremo)

O Laranja vê **somente**:

- a grana que tem a receber;
- valor **bruto** do pagamento que entrou;
- em **qual conta de gateway**;
- **quanto é a parte dele**.

**Não vê:** quem fez a venda, nome da Operação, quem mais recebe, quantos cuts existem, estrutura, etc.

### Pertencimento e dinheiro

- Cadastro na **Organização**; assignment contextual (Operação). Sem entidade Equipe.
- % do nível 1: **Admin** ou **Gateways**; **quotas** das Contas (Gateways/Admin).
- Pode aparecer em hops da **cadeia de repasse** (cut mid-path proporcional, 1× por Laranja no fluxo).

### Tipo conceitual

Mais próximo de **beneficiário com login mínimo + Contas de Gateway** do que de Operador ou Acionista. Papel fino: entra no hub e vê o próprio extrato cego.

### Attrition — DECIDIDO (G4)

Queimar é o evento **mais comum**, não exceção. Modelado em dois níveis, ambos por event sourcing (histórico consultável).

**Estado do Laranja:** `ativo → queimado | saiu (voluntário) | traiu`.
- Queimar o Laranja **sinaliza** as contas dele como suspeitas; **não** hard-burn automático — o Contador decide conta a conta (reality-first). Uma Conta pode morrer sozinha sem o Laranja cair.

**Estado da Conta = dois eixos independentes** (mapeiam os dois pontos de vista):
- `emissão: ok | bloqueada` — pode/não emitir Cobrança nova.
- `saldo: acessível | congelado | perdido` — pode/não sacar/hopar o saldo existente.

**Cobranças Abertas em conta com emissão bloqueada:** não morrem automático — param de nascer novas; as existentes seguem até Paga (Contador materializa) ou expiram. Cancelamento é ato manual.

**Re-atribuição:** não se reescreve a conta de emissão de uma Cobrança já enviada; atualiza-se o **conjunto de contas de emissão disponíveis da Operação** (tira a morta, entra a sã) — as *próximas* Cobranças usam a boa.

**Claim preso** numa Conta com saldo congelado/perdido: query direta de exposição (soma dos claims localizados ali). Vira `perdido` via write-off (G6); se recuperado, hop normal.

**Incidência da perda: mecânica** — cai sobre quem tinha claim naquela Conta (se o Contador já moveu o claim do Operador, sobra a Org; se estava todo mundo, todo mundo perde proporcional). Org **não** absorve na v1 — absorção central é *feature futura* (write-off mecânico a mais, on top).

**Causa (intel):** todo evento de attrition/write-off carrega `causa ∈ { bloqueio_bancario, apreensao, traicao, saida_voluntaria, erro_operacional, desconhecido }`. Reputação por Laranja / tipo de conta / gateway = replay de eventos, não subsistema novo.

## Staff / papéis internos — DECIDIDO (fila #8 + #12)

- Nomeados com responsabilidade clara: **Gateways**, **Contador**, **Gestor de Operações** (presets de mandato).
- **Não** há líder de equipe — ver Mandato: capacidades × escopo (G11).

### Contador (ex-“Finanças”) — cargo de confiança

- Mandato de **book-keeping** pós-Paga (webhook): saques, hops, repasse; sync com o mundo real.
- **Visão ampla do financeiro** (necessário no mundo real): inclui **split completo** das cobranças em liquidação + rotas/destinos possíveis.
- Nexus **guia e registra**; Contador executa no mundo.
- **Não** é Admin e **não** recebe organograma/recrutamento/catálogo de gente de bandeja — ver [visibility.md](./visibility.md).
- Distinto de **Gateways** (trilho de recebimento) — Contador opera a **cadeia pós-gateway**.

### Gestor de Operações — cargo de confiança

- Existe para **delegar** gestão de frentes (braço direito de ops). **≠ Admin**.
- Cria/configura Operações sob mandato; assign; configs locais; **vê split completo das ops dele**.
- Need-to-know ajustado: cargos de confiança veem mais **de propósito**; ainda há fronteira clara vs Admin (sem irrestrito no hub).
- Script/Store: via **operation key** das ops sob mandato.

## Estado

- Separação Membro vs Papel vs classe de confiança: **ok**.
- Dono / Admin / Acionista: **decidido**.
- Laranja: **decidido** (trilho + visão cega; Org + assign contextual).
- Operador (identidade mínima + login por username): **decidido**.
- Recrutamento (competência concedida, linhagem multi-nível inerte, vínculo **global**, assign a ops separado): **decidido**.
- Staff inicial: **Gateways** + **Contador** + **Gestor de Operações** (presets). **Sem entidade Equipe** (G11).
