# language: pt
# @anchors
#   code: RVEFT
#   ref: RVENT
#   updated_at: 2026-11-05
#   layer: feature

@RVENT @ledger
Funcionalidade: ReverseEntry — corrigir sem apagar
  Como o comerciante
  Eu quero estornar um lançamento errado
  Para corrigir o caixa mantendo o histórico

  @RVENT-R01 @nivel-unit @P1
  Cenário: o lançamento de outro comerciante não é encontrado
    Dado um lançamento de outro comerciante
    Quando o estorno é pedido
    Então o pedido é recusado com o código "entry_not_found"

  @RVENT-R02 @nivel-unit @P1
  Cenário: o segundo estorno é recusado
    Dado um lançamento já estornado
    Quando o estorno é pedido com outra chave
    Então o pedido é recusado com o código "already_reversed"

  @RVENT-R03 @nivel-unit @P1
  Cenário: o estorno é gravado com o evento
    Dado um crédito de 35000 centavos
    Quando o estorno é pedido
    Então o armazenamento recebe o estorno e o evento na mesma operação

  @RVENT-R04 @nivel-unit @P1
  Cenário: a chave repetida devolve o estorno original
    Dado um estorno registrado com uma chave
    Quando o estorno é pedido com a mesma chave
    Então o estorno original é devolvido
