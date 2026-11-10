# ADR-0018: Reconstrução do saldo

- **Status:** aceita
- **Data:** 2026-11-06
- **Registro:** escrito depois da implementação, para documentar uma escolha que já estava no código

## Contexto

O consolidado é derivado e precisa poder ser refeito depois de uma perda ou de um defeito. O princípio P3 proíbe que um serviço leia o banco do outro.

## Decisão

Parar o consumidor, zerar o consolidado com `--reset`, recolocar os lançamentos na outbox com `--republish`, religar o consumidor e conferir com `reconcile.sh` quando a fila esvaziar. O `eventId` é o id do lançamento, então o reenvio é seguro.

## Opções

<!-- radar:start -->
![Radar de Reconstrução do saldo](radar/ADR-0018.svg)

| Critério | Peso | **Reenvio dos lançamentos pela outbox** | Restaurar backup do consolidado e reprocessar o que faltou | Leitura direta do banco de lançamentos | Replay na mensageria |
|---|---:|---:|---:|---:|---:|
| Alinhamento com o negócio | 4 | 5 | 4 | Veto | 3 |
| Aderência ao problema | 3 | 3 | 4 | 5 | Veto |
| Confiabilidade | 3 | 4 | 4 | 3 | 3 |
| Escalabilidade | 2 | 2 | 4 | 3 | 4 |
| Operabilidade | 2 | 4 | 3 | 5 | 4 |
| Segurança | 2 | 4 | 4 | 3 | 4 |
| Simplicidade | 2 | 4 | 3 | 5 | 4 |
| Economia | 1 | 5 | 5 | 5 | 3 |
| **Resultado** | | **3,89** | **3,84** | Eliminada | Eliminada |

As perguntas e a evidência de cada nota estão na [avaliação dos radares](../21-avaliacao-dos-radares.md#adr-0018).
<!-- radar:end -->

## Leitura do radar

Ler o banco de lançamentos é o caminho mais rápido, mas quebra o P3 e cria um segundo modo de calcular o saldo. O replay na mensageria depende da retenção do tópico e não alcança todo o histórico. O reenvio é mais lento, mas usa o mesmo caminho e o mesmo código da operação normal, e por isso cada lançamento registrado exercita esse caminho. Restaurar o backup do consolidado fica 0,05 atrás e empata em confiabilidade. Quem decide é o critério de negócio: sem ele, o backup venceria. O `reconcile.sh` não contraria o P3: é uma ferramenta do operador, fora dos serviços, que lê os dois bancos para comparar totais, e não para calcular saldos.

## Consequências

**Positivas**

- Um único caminho de cálculo do saldo
- Funciona com todo o histórico

**Negativas**

- A reconstrução é completa, não por comerciante
- O consolidado fica incompleto até a fila terminar, e a API responde saldo parcial com status 200. Ver a V-08 em [vulnerabilidades](../18-vulnerabilidades.md)
- O reenvio grava em toda a outbox e concorre com as escritas normais pelo publicador
- Com o cache ligado, o reset depende do Redis

## Gatilho de revisão

Rever quando a base for grande demais para a reconstrução completa. A evolução prevista é a reconstrução por comerciante.
