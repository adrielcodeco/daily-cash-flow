<!-- @anchors
  code: MRISO
  updated_at: 2026-11-05
-->

# Isolamento — o dado de um comerciante nunca alcança outro

> **Código**: `MRISO`

## Visão Geral

O fluxo de caixa revela a saúde financeira de um negócio. Mostrar o dado de um comerciante a outro é um incidente de segurança, e a regra vale para escrita, leitura e eventos.

## Regras

### MRISO-R01 — todo registro carrega o comerciante dono

### MRISO-R02 — toda consulta é limitada ao comerciante da requisição

Um registro de outro comerciante é tratado como inexistente.
