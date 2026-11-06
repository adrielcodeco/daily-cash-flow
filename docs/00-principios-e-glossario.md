# Princípios e glossário

## Princípios de arquitetura

| ID | Princípio | Implicação |
|---|---|---|
| P1 | Registrar um lançamento é a operação mais crítica | Nada fora de Lançamentos pode impedir uma escrita |
| P2 | Fatos financeiros são imutáveis | Correção é um novo lançamento, por estorno |
| P3 | Cada serviço é dono dos seus dados | Nenhum serviço lê o banco de outro |
| P4 | Toda entrega pode repetir | Escritas e consumidores são idempotentes |
| P5 | A defasagem deve ser visível | O saldo informa até quando está atualizado |
| P6 | A solução acompanha o tamanho do problema | Cada componente precisa justificar seu custo |
| P7 | Afirmações de qualidade precisam de prova | Cada meta tem medição ou teste |
| P8 | O domínio não conhece a infraestrutura | Banco e mensageria ficam atrás de portas |
| P9 | Documentação em pt-BR e código em en-US | O glossário liga cada termo de negócio ao nome usado no código |
| P10 | Toda decisão e todo trade-off ficam registrados | Cada escolha entra no mesmo commit, em um ADR ou no [registro de decisões](15-registro-de-decisoes.md) |

## Glossário

| Termo | No código | Definição |
|---|---|---|
| Comerciante | `Merchant` | Quem registra o fluxo de caixa e dono dos dados |
| Lançamento | `LedgerEntry` | Registro imutável de uma entrada ou saída de dinheiro |
| Crédito | `Credit` | Lançamento que aumenta o saldo |
| Débito | `Debit` | Lançamento que reduz o saldo |
| Estorno | `Reversal` | Lançamento de natureza inversa que anula outro |
| Data de competência | `AccountingDate` | Dia a que o lançamento pertence, no fuso `America/Sao_Paulo` |
| Lançamento retroativo | `BackdatedEntry` | Lançamento com data de competência anterior à de hoje |
| Saldo de abertura | `OpeningBalance` | Soma de créditos menos débitos de todos os dias anteriores |
| Saldo de fechamento | `ClosingBalance` | Abertura + créditos do dia − débitos do dia |
| Consolidado | `DailyBalance` | Visão por dia dos créditos, débitos e saldos |
| Defasagem | `Lag` | Tempo entre o registro do lançamento e sua presença no saldo |
| Outbox | `Outbox` | Tabela onde o evento é gravado junto com o lançamento |

## Nomes técnicos

| Na documentação | No código |
|---|---|
| Serviço de Lançamentos | `services/ledger`, com `ledger-api` e `outbox-publisher` |
| Serviço do Consolidado | `services/consolidation`, com `consolidation-api` e `consolidation-consumer` |
| Evento de lançamento registrado | `entry.recorded` |
| Rotas de lançamentos | `/v1/entries` |
| Rotas de saldo | `/v1/balances` |
