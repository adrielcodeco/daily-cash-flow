# ADR-0005: Modelo de leitura do saldo

- **Status:** aceita
- **Data:** 2026-11-05

## Contexto

O relatório precisa atender 50 req/s sem depender de Lançamentos. Um lançamento retroativo muda o fechamento daquele dia e a abertura dos dias seguintes.

## Decisão

Manter uma tabela de créditos e débitos por comerciante e dia, atualizada por eventos, com o acumulado calculado na consulta.

O cache em Redis é opcional e fica desligado por padrão. Ele pode ser ligado por rota ou por tabela, com a variável `Cache:Targets`, nos dois serviços. Sem alvos, o sistema nem conecta ao Redis. A chave leva uma versão por comerciante, que muda a cada escrita, então a invalidação afeta só aquele comerciante.

## Opções

<!-- radar:start -->
![Radar de Modelo de leitura do saldo](radar/ADR-0005.svg)

| Pilar | **Agregado diário e cache** | Consulta aos lançamentos | Event sourcing completo |
|---|---:|---:|---:|
| Segurança | 4 | 3 | 4 |
| Desempenho | 5 | 2 | 4 |
| Escalabilidade | 5 | 2 | 4 |
| Confiabilidade | 4 | 2 | 5 |
| Custo | 4 | 4 | 2 |
| Simplicidade | 4 | 5 | 1 |
| Operabilidade | 4 | 3 | 2 |
<!-- radar:end -->

## Leitura do radar

Consultar os lançamentos diretamente é simples, mas cria dependência entre os serviços e faz cada leitura custar mais. Event sourcing completo dá histórico e reconstrução, mas o custo de aprendizado e de operação não se paga. O agregado diário entrega quase o mesmo resultado, porque somas não dependem da ordem dos eventos e Lançamentos continua sendo a fonte para reconstrução.

## Consequências

**Positivas**

- Leitura rápida e isolada dos lançamentos
- Evento fora de ordem ou retroativo altera apenas uma linha

**Negativas**

- O saldo pode ficar alguns segundos atrás, então a resposta informa `updatedThrough`
- A abertura soma o histórico do comerciante, o que pode pedir fotografias mensais no futuro

## Resultado medido

No teste de carga, a latência foi equivalente com e sem cache, com p95 perto de 7 ms. O agregado diário sozinho atende às 50 req/s, e o cache fica como proteção do banco para volumes maiores.

## Revisão de 2026-11-06

A reavaliação às cegas favoreceu o agregado sem cache, com a maior diferença entre as decisões, e a carga já mostrava que o cache não trazia ganho. Como a necessidade muda de caso a caso, o cache virou um recurso configurável, e não uma parte fixa da arquitetura.

ORMs como NHibernate, e EF Core com bibliotecas de terceiros, oferecem cache por tabela. Aqui, uma camada própria na frente do repositório invalida só o comerciante afetado, em vez da tabela inteira, e funciona com escritas feitas em outro processo.

O radar acima mostra a avaliação original. A nova avaliação ainda não foi oficializada.

## Gatilho de revisão

Rever se o histórico por comerciante deixar a soma lenta. O próximo passo são fotografias mensais do saldo.
