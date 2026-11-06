# ADR-0019: Paginação de lançamentos

- **Status:** aceita
- **Data:** 2026-11-06
- **Registro:** escrito depois da implementação, para documentar uma escolha que já estava no código

## Contexto

A listagem de lançamentos de um dia pode ser grande, e novos lançamentos chegam enquanto o cliente percorre as páginas.

## Decisão

Paginar por cursor, ordenando por `id` dentro de comerciante e data, com o índice `ix_ledger_entry_merchant_date`. O limite padrão é 50 e o máximo é 200.

## Opções

<!-- radar:start -->
![Radar de Paginação de lançamentos](radar/ADR-0019.svg)

| Critério | Peso | **Keyset com cursor** | Offset e limite |
|---|---:|---:|---:|
| Alinhamento com o negócio | 4 | 5 | 3 |
| Aderência ao problema | 3 | 5 | 2 |
| Desempenho | 3 | 5 | 2 |
| Escalabilidade | 2 | 5 | 2 |
| Simplicidade | 2 | 4 | 5 |
| **Resultado** | | **4,86** | **2,71** |

As perguntas e a evidência de cada nota estão na [avaliação dos radares](../21-avaliacao-dos-radares.md#adr-0019).
<!-- radar:end -->

## Leitura do radar

Offset é mais simples e permite pular para qualquer página, mas fica mais lento nas páginas finais e repete ou perde itens quando a lista muda. Keyset custa o tempo constante de um índice e não permite pular páginas, o que não é necessário numa API.

## Consequências

**Positivas**

- Tempo de resposta estável em qualquer página
- Lista que muda não duplica nem omite itens já percorridos

**Negativas**

- Não é possível ir direto a uma página numerada
- A ordem é a do `id`, um UUID v7. Ela segue o momento do registro, e não um campo escolhido pelo cliente

## Gatilho de revisão

Rever se os clientes precisarem ordenar por outro campo. A chave do cursor passaria a incluir esse campo.
