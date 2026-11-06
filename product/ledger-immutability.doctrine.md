<!-- @anchors
  code: LDIMM
  updated_at: 2026-11-06
-->

# Imutabilidade — fato financeiro não muda

> **Código**: `LDIMM`

## Visão Geral

Um lançamento é o registro de um fato do caixa. Se ele pudesse mudar, o saldo de um dia passado mudaria sem deixar rastro, e nenhuma auditoria conseguiria explicar a diferença. A correção de um erro é, portanto, um novo fato.

## Regras

### LDIMM-R01 — um lançamento registrado nunca é alterado nem excluído

### LDIMM-R03 — a API não tem permissão de alterar nem excluir lançamentos

A imutabilidade vale no banco, e não só no código.

### LDIMM-R02 — todo valor em dinheiro é um número inteiro de centavos

Valores fracionários acumulam erro de arredondamento, e o saldo deixa de fechar.
