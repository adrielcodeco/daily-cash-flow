# Resultados de testes

Medições feitas a partir de 05/11/2026, na primeira versão completa. Todo o ambiente e o gerador de carga rodaram na mesma máquina, um Apple M1 Pro com 10 CPUs e 7,7 GiB para contêineres. Os números servem de referência local e não representam o ambiente do GCP.

As tabelas de carga e resiliência mostram três medições: a primeira versão, sem segurança nem telemetria; a versão com autenticação, rate limiting e telemetria; e a versão atual, em 09/11/2026, que acrescenta Dapper, row-level security, cache desligado por padrão e as correções da revisão antes da publicação.

## Resumo dos requisitos do desafio

| Requisito | Meta | Resultado | Situação |
|---|---|---|---|
| Lançamentos disponível com o consolidado fora | Nenhuma escrita recusada | 0 falhas em 4.501 escritas nas duas primeiras medições, e em 4.500 na atual | Aprovado |
| Vazão do consolidado | 50 req/s | 50 req/s por 5 minutos | Aprovado |
| Perda de requisições | No máximo 5% | 0% | Aprovado |

## Teste de carga

Comando: `./scripts/load-test.sh`. Cada cenário envia 50 leituras por segundo e 10 lançamentos por segundo ao mesmo tempo, com 50 comerciantes.

| Cenário | Duração | Leituras | Falhas | Leitura p95 | Leitura máx. | Escrita p95 |
|---|---|---:|---:|---:|---:|---:|
| Sem segurança, com cache | 5 min | 15.001 | 0 | 7,71 ms | 28,44 ms | 11,35 ms |
| Sem segurança, com o cache fora | 2 min | 6.001 | 0 | 6,91 ms | 55,47 ms | 11,38 ms |
| Com segurança, com cache | 5 min | 15.001 | 0 | 7,55 ms | 97,78 ms | 11,16 ms |
| Com segurança, com o cache fora | 2 min | 6.001 | 0 | 6,54 ms | 22,95 ms | 11,26 ms |
| Atual, cache desligado | 5 min | 15.000 | 0 | 7,55 ms | 32,63 ms | 13,14 ms |
| Atual, cache ligado | 5 min | 15.001 | 0 | 7,91 ms | 410,58 ms | 12,85 ms |
| Atual, cache ligado e Redis fora | 2 min | 6.001 | 0 | 7,61 ms | 21,01 ms | 12,65 ms |
| Atual, cache desligado, dois anos de histórico | 2 min | 6.001 | 0 | 7,57 ms | 485,56 ms | 11,76 ms |

As leituras são as respostas 200 contadas pelo k6. O gerador de taxa constante fecha a janela com 15.000 ou 15.001 requisições, conforme o arredondamento do fim.

Autenticação e telemetria não mudaram o p95 de forma perceptível, e a diferença entre as medições está dentro da variação esperada. O maior tempo isolado subiu para 97,78 ms no cenário com cache, sem efeito na meta. A carga usa 50 comerciantes, então cada um fica muito abaixo do limite de 100 requisições por segundo.

O row-level security não mudou o p95 de forma perceptível, apesar das idas a mais ao banco em cada leitura. Alguns cenários têm um tempo isolado perto de 400 ms, sem efeito na meta de p95. A causa não foi investigada. O cache continua sem ganho nesta carga.

Na medição anterior, a escrita ficava mais lenta com o cache ligado só para o saldo. A revisão achou a causa: a API de Lançamentos também se conectava ao Redis e invalidava o cache a cada lançamento, mesmo sem alvo seu. Corrigido, o p95 de escrita com e sem cache ficou equivalente.

O cenário com dois anos de histórico dá a cada comerciante 730 dias de saldo antes do dia consultado, o caso em que a soma da abertura pesa. O p95 ficou igual ao do histórico curto, então o limite apontado no ADR-0005 não aparece nesse volume.

Nesta carga, o cache não trouxe ganho de latência, porque o saldo pré-calculado já responde em poucos milissegundos, mesmo com dois anos de histórico. O ganho esperado é reduzir a carga no banco com muito mais volume, e isso ainda não foi medido.

## Teste de resiliência

Comando: `./scripts/resilience.sh`. O teste envia 50 lançamentos por segundo por 90 segundos e, durante a carga, para o consolidado por 60 segundos e congela a mensageria por 20 segundos.

| Medida | Sem segurança | Com segurança | Atual |
|---|---:|---:|---:|
| Lançamentos enviados | 4.501 | 4.501 | 4.500 |
| Lançamentos recusados | 0 | 0 | 0 |
| Latência de escrita p95 | 8,03 ms | 7,90 ms | 8,33 ms |
| Eventos retidos na outbox com a mensageria congelada | 1.042 | Não registrado | 1.046 |
| Saldo final, igual à soma dos lançamentos | 450.100 centavos | 450.100 centavos | 450.000 centavos |
| Tempo para fechar o saldo depois do fim da carga | 3 s | 4 s | 1 s |
| Leituras com falha durante a recuperação | 0 | 0 | 0 |
| Eventos pendentes no final | 0 | 0 | 0 |

## Segurança e telemetria

`./scripts/smoke.sh` verifica estes cenários a cada execução:

| Cenário | Resultado |
|---|---|
| Sem token, com token inválido ou só com `X-Merchant-Id` | 401 |
| Sem o escopo da operação | 403 |
| Lançamento de outro comerciante | 404 |
| `X-Merchant-Id` enviado junto de um token válido | Ignorado |
| Papel da API, sem comerciante definido | Nenhuma linha devolvida |
| Papel da API tentando alterar ou excluir lançamento | Recusado pelo banco |
| Cache desligado | Nenhuma conexão com o Redis, e a consulta segue com o Redis parado |
| `smoke.sh` com o cache ligado | Passou na medição anterior, com acertos medidos nos dois serviços |
| 400 requisições simultâneas de um comerciante | Parte recebe 429, e outro cliente segue com 200 |
| Coletor de telemetria fora do ar | Escrita aceita |
| Um lançamento rastreado | Um traço com API, publicador e consumidor |
| Saldos comparados com os lançamentos, por `reconcile.sh` | Nenhuma diferença |

O fallback do cache, com o cache ligado e o Redis fora, é provado pelo terceiro cenário de `load-test.sh`, e não pelo smoke.

## Testes automatizados

Comando: `./scripts/test.sh`.

| Serviço | Testes | Resultado |
|---|---:|---|
| Ledger | 130 | Todos passaram |
| Consolidation | 76 | Todos passaram |

Os testes de integração usam PostgreSQL real em contêiner, inclusive para chamadas concorrentes. Os testes na fronteira HTTP sobem cada API em memória, com o handler JWT real e tokens assinados no teste, e cobrem autenticação, escopos, comerciante, auditoria, limite de requisições e o formato das recusas. O teste de ponta a ponta, em `./scripts/smoke.sh`, faz 15 verificações no ambiente completo.

## Testes de mutação

Comando: `./scripts/mutation.sh`, com Stryker.NET nos projetos de domínio. Mensagens de exceção ficam fora da medição, porque não são regra de negócio.

| Projeto | Pontuação | Mortos | Sobreviventes |
|---|---:|---:|---:|
| `Ledger.Core` | 96,59% | 85 | 3 |
| `Consolidation.Core` | 98,11% | 52 | 1 |

Os sobreviventes não indicam lacuna de regra. Um é a mensagem de uma exceção interna. Dois removem um bloco que tem o mesmo efeito do caminho normal. O último troca −1 por +1 no tamanho de uma descrição ausente, o que continua separando ausente de vazia. Outros 12 mutantes de `Ledger.Core` e 2 de `Consolidation.Core` não compilaram e ficaram fora da pontuação.

## Reconstrução do consolidado

Medição feita em 2026-11-09 com `./scripts/rebuild-consolidation.sh`, sobre os dados dos testes de carga. O fim é o momento em que `./scripts/reconcile.sh` não acha diferença, conferido a cada segundo.

| Medida | Resultado |
|---|---:|
| Lançamentos | 13.104 |
| Tempo até o saldo completo | 178 s |
| Vazão | Cerca de 74 eventos por segundo |
| Saldos depois, por comerciante e dia | Iguais aos lançamentos, em 201 combinações |

A recolocação na outbox passou a ser um comando por lote, mas a vazão não mudou: o limite é o consumidor, que processa até 16 eventos por vez.

Durante a reconstrução, a API respondeu 200 com saldo 0 e depois com saldo parcial. As consequências estão no [plano de recuperação](17-recuperacao-de-desastres.md#o-que-foi-provado).

## Defeitos encontrados pelos testes

| Teste | Defeito | Correção |
|---|---|---|
| Integração | Evento com horário fora de UTC seria recusado pelo banco e iria para mensagens mortas | Conversão para UTC na gravação |
| Resiliência | Ao voltar de uma queda, o consumidor esgotava as conexões do banco e derrubava a API de saldo | Limite de 16 eventos em processamento e pool de conexões por processo |
| Carga | Com o Redis fora, cada leitura esperava o tempo limite, e o p95 subia para 2,2 s | O cache é ignorado enquanto estiver desconectado |
| Mutação | Faltavam testes de limites exatos, estorno de débito e gravação no cache | Testes acrescentados, com pontuação de 73% e 82% para mais de 97% |
| Fronteira HTTP | Fora do ambiente de desenvolvimento, 401, 403 e corpo inválido respondiam sem corpo | Toda recusa responde ProblemDetails |

## Defeitos que os testes não encontraram

A revisão de segurança achou entradas inválidas aceitas pela API, apesar dos 69 testes de então e da mutação acima de 97%. A mutação avalia os testes que existem e não mostra os cenários que faltam. Os defeitos foram corrigidos, e os detalhes estão no [registro de vulnerabilidades](18-vulnerabilidades.md).

## Limites destas medições

- O gerador de carga divide a máquina com os serviços.
- O histórico mais longo medido tem dois anos por comerciante, com 50 comerciantes. Mais comerciantes ou mais anos não foram medidos.
- A mensageria é o emulador do Pub/Sub, e não o serviço real.
