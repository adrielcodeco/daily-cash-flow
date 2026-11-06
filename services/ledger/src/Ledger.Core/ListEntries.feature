# language: pt
# @anchors
#   code: LSTFT
#   ref: LSTEN
#   updated_at: 2026-11-06
#   layer: feature

@LSTEN @ledger
Funcionalidade: ListEntries — conferir os lançamentos do dia
  Como o comerciante
  Eu quero listar os lançamentos de um dia
  Para conferir o meu caixa

  @LSTEN-R01 @nivel-unit @P1
  Cenário: a lista usa o comerciante e a data pedidos
    Dado um comerciante e uma data
    Quando a lista é pedida
    Então o armazenamento recebe o comerciante e a data

  @LSTEN-R02 @nivel-unit @P2
  Cenário: o limite fora da faixa é recusado
    Dado um limite de 201
    Quando a lista é pedida
    Então o pedido é recusado com o código "invalid_limit"

  @LSTEN-R03 @nivel-unit @P1
  Cenário: a próxima posição aparece quando há mais lançamentos
    Dado três lançamentos no dia
    Quando a lista é pedida com limite 2
    Então a página traz dois lançamentos e a próxima posição

  @LSTEN-R03 @nivel-unit @P1
  Cenário: a última página não tem próxima posição
    Dado dois lançamentos no dia
    Quando a lista é pedida com limite 2
    Então a página não tem próxima posição

  @LSTEN-R04 @nivel-integracao @P1
  Cenário: sem comerciante definido, o banco não devolve nada
    Dado lançamentos de dois comerciantes
    Quando uma consulta sem filtro roda com o papel da API
    Então só aparecem linhas do comerciante definido
