# language: pt
# @anchors
#   code: LDHFT
#   ref: LDHTP
#   updated_at: 2026-11-09
#   layer: feature

@LDHTP @ledger
Funcionalidade: Program — as respostas de erro da API de Lançamentos
  Como quem integra com a API
  Eu quero que toda recusa venha no mesmo formato
  Para tratar os erros de um único jeito

  @LDHTP-R01 @nivel-unit @P2
  Cenário: a recusa de domínio vem no formato de problema
    Dado um lançamento com data futura e uma lista com limite 500
    Quando eles chegam à API
    Então as respostas são 422 e 400 no formato de problema, com os códigos "future_accounting_date" e "invalid_limit"

  @LDHTP-R02 @nivel-unit @P2
  Cenário: a recusa sem corpo próprio vem no formato de problema
    Dado um pedido malformado, um sem token e um sem escopo
    Quando eles chegam à API
    Então as respostas são 400, 401 e 403 no formato de problema
