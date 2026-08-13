# Mapa do domínio Nexus (produto)

Visão única do domínio de negócio fechado nesta conversa. Detalhes e regras: docs linkados. Framing: RPG / seminário de código; software **sob medida** para **uma** organização (singleton).

## Em uma frase

Hub que **modela e gestiona** a organização (gente, operações, dinheiro, scripts/store) com **need-to-know**, sem concentrar o mapa completo em uma só cabeça — e **sem executar** a ponta nem o movimento físico do dinheiro (pós-gateway).

## Diagrama mental

```text
                    ┌──────────── Organização (singleton) ─────────┐
                    │  Acionistas (beneficiários, nível 2)         │
                    │  Regras globais / Admin                      │
                    └──────────────┬───────────────────────────────┘
                                   │
           ┌───────────────────────┼───────────────────────┐
           ▼                       ▼                       ▼
     Operação(ões)            Membros + papéis         Dinheiro (dois livros)
     ciclo de vida            need-to-know             Cobrança → intenção %
     assign operadores                                 → materialização → Claims
           │                       │                   → hops → repasse (Contador)
           │ operation key         │
           ├──────────► Script (delivery à ponta; Nexus não executa)
           └──────────► Store (objects keyed; CRUD + pesquisa)
```

## Conceitos-raiz

| Conceito | Doc |
|----------|-----|
| Visão / filosofia / natureza (singleton, sob medida) | [vision.md](./vision.md) |
| Organização, Membro, papéis, **Mandato (capacidade × escopo)**, presets, delegação | [actors-and-mandates.md](./actors-and-mandates.md) |
| Operação, Script, Store, operation key, **divisão interna** | [operations.md](./operations.md) |
| Dois livros, Cobrança, split (waterfall), materialização, Claim, hops, cut, multi-moeda, reconciliação | [money.md](./money.md) |
| O que cada um vê (need-to-know, estimativa→pendente, log) | [visibility.md](./visibility.md) |
| **Event sourcing + log de auditoria** | [auditability.md](./auditability.md) |
| Termos | [glossary.md](./glossary.md) |
| Índice + adiados + gaps resolvidos (G1–G13) | [README.md](./README.md) · [open-gaps.md](./open-gaps.md) |

## Dinheiro em dois livros (resumo)

- **Livro-mundo (Contas):** saldo fungível (número) + transações. Conta burra.
- **Ledger:** Claims (`{beneficiário, R$, origem, localização, status}`) + eventos. Toda a semântica.
- **Desconectados no modelo** (sem FK); **acoplados no use case** (caixa nunca sozinho; ledger pode sozinho).
- **Invariante:** `soma claims **ativos** == saldo` por Conta **e por moeda** (escopo-org). Divergência só vs realidade, resolvida pela porta de **reconciliação**.
- **Multi-moeda:** Conta é multi-moeda; Nexus **nunca converte** (só "X de Y"); hop pode redenominar (valor declarado).
- **Visibilidade (baixa confiança):** estimativa congelada (base materialização) → flag do Contador no hop final → **pendente/a receber** + relatório controlado; nunca valor ao vivo (vaza path).

## Fluxo de dinheiro (resumo)

1. **Geração** (API): Operação + Operador + valor → **intenção de split em %** (waterfall); Conta de Gateway de **emissão** (fixa o cut nível-1 do Laranja).
2. **Aberta → Paga** (webhook).
3. **Materialização** (fim de período, lote, 1 pagamento por vez): Contador declara **líquido X** (pós-taxa) + **Conta de aterrissagem** → `%` morre, nascem **Claims** (`soma == X`).
4. **Hops:** movem claims entre Contas (livro-mundo ± e ledger relocaliza); destino já líquido; **cut de Laranja no path** = % proporcional sobre os claims do bundle (1× por Laranja).
5. **Repasse:** hop para Conta de payout **fora do escopo-org**; claim → `repassado` (sai da soma). Não fica “marcado pago” numa Conta org.
6. **Perdas:** encolhimento no hop (taxa/FX) vs **write-off** (some sem destino, com causa). Contador **registra**; Nexus é o ledger, guia, **não executa**.

**Split (waterfall):** Laranja(s) nível 1 → Acionistas nível 2 → **Gestão da Op** (opcional) → nível 3 (Operador + Recrutador) → residual Org. Sem pool.

## Mandato & acesso (resumo)

- **Mandato = conjunto de (capacidade × escopo).** Eixos de escopo: Operação (nenhuma/algumas/todas) × Carteira (recrutados diretos). Divisão interna da op **emerge** disso — **sem "Equipe"**.
- **Presets** nomeados (papéis) + fine-tune por exceção; **Admin** = raiz. **Atenuação** (sub-mandato ⊆ concedente). **Autoridade aninha; dinheiro não.**
- **Revogação por causa:** queimado/traiu → suspende cascata; saída → re-parent.

## Auditabilidade (resumo)

- **ES** (verdade do estado) + **Log de auditoria** (accountability incl. **leituras**), acoplados por correlação.
- Log = **joia da coroa**: leitura ~só Admin + **tamper-evidence**.

## Papéis / presets (v1)

Admin · Gestor de Operações · Gateways · Contador · Recrutador · Operador · Laranja · Acionista (login read-only; não é mandato de gestão)

## Critério de completo

Ver [README.md](./README.md). Fase 1 + fase 2 + **todos os gaps críticos (G1–G13) resolvidos**; adiados listados explicitamente.
