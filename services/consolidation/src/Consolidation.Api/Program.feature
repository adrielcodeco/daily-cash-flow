# language: pt
# @anchors
#   code: CNHFT
#   ref: CNHTP
#   updated_at: 2026-11-09
#   layer: feature

@CNHTP @consolidation
Funcionalidade: Program — as consultas HTTP de saldo
  Como o comerciante
  Eu quero consultar só o meu saldo, com erros num formato único
  Para conferir o meu caixa e tratar as recusas de um só jeito

  @CNHTP-R01 @nivel-unit @P1
  Cenário: o comerciante que não pode ser identificado é recusado
    Dado um token com o comerciante "00000000-0000-0000-0000-000000000000"
    Quando ele consulta o saldo do dia ou do período
    Então a resposta é 401 e o armazenamento não é consultado

  @CNHTP-R02 @nivel-unit @P1
  Cenário: o saldo é do comerciante do token
    Dado um crédito de 1500 centavos de um comerciante na data
    Quando ele e outro comerciante consultam o saldo da data
    Então o primeiro vê 1500 centavos de crédito e o segundo vê zero

  @CNHTP-R03 @nivel-unit @P2
  Cenário: a recusa da consulta vem no formato de problema
    Dado um período invertido e um período de 401 dias
    Quando eles são consultados
    Então as respostas são 400 no formato de problema, com os códigos "invalid_period" e "period_too_long"

  @CNHTP-R04 @nivel-unit @P2
  Cenário: a recusa sem corpo próprio vem no formato de problema
    Dado uma data malformada, uma data ausente, um pedido sem token e um sem escopo
    Quando eles chegam à API
    Então as respostas são 400, 400, 401 e 403 no formato de problema
