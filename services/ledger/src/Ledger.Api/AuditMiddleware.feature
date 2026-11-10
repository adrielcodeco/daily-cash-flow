# language: pt
# @anchors
#   code: LDADF
#   ref: LDADS
#   updated_at: 2026-11-09
#   layer: feature

@LDADS @ledger
Funcionalidade: AuditMiddleware — a trilha de auditoria da API de Lançamentos
  Como quem investiga um incidente
  Eu quero ver toda escrita e todo acesso recusado
  Para saber quem fez ou tentou o quê

  @LDADS-R01 @nivel-unit @P1
  Cenário: a escrita fica na trilha de auditoria
    Dado um lançamento registrado
    Quando a trilha de auditoria é lida
    Então ela tem a escrita com o status 201, o cliente e o comerciante

  @LDADS-R02 @nivel-unit @P1
  Cenário: a consulta ao lançamento de outro comerciante fica na trilha
    Dado um lançamento de outro comerciante
    Quando ele é consultado
    Então a trilha de auditoria tem a consulta com o status 404

  @LDADS-R02 @nivel-unit @P1
  Cenário: o acesso recusado fica na trilha
    Dado um pedido sem token e outro sem escopo
    Quando eles são recusados
    Então a trilha de auditoria tem os status 401 e 403

  @LDADS-R02 @nivel-unit @P1
  Cenário: o pedido recusado pela cota fica na trilha
    Dado uma cota de 1 pedido por segundo
    Quando o comerciante passa da cota
    Então a trilha de auditoria tem o status 429 com o comerciante

  @LDADS-R03 @nivel-unit @P2
  Cenário: a leitura bem-sucedida fica fora da trilha
    Dado um lançamento do comerciante
    Quando ele é consultado com sucesso
    Então a trilha de auditoria não tem a consulta
