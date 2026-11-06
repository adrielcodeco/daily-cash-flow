# Resultados de testes

Medições feitas em 05/11/2026, na versão final do M4. Todo o ambiente e o gerador de carga rodaram na mesma máquina, um Apple M1 Pro com 10 CPUs e 7,7 GiB para contêineres. Os números servem de referência local e não representam o ambiente do GCP.

## Resumo dos requisitos do desafio

| Requisito | Meta | Resultado | Situação |
|---|---|---|---|
| Lançamentos disponível com o consolidado fora | Nenhuma escrita recusada | 0 falhas em 4.501 escritas | Aprovado |
| Vazão do consolidado | 50 req/s | 50 req/s por 5 minutos | Aprovado |
| Perda de requisições | No máximo 5% | 0% | Aprovado |

## Teste de carga

Comando: `./scripts/load-test.sh`. Cada cenário envia 50 leituras por segundo e 10 lançamentos por segundo ao mesmo tempo, com 50 comerciantes.

| Cenário | Duração | Leituras | Falhas | Leitura p95 | Leitura máx. | Escrita p95 |
|---|---|---:|---:|---:|---:|---:|
| Com cache | 5 min | 15.001 | 0 | 7,71 ms | 28,44 ms | 11,35 ms |
| Com o cache fora | 2 min | 6.001 | 0 | 6,91 ms | 55,47 ms | 11,38 ms |

Nesta carga, o cache não trouxe ganho de latência, porque o saldo pré-calculado já responde em poucos milissegundos. O ganho esperado é reduzir a carga no banco com mais volume e histórico, e isso ainda não foi medido.

## Teste de resiliência

Comando: `./scripts/resilience.sh`. O teste envia 50 lançamentos por segundo por 90 segundos e, durante a carga, para o consolidado por 60 segundos e congela a mensageria por 20 segundos.

| Medida | Resultado |
|---|---:|
| Lançamentos enviados | 4.501 |
| Lançamentos recusados | 0 |
| Latência de escrita p95 | 8,03 ms |
| Eventos retidos na outbox com a mensageria congelada | 1.042 |
| Saldo final | 450.100 centavos, igual à soma dos lançamentos |
| Tempo para fechar o saldo depois do fim da carga | 3 s |
| Leituras com falha durante a recuperação | 0 |
| Eventos pendentes no final | 0 |

## Testes automatizados

Comando: `./scripts/test.sh`.

| Serviço | Testes | Resultado |
|---|---:|---|
| Ledger | 43 | Todos passaram |
| Consolidation | 26 | Todos passaram |

Os testes de integração usam PostgreSQL real em contêiner, inclusive para chamadas concorrentes. O teste de ponta a ponta, em `./scripts/smoke.sh`, cobre 9 cenários no ambiente completo.

## Testes de mutação

Comando: `./scripts/mutation.sh`, com Stryker.NET nos projetos de domínio. Mensagens de exceção ficam fora da medição, porque não são regra de negócio.

| Projeto | Pontuação | Mortos | Sobreviventes |
|---|---:|---:|---:|
| `Ledger.Core` | 97,96% | 48 | 1 |
| `Consolidation.Core` | 97,14% | 34 | 1 |

Os dois sobreviventes não indicam lacuna de regra: um é a mensagem de uma exceção interna, e o outro remove um bloco que tem o mesmo efeito do caminho normal. Outros 10 mutantes de `Ledger.Core` não compilaram e ficaram fora da pontuação.

## Reconstrução do consolidado

Medição feita em 2026-11-06 com `./scripts/rebuild-consolidation.sh`, sobre os dados dos testes de carga.

| Medida | Resultado |
|---|---:|
| Lançamentos | 8.809 |
| Tempo até o saldo completo | 126 s |
| Vazão | Cerca de 74 eventos por segundo |
| Soma do consolidado depois | Igual à soma dos lançamentos |

Durante a reconstrução, a API respondeu 200 com saldo 0 e depois com saldo parcial. As consequências estão no [plano de recuperação](17-recuperacao-de-desastres.md#o-que-foi-provado).

## Defeitos encontrados pelos testes

| Teste | Defeito | Correção |
|---|---|---|
| Integração | Evento com horário fora de UTC seria recusado pelo banco e iria para mensagens mortas | Conversão para UTC na gravação |
| Resiliência | Ao voltar de uma queda, o consumidor esgotava as conexões do banco e derrubava a API de saldo | Limite de 16 eventos em processamento e pool de conexões por processo |
| Carga | Com o Redis fora, cada leitura esperava o tempo limite, e o p95 subia para 2,2 s | O cache é ignorado enquanto estiver desconectado |
| Mutação | Faltavam testes de limites exatos, estorno de débito e gravação no cache | Testes acrescentados, com pontuação de 73% e 82% para 98% e 97% |

## Defeitos que os testes não encontraram

A revisão de segurança achou entradas inválidas aceitas pela API, apesar de 69 testes e da mutação acima de 97%. A mutação avalia os testes que existem e não mostra os cenários que faltam. Os detalhes estão no [registro de vulnerabilidades](18-vulnerabilidades.md).

## Limites destas medições

- O gerador de carga divide a máquina com os serviços.
- A carga usa um histórico pequeno. Um histórico longo pode deixar a soma da abertura mais lenta, como aponta o ADR-0005.
- A mensageria é o emulador do Pub/Sub, e não o serviço real.
- A autenticação ainda não está ativa nestas medições. Ela entra no M6, e os testes serão repetidos.
