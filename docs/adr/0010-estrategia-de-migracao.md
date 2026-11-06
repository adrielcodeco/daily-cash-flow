# ADR-0010: Estratégia de migração

- **Status:** aceita
- **Data:** 2026-11-06

## Contexto

Em um cenário com legado, a mudança não pode interromper o registro de lançamentos nem gerar saldos diferentes entre os sistemas. O cenário de legado é uma premissa deste projeto.

## Decisão

Usar o padrão strangler fig, com um gateway de roteamento, comparação de saldos e migração por grupos de comerciantes. Cada comerciante escreve em um único sistema por vez.

## Opções

<!-- radar:start -->
![Radar de Estratégia de migração](radar/ADR-0010.svg)

| Pilar | **Strangler fig** | Migração de uma vez | Escrita dupla |
|---|---:|---:|---:|
| Segurança | 4 | 3 | 3 |
| Desempenho | 4 | 4 | 3 |
| Escalabilidade | 4 | 4 | 3 |
| Confiabilidade | 5 | 1 | 2 |
| Custo | 2 | 4 | 3 |
| Simplicidade | 3 | 4 | 2 |
| Operabilidade | 4 | 2 | 2 |
<!-- radar:end -->

## Leitura do radar

A migração de uma vez é a mais simples e a mais barata em tempo, mas concentra todo o risco em uma janela e tem retorno difícil. A escrita dupla parece segura, mas é o mesmo problema que a outbox resolve: duas gravações sem transação comum geram divergência. O strangler fig custa mais e dura mais, mas permite parar e voltar em cada fase, e esse é o pilar decisivo em um sistema financeiro.

## Consequências

**Positivas**

- Cada fase tem critério de avanço e plano de retorno
- Comerciantes migram aos poucos, o que limita o impacto de um erro

**Negativas**

- Dois sistemas convivem por mais tempo, com custo duplicado
- Gateway, CDC e comparador são componentes temporários que precisam ser construídos

## Gatilho de revisão

Rever se o legado real for pequeno o bastante para uma janela de manutenção aceita pelo negócio.
