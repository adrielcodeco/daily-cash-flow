# Registro de riscos

Probabilidade e impacto vão de 1 a 3. A exposição é o produto dos dois.

| ID | Risco | Prob. | Impacto | Exposição | Resposta | Dono |
|---|---|---:|---:|---:|---|---|
| R01 | Requisito regulatório não previsto atrasar a produção | 2 | 3 | 6 | Validar com conformidade antes do desenvolvimento final | Conformidade |
| R02 | Saldo divergir dos lançamentos | 1 | 3 | 3 | Idempotência, reconstrução e reconciliação | Líder técnico |
| R03 | Mensagem morta sem tratamento deixar valor fora do saldo | 2 | 3 | 6 | Alerta imediato e runbook | SRE |
| R04 | Conexões do banco se esgotarem ao escalar | 2 | 2 | 4 | Orçamento de conexões, alerta e pooler | SRE |
| R05 | Comerciante decidir com saldo defasado | 2 | 2 | 4 | `updatedThrough` e SLO de defasagem | Dono do produto |
| R06 | Histórico longo deixar a abertura lenta | 2 | 2 | 4 | Fotografias mensais, conforme ADR-0005 | Líder técnico |
| R07 | Dependência do GCP dificultar uma troca de provedor | 1 | 2 | 2 | Portas e adaptadores, contêineres e OpenTelemetry | Arquiteto |
| R08 | Perda da região sem alternativa no Brasil | 1 | 3 | 3 | Avaliar réplica externa com conformidade | Arquiteto |
| R09 | Emulador divergir do Pub/Sub real | 2 | 2 | 4 | Teste no GCP antes de produção | Líder técnico |
| R10 | Vazamento de token ou credencial | 1 | 3 | 3 | Tokens curtos, escopos, cofre e rotação | Segurança |
| R11 | Custo crescer além do previsto | 1 | 2 | 2 | Orçamento com alerta e revisão mensal | Patrocinador |
| R12 | Equipe sem prática em operação assíncrona | 2 | 2 | 4 | Runbooks, treinamento e exercícios | Líder técnico |

## Mapa de exposição

| Probabilidade \ Impacto | 1 | 2 | 3 |
|---|---|---|---|
| **3** | | | |
| **2** | | R04, R05, R06, R09, R12 | R01, R03 |
| **1** | | R07, R11 | R02, R08, R10 |
