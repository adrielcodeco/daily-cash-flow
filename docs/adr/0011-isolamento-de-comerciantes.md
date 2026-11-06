# ADR-0011: Isolamento entre comerciantes

- **Status:** aceita
- **Data:** 2026-11-06
- **Registro:** escrito depois da implementação, para documentar uma escolha que já estava no código

## Contexto

O sistema atende vários comerciantes. O dado de um nunca pode alcançar outro, e o custo por comerciante precisa ficar baixo, porque a base pode ter muitos clientes pequenos.

## Decisão

Usar tabelas compartilhadas, com `merchant_id` em toda linha e em toda consulta, e row-level security do PostgreSQL como segunda barreira. Cada transação da API define o comerciante, e sem isso o banco não devolve nenhuma linha. Registro de outro comerciante responde 404, para não revelar que ele existe.

As aplicações usam papéis próprios, sem superusuário. A API não tem permissão de alterar nem excluir lançamentos. Publicador e consumidor trabalham com todos os comerciantes, então usam um papel que ignora o RLS.

## Opções

<!-- radar:start -->
![Radar de Isolamento entre comerciantes](radar/ADR-0011.svg)

| Critério | Peso | **Tabelas compartilhadas com row-level security** | Tabelas compartilhadas com filtro no código | Esquema por comerciante | Banco por comerciante |
|---|---:|---:|---:|---:|---:|
| Alinhamento com o negócio | 4 | 5 | 3 | 3 | 3 |
| Operabilidade | 3 | 4 | 5 | 3 | 3 |
| Segurança | 3 | 3 | 2 | 3 | 3 |
| Aderência ao problema | 2 | 5 | 3 | 3 | 3 |
| Economia | 2 | 5 | 5 | 4 | 3 |
| Escalabilidade | 2 | 4 | 4 | 2 | 1 |
| Simplicidade | 2 | 4 | 5 | 4 | 4 |
| Desempenho | 1 | 5 | 5 | 3 | 2 |
| **Resultado** | | **4,32** | **3,79** | **3,11** | **2,84** |

As perguntas e a evidência de cada nota estão na [avaliação dos radares](../21-avaliacao-dos-radares.md#adr-0011).
<!-- radar:end -->

## Leitura do radar

Banco por comerciante dá o isolamento mais forte, mas o custo e a operação crescem com cada cliente. Esquema por comerciante melhora pouco e multiplica migrações. Tabelas compartilhadas só com filtro no código deixam o isolamento depender de cada consulta. Com RLS, o banco aplica o isolamento, e o modelo continua simples e barato.

## Consequências

**Positivas**

- Uma migração atende todos os comerciantes
- Custo não cresce com o número de clientes

**Negativas**

- Cada leitura precisa de uma transação para definir o comerciante, com idas a mais ao banco
- Publicador e consumidor ignoram o RLS, então dependem do código e do controle de acesso ao papel
- Um comerciante muito grande afeta os demais no mesmo banco

## Revisão de 2026-11-06

A decisão original deixava o isolamento só no código. A reavaliação às cegas favoreceu o RLS, porque esse modelo é fraco para uma instituição financeira, e o RLS foi adotado.

## Gatilho de revisão

Rever se um cliente exigir isolamento físico por contrato ou regulação.
