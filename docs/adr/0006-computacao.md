# ADR-0006: Plataforma de computação

- **Status:** aceita
- **Data:** 2026-11-05

## Contexto

Os serviços são contêineres sem estado, com carga variável. A equipe de operação deve ser pequena para esse sistema.

## Decisão

Usar Cloud Run com instâncias mínimas, 6 no cenário Início, e CPU sempre alocada. O publicador e o consumidor precisam dela, porque trabalham fora de requisições; a [estimativa de custos](../05-custos.md) aplica o mesmo faturamento por instância a todos os serviços.

## Opções

<!-- radar:start -->
![Radar de Plataforma de computação](radar/ADR-0006.svg)

| Critério | Peso | **Cloud Run** | GKE Autopilot | Compute Engine |
|---|---:|---:|---:|---:|
| Alinhamento com o negócio | 4 | 5 | 5 | 3 |
| Confiabilidade | 3 | 5 | 5 | 5 |
| Operabilidade | 3 | 4 | 4 | 3 |
| Desempenho | 2 | 4 | 5 | 5 |
| Economia | 2 | 4 | 3 | 2 |
| Escalabilidade | 2 | 5 | 5 | 4 |
| Simplicidade | 2 | 4 | 3 | 3 |
| Portabilidade | 1 | 5 | 5 | 5 |
| Segurança | 2 | 4 | 3 | 3 |
| **Resultado** | | **4,48** | **4,29** | **3,57** |

As perguntas e a evidência de cada nota estão na [avaliação dos radares](../21-avaliacao-dos-radares.md#adr-0006).
<!-- radar:end -->

## Leitura do radar

Em confiabilidade, as três opções empatam. GKE tem mais controle e desempenho melhor, mas é menos simples, custa mais e tem nota menor em segurança. Compute Engine empata com o GKE em desempenho, mas traz trabalho de sistema operacional e o maior custo. Se o padrão do cliente for GKE, a troca é simples, porque os serviços já são contêineres.

## Consequências

**Positivas**

- Escala automática e operação reduzida
- Escala automática acima do mínimo, pagando o excedente conforme o uso

**Negativas**

- Menos controle de rede e de execução do que no Kubernetes
- Processos contínuos precisam de configuração própria para não serem suspensos
- Instâncias mínimas sempre ativas formam um custo fixo

## Gatilho de revisão

Rever se o cliente tiver GKE como padrão, ou se a quantidade de serviços crescer.
