# ADR-0001: Estilo arquitetural

- **Status:** aceita
- **Data:** 2026-11-05

## Contexto

O comerciante precisa registrar lançamentos mesmo se o relatório cair. O sistema tem duas cargas diferentes: escrita transacional e leitura em picos de 50 req/s.

## Decisão

Construir dois serviços independentes, Lançamentos e Consolidado, cada um com processo, banco e deploy próprios, integrados por eventos. Dentro de cada serviço, usar arquitetura hexagonal.

## Opções

<!-- radar:start -->
![Radar de Estilo arquitetural](radar/ADR-0001.svg)

| Pilar | **Dois serviços com eventos** | Monolito modular | Funções serverless |
|---|---:|---:|---:|
| Segurança | 4 | 4 | 4 |
| Desempenho | 4 | 5 | 3 |
| Escalabilidade | 5 | 3 | 5 |
| Confiabilidade | 5 | 2 | 4 |
| Custo | 3 | 5 | 4 |
| Simplicidade | 3 | 5 | 3 |
| Operabilidade | 3 | 4 | 4 |
<!-- radar:end -->

## Leitura do radar

Confiabilidade é o pilar decisivo, porque atende ao requisito central. O monolito modular ganha em simplicidade e custo, mas compartilha processo, banco e ciclo de deploy, então uma falha pode atingir os lançamentos. Funções serverless também isolam falhas, mas pioram a latência em partida a frio e dificultam a execução local.

SOA com barramento foi descartada sem radar: um barramento central é desproporcional para dois serviços.

## Consequências

**Positivas**

- Falhas e picos do consolidado não afetam os lançamentos
- Cada serviço escala e evolui separadamente

**Negativas**

- O saldo passa a ter consistência eventual
- Há mais componentes para operar e observar

## Gatilho de revisão

Rever se o negócio exigir saldo com consistência imediata, ou se novos contextos justificarem separar mais serviços.
