# language: pt
# @anchors
#   code: RCEFT
#   ref: RCENT
#   updated_at: 2026-11-05
#   layer: feature

@RCENT @ledger
Funcionalidade: RecordEntry — registrar o fato e o aviso juntos
  Como o serviço de lançamentos
  Eu quero gravar o lançamento e o evento na mesma operação
  Para que nenhum lançamento confirmado fique sem aviso

  @RCENT-R01 @nivel-unit @P1
  Cenário: o lançamento válido é entregue com o evento
    Dado um crédito válido de 35000 centavos
    Quando o lançamento é registrado
    Então o armazenamento recebe o lançamento e o evento na mesma operação

  @RCENT-R01 @nivel-unit @P1
  Cenário: o lançamento inválido não chega ao armazenamento
    Dado um movimento de 0 centavos
    Quando o lançamento é registrado
    Então o armazenamento não recebe nada

  @RCENT-R02 @nivel-unit @P1
  Cenário: o registro usa apenas o armazenamento
    Dado um crédito válido de 35000 centavos
    Quando o lançamento é registrado
    Então nenhuma outra dependência é chamada

  @RCENT-R03 @nivel-unit @P1
  Cenário: o evento repete os dados do lançamento
    Dado um crédito válido de 35000 centavos
    Quando o lançamento é registrado
    Então o evento tem identidade própria e os dados do lançamento
