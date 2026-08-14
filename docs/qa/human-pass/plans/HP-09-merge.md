---
name: HP-09 Merge conclusões
overview: Depois dos oito findings, unificar em conclusions.md. Não navega o site de novo salvo para confirmar duplicata.
isProject: false
---

# HP-09 — Merge (só depois de HP-01…08)

Não corre em paralelo com os oito. Espera os arquivos `docs/qa/human-pass/findings/HP-0N.md` com checklist marcado.

## Fazer

1. Ler os oito findings.
2. Deduplicar pelo par (rota + controle + dor).
3. Escrever [`docs/qa/human-pass/conclusions.md`](../conclusions.md): resumo 10, cobertura sim/não, P0–P3, síntese copy, síntese UX.
4. Atualizar a tabela Origem (status feito).
5. Não inventar item que ninguém viu no browser.

## Não fazer

Não “completar” checklist de outro agente. Não abrir PRs de código neste passo.
