<!-- @anchors
  code: WRIND
  updated_at: 2026-11-05
-->

# Independência da escrita — nada fora de Lançamentos impede um registro

> **Código**: `WRIND`

## Visão Geral

Registrar o movimento do caixa é a operação mais crítica do sistema. O desafio exige que ela continue disponível quando o consolidado cair. A regra também vale para a mensageria, porque um registro que depende do broker herdaria a queda dele.

## Regras

### WRIND-R01 — registrar um lançamento depende apenas do banco de lançamentos

### WRIND-R02 — todo lançamento confirmado tem seu evento gravado na mesma transação

### WRIND-R03 — a assinatura do consolidado existe antes de qualquer publicação

A mensageria descarta o que chega a um tópico sem assinatura, então um evento publicado antes dela seria perdido.
