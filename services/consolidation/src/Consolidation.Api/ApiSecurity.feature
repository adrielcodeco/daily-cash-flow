# language: pt
# @anchors
#   code: CNSCF
#   ref: CNSCP
#   updated_at: 2026-11-09
#   layer: feature

@CNSCP @consolidation
Funcionalidade: ApiSecurity — quem consulta a API do Consolidado, e quanto
  Como o comerciante
  Eu quero que só quem tem o meu token e o escopo de saldo veja o meu saldo
  Para que o meu caixa não fique exposto

  @CNSCP-R01 @nivel-unit @P1
  Cenário: o pedido sem token é recusado
    Dado um pedido sem token
    Quando ele consulta o saldo do dia ou do período
    Então a resposta é 401

  @CNSCP-R01 @nivel-unit @P1
  Cenário: o token de outra API é recusado
    Dado um token emitido para outra API
    Quando ele consulta o saldo do dia
    Então a resposta é 401

  @CNSCP-R02 @nivel-unit @P1
  Cenário: a consulta sem o escopo de saldo é recusada
    Dado um token só com "entries.read"
    Quando ele consulta o saldo do dia ou do período
    Então a resposta é 403

  @CNSCP-R02 @nivel-unit @P2
  Cenário: o escopo de saldo abre as duas consultas
    Dado um token com "balances.read"
    Quando ele consulta o saldo do dia e do período
    Então as respostas são 200

  @CNSCP-R03 @nivel-unit @P1
  Cenário: o token sem o comerciante é recusado
    Dado um token válido sem o comerciante
    Quando ele consulta o saldo
    Então a resposta é 403 e o armazenamento não é consultado

  @CNSCP-R04 @nivel-unit @P1
  Cenário: o comerciante chamado como a verificação de saúde é limitado
    Dado uma cota de 3 pedidos por segundo e um token com o comerciante "health"
    Quando ele faz pedidos seguidos
    Então um deles é recusado com 429

  @CNSCP-R04 @nivel-unit @P1
  Cenário: a cota é de cada comerciante e não alcança a verificação de saúde
    Dado uma cota de 2 pedidos por segundo
    Quando um comerciante passa da cota
    Então ele recebe 429 com o código "rate_limited", outro comerciante segue atendido e a verificação de saúde responde 200
