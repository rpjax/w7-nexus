# Nexus — Domínio de negócio (produto)

Documentação **conceitual de produto/negócio**. Não descreve código, APIs nem arquitetura de software.

Contexto: exercício recreativo de modelagem (seminário / RPG de código). A linguagem abaixo é a do *mundo fictício* do produto.

**Status: NÚCLEO FECHADO.** Todos os gaps críticos **G1–G9 resolvidos** (ver [open-gaps.md](./open-gaps.md)). Modelo de dinheiro = **dois livros** (Livro-mundo + Ledger claim-cêntrico), reescrito em [money.md](./money.md). Pendente só: refinos adiados de propósito (equipes, ciclo de quota, integração API de hops) e polimento contínuo.

## Como usamos estes docs

1. Anotar o que já está decidido.
2. Marcar lacunas como **Pergunta aberta**.
3. Tratar **um ponto por vez** (facilitação abaixo).
4. Atualizar o doc correspondente assim que houver decisão — não deixar só no chat.

## Mapa

| Doc | Responde |
|-----|----------|
| [vision.md](./vision.md) | Por que Nexus existe, filosofia, fronteiras |
| [actors-and-mandates.md](./actors-and-mandates.md) | Quem existe no mundo, confiança, papéis |
| [operations.md](./operations.md) | O que é uma Operação e o que ela isola |
| [money.md](./money.md) | Cobrança, split (direitos), cadeia de repasse |
| [visibility.md](./visibility.md) | O que cada ator vê (extratos / need-to-know) |
| [domain-map.md](./domain-map.md) | Mapa-resumo do domínio completo |
| [glossary.md](./glossary.md) | Termos canônicos (evitar sinônimos soltos) |
| [open-gaps.md](./open-gaps.md) | Gaps críticos (G1–G10) — histórico de decisões; núcleo fechado |
| [../implementation/](../implementation/) | **Processo de implementação** — etapas, ordem, DoD (código) |

## Critério de “domínio completo” (esta conversa)

O domínio de produto está **completo** quando existir, nos docs:

1. Visão / problema / fronteiras
2. Mapa de conceitos e glossário estável
3. Atores, mandatos e beneficiários (pendências explícitas ok)
4. Operação + Store + resultado
5. Dinheiro ponta a ponta: Cobrança → Paga → **direitos (split)** → **cadeia de repasse** → fatias pagas; quem vê o quê
6. Lista do que está **de propósito fora** da v1 de domínio (adiado, não esquecido)

Código e UX fina **não** fazem parte deste critério.

## Fila de decisão (condução)

### Fase 1 — núcleo (fechada)

| # | Ponto | Status | Doc |
|---|--------|--------|-----|
| 1 | Ordem dos cortes no dinheiro | **decidido** | money |
| 2 | Dono vs Admin vs Acionista | **decidido** | actors / money |
| 3 | Laranja — para que existe | **decidido** | actors / money |
| 4 | Identidade mínima do Operador | **decidido** | actors |
| 5 | Recrutador: regras de overlap e visão | **decidido** | actors / money |
| 6 | Resultado canônico da Operação (dinheiro) | **decidido** | operations |
| 7 | Store/dados dinâmicos da Operação (recurso) | **decidido** | operations |
| 8 | Papéis iniciais de staff | **decidido** — Gateways + Contador + Gestor de Operações | actors |

### Fase 2 — fechar o domínio inteiro

| # | Ponto | Status | Doc |
|---|--------|--------|-----|
| 9 | Cobrança: geração, amarração, Aberta→Paga, Conta de Gateway (2 pontos de vista) | **decidido** | money |
| 9b | Cadeia de repasse (materialização, hops, quotas, dois livros) | **decidido** — G1/G2/G6/G8/G9 | money |
| 10 | Rateio nível 3 (sem pool; waterfall; deal autor único) | **decidido** — G5 | money |
| 11 | Visões de extrato (need-to-know + ajustes de rota) | **decidido** | visibility |
| 12 | Ciclo Operação + Script/Store desacoplados (operation key) | **decidido** | operations |
| 13 | Adiados explícitos da v1 | **decidido** | README |
| 14 | Mapa-resumo do domínio completo | **decidido** | domain-map.md |
| G4 | Attrition (estados Laranja/Conta, claim preso, perda mecânica, causa) | **decidido** | actors / money |
| G7 | Organização singleton (software sob medida) | **decidido** | vision |

## Adiados explícitos da v1 de domínio (fila #13)

Itens **conhecidos e de propósito fora** do fechamento atual — não esquecidos:

| Item | Por quê adiado |
|------|----------------|
| **Equipes** / **Líder de equipe** | Assignment de Laranja “por equipe” ficou incerto; Operação basta na v1 |
| Override de % Recrutador/Operador **por Operação** | Vínculo global resolve; override só se doer |
| Árvore de sub-recrutadores | Rejeitada (pirâmide) |
| Ordem de split configurável | Rejeitada (invariante) |
| Portal rico de Acionista / ver outros sócios | Extrato mínimo; default não vê pares |
| Integração API automática de hops/saque | v1 = book-keeping manual pelo Contador |
| SQL/analytics genérico no Store | Object store keyed + CRUD/pesquisa |
| Chat / rede social interna | Fora da promessa |
| Detalhe fino de canais/releases de Script | Existe no ecossistema de delivery; domínio só exige entidade Script + key + delivery à ponta |

Quando um item acima doer de verdade, volta para a fila de decisão — não inventar agora.

- Nexus é o hub centralizador de gestão da organização (a “quadrilha” no RPG).
- Filosofia: desacoplamento + mínimo privilégio / need-to-know.
- Duas classes de confiança: ponta (Operadores) vs mandato interno (staff).
- “Internal staff” **não** é um papel — é conceito de confiança; papéis revelam o sistema.
- Operação é fronteira de isolamento (identidade, gente, lucro local, dados do script).
- Dinheiro: cobranças com ciclo de vida rastreável + divisão configurável em **quem/%**.
- Ordem de split **fixa (waterfall)**: (1) Laranja(s) nível 1 → (2) Acionistas → (3) nível 3 (Operador + Recrutador + residual Org). Aplicada sobre o **líquido X** na materialização; sem pool.
- **Software sob medida, singleton:** um Nexus = uma organização (a websete/Grupo Thal); não é produto de prateleira / multi-tenant.
- Recrutamento: só direto (sem árvore); competência de recrutar **concedida**; vínculo **global** na Org + assign do Operador às ops separado; visão só da própria carteira.
- Acionistas: beneficiários (não role) com participação global no nível 2; Admin configura a lista.
- Dono: fora do produto (infra/código/deploy). Admin: papel irrestrito no hub. Need-to-know não se aplica ao Admin.
- Operador: identidade mínima = ID + handle único (login) + payout + vínculo com Recrutador; sem nome civil.
- Resultado da Operação = movimentação financeira rastreada atribuível a ela (base do split); sem segundo KPI de domínio.
- **Script** e **Store** são entidades **desacopladas**, ligadas à Operação só por **operation key** (agrupa/filtra; impede mistura). Script: delivery à ponta; Nexus não executa.
- Operação: ciclo Rascunho→Ativa→Pausada→Encerrada; gestão por Admin + **Gestor de Operações**.
- Cobrança: API (op+operador+valor) ou painel fallback; **Conta de Gateway de emissão** (fixa cut nível-1 do Laranja); Aberta→Paga (webhook).
- **Dois livros** (G9): Livro-mundo (Contas fungíveis: número + transações) + Ledger (Claims). Desconectados no modelo, acoplados no use case. **Invariante:** `soma claims == saldo` (escopo-org); divergência só vs realidade, via **reconciliação**.
- Materialização: fim de período, em lote, 1 pagamento por vez; declara **líquido X** (pós-taxa) + **Conta de aterrissagem**; % morre → nascem **Claims** (soma == X).
- Após materialização: hops relocalizam claims (cut mid-path **proporcional** sobre o bundle); **quotas** de Conta; extrato com **ajustes de rota**. Perdas: encolhimento no hop vs **write-off** (com causa).
- Attrition (G4): estados de Conta em **dois eixos** (emissão/saldo), estados de Laranja; claim preso = exposição; perda **mecânica**; causa categorizada p/ intel.
- **Multi-moeda (G10):** Nexus **nunca converte** (só "X de Y"); Conta é **multi-moeda**; invariante `soma claims == saldo` **por moeda**; hop pode redenominar (valor declarado pelo Contador).
- **Visibilidade do beneficiário (G10):** vê **estimativa congelada** (base = materialização inicial), **sem** update ao vivo (que vazaria o path); Contador marca **visível** no hop final → vira **pendente/a receber** + **relatório controlado** (nunca hop-by-hop).
- **Contador** + **Admin** registram fluxo financeiro; Contador usa quotas para rotas viáveis. Nexus é o ledger, **não executa**.
- Staff: **Gateways** + **Contador** + **Gestor de Operações**. Líder de equipe: adiado.
- Nível 3: **sem pool** — base calculada; `operador_pct` + `recrutador_pct` ≤ 100% (deal de agenciamento, autor único); resto = residual da Org.
- Visões: matriz need-to-know em [visibility.md](./visibility.md) (Acionista não vê outros sócios; Operador vê a própria Operação nas cobranças dele).
