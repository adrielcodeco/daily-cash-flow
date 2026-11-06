# language: pt
# @anchors
#   code: LEDFT
#   ref: LEDEN
#   updated_at: 2026-11-06
#   layer: feature

@LEDEN @ledger
Funcionalidade: LedgerEntry — o que é um lançamento válido
  Como o serviço de lançamentos
  Eu quero recusar movimentos inválidos
  Para que o caixa guarde apenas fatos corretos

  @LEDEN-R01 @nivel-unit @P1
  Cenário: o lançamento sem comerciante é recusado
    Dado um movimento sem comerciante
    Quando o lançamento é criado
    Então o lançamento é recusado com o código "merchant_required"

  @LEDEN-R02 @nivel-unit @P1
  Cenário: o lançamento com valor zero é recusado
    Dado um movimento de 0 centavos
    Quando o lançamento é criado
    Então o lançamento é recusado com o código "invalid_amount"

  @LEDEN-R03 @nivel-unit @P1
  Cenário: a data do dia seguinte é recusada
    Dado que são 12h de 05/11/2026 em São Paulo
    E um movimento com data de 06/11/2026
    Quando o lançamento é criado
    Então o lançamento é recusado com o código "future_accounting_date"

  @LEDEN-R03 @nivel-unit @P1
  Cenário: a data válida segue o fuso de São Paulo
    Dado que são 23h de 05/11/2026 em São Paulo, já 06/11/2026 em UTC
    E um movimento com data de 06/11/2026
    Quando o lançamento é criado
    Então o lançamento é recusado com o código "future_accounting_date"

  @LEDEN-R04 @nivel-unit @P2
  Cenário: a descrição longa é recusada
    Dado um movimento com descrição de 201 caracteres
    Quando o lançamento é criado
    Então o lançamento é recusado com o código "description_too_long"

  @LEDEN-R05 @nivel-unit @P1
  Cenário: o lançamento válido nasce com identidade
    Dado um crédito de 35000 centavos com data de hoje
    Quando o lançamento é criado
    Então o lançamento tem identidade e momento de criação

  @LEDEN-R06 @nivel-unit @P1
  Cenário: o estorno inverte o tipo e mantém valor e data
    Dado um crédito de 35000 centavos
    Quando o estorno é criado
    Então o estorno é um débito de 35000 centavos na mesma data
    E o estorno aponta para o lançamento original

  @LEDEN-R07 @nivel-unit @P1
  Cenário: o estorno não pode ser estornado
    Dado um estorno já registrado
    Quando o estorno dele é criado
    Então o lançamento é recusado com o código "reversal_not_allowed"

  @LEDEN-R08 @nivel-unit @P1
  Cenário: o valor acima de R$ 1 bilhão é recusado
    Dado um movimento de 100000000001 centavos
    Quando o lançamento é criado
    Então o lançamento é recusado com o código "amount_too_large"

  @LEDEN-R08 @nivel-unit @P1
  Cenário: o valor de exatamente R$ 1 bilhão é aceito
    Dado um movimento de 100000000000 centavos
    Quando o lançamento é criado
    Então o lançamento tem identidade e momento de criação

  @LEDEN-R09 @nivel-unit @P1
  Cenário: o tipo ausente é recusado
    Dado um movimento sem tipo
    Quando o lançamento é criado
    Então o lançamento é recusado com o código "invalid_entry_type"

  @LEDEN-R09 @nivel-unit @P1
  Cenário: o tipo desconhecido é recusado
    Dado um movimento com o tipo 7
    Quando o lançamento é criado
    Então o lançamento é recusado com o código "invalid_entry_type"

  @LEDEN-R09 @nivel-unit @P1
  Cenário: o tipo em número não é lido
    Dado o tipo enviado como o número 1
    Quando o tipo é lido
    Então a leitura falha

  @LEDEN-R10 @nivel-integracao @P1
  Cenário: a API não altera nem exclui lançamentos
    Dado um lançamento registrado
    Quando o papel da API tenta alterar ou excluir o lançamento
    Então o banco recusa a operação
