# language: pt
# @anchors
#   code: LDMRF
#   ref: LDMRC
#   updated_at: 2026-11-09
#   layer: feature

@LDMRC @ledger
Funcionalidade: MerchantContext — o comerciante do pedido vem do token
  Como o comerciante
  Eu quero que a API só aja em meu nome quando o meu token disser
  Para que ninguém escolha agir como eu

  @LDMRC-R01 @nivel-unit @P1
  Cenário: o comerciante é lido do token
    Dado um token com um comerciante
    Quando o comerciante do pedido é lido
    Então ele é o comerciante do token

  @LDMRC-R02 @nivel-unit @P1
  Cenário: o comerciante inválido não é lido
    Dado um token com o comerciante "00000000-0000-0000-0000-000000000000"
    Quando o comerciante do pedido é lido
    Então a leitura falha

  @LDMRC-R02 @nivel-unit @P1
  Cenário: o token sem o comerciante não é lido
    Dado um token sem o comerciante
    Quando o comerciante do pedido é lido
    Então a leitura falha

  @LDMRC-R03 @nivel-unit @P1
  Cenário: o comerciante escolhido pelo cliente é ignorado
    Dado um pedido com o comerciante no cabeçalho e no parâmetro, e sem ele no token
    Quando o comerciante do pedido é lido
    Então a leitura falha
