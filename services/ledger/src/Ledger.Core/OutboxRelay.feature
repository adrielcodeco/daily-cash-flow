# language: pt
# @anchors
#   code: OBRFT
#   ref: OBRLY
#   updated_at: 2026-11-09
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

  @OBRLY-R03 @nivel-unit @P1
  Cenário: o encerramento no meio do lote mantém o que foi confirmado
    Dado três eventos pendentes
    Quando o publicador é encerrado durante o segundo envio
    Então só o primeiro fica marcado como publicado

  @OBRLY-R04 @nivel-unit @P1
  Cenário: o evento que falhou espera antes de nova tentativa
    Dado três eventos pendentes
    Quando o segundo envio falha
    Então o segundo evento é adiado
