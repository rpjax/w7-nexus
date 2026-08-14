---
name: HP-03 Identidades mandatos
overview: Passe humano na tela Contas — poder, presets, attrition. Findings só HP-03.md.
isProject: false
---

# HP-03 — Identidades e mandatos

Contrato: [`README.md`](../../README.md). Output: [`findings/HP-03.md`](../findings/HP-03.md). Prefix: `qa03-`.

## Qualidade

Você é quem abre e fecha o portão. Medo: **tirar o poder da pessoa errada** ou **não entender Admin vs preset vs capacidade**. Copy: “Contas” parece banco?

## Browser

URL do front, `admin` / `adminadmin`.

## Script (`/dashboard/accounts`)

1. Ler header, filtros, tabela vazia vs cheia.
2. Buscar `admin`; filtrar status/papel se existir. Paginar se houver.
3. Criar `qa03-membro` (senha ≥ 8). Validar senha curta.
4. Abrir o detalhe: presets, capacidades, attrition, reset senha — a hierarquia visual é óbvia?
5. Conceder um preset **não-Admin** na conta nova. Revogar.
6. Capacidade specific (operações): sem operação selecionada; com IDs manuais se a lista de ops falhar.
7. Reset senha da conta `qa03-*` (não do seed).
8. Attrition na conta de teste: causas em PT? “sinalização” o humano entende?
9. Desabilitar `qa03-*` e reabilitar. **Nunca** desabilitar `admin`.

## Lentes

- Username vs usuário vs ID.
- Preset Admin na UI vs papel Administrator.
- Confirmações destrutivas: dá para clicar sem ler?

## Não fazer

Livro-mundo, claims, shareholders.
