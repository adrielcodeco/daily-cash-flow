# language: pt
# @anchors
#   code: CESFT
#   ref: CESTR
#   updated_at: 2026-11-09
#   layer: feature

@CESTR @ledger
Funcionalidade: CachedEntryStore — o cache configurável
  Como quem opera o sistema
  Eu quero ligar o cache só onde ele vale a pena
  Para pagar o custo dele apenas quando houver ganho

  @CESTR-R01 @nivel-unit @P1
  Cenário: sem alvo ligado, o cache não é usado
    Dado nenhum alvo de cache
    Quando a consulta é feita
    Então o cache não é tocado

  @CESTR-R01 @nivel-unit @P1
  Cenário: o alvo de outro serviço não liga o cache
    Dado o cache ligado só para uma tabela do consolidado
    Quando uma escrita acontece
    Então o cache não é tocado

  @CESTR-R02 @nivel-unit @P1
  Cenário: o alvo de rota vale só para aquela rota
    Dado o cache ligado para uma rota
    Quando a outra rota é consultada
    Então o cache não é tocado

  @CESTR-R03 @nivel-unit @P1
  Cenário: o alvo de tabela vale para todas as rotas
    Dado o cache ligado para a tabela
    Quando as rotas são consultadas duas vezes
    Então o armazenamento é consultado uma vez por rota

  @CESTR-R04 @nivel-unit @P1
  Cenário: o valor em cache evita a consulta
    Dado um valor já em cache
    Quando a consulta é feita
    Então o armazenamento não é consultado

  @CESTR-R05 @nivel-unit @P1
  Cenário: a consulta funciona com o cache fora do ar
    Dado um cache fora do ar
    Quando a consulta é feita
    Então o valor vem do armazenamento

  @CESTR-R06 @nivel-unit @P1
  Cenário: a escrita invalida o cache do comerciante
    Dado o cache ligado
    Quando uma escrita acontece
    Então o cache do comerciante é invalidado

  @CESTR-R07 @nivel-unit @P1
  Cenário: a falha do cache não impede a escrita
    Dado um cache fora do ar
    Quando uma escrita acontece
    Então a escrita termina normalmente

  @CESTR-R08 @nivel-unit @P1
  Cenário: a invalidação durante a consulta não guarda valor antigo
    Dado uma invalidação durante a consulta
    Quando a consulta seguinte é feita
    Então o valor vem do armazenamento
