# ADR-0012: Acesso a dados

- **Status:** aceita
- **Data:** 2026-11-06
- **Registro:** escrito depois da implementação, para documentar uma escolha que já estava no código

## Contexto

Cada serviço tem poucas consultas, e as mais importantes usam recursos próprios do PostgreSQL: `FOR UPDATE SKIP LOCKED` na outbox e `ON CONFLICT` na idempotência e no agregado.

## Decisão

Usar Dapper sobre Npgsql, com SQL escrito à mão dentro dos adaptadores de infraestrutura. O domínio só conhece as portas.

## Opções

<!-- radar:start -->
![Radar de Acesso a dados](radar/ADR-0012.svg)

| Pilar | **Npgsql com SQL** | EF Core | Dapper |
|---|---:|---:|---:|
| Segurança | 4 | 4 | 4 |
| Desempenho | 5 | 3 | 5 |
| Escalabilidade | 4 | 4 | 4 |
| Confiabilidade | 4 | 4 | 4 |
| Custo | 4 | 4 | 4 |
| Simplicidade | 3 | 4 | 4 |
| Operabilidade | 4 | 3 | 4 |
<!-- radar:end -->

## Leitura do radar

As notas ficaram próximas, e nenhuma opção compromete um requisito. Dapper reduz o código de leitura e mantém visível o SQL que sustenta as garantias, ao custo de uma dependência a mais. EF Core ajudaria num modelo grande, mas aqui seus benefícios são pequenos, e as consultas críticas precisariam de SQL de qualquer forma.

## Consequências

**Positivas**

- O SQL que sustenta as garantias fica à vista na revisão
- Sem camada entre o código e o banco para ajustar ou diagnosticar

**Negativas**

- O mapeamento de colunas só falha em execução, por isso há testes de integração com PostgreSQL real
- Mais uma dependência, e tipos como `DateOnly` precisam de um conversor

## Revisão de 2026-11-06

A primeira versão usava Npgsql direto. A reavaliação às cegas favoreceu o Dapper, que elimina o mapeamento manual e mantém o SQL explícito, e a troca foi feita.

O radar acima mostra a avaliação original. A nova avaliação ainda não foi oficializada.

## Gatilho de revisão

Rever se o número de consultas crescer muito ou se o mapeamento manual passar a gerar defeitos. O próximo passo é um ORM, sem mudar as portas.
