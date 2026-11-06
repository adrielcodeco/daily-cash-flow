# language: pt
# @anchors
#   code: RPEFT
#   ref: RPENT
#   updated_at: 2026-11-05
#   layer: feature

@RPENT @ledger
Funcionalidade: RepublishEntries — reenviar os fatos
  Como a operação
  Eu quero reenviar todos os lançamentos
  Para reconstruir o consolidado

  @RPENT-R01 @nivel-unit @P1
  Cenário: todos os lançamentos voltam para a fila
    Dado três lançamentos registrados
    Quando o reenvio roda em lotes de 2
    Então os três eventos ficam pendentes

  @RPENT-R02 @nivel-unit @P1
  Cenário: o evento reenviado mantém a identidade
    Dado um lançamento registrado
    Quando o reenvio roda
    Então o evento tem a identidade do lançamento
