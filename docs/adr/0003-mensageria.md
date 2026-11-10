# ADR-0003: Mensageria

- **Status:** aceita
- **Data:** 2026-11-05

## Contexto

O alvo é GCP. A mensageria precisa de durabilidade, retentativas e fila de mensagens mortas, com pouca operação para a equipe.

## Decisão

Usar Pub/Sub, com tópico de mensagens mortas. O ambiente local usa o emulador. O RabbitMQ era o plano B e foi descartado depois que o emulador validou a entrega e as mensagens mortas.

## Opções

<!-- radar:start -->
![Radar de Mensageria](radar/ADR-0003.svg)

| Critério | Peso | **Pub/Sub** | Kafka gerenciado | RabbitMQ autogerenciado |
|---|---:|---:|---:|---:|
| Alinhamento com o negócio | 4 | 5 | 5 | 4 |
| Confiabilidade | 3 | 5 | 5 | 4 |
| Operabilidade | 3 | 4 | 4 | 2 |
| Aderência ao problema | 2 | 3 | 2 | 3 |
| Economia | 2 | 3 | 3 | 2 |
| Escalabilidade | 2 | 5 | 4 | 2 |
| Simplicidade | 2 | 4 | 3 | 4 |
| Portabilidade | 1 | 2 | 5 | 5 |
| Segurança | 2 | 5 | 5 | 2 |
| **Resultado** | | **4,24** | **4,10** | **3,10** |

As perguntas e a evidência de cada nota estão na [avaliação dos radares](../21-avaliacao-dos-radares.md#adr-0003).
<!-- radar:end -->

## Leitura do radar

Pub/Sub e Kafka gerenciado empatam em confiabilidade, operabilidade e economia. Pub/Sub vence em escalabilidade, simplicidade e aderência ao problema, e Kafka vence em portabilidade e permite reprocessar histórico, o que esse volume não pede. RabbitMQ exigiria operar um cluster próprio, o que explica a nota mais baixa em operabilidade. Em segurança, Pub/Sub e Kafka gerenciado empatam, com acesso por IAM, e RabbitMQ fica atrás, com usuários próprios fora do IAM e VMs a proteger.

## Consequências

**Positivas**

- Sem cluster para operar, com alta disponibilidade gerenciada
- Permissões por tópico e assinatura usando IAM

**Negativas**

- A ordem não é garantida por padrão, então o consumidor não pode depender dela
- O teste local usa emulador, que pode diferir do serviço real. As mensagens mortas foram validadas no emulador, com 5 tentativas antes do desvio
- Mensagens enviadas a um tópico sem assinatura são descartadas, então a assinatura precisa ser provisionada antes da primeira publicação

## Gatilho de revisão

Rever se surgirem vários consumidores que precisem reprocessar histórico longo. Nesse caso, Kafka volta à mesa.
