# language: pt
# @anchors
#   code: CNADF
#   ref: CNADS
#   updated_at: 2026-11-09
#   layer: feature

@CNADS @consolidation
Funcionalidade: AuditMiddleware — a trilha de auditoria da API do Consolidado
  Como quem investiga um incidente
  Eu quero ver todo acesso recusado ao saldo
  Para saber quem tentou o quê

  @CNADS-R01 @nivel-unit @P1
  Cenário: o acesso recusado fica na trilha
    Dado um pedido sem token e outro sem escopo
    Quando eles são recusados
    Então a trilha de auditoria tem os status 401 e 403, com o cliente e o comerciante

  @CNADS-R01 @nivel-unit @P1
  Cenário: o pedido recusado pela cota fica na trilha
    Dado uma cota de 1 pedido por segundo
    Quando o comerciante passa da cota
    Então a trilha de auditoria tem o status 429 com o comerciante

  @CNADS-R02 @nivel-unit @P2
  Cenário: a consulta bem-sucedida fica fora da trilha
    Dado um comerciante com o escopo de saldo
    Quando ele consulta o saldo com sucesso
    Então a trilha de auditoria não tem a consulta
