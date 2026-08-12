# Visões e extratos (need-to-know)

O que cada ator **vê**. Princípio: **mínimo ao mandato** — com exceção explícita de **cargos de confiança** (ver [vision.md](./vision.md)).

## Princípio

- **Ponta / mandato estreito:** só a fatia da própria competência — sem montar organograma.
- **Cargo de confiança:** visão ampla **no domínio do cargo** (financeiro ou ops), porque o mundo real exige braço direito.
- **Admin:** irrestrito.
- Inferência estatística ao longo do tempo (ex.: Contador contar handles únicos em splits) é **risco residual aceito** de cargo de confiança — o hub não entrega catálogo/organograma de bandeja.

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

## Notas

- A estimativa é congelada na materialização inicial e **não** reflete cuts/hops até o Contador liberar.
- Acionista = extrato mínimo de beneficiário (mesma lógica estimativa → pendente).
