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

Banco por comerciante dá o isolamento mais forte, mas o custo e a operação crescem com cada cliente. Esquema por comerciante melhora pouco e multiplica migrações. Tabelas compartilhadas têm a pior nota em segurança, porque o isolamento depende do código. Por isso, o RLS foi acrescentado depois, conforme a revisão abaixo.

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

O radar acima mostra a avaliação original. A nova avaliação ainda não foi oficializada.

## Gatilho de revisão

Rever se um cliente exigir isolamento físico por contrato ou regulação.
