# language: pt
# @anchors
#   code: DYBFT
#   ref: DYBAL
#   updated_at: 2026-11-05
#   layer: feature

@DYBAL @consolidation
Funcionalidade: DailyBalance — como o saldo fecha
  Como o comerciante
  Eu quero um saldo de fechamento correto
  Para decidir sobre o caixa

  @DYBAL-R01 @nivel-unit @P1
  Cenário: o fechamento soma créditos e subtrai débitos
    Dado uma abertura de 100000 centavos
    E créditos de 35000 centavos e débitos de 12000 centavos
    Quando o saldo é calculado
    Então o fechamento é 123000 centavos

  @DYBAL-R02 @nivel-unit @P1
  Cenário: o dia sem movimento repete a abertura
    Dado uma abertura de 100000 centavos
    E nenhum movimento no dia
    Quando o saldo é calculado
    Então o fechamento é 100000 centavos
