# Arquitetura-alvo

Os diagramas seguem o modelo C4. As decisões estão nos [ADRs](adr/README.md).

## C4 nível 1: contexto

```mermaid
flowchart TB
    COM["Comerciante<br/>Registra movimentos e consulta o saldo"]
    SIS["Sistema de Fluxo de Caixa<br/>Lançamentos e saldo diário"]
    IDP["Provedor de identidade<br/>Autentica e emite tokens"]
    FUT["Sistemas futuros<br/>Conciliação e fiscal"]
    COM -- "HTTPS, JSON" --> SIS
    COM -- "OIDC" --> IDP
    SIS -- "Valida tokens" --> IDP
    SIS -. "Eventos" .-> FUT
```

## C4 nível 2: contêineres

```mermaid
flowchart TB
    COM[Comerciante]
    subgraph SIS[Sistema de Fluxo de Caixa]
        GW["Gateway<br/>Cloud Load Balancing, Cloud Armor, API Gateway"]
        subgraph LS[Serviço Lançamentos]
            LA["API de Lançamentos<br/>ASP.NET Core"]
            LP["Publicador da outbox<br/>Worker .NET"]
            LD[("Banco de lançamentos<br/>PostgreSQL")]
        end
        MQ[["Mensageria<br/>Pub/Sub"]]
        subgraph CS[Serviço Consolidado]
            CC["Consumidor<br/>Worker .NET"]
            CA["API do Consolidado<br/>ASP.NET Core"]
            CD[("Banco do consolidado<br/>PostgreSQL")]
            CR[("Cache opcional<br/>Redis")]
        end
    end
    COM -- "HTTPS" --> GW
    GW -- "Escrita e consulta" --> LA
    GW -- "Saldo" --> CA
    LA -- "Lançamento e outbox na mesma transação" --> LD
    LP -- "Lê pendentes" --> LD
    LP -- "Publica evento" --> MQ
    MQ -- "Entrega at-least-once" --> CC
    CC -- "Atualiza agregado" --> CD
    CC -- "Invalida versão" --> CR
    CA -- "Consulta primeiro" --> CR
    CA -- "Consulta em caso de falta" --> CD
```

## C4 nível 3: componentes de Lançamentos

```mermaid
flowchart LR
    subgraph ENT[Entrada]
        EP[Endpoints HTTP]
        AU[Autenticação e escopos]
    end
    subgraph APP[Aplicação]
        RG[Registrar lançamento]
        ES[Estornar lançamento]
        CO[Consultar lançamentos]
    end
    subgraph DOM[Domínio]
        LC[Lançamento]
        RN[Regras de negócio]
    end
    subgraph ADP[Adaptadores]
        RP[Repositório]
        ID[Armazém de idempotência]
        OB[Outbox]
        PB[Publicador]
    end
    EP --> AU --> RG & ES & CO
    RG & ES --> LC --> RN
    RG & ES --> RP & ID & OB
    CO --> RP
    PB --> OB
```

## C4 nível 3: componentes do Consolidado

```mermaid
flowchart LR
    subgraph ENT[Entrada]
        EP[Endpoints HTTP]
        SB[Assinante de eventos]
    end
    subgraph APP[Aplicação]
        AP[Aplicar evento]
        CS[Consultar saldo]
        RC[Reconstruir consolidado]
    end
    subgraph DOM[Domínio]
        SD[Saldo diário]
    end
    subgraph ADP[Adaptadores]
        RP[Repositório de saldo]
        EV[Registro de eventos processados]
        CH[Cache]
    end
    SB --> AP --> SD
    AP --> RP & EV & CH
    EP --> CS --> CH & RP
    RC --> RP
```

## C4 de implantação

```mermaid
flowchart TB
    NET((Internet))
    subgraph GCP["GCP: southamerica-east1"]
        LB["Cloud Load Balancing e Cloud Armor"]
        subgraph RUN[Cloud Run]
            A1[ledger-api: 2 a 10 instâncias]
            A2[outbox-publisher: 1 a 2 instâncias]
            A3[consolidation-api: 2 a 10 instâncias]
            A4[consolidation-consumer: 1 a 5 instâncias]
        end
        subgraph DADOS[Dados gerenciados]
            S1[("Cloud SQL lançamentos: alta disponibilidade regional")]
            S2[("Cloud SQL consolidado: alta disponibilidade regional")]
            RD[("Memorystore for Redis<br/>opcional")]
        end
        PS[["Pub/Sub e tópico de mensagens mortas"]]
        OBS["Cloud Logging, Monitoring e Trace"]
        SM["Secret Manager e Cloud KMS"]
    end
    NET --> LB --> A1 & A3
    A1 --> S1
    A2 --> S1
    A2 --> PS --> A4
    A4 --> S2
    A4 --> RD
    A3 --> RD
    A3 --> S2
    RUN -.-> OBS
    RUN -.-> SM
```

A rede está em [10-rede.md](10-rede.md).

## Sequências

### Registrar lançamento

```mermaid
sequenceDiagram
    actor C as Comerciante
    participant A as API de Lançamentos
    participant D as Banco de lançamentos
    participant P as Publicador
    participant M as Mensageria
    C->>A: POST /v1/entries com Idempotency-Key
    A->>A: Valida token e regras
    A->>D: Transação: idempotência, lançamento e outbox
    D-->>A: Confirmado
    A-->>C: 201 Created
    P->>D: Lê eventos pendentes
    P->>M: Publica evento
    M-->>P: Confirma recebimento
    P->>D: Marca como publicado
```

### Consolidar evento

```mermaid
sequenceDiagram
    participant M as Mensageria
    participant K as Consumidor
    participant D as Banco do consolidado
    participant R as Cache
    M->>K: Entrega evento
    K->>D: Inicia transação
    K->>D: Registra eventId
    alt Evento novo
        K->>D: Soma no agregado do dia
        K->>D: Confirma transação
        K->>R: Muda a versão do comerciante
    else Evento repetido
        K->>D: Desfaz a transação
    end
    K-->>M: Confirma processamento
```

### Consultar saldo

```mermaid
sequenceDiagram
    actor C as Comerciante
    participant A as API do Consolidado
    participant R as Cache
    participant D as Banco do consolidado
    C->>A: GET /v1/balances/daily?date=2026-11-05
    A->>R: Busca saldo da versão atual
    alt Encontrou
        R-->>A: Saldo
    else Não encontrou ou cache fora do ar
        A->>D: Calcula abertura e dia
        D-->>A: Saldo
        A--)R: Guarda com TTL curto
    end
    A-->>C: 200 com saldo e updatedThrough
```

### Consolidado fora do ar

```mermaid
sequenceDiagram
    actor C as Comerciante
    participant A as API de Lançamentos
    participant M as Mensageria
    participant K as Consumidor
    Note over K: Fora do ar
    C->>A: POST /v1/entries
    A-->>C: 201 Created
    A--)M: Evento publicado pelo publicador
    Note over M: Evento retido
    Note over K: Serviço volta
    M->>K: Entrega os eventos retidos
    Note over K: Saldo atualizado
```

## Estados

### Lançamento

```mermaid
stateDiagram-v2
    [*] --> Ativo: registrado
    Ativo --> Estornado: estorno registrado
    Estornado --> [*]
```

O estado é derivado. O registro original nunca muda, e "Estornado" significa que existe um lançamento com `reversal_of` apontando para ele.

### Evento na outbox

```mermaid
stateDiagram-v2
    [*] --> Pendente: gravado com o lançamento
    Pendente --> Publicado: broker confirmou
    Pendente --> Pendente: falha e nova tentativa
    Publicado --> [*]: removido após a retenção
```

## Modelo de dados

```mermaid
erDiagram
    LEDGER_ENTRY {
        uuid id PK
        uuid merchant_id
        text entry_type
        bigint amount_cents
        date accounting_date
        text description
        uuid reversal_of FK
        timestamptz created_at
    }
    OUTBOX {
        uuid id PK
        text type
        jsonb payload
        timestamptz created_at
        timestamptz published_at
    }
    IDEMPOTENCY_KEY {
        uuid merchant_id PK
        text key PK
        text request_hash
        uuid entry_id FK
        timestamptz created_at
    }
    DAILY_BALANCE {
        uuid merchant_id PK
        date date PK
        bigint credits_cents
        bigint debits_cents
        timestamptz updated_at
    }
    PROCESSED_EVENT {
        uuid event_id PK
        timestamptz processed_at
    }
    LEDGER_ENTRY |o--o| LEDGER_ENTRY : reverses
```

`LEDGER_ENTRY`, `OUTBOX` e `IDEMPOTENCY_KEY` ficam no banco de lançamentos. `DAILY_BALANCE` e `PROCESSED_EVENT` ficam no banco do consolidado. Um índice único em `reversal_of` garante RN-05 mesmo com requisições concorrentes.

## Tratamento de falhas

| Se falhar | Efeito | Recuperação |
|---|---|---|
| Consolidado | Lançamentos segue normal, e os eventos ficam retidos | O serviço volta e processa a fila |
| Mensageria | Lançamentos segue normal, e os eventos ficam na outbox | O publicador retoma o envio |
| Publicador | Lançamentos segue normal, e a outbox cresce | Nova instância retoma do pendente |
| Banco de lançamentos | Escrita indisponível, e o saldo calculado segue consultável | Failover do Cloud SQL |
| Banco do consolidado | Consultas sem cache falham, e os eventos ficam retidos | Failover e processamento da fila |
| Cache | A consulta vai ao banco, com latência maior | O cache se preenche com o uso |
| Evento inválido | Vai para o tópico de mensagens mortas após as retentativas | Alerta, correção e reenvio |
| Provedor de identidade | Novos tokens falham, e os válidos seguem aceitos até expirar | Failover do provedor |

## Impacto por área

| Área | Impacto |
|---|---|
| Dados | Duas bases independentes, retenção definida e reconstrução a partir dos lançamentos |
| Segurança | OIDC, escopos, TLS, criptografia em repouso, auditoria e WAF |
| Infraestrutura | Serviços gerenciados no GCP, rede privada e alta disponibilidade regional |
| Soluções | Contrato de evento aberto para novos consumidores, como conciliação |
| Software | Duas bases de código em C#, arquitetura hexagonal e testes automatizados |

## Riscos de arquitetura

| Risco | Mitigação |
|---|---|
| Defasagem do saldo gerar decisão errada | Informar `updatedThrough` e monitorar o atraso |
| Agregado divergir dos lançamentos | Reconstrução e reconciliação periódica. Como `eventId` é a identidade do lançamento, reenviar eventos não conta valores duas vezes |
| Outbox crescer sem controle | Alerta de idade do evento mais antigo e limpeza |
| Mensagens mortas ficarem sem tratamento | Alerta e runbook de reenvio |
| Fila acumulada esgotar as conexões do banco ao voltar de uma queda | Limite de 16 eventos em processamento e pool de conexões por processo. Encontrado no teste de resiliência |
| Evento publicado antes de existir assinatura ser descartado pelo Pub/Sub | Provisionar tópico e assinatura antes do publicador, como etapa própria |
