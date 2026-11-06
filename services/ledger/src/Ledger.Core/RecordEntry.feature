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
    Então o evento usa a identidade do lançamento e repete os dados dele

  @RCENT-R04 @nivel-unit @P1
  Cenário: a chave repetida devolve o lançamento original
    Dado um crédito já registrado com uma chave
    Quando o mesmo crédito é registrado com a mesma chave
    Então o lançamento original é devolvido
    E o armazenamento não recebe nada novo

  @RCENT-R05 @nivel-unit @P1
  Cenário: a chave reutilizada com outro conteúdo é recusada
    Dado um crédito já registrado com uma chave
    Quando outro valor é registrado com a mesma chave
    Então o lançamento é recusado com o código "idempotency_key_reused"

  @RCENT-R06 @nivel-unit @P1
  Cenário: a chamada simultânea recebe o lançamento vencedor
    Dado que outra chamada gravou a mesma chave durante o registro
    Quando o armazenamento informa a chave já usada
    Então o lançamento vencedor é devolvido
