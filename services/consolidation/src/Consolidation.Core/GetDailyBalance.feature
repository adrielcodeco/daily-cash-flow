# language: pt
# @anchors
#   code: GTBFT
#   ref: GTBAL
#   updated_at: 2026-11-05
#   layer: feature

@GTBAL @consolidation
Funcionalidade: GetDailyBalance — consultar o saldo do dia
  Como o comerciante
  Eu quero consultar o saldo de um dia
  Para acompanhar o meu caixa

  @GTBAL-R01 @nivel-integracao @P1
  Cenário: a consulta não mostra outro comerciante
    Dado um saldo do comerciante A
    Quando o comerciante B consulta o mesmo dia
    Então o saldo devolvido é zerado

  @GTBAL-R02 @nivel-integracao @P1
  Cenário: a abertura acumula os dias anteriores
    Dado créditos de 100000 centavos no dia anterior
    Quando o comerciante consulta o dia seguinte
    Então a abertura é 100000 centavos

  @GTBAL-R03 @nivel-integracao @P1
  Cenário: a resposta informa a atualização
    Dado um evento aplicado às 14h32
    Quando o comerciante consulta o dia
    Então a resposta informa 14h32 como última atualização

  @GTBAL-R04 @nivel-integracao @P2
  Cenário: o comerciante sem movimento recebe saldo zerado
    Dado um comerciante sem eventos
    Quando o comerciante consulta o dia
    Então o saldo é zerado e não há momento de atualização
