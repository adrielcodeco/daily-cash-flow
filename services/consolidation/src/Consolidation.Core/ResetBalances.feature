# language: pt
# @anchors
#   code: RSBFT
#   ref: RSBAL
#   updated_at: 2026-11-05
#   layer: feature

@RSBAL @consolidation
Funcionalidade: ResetBalances — refazer o consolidado
  Como a operação
  Eu quero limpar o consolidado
  Para refazer o saldo a partir dos lançamentos

  @RSBAL-R01 @nivel-integracao @P1
  Cenário: a limpeza apaga o saldo
    Dado um saldo consolidado
    Quando o consolidado é limpo
    Então o saldo do dia é zerado

  @RSBAL-R02 @nivel-integracao @P1
  Cenário: os eventos reenviados recompõem o saldo
    Dado um saldo consolidado e limpo depois
    Quando os mesmos eventos são aplicados de novo
    Então o saldo volta ao valor original
