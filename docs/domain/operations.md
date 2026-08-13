# Operações

## Ideia central

**Operação** é a fronteira principal de isolamento no **negócio**: uma frente com identidade própria, gente atribuída, regras locais de resultado — sem misturar com outras operações.

**Script** e **Store (objetos)** são entidades **desacopladas** da Operação. Não “moram dentro” dela como filhos obrigatórios. O vínculo é uma **operation key** (chave de agrupamento) em Scripts e em objetos do Store — para agrupar, filtrar e **impedir misturas** entre frentes.

## Ciclo de vida da Operação — DECIDIDO (fila #12 + endurecimento G13)

```
Rascunho ⇄ Ativa ⇄ Pausada → Encerrada   (Rascunho também pode ir direto a Encerrada = abandonado)
```

| Estado | Significado |
|--------|-------------|
| **Rascunho** | Em preparação; ainda não é frente viva |
| **Ativa** | Em operação: cobranças via API, operadores assigned, keys de script/store usáveis |
| **Pausada** | Não aceita cobrança **nova**; book-keeping do que já existe continua |
| **Encerrada** | Terminal. Só leitura / liquidação do que restar |

### Transições permitidas (e guardas)

| De → Para | Permitido? | Guarda (capacidade) |
|-----------|-----------|---------------------|
| Rascunho → Ativa | sim | `gerir_operacao` no escopo |
| Ativa → Pausada | sim | `gerir_operacao` |
| Pausada → Ativa | **sim** (retomar) | `gerir_operacao` |
| Ativa/Pausada → Encerrada | sim | `gerir_operacao` |
| Rascunho → Encerrada | sim (abandonar) | `gerir_operacao` |
| Encerrada → qualquer | **não** (terminal) | — |

Toda transição é evento de ES + entrada de log de auditoria. Só Admin ou quem tem `gerir_operacao` **no escopo daquela op** transiciona.

### Efeito de Pausada / Encerrada sobre Script, Store e assigns — DECIDIDO (G13)

| Recurso | Pausada | Encerrada |
|---------|---------|-----------|
| **Emissão de Cobrança (API)** | bloqueada (não nasce nova) | bloqueada |
| **Cobranças em voo / claims / hops** | seguem normal (book-keeping continua) | seguem até liquidar; nada novo |
| **Script (delivery à ponta)** | **para de resolver** (a frente está pausada) | para de resolver |
| **Store (CRUD/pesquisa via key)** | **read-only** | read-only |
| **Operadores assigned** | continuam assigned (vínculo preservado), mas sem trabalho novo | assign encerrado ao fim da liquidação |

## O que a Operação é (e o que não é)

| É | Não é |
|---|--------|
| Identidade de frente de negócio | Container técnico que engole Script/Store |
| Assign de Operadores (e demais locais) | O runtime que executa o monkeypatch |
| Âncora da **operation key** | Dona exclusiva do catálogo global de scripts |
| Regras locais (cut de gestão da op, assign) | Cadeia de repasse (isso é Contador) |

## Quem gestiona Operações

| Papel | Papel |
|-------|--------|
| **Admin** | Pode tudo |
| **Gestor de Operações** | Mandato para criar/configurar ciclo de vida, assign de gente e configs locais da op — **sem** ser Admin irrestrito |

Ver [actors-and-mandates.md](./actors-and-mandates.md).

## Divisão interna da Operação — DECIDIDO (G11)

**Não existe entidade "Equipe".** A divisão interna **emerge** da interseção de dois eixos de mandato (ver [actors-and-mandates.md](./actors-and-mandates.md) — Mandato: capacidades × escopo):

- **Eixo Operação:** quem tem mandato sobre a op (uma, várias ou todas) vê/gere a op inteira.
- **Eixo Carteira:** cada Recrutador vê só a **fatia dele** (seus recrutados) dentro da op.

Assim a op se compartimentaliza sozinha, sem container novo. Gestão delegável com granularidade (mandato escopado + capacidades), autoridade aninha atenuando, need-to-know preservado.

**Dinheiro da gestão local:** um mandato de gestão de op pode ter um **cut fixo** opcional sobre o resultado da op — linha no waterfall, decidida de cima, **flat** (nunca % por subordinado). Ver [money.md](./money.md).

## Script — entidade independente — DECIDIDO (fila #12)

### O que é

Artefato de ponta (JS / monkeypatch) **entregue** pelo Nexus ao runtime na borda. O Nexus **não executa** a missão no site alvo — **publica/resolve** o script para a ponta carregar.

No produto atual/refactor de delivery (visão alinhada):

- Script versionado (releases), canais (ex. prod/staging/dev), resolução por host/nome/canal.
- Delivery: a ponta pede o script; o hub devolve o código a injetar.

### Relação com Operação

- Script **não** é filho da Operação.
- Script carrega uma **operation key** (agrupamento / filtro).
- Uma Operação pode ter zero ou N scripts associados via key; um script aponta para uma key de op.

Detalhe de build, hosts XSS/MITM/extensão = fora deste doc de domínio de negócio (vive no ecossistema Monkeypatches).

## Store (object store) — desacoplado, keyed — DECIDIDO (ajuste fila #7/#12)

### Para quê existe

Base de objetos dinâmicos (CRUD + pesquisa) para a ponta **não** precisar de backend novo por frente.

Filosofia: igual ao Script — **desacoplada**. Objetos não “entram na Operação” como sub-agregado; cada objeto (e o bucket lógico) carrega **operation key**.

### Isolamento

- A key **agrupa** e **filtra**.
- Impede misturar objects de keys diferentes (leitura/escrita só no escopo da key autorizada).

### operation key × ID da Operação — DECIDIDO (G13)

- Toda Operação tem **exatamente uma** operation key canônica (relação 1:1 com o ID interno da op). A key é o identificador **estável e público-à-ponta** usado por Script e Store; o ID interno é o identificador de domínio usado internamente (ex.: amarração de Cobrança).
- Como é 1:1, "backend da frente" (Cobrança por ID + Store/Script por key) **sempre** correlaciona sem ambiguidade. A distinção key/ID é de *fronteira* (a ponta nunca vê o ID interno, só a key), não de cardinalidade.
- Uma key **nunca** é reusada por outra Operação (integridade histórica, como o handle).

### Quem usa

| Quem | Acesso |
|------|--------|
| Script na ponta (com key da op) | CRUD/pesquisa no escopo da key |
| Admin | Gestão/manutenção |
| Gestor de Operações | Manutenção no escopo das ops que gestiona (alinhado ao mandato) |

Modelo mental:

> Script (keyed) + Store API (mesma key) + Cobranças API (ID da Operação) ≈ backend da frente — sem fundir as entidades no modelo.

Store **não** mistura com Cobrança/dinheiro ([money.md](./money.md)).

## Resultado da Operação — DECIDIDO (fila #6)

**Resultado canônico = movimentação financeira rastreada** atribuível à Operação (Cobranças → materialização em **líquido X** → split → repasse).

Outros eventos do script = detalhe da ponta / Store — não segundo KPI de domínio.

## Relação com a Organização

- Regras **globais** (acionistas, etc.) → Organização.
- Regras **locais** (cut de gestão da op, assign) → Operação.
- Script/Store → entidades próprias ligadas por **operation key**.

## Estado

- Ciclo Rascunho/Ativa/Pausada/Encerrada: **decidido**.
- Script independente + delivery; Store desacoplado; ambos com **operation key**: **decidido**.
- **Gestor de Operações** nomeado: **decidido**.
- Divisão interna: **mandato (capacidade × escopo)** — **sem entidade Equipe** (G11).
