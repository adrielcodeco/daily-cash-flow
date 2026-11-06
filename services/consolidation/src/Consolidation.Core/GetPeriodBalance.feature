# language: pt
# @anchors
#   code: GTPFT
#   ref: GTPRD
#   updated_at: 2026-11-05
#   layer: feature

@GTPRD @consolidation
Funcionalidade: GetPeriodBalance — a evolução do caixa
  Como o comerciante
  Eu quero ver o saldo de vários dias
  Para acompanhar o meu caixa

  @GTPRD-R01 @nivel-unit @P1
  Cenário: o início depois do fim é recusado
    Dado um período com início depois do fim
    Quando o período é consultado
    Então a consulta é recusada com o código "invalid_period"

  @GTPRD-R02 @nivel-unit @P1
  Cenário: o período longo é recusado
    Dado um período de 367 dias
    Quando o período é consultado
    Então a consulta é recusada com o código "period_too_long"

  @GTPRD-R03 @nivel-unit @P1
  Cenário: o dia sem movimento aparece
    Dado movimento apenas no primeiro de três dias
    Quando o período é consultado
    Então a resposta traz os três dias

  @GTPRD-R04 @nivel-unit @P1
  Cenário: o fechamento vira a abertura seguinte
    Dado um crédito de 100 centavos no primeiro dia
    Quando o período é consultado
    Então a abertura do segundo dia é 100 centavos

  @GTPRD-R05 @nivel-unit @P1
  Cenário: a consulta usa o comerciante da requisição
    Dado um comerciante
    Quando o período é consultado
    Então o armazenamento recebe esse comerciante
