# language: pt
# @anchors
#   code: LDSCF
#   ref: LDSCP
#   updated_at: 2026-11-09
#   layer: feature

@LDSCP @ledger
Funcionalidade: ApiSecurity — quem chama a API de Lançamentos, e quanto
  Como o comerciante
  Eu quero que só quem tem o meu token e o escopo certo use a API
  Para que o meu caixa só mude por pedidos meus

  @LDSCP-R01 @nivel-unit @P1
  Cenário: o pedido sem token é recusado
    Dado um pedido sem token
    Quando ele chega a qualquer operação
    Então a resposta é 401 e nada é gravado

  @LDSCP-R01 @nivel-unit @P1
  Cenário: o token de outra API ou de outra chave é recusado
    Dado um token emitido para outra API, ou assinado por outra chave
    Quando ele é usado na lista
    Então a resposta é 401

  @LDSCP-R02 @nivel-unit @P1
  Cenário: a operação sem o seu escopo é recusada
    Dado um token só com "entries.read"
    Quando ele registra um lançamento
    Então a resposta é 403 e nada é gravado

  @LDSCP-R02 @nivel-unit @P2
  Cenário: cada escopo abre as suas operações
    Dado um token com "entries.write" e outro com "entries.read"
    Quando o primeiro registra e o segundo consulta
    Então as respostas são 201 e 200

  @LDSCP-R03 @nivel-unit @P1
  Cenário: o token sem o comerciante é recusado
    Dado um token válido sem o comerciante
    Quando ele registra e lista
    Então as respostas são 403 e nada é gravado

  @LDSCP-R04 @nivel-unit @P1
  Cenário: o comerciante chamado como a verificação de saúde é limitado
    Dado uma cota de 3 pedidos por segundo e um token com o comerciante "health"
    Quando ele faz pedidos seguidos
    Então um deles é recusado com 429

  @LDSCP-R04 @nivel-unit @P1
  Cenário: a cota é de cada comerciante e não alcança a verificação de saúde
    Dado uma cota de 2 pedidos por segundo
    Quando um comerciante passa da cota
    Então ele recebe 429 com o código "rate_limited", outro comerciante segue atendido e a verificação de saúde responde 200
