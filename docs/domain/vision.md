# Visão do produto

## Framing

Este domínio descreve um **produto fictício** de um seminário / RPG de código: um hub de gestão de uma organização ilícita simulada. Serve para exercitar modelagem de produto, acesso e dinheiro — não é documentação de operação real.

## Natureza do produto — DECIDIDO (G7)

Nexus é **software sob medida** para **uma única Organização** — não é produto de prateleira, SaaS nem multi-tenant. **Um Nexus = uma organização** (singleton por deploy). "Global" sempre significa global àquela org; username único é único no deploy. Um eventual segundo "capítulo" seria **outro deploy** — melhor isolamento (raio de explosão menor), coerente com a blindagem. `tenant_id` e abstrações de multi-org são **anti-requisito** (YAGNI + segurança).

## Problema

Coordenar pessoas, missões e dinheiro de uma organização sem concentrar, em uma só cabeça, o mapa completo de quem é quem, quem ganha o quê e o que cada frente faz.

Hoje (no mundo do RPG), sem um hub assim, a gestão ou fica na memória de poucos (ponto único de falha e de vazamento) ou se fragmenta sem rastreio de dinheiro e de mandatos.

## Promessa

Nexus é o **ponto centralizador de gestão** da organização: operações, gente sob mandato, cobranças e divisão de resultado — com a regra de ouro de que **cada um só acessa o mínimo necessário ao seu mandato**, e o sistema **se revela conforme os papéis** são atribuídos.

Em uma frase:

> Consigo conduzir a organização pelo hub sem que a *ponta* e os mandatos localizados montem o organograma — aceitando que **cargos de confiança** (Contador, Gestor de Operações, Admin) veem mais, de propósito, porque o mundo real exige braços direitos.

## Filosofia

1. **Desacoplamento** — frentes (operações, finanças, recrutamento) não exigem que a mesma pessoa saiba de tudo.
2. **Mínimo privilégio** — visão e ação só com mandato explícito.
3. **Need-to-know (default)** — laterais e superiores da cadeia de recrutamento ficam opacos por padrão para ponta e mandatos estreitos.
4. **Cargos de confiança** — alguns mandatos (Contador, Gestor de Operações, Admin) **ampliam** visão de propósito. Não dá para ter braço direito financeiro/operacional sem isso. Need-to-know **não** é “ninguém de confiança sabe nada”; é “a ponta e o lateral não montam o mapa”.
5. **Revelação progressiva** — capacidades do produto aparecem quando o papel é concedido.
6. **Gestão fina de acesso** — preferir mandatos localizados a papéis genéricos gordos; cargos de confiança são poucos e explícitos.
7. **Admin ≠ Gestor ≠ Contador** — Admin é irrestrito no hub; Gestor e Contador são confiança **com escopo** (ops / financeiro), não deus mode.
8. **Modelar a realidade, não forçar o fluxo** — pós-gateway: hub registra e guia; Contador move no mundo e synca.

### Hierarquia prática (mundo real)

| Camada | Exemplos | Visão |
|--------|----------|--------|
| **Ponta / estreito** | Operador, Laranja, Recrutador, Gateways, Acionista | Só a fatia da competência |
| **Confiança com escopo** | Contador (financeiro), Gestor de Operações (ops sob mandato) | Ampla no *seu* domínio; não é Admin |
| **Irrestrito** | Admin | Tudo |

Risco residual aceito: quem vê muito financeiro (Contador) **pode inferir** volumes/contagens ao longo do tempo. O produto não promete impedir inferência estatística — promete não entregar organograma/recrutamento/catálogo operacional de bandeja.

## O que Nexus é

- Hub de gestão da organização (visão do “dono” / admin conforme mandato).
- Isolador de **Operações** (cada uma com identidade e fronteira próprias).
- Registro de **movimentos financeiros** com ciclo de vida definido.
- Mecanismo de **participações e cortes** (global e local), incluindo agenciamento por recrutadores.
- Base de mandato: quem pode ver e fazer o quê.
- Por Operação (negócio): identidade, gente, resultado financeiro.
- **Script** e **Store**: entidades desacopladas, agrupadas por **operation key** (ponta + base de objetos sem backend novo por frente).

## O que Nexus não é (fronteira)

Decisões iniciais — ajustar se o RPG pedir o contrário:

- Não é a ferramenta que *executa* a missão no site alvo (isso vive na ponta / script); Nexus **gestiona**, **entrega** scripts e oferece Store keyed.
- Não funde Script/Store dentro da Operação — só **operation key**.
- Não é um DBMS/analytics genérico de mercado — o Store é object store **keyed** por operação.
- Não pretende ser rede social interna nem chat geral da organização.
- Não exige que o “dono” conheça a identidade civil da ponta (Operadores).
- Não promete criptografia social milagrosa: a blindagem é **regra de visibilidade e mandato**, não “ninguém jamais consegue inferir nada”.

## Classes de confiança (não são papéis)

| Classe | Ideia | Exemplos no RPG |
|--------|--------|-----------------|
| **Ponta** | Baixa confiança depositada no hub; identidade mínima | Operadores |
| **Mandato interno** | Confiança localizada numa competência | Contador, Admin, Recrutador, Gateways, etc. |

“Internal staff” na conversa = pessoas com **mandato interno**. Não é um role do sistema.

## Estado

- Visão e filosofia: **decididas o suficiente para seguir**.
- Fronteiras finas (chat, execução de script, etc.): podem ser refinadas depois sem bloquear o núcleo.
