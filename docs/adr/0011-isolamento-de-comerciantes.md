# ADR-0011: Isolamento entre comerciantes

- **Status:** aceita
- **Data:** 2026-11-06
- **Registro:** escrito depois da implementação, para documentar uma escolha que já estava no código

## Contexto

O sistema atende vários comerciantes. O dado de um nunca pode alcançar outro, e o custo por comerciante precisa ficar baixo, porque a base pode ter muitos clientes pequenos.

## Decisão

Usar tabelas compartilhadas, com `merchant_id` em toda linha e em toda consulta. Registro de outro comerciante responde 404, para não revelar que ele existe.

## Opções

<!-- radar:start -->
![Radar de Isolamento entre comerciantes](radar/ADR-0011.svg)

| Pilar | **Tabelas compartilhadas** | Esquema por comerciante | Banco por comerciante |
|---|---:|---:|---:|
| Segurança | 3 | 4 | 5 |
| Desempenho | 5 | 4 | 4 |
| Escalabilidade | 5 | 3 | 2 |
| Confiabilidade | 4 | 4 | 5 |
| Custo | 5 | 3 | 1 |
| Simplicidade | 5 | 2 | 1 |
| Operabilidade | 4 | 2 | 1 |
<!-- radar:end -->

## Leitura do radar

Banco por comerciante dá o isolamento mais forte, mas o custo e a operação crescem com cada cliente. Esquema por comerciante melhora pouco e multiplica migrações. Tabelas compartilhadas têm a pior nota em segurança, porque o isolamento depende do código. Aceitamos esse risco com testes de isolamento e com a doutrina `product/merchant-isolation.doctrine.md`.

## Consequências

**Positivas**

- Uma migração atende todos os comerciantes
- Custo não cresce com o número de clientes

**Negativas**

- Uma consulta sem filtro exporia dados de outros comerciantes
- Um comerciante muito grande afeta os demais no mesmo banco

## Gatilho de revisão

Rever se um cliente exigir isolamento físico por contrato ou regulação. Row-level security do PostgreSQL é o reforço mais barato antes de separar bancos.
