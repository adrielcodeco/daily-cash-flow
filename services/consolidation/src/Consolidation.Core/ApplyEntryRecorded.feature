# language: pt
# @anchors
#   code: APEFT
#   ref: APENT
#   updated_at: 2026-11-05
#   layer: feature

@APENT @consolidation
Funcionalidade: ApplyEntryRecorded — aplicar cada evento uma vez
  Como o consumidor do consolidado
  Eu quero aplicar eventos sem contar duas vezes
  Para que o saldo seja igual à soma dos lançamentos

  @APENT-R01 @nivel-unit @P1
  Cenário: o crédito soma nos créditos do dia
    Dado um evento de crédito de 35000 centavos
    Quando o evento é aplicado
    Então os créditos do dia aumentam 35000 centavos

  @APENT-R01 @nivel-unit @P1
  Cenário: o débito soma nos débitos do dia
    Dado um evento de débito de 12000 centavos
    Quando o evento é aplicado
    Então os débitos do dia aumentam 12000 centavos

  @APENT-R02 @nivel-unit @P1
  Cenário: o evento repetido é ignorado
    Dado um evento de crédito já aplicado
    Quando o mesmo evento é aplicado de novo
    Então os créditos do dia não mudam

  @APENT-R03 @nivel-unit @P1
  Cenário: a ordem de chegada não muda o saldo
    Dado um crédito e um débito do mesmo dia
    Quando os eventos são aplicados em ordem inversa
    Então o saldo do dia é o mesmo

  @APENT-R04 @nivel-unit @P1
  Cenário: o agregado fica no comerciante do evento
    Dado eventos de dois comerciantes no mesmo dia
    Quando os eventos são aplicados
    Então cada comerciante tem o próprio agregado

  @APENT-R05 @nivel-unit @P1
  Cenário: o evento aplicado invalida o cache
    Dado um evento de crédito novo
    Quando o evento é aplicado
    Então o cache do comerciante é invalidado

  @APENT-R06 @nivel-unit @P1
  Cenário: a falha do cache não impede a aplicação
    Dado um cache fora do ar
    Quando o evento é aplicado
    Então os créditos do dia aumentam mesmo assim
