# ADR-0007: Linguagem e plataforma

- **Status:** aceita
- **Data:** 2026-11-05

## Contexto

O PDF aceita a linguagem que o autor domina. O cliente mantém sua stack principal em C#.

## Decisão

Usar C# com .NET 10 LTS e ASP.NET Core.

## Opções

<!-- radar:start -->
![Radar de Linguagem e plataforma](radar/ADR-0007.svg)

| Pilar | **C# com .NET 10** | TypeScript com Node.js | Go |
|---|---:|---:|---:|
| Segurança | 4 | 3 | 4 |
| Desempenho | 4 | 3 | 5 |
| Escalabilidade | 4 | 3 | 4 |
| Confiabilidade | 4 | 3 | 4 |
| Custo | 3 | 3 | 4 |
| Simplicidade | 4 | 4 | 4 |
| Operabilidade | 5 | 3 | 3 |
<!-- radar:end -->

## Leitura do radar

Go tem o melhor desempenho e o menor consumo, mas a operabilidade pesa mais aqui: quem vai manter o sistema é uma equipe que já domina C#. O .NET moderno entrega bom desempenho, com suporte de longo prazo e bibliotecas maduras. TypeScript não traz vantagem para este caso.

## Consequências

**Positivas**

- Alinhamento com a equipe e o ecossistema do cliente
- Suporte de longo prazo e ferramentas maduras

**Negativas**

- Imagens e consumo de memória maiores que os de Go
- Partida a frio mais lenta, atenuada por instância mínima

## Gatilho de revisão

Rever se a empresa adotar outra plataforma padrão, ou se o custo de memória se tornar relevante.
