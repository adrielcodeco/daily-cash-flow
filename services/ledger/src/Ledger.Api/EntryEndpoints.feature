# language: pt
# @anchors
#   code: LDEPF
#   ref: LDEPS
#   updated_at: 2026-11-09
#   layer: feature

@LDEPS @ledger
Funcionalidade: EntryEndpoints — as operações HTTP de lançamentos
  Como o comerciante
  Eu quero que a API recuse pedidos malformados e esconda o que não é meu
  Para que o meu caixa só mude por pedidos válidos

  @LDEPS-R01 @nivel-unit @P1
  Cenário: a chave fora do tamanho é recusada
    Dado uma Idempotency-Key ausente, de 7 ou de 129 caracteres
    Quando um lançamento é registrado ou estornado
    Então a resposta é 400 com o código "invalid_idempotency_key" e nada é gravado

  @LDEPS-R01 @nivel-unit @P2
  Cenário: os limites da chave são aceitos
    Dado uma Idempotency-Key de 8 ou de 128 caracteres
    Quando um lançamento é registrado
    Então a resposta é 201

  @LDEPS-R02 @nivel-unit @P1
  Cenário: o tipo em número é recusado
    Dado um lançamento com o tipo enviado como o número 1
    Quando ele é registrado
    Então a resposta é 400 e nada é gravado

  @LDEPS-R03 @nivel-unit @P1
  Cenário: o lançamento de outro comerciante não é encontrado
    Dado um lançamento de outro comerciante
    Quando ele é consultado ou estornado
    Então a resposta é 404 com o código "entry_not_found"

  @LDEPS-R03 @nivel-unit @P1
  Cenário: a lista não mostra lançamentos de outro comerciante
    Dado um lançamento de outro comerciante na data
    Quando a lista da data é pedida
    Então a lista vem vazia

  @LDEPS-R04 @nivel-unit @P1
  Cenário: o comerciante que não pode ser identificado é recusado
    Dado um token com o comerciante "00000000-0000-0000-0000-000000000000"
    Quando ele registra, estorna, lista e consulta
    Então as respostas são 401 e nada é gravado
