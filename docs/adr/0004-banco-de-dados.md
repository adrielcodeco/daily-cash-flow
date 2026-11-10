# ADR-0004: Banco de dados

- **Status:** aceita
- **Data:** 2026-11-05

## Contexto

Lançamentos exige transação entre o registro e a outbox. O consolidado agrega valores por dia. Os dados são financeiros, relacionais e de volume moderado.

## Decisão

Usar PostgreSQL em instâncias separadas por serviço, com Cloud SQL em alta disponibilidade regional no alvo.

## Opções

<!-- radar:start -->
![Radar de Banco de dados](radar/ADR-0004.svg)

| Critério | Peso | **Cloud SQL para PostgreSQL** | AlloyDB | Spanner | Firestore |
|---|---:|---:|---:|---:|---:|
| Alinhamento com o negócio | 4 | 5 | 4 | 5 | 4 |
| Aderência ao problema | 3 | 5 | 4 | 4 | 3 |
| Confiabilidade | 3 | 5 | 5 | 5 | 5 |
| Operabilidade | 3 | 3 | 4 | 4 | 4 |
| Segurança | 3 | 5 | 5 | 4 | 4 |
| Desempenho | 2 | 5 | 5 | 4 | 3 |
| Economia | 2 | 3 | 3 | 2 | 2 |
| Escalabilidade | 2 | 2 | 3 | 5 | 4 |
| Simplicidade | 2 | 4 | 4 | 2 | 2 |
| Portabilidade | 1 | 5 | 4 | 1 | 1 |
| **Resultado** | | **4,28** | **4,16** | **3,92** | **3,48** |

As perguntas e a evidência de cada nota estão na [avaliação dos radares](../21-avaliacao-dos-radares.md#adr-0004).
<!-- radar:end -->

## Leitura do radar

Em confiabilidade, as quatro opções empatam. Spanner tem a melhor escala, mas seu custo não se justifica para esse volume. Firestore escala bem, mas agregações por período e restrições relacionais ficariam na aplicação. AlloyDB empata com o Cloud SQL em simplicidade e economia e é melhor em operabilidade e escala, mas tem capacidade acima da necessidade atual. PostgreSQL no Cloud SQL oferece transações, restrições e SQL com a maior aderência ao problema. A margem sobre o AlloyDB vem do critério de negócio: sem ele, o AlloyDB venceria. A decisão, portanto, depende do julgamento de que a capacidade a mais não se paga hoje, e do preço do AlloyDB, ainda por confirmar.

## Consequências

**Positivas**

- Transações ACID sustentam a outbox e a idempotência
- O mesmo motor roda localmente e no alvo

**Negativas**

- A escrita escala verticalmente até haver particionamento
- Há duas instâncias para manter

## Decisão do arquiteto

Na avaliação às cegas, AlloyDB ficou 0,04 à frente. O autor reduziu a nota dele em Alinhamento de 5 para 4, porque a capacidade está acima da necessidade atual do negócio, conforme o princípio P6. O Cloud SQL atende às mesmas garantias. O preço do AlloyDB ainda precisa ser confirmado na calculadora do GCP.

## Gatilho de revisão

Rever se a escrita se aproximar do limite da instância após otimizações. O caminho é particionar por comerciante ou avaliar Spanner.
