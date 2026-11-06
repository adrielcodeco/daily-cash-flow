# ADR-0020: Estilo da API HTTP

- **Status:** aceita
- **Data:** 2026-11-06
- **Registro:** escrito depois da implementação, para documentar uma escolha que já estava no código

## Contexto

As APIs têm poucas rotas, consumidas por clientes externos pelo gateway, e o contrato é publicado em OpenAPI.

## Decisão

Usar ASP.NET Core Minimal APIs, com REST e JSON, rotas em `/v1` e erros no formato Problem Details.

## Opções

<!-- radar:start -->
![Radar de Estilo da API HTTP](radar/ADR-0020.svg)

| Pilar | **Minimal APIs** | Controllers MVC | gRPC |
|---|---:|---:|---:|
| Segurança | 4 | 4 | 4 |
| Desempenho | 5 | 4 | 5 |
| Escalabilidade | 4 | 4 | 5 |
| Confiabilidade | 4 | 4 | 4 |
| Custo | 4 | 4 | 4 |
| Simplicidade | 5 | 3 | 2 |
| Operabilidade | 4 | 4 | 2 |
<!-- radar:end -->

## Leitura do radar

Controllers organizam bem APIs grandes, mas acrescentam estrutura para um punhado de rotas. gRPC tem melhor desempenho, mas dificulta o consumo externo e o gateway, e a medição mostrou que a latência não é o gargalo. Minimal APIs mantêm cada rota curta e próxima do caso de uso.

## Consequências

**Positivas**

- Pouco código entre a rota e o caso de uso
- Sem camada de controllers para manter

**Negativas**

- Com muitas rotas, a organização depende de convenção própria
- Parte do ecossistema ainda é pensada primeiro para controllers

## Gatilho de revisão

Rever se o número de rotas crescer muito, ou se surgir comunicação interna de alto volume, caso em que gRPC volta à mesa.
