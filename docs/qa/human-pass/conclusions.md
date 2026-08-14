# Human pass — conclusões

Fonte: `docs/qa/human-pass/` (14 ago 2026). Merge dos oito `findings/HP-0N.md`. Nada inventado neste arquivo.

**Unificados:** 0 P0 · 23 P1 · 36 P2 · 13 P3 (72 itens; 75 findings brutos, 4 colisões de «Conta» viraram 1).

## Resumo executivo

As 10 dores que mais melhoram a vida do usuário:

1. **Atalhos do Início e a nav falam inglês interno e etapas de implementação** — o novato não acha «meu dinheiro». Origem: HP-01-001 (também HP-04-005, HP-06-008, HP-07-007 no atalho).
2. **A palavra «Conta» serve para login, membro e livro-mundo** — três conceitos no mesmo chrome. Origem: HP-01-003, HP-03-004, HP-07-007, HP-08-006 (combo da carteira: HP-02-004).
3. **Extrato vazio não ensina o que é extrato neste produto** — empty genérico + jargão de Contador. Origem: HP-01-002.
4. **Recrutador: carteira vazia não aponta o deal; % vazios passam; duas telas «Deals ativos».** Origem: HP-02-001, HP-02-003, HP-02-004 (copy: HP-02-002).
5. **Attrition «Queimado» não tira o poder visível** — Recrutador continua na ficha. Origem: HP-03-001.
6. **Store da operação quebra (HTTP 500)** ao abrir o detalhe; salvar compete com «lista vazia». Origem: HP-04-001.
7. **Encerrar a frente é um clique, sem diálogo** — irreversível. Origem: HP-04-004.
8. **Marcar paga: toast de erro / HTTP 500, mas a cobrança vira Paga** — confiança no status some. Origem: HP-05-002.
9. **Hop com destino &lt; bundle vira «perda» sem aviso** no diálogo. Origem: HP-06-001.
10. **Livro-mundo: Gateway exige Laranja numa lista de logins; emissão Ok em Banco; Lost reaplicável.** Origem: HP-07-001, HP-07-002, HP-07-004.

## Cobertura

| Superfície | Dono | Feito no browser |
|------------|------|------------------|
| Auth | HP-01 | sim |
| Início | HP-01 | sim |
| Perfil | HP-01 | sim (troca de username do seed **não** testada de propósito) |
| Extrato | HP-01 | sim (lista vazia neste passe) |
| Carteira | HP-02 | sim |
| Deals | HP-02 | sim |
| Contas | HP-03 | sim (seed Admin não desabilitado) |
| Operações | HP-04 | sim (trilho ligar/desligar e Store remover **não testáveis** neste ambiente; Store list 500) |
| Cobranças | HP-05 | sim |
| Claims | HP-06 | sim (estorno confirmado só até o diálogo; cancelado para não tocar cobrança alheia) |
| Livro-mundo | HP-07 | sim (Gateway só no caminho de erro) |
| Acionistas | HP-08 | sim |
| Chrome / nav / sair / mobile | HP-08 | sim |
| Erros / toasts / confirmações (consistência) | HP-08 | sim (passe de chrome nas outras rotas) |

## Findings unificados (por severidade)

### P0

Nenhum. Nenhum agente registrou tela branca, loop de login ou `Failed to fetch` bloqueante.

### P1

#### [HP-01-001] Atalhos do Início falam inglês interno e etapas de implementação
- **Lentes:** copy | ux | polish
- **Onde:** `/dashboard` → cards Operações, Cobranças, Claims, Livro-mundo
- **O que vi:** «Ciclo, assign, Script e Store por operation key.»; «Emissão, split e webhook Paga.»; nav mistura PT («Cobranças») com EN («Claims», «Deals»).
- **Por que dói:** Novato não sabe o que clicar para ver o dinheiro.
- **Sugestão:** Linha humana no atalho; jargão no subtítulo, não no título da nav.
- **Origem:** HP-01-001 (eco: HP-04-005, HP-06-008, HP-07-007)

#### [HP-01-002] Extrato vazio não ensina o que é extrato neste produto
- **Lentes:** gap | copy | ux
- **Onde:** `/dashboard/statement` → empty «Nada a mostrar»; card Fatias
- **O que vi:** Subtítulo com materialização, hops e path; atalho do Início «Movimentação visível da sua conta.»
- **Por que dói:** «Onde está meu dinheiro?» recebe empty genérico + jargão de Contador. «Fatias» fora do glossário.
- **Sugestão:** Empty do tipo «Quando uma cobrança for materializada… Pendente só depois que o Contador revelar.»
- **Origem:** HP-01-002 (empty inconsistente: HP-08-004)

#### [HP-02-001] Empty da carteira não ensina o próximo passo (criar deal)
- **Lentes:** gap | copy | ux
- **Onde:** `/dashboard/carteira` → empty + Conceder…
- **O que vi:** «Nenhum operador na sua carteira.» Toast ao conceder Operador sem deal: «Preset Operator exige um AgencyDeal ativo (Recrutador-raiz Admin com pct=0 e valido).» Sem CTA para Deals.
- **Por que dói:** O recrutador só vê conceder mandato; o produto exige deal primeiro.
- **Sugestão:** Empty apontando Pessoas → Deals; toast humano.
- **Origem:** HP-02-001 (toast irmão: HP-03-006, HP-04-002)

#### [HP-02-002] Título «Deals de agenciamento» e subtítulo de fórmula
- **Lentes:** copy | polish
- **Onde:** `/dashboard/deals` → título e parágrafo; nav Pessoas → Deals
- **O que vi:** «Soma dos % ≤ 100; resto = Residual da Org. Raiz: Admin com recrutador_pct = 0.»
- **Por que dói:** Recrutador pergunta fatia da turma e recebe inequação.
- **Sugestão:** Título humano («Agenciamento»); fórmula num «como funciona».
- **Origem:** HP-02-002 (nav vs H1: HP-08-005)

#### [HP-02-003] Salvar deal aceita % vazios e o diálogo mostra «(% / %)»
- **Lentes:** bug | ux
- **Onde:** `/dashboard/deals` → Salvar deal… + diálogo
- **O que vi:** Sem mensagem de campo; diálogo «Vincular qa02-op1 a admin (% / %).»
- **Por que dói:** Caminho burro confirma fatia em branco.
- **Sugestão:** Bloquear sem % válidos (0–100, soma ≤ 100) com texto no campo.
- **Origem:** HP-02-003

#### [HP-02-004] Carteira e Deals são dois mundos; «copiar ID» não existe na carteira
- **Lentes:** gap | ux | copy
- **Onde:** `/dashboard/carteira` lista vs `/dashboard/deals`; combo Conta
- **O que vi:** Carteira só username + %; Deals tem Encerrar; combo lista todas as identidades; sem link «abrir este deal».
- **Por que dói:** Duas telas «Deals ativos» sem explicar downline vs cadastro global. «Conta» no combo é login.
- **Sugestão:** Linha = pessoa + fatia + atalho ao agenciamento; combo «Pessoa» / «Usuário».
- **Origem:** HP-02-004

#### [HP-01-003 + HP-03-004 + HP-07-007 + HP-08-006] «Conta» colide com Conta do livro-mundo
- **Lentes:** copy | ux
- **Onde:** `/dashboard` kicker «Resumo da conta»; `/dashboard/profile` kicker CONTA; `/dashboard/accounts` título e tipo **Conta** \| **Admin**; nav Pessoas → Contas; Livro-mundo card Contas; Acionistas coluna CONTA (username)
- **O que vi:** Glossário reserva Conta ao livro-mundo; a UI chama identidade/login de Conta em vários sítios, ao lado de «Livro-mundo».
- **Por que dói:** Três «contas» na mesma sessão; gateways misturam login e caixa real.
- **Sugestão:** Perfil/início «Sessão» / «Identidade»; nav Pessoas «Membros» / «Identidades»; tipo Usuário vs Admin; coluna de Acionistas «Usuário»; reservar Conta ao livro-mundo.
- **Origem:** HP-01-003, HP-03-004, HP-07-007, HP-08-006

#### [HP-03-001] Attrition «Queimado» não tira o poder visível
- **Lentes:** bug | gap | ux
- **Onde:** `/dashboard/accounts` → detalhe → Mandatos / Attrition
- **O que vi:** «Queimado · Saída voluntária» com preset Recrutador ainda ativo; disable só bloqueia login.
- **Por que dói:** Guarda fecha o portão no papel e a pessoa continua Recrutador.
- **Sugestão:** Explicitar login vs mandato; oferecer revogação em cascata no mesmo diálogo.
- **Origem:** HP-03-001

#### [HP-03-006] Erro ao conceder Operador é jargão de API
- **Lentes:** copy | ux
- **Onde:** `/dashboard/accounts` → Mandatos → Operador
- **O que vi:** Toast «Preset Operator exige um AgencyDeal ativo (Recrutador-raiz Admin com pct=0 e valido).»
- **Por que dói:** Não diz o que clicar (Deals).
- **Sugestão:** Toast em PT com atalho à tela Deals.
- **Origem:** HP-03-006 (mesmo texto: HP-02-001)

#### [HP-04-001] Store quebra ao abrir o detalhe (HTTP 500)
- **Lentes:** bug | ux
- **Onde:** `/dashboard/operations` → selecionar operação → aba Store
- **O que vi:** «Não foi possível listar o Store.»; após Salvar: sucesso **e** o mesmo erro; «Nenhum objeto no Store.»; sem Remover.
- **Por que dói:** Gestor não confirma se o objeto existe.
- **Sugestão:** Corrigir o list; não fingir lista vazia depois de «salvo».
- **Origem:** HP-04-001

#### [HP-04-002] Associar operador exige deal/preset que a tela não ensina
- **Lentes:** gap | copy | ux
- **Onde:** `/dashboard/operations` → Operadores → Associar
- **O que vi:** Toast «Membro precisa ser Operator com AgencyDeal ativo.» Combo cheio de logins; sem link para Deals/Contas.
- **Por que dói:** Montar a frente parece possível; o domínio só aparece no toast.
- **Sugestão:** Filtrar contas elegíveis ou empty com pré-req e atalho.
- **Origem:** HP-04-002

#### [HP-04-003] Sem trilho, a UI não diz o que falta no mundo
- **Lentes:** gap | copy | ux
- **Onde:** `/dashboard/operations` → Trilhos de emissão
- **O que vi:** «Nenhum trilho cadastrado»; Ligar desabilitado; livro-mundo vazio na sessão HP-04.
- **Por que dói:** Gestor não sabe se o produto quebrou ou falta abrir conta-mundo.
- **Sugestão:** Empty com próximo passo (abrir conta no livro-mundo e emitir trilho).
- **Origem:** HP-04-003

#### [HP-04-004] Encerrar a frente não pede confirmação
- **Lentes:** ux | gap
- **Onde:** `/dashboard/operations` → Ciclo → Encerrar
- **O que vi:** Clique único; status Encerrada; «sem transições.»
- **Por que dói:** Caminho burro não tem cancelar; irreversível.
- **Sugestão:** Diálogo «não volta a Ativa» com Cancelar / Confirmar.
- **Origem:** HP-04-004

#### [HP-05-001] Admin não gera cobrança sem operador assignado — a UI diz que é opcional
- **Lentes:** bug | copy | ux
- **Onde:** `/dashboard/charges` → Operador (opcional) + Gerar
- **O que vi:** Texto de emissão sem operador; toast `Operador nao esta assigned nesta Operacao.` Só gerou após escolher operador no combo.
- **Por que dói:** Caminho feliz do Admin quebra. Campo vazio trata o logado como Operator.
- **Sugestão:** Emitir sem ser Operator **ou** campo obrigatório + erro em PT.
- **Origem:** HP-05-001

#### [HP-05-002] Marcar paga: toast de erro, mas a cobrança vira Paga
- **Lentes:** bug | ux
- **Onde:** `/dashboard/charges` → Marcar paga
- **O que vi:** Toast «Não foi possível marcar como Paga.»; `POST …/mark-paid` **500**; após recarregar, status **Paga**.
- **Por que dói:** Emissor acha que falhou e tenta de novo; o fato já persistiu.
- **Sugestão:** Sucesso se o evento persistiu; nunca 500 + lista mentindo.
- **Origem:** HP-05-002

#### [HP-06-001] Hop com destino &lt; bundle vira «perda» sem aviso
- **Lentes:** bug | ux | copy
- **Onde:** `/dashboard/claims` → Registrar hop
- **O que vi:** Destino 14 de 44,1; toast «Hop registrado.»; painel hops «perda 30.1». Diálogo só contou destinos.
- **Por que dói:** Contador acha que move 14 e deixa resto; a UI registra perda.
- **Sugestão:** Mostrar perda no diálogo; exigir causa se perda &gt; 0; ou oferecer resto na origem.
- **Origem:** HP-06-001

#### [HP-06-003] Status em inglês e «Activevisível» colado
- **Lentes:** copy | bug | polish
- **Onde:** `/dashboard/claims` coluna Status; filtro Cobrança
- **O que vi:** `Active`; após revelar `Activevisível`; filtro `Materialized` / `Paid` / `Open`; hop `Bank` vs Livro-mundo `Banco`.
- **Por que dói:** Glossário pede ativo | repassado | perdido | estornado | arquivado. Concatenação parece bug.
- **Sugestão:** PT canônico; flag visível separado; filtros Paga / Aberta / Materializada.
- **Origem:** HP-06-003

#### [HP-06-004] Ficha do claim é uma linha, não o fato
- **Lentes:** gap | ux | copy
- **Onde:** `/dashboard/claims` → Ficha
- **O que vi:** Linha sob a tabela com UUID, montante, conta, `Active`. Sem linhagem de hops nem cobrança origem.
- **Por que dói:** CRUD de IDs, não o livro do Contador.
- **Sugestão:** Drawer: beneficiário, Conta, montante, status, visível, cobrança, hops.
- **Origem:** HP-06-004

#### [HP-06-007] Estornar cobrança usa o filtro e pode apontar outra cobrança
- **Lentes:** ux | bug | copy
- **Onde:** `/dashboard/claims` → filtro Cobrança + Estornar cobrança
- **O que vi:** Sem filtro: «Escolha a cobrança nos filtros». Primeira opção do combo era cobrança de outro agente (`Paid · 61 BRL`, lista **Nenhum claim**). Duas portas (Claims vs Cobranças). Sem causa `estorno` no diálogo.
- **Por que dói:** Fácil estornar a cobrança errada. Empty + diálogo contradiz.
- **Sugestão:** Estornar só com seleção explícita (rótulo da op); pedir causa; uma porta canônica.
- **Origem:** HP-06-007 (causa também: HP-05-006)

#### [HP-07-001] Abrir Gateway exige Laranja, mas a lista é de logins
- **Lentes:** bug | gap | copy | ux
- **Onde:** `/dashboard/world-accounts` → Tipo Gateway · Laranja · Abrir
- **O que vi:** Dropdown de logins; Abrir habilita só com rótulo; toast «Conta precisa existir e atuar como Laranja.»
- **Por que dói:** Operador de gateways não abre a conta de emissão. «Conta» aqui é login.
- **Sugestão:** Filtrar só Laranja; desabilitar Abrir sem Laranja; toast de produto.
- **Origem:** HP-07-001 (pré-req também: HP-06-011)

#### [HP-07-002] Emissão Ok/Bloqueada aparece em Banco/Crypto/Payout
- **Lentes:** gap | copy | ux
- **Onde:** detalhe da conta-mundo → Emissão e saldo
- **O que vi:** Em Banco, toast «Eixo de emissao so existe em Conta de Gateway.» Coluna Emissão continua **Ok**.
- **Por que dói:** UI mostra emissão em tipos que não emitem.
- **Sugestão:** Esconder o eixo fora de Gateway.
- **Origem:** HP-07-002

#### [HP-07-003] Reconciliar não opera o livro-mundo sozinho
- **Lentes:** gap | copy | ux
- **Onde:** detalhe → Saldo observado · Reconciliar
- **O que vi:** Sem claims: «Sem claims ativos para ratear a falta.» / «Invariante soma claims != saldo apos reconciliacao.» Saldo inalterado. Causa de attrition no mesmo controlo que Lost.
- **Por que dói:** Pede um número e recusa em jargão de invariante.
- **Sugestão:** Explicar claims = saldo; oferecer observação (crédito/débito) quando não há claims.
- **Origem:** HP-07-003

#### [HP-07-004] Perdido de novo na conta já perdida ainda confirma e «aplica»
- **Lentes:** bug | ux
- **Onde:** conta já Perdido → Perdido
- **O que vi:** Mesmo diálogo e toast de sucesso; Congelar/Débito/Reconciliar continuam ativos.
- **Por que dói:** Ação irreversível deveria estar morta.
- **Sugestão:** Desabilitar Perdido/observação quando já perdida.
- **Origem:** HP-07-004

### P2

#### [HP-01-004] Aba «Primeiro admin» parece cadastro; chave mestra à vista
- **Onde:** `/auth` → Primeiro admin
- **Origem:** HP-01-004
- **Dor:** Bootstrap de emergência compete com onboarding; `local-dev-master-key` e senha seed impressas.
- **Sugestão:** Esconder atrás de «Problemas para entrar?»; não pré-preencher segredos.

#### [HP-01-007] Perfil: «tokens da sessão» e aviso de nome eterno assustam
- **Onde:** `/dashboard/profile`
- **Origem:** HP-01-007
- **Dor:** Validação ok; tom de «para sempre» (três vezes) e sem diálogo na senha.
- **Sugestão:** «Trocar a senha encerra outras sessões.» Um aviso de nome; confirmar só se mudar o usuário.

#### [HP-02-005] Carteira no grupo Eu, Deals no grupo Pessoas
- **Onde:** shell Eu / Pessoas
- **Origem:** HP-02-005
- **Dor:** Downline junto de Perfil/Extrato; agenciar noutro sítio com anglicismo.
- **Sugestão:** Juntar sob Pessoas («Minha carteira» + «Agenciar»).

#### [HP-02-006] Confirmações e toasts misturam humano com jargão de mandato
- **Onde:** `/dashboard/deals` e `/dashboard/carteira` diálogos
- **Origem:** HP-02-006
- **Dor:** «Deal», «mandato Operador», «Preset concedido».
- **Sugestão:** «Salvar vínculo», «Encerrar agenciamento», «Operador liberado para …».

#### [HP-02-007] «Carteira» ainda puxa saldo, não downline
- **Onde:** nav Eu → Carteira
- **Origem:** HP-02-007
- **Dor:** Fora do glossário, carteira = dinheiro; está ao lado de Extrato.
- **Sugestão:** Nav «Minha gente» / «Downline».

#### [HP-03-002] Causa e status de attrition aceitam combinação sem sentido
- **Onde:** `/dashboard/accounts` Status/Causa
- **Origem:** HP-03-002
- **Dor:** Queimado + Saída voluntária sem reclamar.
- **Sugestão:** Filtrar causas por status.

#### [HP-03-003] «Sinalização», «attrition» e «write-off» no caminho de desligar gente
- **Onde:** diálogo Registrar attrition
- **Origem:** HP-03-003
- **Dor:** Humano não sabe se queima Laranja, corta login ou só anota.
- **Sugestão:** Título em PT; «Não bloqueia o login sozinho; não mexe em dinheiro.»

#### [HP-03-007] Filtro «Todos os presets» só conhece Admin
- **Onde:** lista Contas → combobox presets
- **Origem:** HP-03-007
- **Dor:** Recrutador e outros mandatos invisíveis na lista/filtro.
- **Sugestão:** Coluna Admin? vs Mandatos; filtrar por preset de produto.

#### [HP-03-008] Conta nova some da lista se a busca anterior ficou ligada
- **Onde:** busca + Nova conta
- **Origem:** HP-03-008
- **Dor:** Parece que a criação falhou.
- **Sugestão:** Limpar keyword ao criar ou garantir a linha nova.

#### [HP-03-009] Hierarquia visual: attrition no meio dos mandatos
- **Onde:** detalhe Contas, bloco Mandatos
- **Origem:** HP-03-009
- **Dor:** Desligar gente em dois sítios (attrition vs disable) sem diferença; IDs `Recruiter` etc.
- **Sugestão:** Bloco Poder vs Baixa; IDs só em tooltip.

#### [HP-03-010] Grants específicos mostram código e GUID, não o nome da operação
- **Onde:** Ajuste fino · gerir operação
- **Origem:** HP-03-010
- **Dor:** Revogar a op errada; toast «Capacidade Specific».
- **Sugestão:** «Gerir operação · qa04-frente».

#### [HP-03-011] Confirmar destrutivo é o mesmo botão primário em tudo
- **Onde:** diálogos Conceder Admin / attrition / Desabilitar; reset senha sem diálogo
- **Origem:** HP-03-011
- **Dor:** Admin ou queimado no piloto automático.
- **Sugestão:** Primário nomeado («Desabilitar», «Tornar Admin»); confirmar reset.

#### [HP-04-005] Chave `op_…` e inglês de infra na cara do gestor
- **Onde:** lista/detalhe de operação; Script avançado
- **Origem:** HP-04-005
- **Dor:** UUID + assign/edge/SQL; humano opera pelo nome da frente.
- **Sugestão:** Chave só em copiar ID / avançado.

#### [HP-04-006] «Cut» e erro com `null` não explicam a regra
- **Onde:** Cut de gestão (%)
- **Origem:** HP-04-006
- **Dor:** `Cut de gestao deve ser null ou entre 0 e 100.`
- **Sugestão:** «Percentual de gestão»; erro 0–100 ou vazio.

#### [HP-04-007] No telemóvel a lista e o detalhe empilham e competem
- **Onde:** `/dashboard/operations` ~390px
- **Origem:** HP-04-007
- **Dor:** Fácil acionar Encerrar/Salvar sem contexto.
- **Sugestão:** Detalhe em ecrã próprio ou lista some após o toque.

#### [HP-05-003] «Paga» vs «Materializada» na lista é rótulo, não pedagogia
- **Onde:** `/dashboard/charges` lista + diálogo Marcar paga
- **Origem:** HP-05-003
- **Dor:** Mesmo verde de sucesso; diálogo de paga não diz que ainda não cria claims. Split em inglês (`Orange`, `Agency`…).
- **Sugestão:** Diferenciar visualmente; linha «Ainda não materializa».

#### [HP-05-004] Gerar sem operação só desliga o botão
- **Onde:** `/dashboard/charges` → Gerar
- **Origem:** HP-05-004
- **Dor:** Sem texto; combo `Draft` / `Closed` vs Rascunho/Encerrada na lista de ops. Toast `So Operacao Ativa aceita nova Cobrança.`
- **Sugestão:** Texto sob o botão; status PT no combo.

#### [HP-05-005] Aterrissagem: label certa, placeholder de outra conta
- **Onde:** Materializar → Conta de aterrissagem
- **Origem:** HP-05-005
- **Dor:** Placeholder «conta do livro-mundo»; combo não destaca o trilho; líquido 95 vs bruto 123 sem explicação.
- **Sugestão:** «Onde o líquido chegou»; pré-selecionar trilho; validar líquido vs bruto.

#### [HP-05-006] Estorno: aviso curto e sem causa
- **Onde:** `/dashboard/charges` → Estornar
- **Origem:** HP-05-006
- **Dor:** Destrutivo no livro sem causa do glossário.
- **Sugestão:** Pedir causa; repetir valor + «reverte claims e o livro-mundo».

#### [HP-05-007] Cancelar / Expirar / Falhou sem confirmação
- **Onde:** ficha Aberta
- **Origem:** HP-05-007
- **Dor:** Clique mata Aberta; Paga esconde o trio sem explicar.
- **Sugestão:** Mesmo padrão de diálogo; linha «indisponível depois de Paga».

#### [HP-06-002] Origem = destino e 0 destinos passam do formulário
- **Onde:** `/dashboard/claims` Registrar hop
- **Origem:** HP-06-002
- **Dor:** Diálogo «0 destino(s)»; origem=destino só falha o montante depois.
- **Sugestão:** Bloquear Confirmar; mensagem no campo.

#### [HP-06-005] Revelar esconde o relatório e não explica o extrato
- **Onde:** Claims → Cut e repasse → Revelar; `/dashboard/statement`
- **Origem:** HP-06-005
- **Dor:** Relatório controlado oculto no acordeão; toast «Relatorio controlado exige causa generica.»
- **Sugestão:** Campo visível ao lado de Revelar; preview da ponta; PT no toast.

#### [HP-06-006] Repasse sem formulário — só toast e campos no acordeão
- **Onde:** `/dashboard/claims` → Repasse
- **Origem:** HP-06-006
- **Dor:** Botão órfão; «Cut in-place» / «payout».
- **Sugestão:** Fluxo Repasse próprio (conta payout, status → repassado).

#### [HP-06-008] Nav «Claims» vs direitos; mistura Conta / ledger / UUID
- **Onde:** shell DINHEIRO → Claims; tabela Claim = UUID
- **Origem:** HP-06-008
- **Dor:** Subtítulo promete «sem colar UUID»; Residual = `11111111…`; empty «ledger».
- **Sugestão:** Nav «Direitos» ou «Claims (a receber)»; beneficiário com username.

#### [HP-06-010] Cut mid-path existe, mas a UI não ensina o bundle
- **Onde:** Claims → Cut e repasse
- **Origem:** HP-06-010
- **Dor:** Sem texto «bundle deste hop»; cut in-place é jargão.
- **Sugestão:** «Claims no hop (bundle)» + cut proporcional.

#### [HP-06-011] Sequência mínima até o claim não está na tela de Claims
- **Onde:** empty inicial de Claims
- **Origem:** HP-06-011
- **Dor:** Manda materializar; não lista op ativa, trilho, Paga, aterrissagem. Abrir Conta exige Laranja.
- **Sugestão:** Empty com 4 passos clicáveis.

#### [HP-07-005] Congelar saldo não pede confirmação (Lost pede)
- **Onde:** Congelar saldo / Descongelar
- **Origem:** HP-07-005
- **Dor:** Clique único trava hops/exposição; assimétrico com Lost.
- **Sugestão:** Confirmar congelar; descongelar pode ficar direto.

#### [HP-07-006] Exposição nunca explica «claim preso» e fica vazia
- **Onde:** Exposição (congelado/perdido)
- **Origem:** HP-07-006
- **Dor:** Sempre «Sem exposição registada.» com saldo congelado/perdido sem claims. PT-PT.
- **Sugestão:** Empty: saldo observado não conta; exposição = direitos presos.

#### [HP-07-008] Transações e write-off em inglês / slug
- **Onde:** lista de transações no detalhe livro-mundo
- **Origem:** HP-07-008
- **Dor:** `Credit` / `Debit` / `write-off:bloqueio_bancario`; memo partilhado no lost.
- **Sugestão:** Crédito/Débito/Write-off; causa humana; memo próprio.

#### [HP-07-009] Quota e Observação partilham estado; Quota vs teto
- **Onde:** formulário Abrir + Quota + Observação
- **Origem:** HP-07-009
- **Dor:** Quota 5000 vazou para contas novas; Cut nível-1 = «10» sem %; Payout sem PT.
- **Sugestão:** Estado Abrir separado do detalhe; «Quota restante neste ciclo».

#### [HP-07-010] Lost: copy mistura write-off; emissão Ok na conta perdida
- **Onde:** diálogo Perdido; cabeçalho da conta
- **Origem:** HP-07-010
- **Dor:** Write-off para quem opera gateway; dois eixos sem legenda.
- **Sugestão:** Copy humana de caixa perdida; esconder emissão se Perdido.

#### [HP-08-001] URL inexistente some no Início sem dizer que a rota não existe
- **Onde:** `/dashboard/nao-existe` (catch-all → dashboard)
- **Origem:** HP-08-001
- **Dor:** Deep link quebrado parece «me mandaram para casa».
- **Sugestão:** 404 interno no shell com o path pedido.

#### [HP-08-002] Toast de soma &gt; 100% chega sem acentos
- **Onde:** `/dashboard/shareholders` → Salvar participação
- **Origem:** HP-08-002
- **Dor:** `A soma das participacoes de Acionistas nao pode exceder 100%.`
- **Sugestão:** Mesma frase no cliente, com acentos.

#### [HP-08-003] Diálogo «Remover participação» não diz quem sai
- **Onde:** `/dashboard/shareholders` → Remover…
- **Origem:** HP-08-003
- **Dor:** Sem username nem % (salvar nomeia a conta). Clique errado em `minuteman` nesta sessão.
- **Sugestão:** Repetir username + % no corpo.

#### [HP-08-004] Empty states falam línguas diferentes
- **Onde:** passe nas rotas da nav
- **Origem:** HP-08-004
- **Dor:** «Nada a mostrar» vs «Nenhum operador…» vs «Nenhum claim»; pontuação irregular.
- **Sugestão:** Padrão «Nenhum(a) {coisa do título}.» + próximo passo.

#### [HP-08-010] Copy de Acionista vs Admin vs «nível 2» não fecha o conceito
- **Onde:** `/dashboard/shareholders`
- **Origem:** HP-08-010
- **Dor:** «nível 2» e «login read-only» não aparecem noutro chrome; risco de achar Acionista = dono da casa.
- **Sugestão:** Fatia residual, sem poder de gestão; ligação a Residual da Org.

### P3

#### [HP-01-005] Login falho: mesmo texto no form e no toast; «Usuario» sem acento
- **Onde:** `/auth` → Entrar
- **Origem:** HP-01-005

#### [HP-01-006] Copy da tela de auth («token de acesso»)
- **Onde:** `/auth` painel esquerdo
- **Origem:** HP-01-006

#### [HP-01-008] Sair funciona; `?redirect=` na URL é técnico
- **Onde:** shell → Sair
- **Origem:** HP-01-008 (Sair também observado por HP-08; funciona)

#### [HP-03-005] Copy fantasma «etapa 02» no create
- **Onde:** diálogo Nova conta
- **Origem:** HP-03-005

#### [HP-03-012] Empty da lista vs empty do detalhe
- **Onde:** busca zerada em Contas
- **Origem:** HP-03-012

#### [HP-04-008] Script resolve ecoando o corpo; «edge» não ensina
- **Onde:** Operações → Script → Resolver (edge)
- **Origem:** HP-04-008

#### [HP-05-008] Ficha da cobrança fala em IDs, não em nomes
- **Onde:** ficha da cobrança
- **Origem:** HP-05-008

#### [HP-06-009] Filtros: combos em inglês, Filtrar obrigatório
- **Onde:** `/dashboard/claims` → Filtros
- **Origem:** HP-06-009

#### [HP-07-011] Erros e empty em mistura PT/EN e PT-PT
- **Onde:** toasts e empty do livro-mundo
- **Origem:** HP-07-011

#### [HP-08-005] Nav e H1 não usam o mesmo nome; `document.title` = Nexus
- **Onde:** shell vs `h1`
- **Origem:** HP-08-005

#### [HP-08-007] Início de Admin usa kicker «ADMINISTRAÇÃO» no grupo Eu
- **Onde:** `/dashboard`
- **Origem:** HP-08-007

#### [HP-08-008] Loading some rápido demais; empty vs erro de load
- **Onde:** listas com tabela
- **Origem:** HP-08-008

#### [HP-08-009] Overlay full-screen do menu mobile é difícil de acertar
- **Onde:** viewport ~390px, Abrir menu
- **Origem:** HP-08-009

## Copy e vocabulário (síntese)

Termos que **não passam** nas três perguntas (fácil? humano? domínio?):

| Termo na UI | Problema | Onde (origens) |
|-------------|----------|----------------|
| **Conta** | Login, membro e livro-mundo no mesmo chrome | HP-01-003, HP-03-004, HP-07-007, HP-08-006, HP-02-004 |
| **Claims / Deals** | Inglês na nav; H1 às vezes alonga («Deals de agenciamento») | HP-01-001, HP-02-002, HP-06-008, HP-08-005 |
| **Carteira** | Soa saldo; é downline | HP-02-007 |
| **AgencyDeal / Operator / assigned / pct=0** | Toast de API no caminho do recrutador, guarda e gestor | HP-02-001, HP-03-006, HP-04-002, HP-05-001 |
| **Attrition / sinalização / write-off** | Tela de identidades e Lost de gateway | HP-03-003, HP-07-010, HP-07-008 |
| **Fatias** | Fora do glossário no extrato | HP-01-002 |
| **Cut / cut in-place / payout / edge / Store / operation key** | Infra no Início e nas telas de ops/claims | HP-01-001, HP-04-005, HP-04-006, HP-06-006, HP-06-010 |
| **Active / Paid / Open / Materialized / Draft / Closed / Credit / Debit / Bank** | Enums crus; `Activevisível` | HP-05-004, HP-06-003, HP-07-008 |
| **Token de acesso / tokens da sessão** | Auth e perfil para novato | HP-01-006, HP-01-007 |
| **Nível 2 / login read-only** | Acionistas sem âncora no resto do chrome | HP-08-010 |
| **Trilho** | Sem glossário na operação vazia | HP-04-003 |
| Acentos / PT-PT | `Usuario`, `gestao`, `emissao`, `participacoes`, `registada`, `Relatorio`, `generica` | HP-01-005, HP-04-006, HP-06-005, HP-07-002, HP-07-006, HP-07-011, HP-08-002 |

O que **bate** o glossário quando há dados: colunas Estimativa / Pendente no extrato (HP-01, HP-06); status de operação Rascunho/Ativa/Pausada/Encerrada em PT (HP-04); Paga vs Materializada como fatos distintos no kicker de Cobranças, ainda que a lista não pedagogize (HP-05-003); hop canónico com perda = origem − destinos — a UI calcula, mas **não avisa** (HP-06-001).

## UX / conveniência (síntese)

Onde o usuário se bate:

1. **Pré-requisitos invisíveis** — deal antes de Operador (carteira, contas, operações); Laranja antes de Gateway; operador assignado antes de gerar cobrança apesar de «opcional»; trilho/quota antes de emitir. A UI habilita o botão e o toast fala domínio.
2. **Confirmação irregular** — Encerrar operação, Cancelar/Expirar/Falhou cobrança, Congelar saldo e reset de senha: clique único. Lost, materializar, estorno, deals: diálogo. Primário sempre «Confirmar», inclusive Admin e queimado (HP-03-011). Remover acionista não nomeia a linha (HP-08-003).
3. **Dois sítios para o mesmo fato** — Carteira vs Deals; attrition vs desabilitar; Estornar em Cobranças e em Claims (filtro perigoso); emissão Ok em tipos que não emitem.
4. **IDs na cara** — `op_…`, GUIDs de claim/trilho/operador, Residual `11111111…`, `noop-…`. Ficha de claim e de cobrança são recibos técnicos.
5. **Empty que não ensina** — Extrato «Nada a mostrar»; Claims manda materializar sem a cadeia; carteira sem CTA de deal; operações sem trilho; exposição vazia com dinheiro congelado. HP-08-004: cada tela uma voz.
6. **Erro vs verdade** — Marcar paga 500 mas persiste; Store 500 com empty; Lost de novo «aplica»; reconciliação recusa em invariante.
7. **Mobile** — Operações lista+detalhe na mesma rolagem (HP-04-007); overlay do menu difícil de fechar no meio (HP-08-009). 404 interno inexistente (HP-08-001).
8. **Formulários que vazam estado** — busca ligada esconde conta nova; quota do detalhe vaza para Abrir; % de deal vazios no diálogo.

O que tornaria a UI mais didática: uma voz PT-BR; empty = conceito + próximo clique; pré-reqs filtrados ou desabilitados com texto; diálogos destrutivos nomeando objeto, montante e efeito (perda, claims, login vs mandato); nav alinhada ao glossário (Membros / Agenciamento / Direitos / Conta só no livro-mundo).

## Origem

| Agente | Arquivo | Status |
|--------|---------|--------|
| HP-01 | findings/HP-01.md | feito |
| HP-02 | findings/HP-02.md | feito |
| HP-03 | findings/HP-03.md | feito |
| HP-04 | findings/HP-04.md | feito |
| HP-05 | findings/HP-05.md | feito |
| HP-06 | findings/HP-06.md | feito |
| HP-07 | findings/HP-07.md | feito |
| HP-08 | findings/HP-08.md | feito |
| HP-09 | este arquivo | feito (merge) |
