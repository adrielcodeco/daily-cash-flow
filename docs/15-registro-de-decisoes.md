# Registro de decisões

Toda decisão e todo trade-off ficam registrados, conforme o princípio P10. Este documento guarda as decisões menores e os parâmetros. As escolhas com alternativas reais ficam nos [ADRs](adr/README.md), com radar. A lista só com o que foi decidido está no [resumo das decisões](16-resumo-de-decisoes.md).

## Como registrar

| Tipo de decisão | Onde fica |
|---|---|
| Escolha entre alternativas reais, com efeito na arquitetura | ADR com radar |
| Parâmetro, convenção ou escolha menor | Este registro |
| Regra de negócio | Spec ao lado do código e doutrina em `product/` |
| Decisão revista | Seção "Decisões revistas", sem apagar a original |

O registro entra no mesmo commit da decisão, com o resumo gerado de novo por `node tools/decisions/generate.mjs`. Antes de fechar um marco, o código é conferido contra este documento.

A coluna **Base** diz de onde veio cada escolha:

| Base | Significado |
|---|---|
| Requisito | Vem do desafio ou de um requisito aceito |
| Medição | Foi definida ou confirmada por um teste |
| Julgamento | Escolha fundamentada, sem medição |
| Valor inicial | Número razoável para começar, ainda não medido nem ajustado |

## Domínio e dados

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-01 | Dinheiro em `long`, em centavos | `decimal`, tipo monetário do banco | Só BRL, e toda exibição precisa formatar | Julgamento | Houver outra moeda ou fração de centavo |
| D-02 | Uma única moeda: BRL | Moeda por lançamento | Sem suporte a outras moedas | Julgamento | O produto atender outro país |
| D-03 | Dia pela data de competência, em `America/Sao_Paulo` | UTC, fuso por comerciante | Comerciante em outro fuso segue o dia de São Paulo | Julgamento | Houver comerciante fora do fuso |
| D-04 | Correção por estorno; lançamento é imutável | Atualizar ou excluir | Corrigir um erro gera dois registros a mais no histórico | Requisito | Não previsto: é doutrina do produto |
| D-05 | Um lançamento só pode ser estornado uma vez, garantido por índice único | Regra só no código, estorno parcial | Não há estorno parcial | Julgamento | O negócio pedir estorno parcial |
| D-06 | Lançamento retroativo é permitido | Bloquear dias fechados | Um dia passado pode mudar, sem fechamento contábil | Julgamento | Houver fechamento de período |
| D-07 | Abertura calculada na consulta | Gravar o acumulado por dia | A consulta soma o histórico do comerciante | Julgamento | A soma ficar lenta: ver ADR-0005 |
| D-08 | Ids em UUID v7 | UUID v4, sequência do banco | Ids mais longos que uma sequência, e revelam o momento da criação | Julgamento | O tamanho dos índices pesar |
| D-09 | `eventId` igual ao id do lançamento | Id próprio do evento | Um lançamento só pode gerar um evento desse tipo | Julgamento | Surgirem novos eventos por lançamento |
| D-10 | Evento carrega os dados do lançamento, sem exigir consulta de volta | Evento só com o id | Mensagem maior, e o contrato expõe mais campos | Julgamento | O evento passar a ter dados sensíveis |
| D-11 | Tópico versionado no nome: `entries-recorded-v1` | Versão só no conteúdo | Mudança incompatível exige novo tópico e publicação dupla | Julgamento | Houver mudança incompatível |
| D-12 | `processed_event` guarda todo evento aplicado | Janela de retenção | A tabela cresce sem limite | Julgamento | O volume pedir retenção, maior que a retenção de reenvio |
| D-13 | Limpeza da outbox fica como proposta | Limpar desde o início | A tabela cresce até a limpeza existir | Julgamento | Antes de produção |

## Mensageria e fluxo

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-14 | Lote de 100 eventos na outbox | Lote maior ou menor | Um lote grande prende a transação por mais tempo | Valor inicial | A vazão exigir: ver ADR-0015 |
| D-15 | Espera de 200 ms com a fila vazia | Espera maior, aviso do banco | Consultas contínuas sem eventos | Valor inicial | A carga no banco pesar |
| D-16 | Espera de 2 s depois de falha ao publicar | Espera exponencial | Recuperação simples, mas não adaptativa | Valor inicial | Falhas longas gerarem ruído |
| D-17 | 5 tentativas antes das mensagens mortas | Mais tentativas, sem desvio | Uma falha longa do banco pode desviar eventos válidos, que exigem reenvio | Valor inicial | O runbook for acionado por falhas transitórias |
| D-18 | Nova entrega com espera de 1 a 60 s | Espera fixa | A recuperação pode atrasar até 60 s por evento | Valor inicial | A defasagem passar do SLO |
| D-19 | Provisionar a assinatura antes da primeira publicação | Criar na subida do consumidor | Um passo a mais na implantação | Medição | Não previsto: mensagem sem assinatura é descartada |
| D-20 | Lote de 500 lançamentos no reenvio | Lote maior ou menor | Mais idas ao banco que um lote maior | Valor inicial | A reconstrução ficar lenta |
| D-21 | 10 s para o consumidor encerrar | Encerrar na hora | Eventos em andamento além desse prazo são entregues de novo | Valor inicial | O encerramento gerar repetição demais |

## Cache

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-22 | Versão por comerciante na chave do cache | Invalidar dia a dia | Toda mudança descarta todo o cache daquele comerciante | Julgamento | A taxa de acerto cair demais |
| D-23 | TTL de 60 s | Sem TTL, TTL maior | Se a invalidação falhar com o Redis fora, o saldo pode ficar até 60 s desatualizado | Valor inicial | O negócio exigir menos defasagem |
| D-24 | Ignorar o cache enquanto estiver desconectado | Aguardar o tempo limite | Sem o cache, toda leitura vai ao banco | Medição | Não previsto: esperar elevou o p95 a 2,2 s |
| D-25 | Manter o cache mesmo sem ganho medido de latência | Remover o Redis | Custo e um componente a mais, sem benefício medido nesta carga | Medição | Em revisão: a justificativa é proteger o banco em volumes maiores, o que não foi medido |
| D-26 | Redis local sem persistência, com 64 MB e `allkeys-lru` | Persistir | Reiniciar perde o cache | Julgamento | Não previsto: o dado é descartável |

## API e contratos

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-27 | Versão na rota: `/v1` | Versão em cabeçalho | URLs mudam quando a versão muda | Julgamento | Houver `/v2` |
| D-28 | Registro de outro comerciante responde 404 | Responder 403 | O cliente não diferencia inexistente de proibido | Julgamento | Não previsto: evita revelar dados |
| D-29 | `Idempotency-Key` de 8 a 128 caracteres | Formato fixo | Aceita chaves fracas de quem chama | Valor inicial | Houver colisão por chave ruim |
| D-30 | Listagem com 50 itens por padrão e 200 no máximo | Outros limites | Mais chamadas para percorrer dias grandes | Valor inicial | O uso real pedir |
| D-31 | Resposta informa `updatedThrough` | Esconder a defasagem | Mais um campo que o cliente precisa entender | Requisito | Não previsto: princípio P5 |
| D-32 | `X-Merchant-Id` provisório até o M6 | Autenticação desde o início | Até o M6, quem chama escolhe o comerciante. Não pode ir a produção | Julgamento | No M6, com a troca por JWT |

## Ambiente, teste e entrega

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-33 | Emulador do Pub/Sub no ambiente local | RabbitMQ, projeto real no GCP | Ele pode diferir do serviço real: risco R09 | Medição | Antes de produção, com teste no GCP |
| D-34 | Docker Compose, com build em contêiner | Instalar o SDK na máquina | Primeira subida mais lenta | Julgamento | Não previsto |
| D-35 | Sem deploy real no GCP | Implantar | Rede, IAM e serviços gerenciados não foram exercitados | Julgamento | Antes de produção |
| D-36 | Portas locais 18081 e 18082 | 8081 e 8082 | Portas menos usuais | Julgamento | Não previsto: evitam conflito com serviços que costumam usar a faixa 8080 |
| D-37 | Pool local de 30 conexões, e orçamento menor no GCP | Mesmo valor nos dois | O teste local não valida o pool proposto para a nuvem | Valor inicial | No teste de carga no GCP |
| D-38 | PostgreSQL 17 e Redis 8 no ambiente local | Outras versões | Falta confirmar as versões oferecidas pelo Cloud SQL e pelo Memorystore | Julgamento | Antes de produção |
| D-39 | Carga com 50 leituras e 10 escritas por segundo | Só leituras | A proporção de escritas é premissa | Julgamento | Houver dados reais de uso |
| D-40 | Mutação com corte em 70% e meta de 90% | Sem corte | Um corte baixo aceita regressão | Valor inicial | A pontuação estabilizar |
| D-41 | Mutação fora do CI | Rodar a cada commit | A pontuação pode ficar defasada | Julgamento | Ver ADR-0021 |
| D-42 | Pacotes com versão central e sem vulnerabilidade conhecida | Versão por projeto | Atualização exige verificação manual | Julgamento | Houver atualização automática |

## Método e documentação

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-43 | Documentação em pt-BR, código em en-US | Um idioma só | O glossário precisa ligar os termos | Requisito | Não previsto |
| D-45 | Nota de radar é qualitativa, com sete pilares e três opções | Pontuação ponderada | A nota é opinião fundamentada, e não medição | Julgamento | Houver dados de operação |
| D-46 | Preços de lista em São Paulo, sem desconto | Preços com desconto | O custo real tende a ser menor | Julgamento | Houver contrato com o provedor |
| D-47 | Volumes de custo são premissas, e só o pico vem do desafio | Só o pico | As médias podem estar erradas | Julgamento | Houver dados reais |

## Decisões revistas

| Data | Decisão original | O que mudou | Motivo |
|---|---|---|---|
| 2026-11-05 | RabbitMQ como plano B no ambiente local | Descartado | O emulador validou entrega e mensagens mortas |
| 2026-11-05 | Pool padrão e consumo sem limite | Limite de 16 eventos e pool definido | O teste de resiliência esgotou as conexões: ver ADR-0017 |
| 2026-11-05 | Esperar o tempo limite do Redis | Ignorar o cache desconectado | A carga mostrou p95 de 2,2 s |
| 2026-11-05 | Cache como ganho de latência | Cache como proteção do banco | A carga não mostrou ganho de latência: ver ADR-0005 |
