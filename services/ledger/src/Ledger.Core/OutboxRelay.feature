# language: pt
# @anchors
#   code: OBRFT
#   ref: OBRLY
#   updated_at: 2026-11-05
#   layer: feature

@OBRLY @ledger
Funcionalidade: OutboxRelay — enviar sem perder eventos
  Como o publicador da outbox
  Eu quero marcar apenas o que foi confirmado
  Para que nenhum evento se perca em uma falha

  @OBRLY-R01 @nivel-unit @P1
  Cenário: o evento confirmado é marcado como publicado
    Dado dois eventos pendentes
    Quando o lote é enviado
    Então os dois eventos ficam marcados como publicados

  @OBRLY-R02 @nivel-unit @P1
  Cenário: a falha interrompe o lote
    Dado três eventos pendentes
    E que a mensageria falha no segundo evento
    Quando o lote é enviado
    Então o terceiro evento não é enviado

  @OBRLY-R03 @nivel-unit @P1
  Cenário: o evento anterior à falha continua marcado
    Dado três eventos pendentes
    E que a mensageria falha no segundo evento
    Quando o lote é enviado
    Então apenas o primeiro evento fica marcado como publicado
