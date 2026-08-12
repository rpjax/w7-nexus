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

Conjunto de permissões e de fatias de visão. Papéis podem se sobrepor no começo do RPG; a premissa de produto continua sendo gestão fina, não um único super-role.

### Classes de confiança

Ver [vision.md](./vision.md). Resumo:

- **Ponta** — Operadores; “nem preciso saber quem são” no sentido civil / pessoal.
- **Mandato interno** — confiança depositada para uma competência (Finanças, Admin, Recrutamento, …).

## Dono vs Admin vs Acionista — DECIDIDO (fila #2)

Três coisas **diferentes**. Não colapsar no modelo de produto.

| Conceito | Existe no Nexus? | O que é |
|----------|------------------|---------|
| **Dono** | **Não** | Pessoa no mundo real do RPG que criou o sistema e tem infra, código, deploy etc. O hub **não modela** “Dono” e isso **não faz diferença** para o domínio de produto. |
| **Admin** | **Sim — papel** | Administra o sistema. **Acesso irrestrito** no hub. Pode ser a mesma pessoa que é Dono no mundo, ou outra; o produto só conhece Admin. |
| **Acionista** | **Sim — não como role tradicional** | **Beneficiário** do nível 2 do split: facilita dizer “fulano leva X% do lucro do sistema”. Não é mandato de gestão. Quem configura a lista (na prática o Admin) também pode (e em geral vai) constar como Acionista. |

### Implicações

- **Need-to-know vale para todos, menos Admin** — Admin é a exceção explícita (vê/administra tudo). A blindagem da estrutura é contra a ponta e mandatos localizados, não contra Admin.
- **Acionista ≠ login rico por definição** — é entrada de participação (nome/identificador + %). Se no futuro um Acionista tiver login só para ver “meu extrato”, isso é extra; o conceito raiz é beneficiário.
- Não criar papel `Dono` no hub “por simetria”.

## Papéis e beneficiários

### Nomeados e claros o suficiente

| Nome | Tipo | Classe | Notas |
|------|------|--------|--------|
| **Admin** | Papel | Mandato interno | Acesso **irrestrito**; administrar o sistema |
| **Operador** | Papel | Ponta | Login = handle único; visão mínima; payout + vínculo com Recrutador |
| **Laranja** | Papel fino + trilho | Própria | Credenciais/gateway; vê só o próprio a receber |
| **Recrutador** (“sargento”) | Papel | Mandato interno | Competência **concedida**; carteira direta; vínculo global |
| **Gateways** | Papel | Mandato interno | Laranjas, contas de gateway, % do trilho (nível 1 inicial) |
| **Contador** | Papel | Mandato interno | Book-keeping do dinheiro pós-Paga: saques, hops, repasse; sync com o mundo real |
| **Gestor de Operações** | Papel | Mandato interno | Ciclo de vida das ops, assign de gente, configs locais; não é Admin irrestrito |
| **Acionista** | Beneficiário | — | Fatia global nível 2; não é role de gestão |

### Pendentes (nomear/escopar quando a conversa clarear)

| Nome candidato | Por que ainda não |
|----------------|-------------------|
| **Líder de equipe** | Equipes ainda não modeladas; assignment de Laranja “por equipe” adiado |

Regra de condução: **não inventar papel** só por simetria. Nomear quando o mandato tiver responsabilidade clara.

## Recrutamento e visão — DECIDIDO (fila #5)

### Competência de recrutar (não é automático)

- Uma pessoa **pode** acumular Recrutador + Operador.
- **Recrutar** só existe se alguém com a devida competência **permitir** (concessão de mandato/capacidade). Sem isso, vira bagunça de “todo mundo recruta”.
- Quem concede essa permissão no dia a dia: **Admin** com certeza; outros mandatos só se forem nomeados depois com essa competência explícita.

### Downline

- **Só recrutamento direto** (um nível: Recrutador → Operadores que ele trouxe/agencia).
- **Sem árvore** de sub-recrutadores — evitar pirâmide de níveis.

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
| **Handle / nickname** | sim | **Único**; é o **login** no Nexus |
| **Meio de payout** | sim | Como recebe a parte do nível 3 |
| **Vínculo com Recrutador** | sim | Agenciamento **global** na Org (quem trouxe / agencia) |

### O que não guarda

Nome civil, documentos e demais identidade “do mundo” — a ponta permanece opaca nesse sentido.

### Login

- Sim: o Operador entra no hub com o **handle único**.
- Visão: **mínimo privilégio** — ver [visibility.md](./visibility.md). Extrato: próprias cobranças (com Operação), própria fatia, status grosso.

### Implicação

Handle é ao mesmo tempo **identidade de produto** e **credencial de acesso**. Colisões de nickname são regra de negócio (unicidade na Organização / no hub).

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

- Cadastro na **Organização**; assignment contextual (Operação; equipe adiada).
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

- Nomeados com responsabilidade clara: **Gateways**, **Contador**, **Gestor de Operações**.
- Pendente: líder de equipe (equipes adiadas).

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
- Operador (identidade mínima + login por handle): **decidido**.
- Recrutamento (competência concedida, só direto, vínculo **global**, assign a ops separado): **decidido**.
- Staff inicial: **Gateways** + **Contador** + **Gestor de Operações** nomeados; líder de equipe **pendente** (equipes adiadas).
