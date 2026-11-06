# language: pt
# @anchors
#   code: CBSFT
#   ref: CBSTR
#   updated_at: 2026-11-06
#   layer: feature

@CBSTR @consolidation
Funcionalidade: CachedBalanceStore — o cache configurável
  Como quem opera o sistema
  Eu quero ligar o cache só onde ele vale a pena
  Para pagar o custo dele apenas quando houver ganho

  @CBSTR-R01 @nivel-unit @P1
  Cenário: sem alvo ligado, o cache não é usado
    Dado nenhum alvo de cache
    Quando a consulta é feita
    Então o cache não é tocado

  @CBSTR-R02 @nivel-unit @P1
  Cenário: o alvo de rota vale só para aquela rota
    Dado o cache ligado para uma rota
    Quando a outra rota é consultada
    Então o cache não é tocado

  @CBSTR-R03 @nivel-unit @P1
  Cenário: o alvo de tabela vale para todas as rotas
    Dado o cache ligado para a tabela
    Quando as rotas são consultadas duas vezes
    Então o armazenamento é consultado uma vez por rota

  @CBSTR-R04 @nivel-unit @P1
  Cenário: o valor em cache evita a consulta
    Dado um valor já em cache
    Quando a consulta é feita
    Então o armazenamento não é consultado

  @CBSTR-R05 @nivel-unit @P1
  Cenário: a consulta funciona com o cache fora do ar
    Dado um cache fora do ar
    Quando a consulta é feita
    Então o valor vem do armazenamento

  @CBSTR-R06 @nivel-unit @P1
  Cenário: a escrita invalida o cache do comerciante
    Dado o cache ligado
    Quando uma escrita acontece
    Então o cache do comerciante é invalidado

  @CBSTR-R07 @nivel-unit @P1
  Cenário: a falha do cache não impede a escrita
    Dado um cache fora do ar
    Quando uma escrita acontece
    Então a escrita termina normalmente

  @CBSTR-R08 @nivel-unit @P1
  Cenário: a invalidação durante a consulta não guarda valor antigo
    Dado uma invalidação durante a consulta
    Quando a consulta seguinte é feita
    Então o valor vem do armazenamento
