# Human pass — QA coordenado no browser

Objetivo: **vários agentes em paralelo** usam o Nexus **como humano**, no **navegador do Cursor**, cada um com uma qualidade/persona diferente. A união dos passes cobre **todas as rotas e ações visíveis**. As conclusões convergem num único arquivo.

Não é teste de API, não é leitura de código como evidência. Código só para confirmar um rótulo ou rota depois de ver a UI. Evidência = o que aconteceu no browser.

## Alvo

| | |
|--|--|
| URL | `https://nexus.websete.localhost:9143` |
| Login seed | `admin` / `adminadmin` |
| API | mesmo origin (`/api/...`). Não abrir `api.nexus.websete.localhost` (certificado separado). |
| Certificado | self-signed no host do front — aceitar se o browser pedir |

## Arquivos

| Arquivo | Dono |
|---------|------|
| [conclusions.md](./conclusions.md) | **único** destino final (merge) |
| [findings/HP-01.md](./findings/HP-01.md) … [HP-08.md](./findings/HP-08.md) | rascunho **exclusivo** de cada agente |
| [plans/HP-01-*.md](./plans/HP-01-novato-identidade.md) … | plano de execução do agente |
| Este README | contrato; agentes **não** reescrevem |

## Qualidades (personas)

Cada agente **entra como Admin** (único login estável no seed), mas **interpreta a UI** na qualidade abaixo. Se criar contas/mandatos, usa o prefixo do plano para não colidir.

| ID | Qualidade | Olho |
|----|-----------|------|
| HP-01 | Novato / identidade | “Entendi o que é isso? Consigo entrar, me achar, ver meu dinheiro?” |
| HP-02 | Recrutador | “Consigo cuidar da minha gente sem jargão de ledger?” |
| HP-03 | Guarda de identidades | “Conceder poder e desligar gente é seguro e claro?” |
| HP-04 | Gestor de operações | “Dá para montar uma frente de trabalho sem se perder?” |
| HP-05 | Quem emite cobrança | “Do pedido de pagamento até ‘está pago’ faz sentido?” |
| HP-06 | Contador (ledger) | “Claims, hops, revelar, estorno — o vocabulário e o fluxo batem com o domínio?” |
| HP-07 | Gateways / livro-mundo | “Conta real, saldo, congelar, perder, reconciliar — dá para operar sem medo?” |
| HP-08 | Admin transversal | Chrome, nav, vazio, erro, acionistas, mobile, consistência entre telas |

## Matriz de cobertura (obrigatória)

Nada desta tabela pode ficar sem passe **clicado** no browser. Itens “também” = o dono anota chrome daquela tela; HP-08 faz o passe transversal.

| Superfície | Dono | Também |
|------------|------|--------|
| `/auth` entrar, validação, bootstrap admin (olhar, **não** criar admin extra se não precisar) | HP-01 | HP-08 copy |
| `/dashboard` Início, atalhos, empty/erro | HP-01 | HP-08 |
| `/dashboard/profile` usuário + senha | HP-01 | |
| `/dashboard/statement` lista, empty, copy | HP-01 | HP-06 vocabulário |
| `/dashboard/carteira` | HP-02 | |
| `/dashboard/deals` | HP-02 | |
| `/dashboard/accounts` busca, criar, presets, capacidades, attrition, senha | HP-03 | |
| `/dashboard/operations` CRUD, status, cut, operadores, trilhos, script, store | HP-04 | |
| `/dashboard/charges` lista, gerar, paga, materializar, estorno, status | HP-05 | |
| `/dashboard/claims` lista, hop, repasse, revelar, estorno, ficha | HP-06 | |
| `/dashboard/world-accounts` abrir, observar, quota, freeze, emissão, lost, reconcile, exposição | HP-07 | |
| `/dashboard/shareholders` | HP-08 | |
| Shell: grupos Eu/Dinheiro/Pessoas, Sair, mobile menu, avatar | HP-08 | todos anotam se travar |
| Toasts / erros / diálogos de confirmação | dono da tela | HP-08 consistência |
| Rotas bloqueadas / 404 interno (`*` → dashboard) | HP-08 | HP-01 redirect pós-login |

## Protocolo no browser

1. Abrir **só** o URL do front. Hard refresh se o JS for bundle velho.
2. Agir como humano: ler títulos, subtítulos, placeholders, empty states, botões, confirmações, toasts. **Não** pular copy.
3. Tentar o caminho feliz **e** o caminho burro (campo vazio, cancelar diálogo, filtro que zera a lista).
4. Não desabilitar o último Admin. Não marcar a conta seed como perdida.
5. Dados de teste: prefixo do plano (`qa01-`, `qa02-`, …). Anotar IDs criados no findings.
6. Se uma ação exigir pré-requisito de outro agente (ex. cobrança precisa de operação), **criar o mínimo** na própria sessão em vez de esperar. Não editar o que outro agente acabou de marcar lost/repassado se o ID não for seu.

## Três lentes (todo finding)

Cada item no findings deve responder, quando couber:

1. **Bug / gap / polish** — quebrou, falta, ou só está cru?
2. **Copy** — fácil de entender? jargão humano? jargão de domínio (ver `docs/domain/glossary.md`)? misturou “Conta” de login com Conta do livro-mundo?
3. **UI/UX** — fácil de usar ou o humano se bate? dá para ser mais conveniente e didático?

## Formato de finding (obrigatório)

```md
### [HP-0N-###] título curto
- **Lentes:** bug | gap | polish | copy | ux (uma ou mais)
- **Onde:** rota + controle (ex. `/dashboard/claims` → botão Revelar)
- **O que fiz:** passos humanos
- **O que vi:** fato (texto na tela, toast, URL)
- **Por que dói:** pergunta das lentes
- **Sugestão:** uma frase acionável
- **Severidade:** P0 bloqueia uso · P1 caminho principal · P2 atrito · P3 nit
```

## Merge → conclusions.md

Agentes **não** editam a seção de outro. Escrevem só o próprio `findings/HP-0N.md`.

Depois que **os oito** findings existirem (mesmo que “nada encontrado” com checklist cumprido), **um** agente de merge (ou o operador humano) copia para [conclusions.md](./conclusions.md):

1. Deduplicar (mesmo controle, mesmo texto → um item, citar IDs origem).
2. Ordenar por severidade, depois por rota.
3. Preencher o checklist de cobertura (sim/não por linha da matriz).
4. Resumo executivo: as 10 dores que mais melhoram a vida do usuário.

Não inventar finding na hora do merge.

## Fora de escopo deste passe

- Consertar o código (salvo o operador pedir na sequência).
- Testar `api.*` direto, Postman, testes .NET.
- Redesenhar o domínio; só apontar se a UI **trai** o glossário.
