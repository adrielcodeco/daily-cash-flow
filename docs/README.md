# Documentação de arquitetura

| Documento | Conteúdo |
|---|---|
| [Princípios e glossário](00-principios-e-glossario.md) | Regras que guiam as decisões e linguagem do domínio |
| [Domínios e capacidades](01-dominios-e-capacidades.md) | Capacidades, mapa de contextos e eventos |
| [Requisitos](02-requisitos.md) | Requisitos, premissas e cenários de qualidade |
| [Arquitetura-alvo](03-arquitetura-alvo.md) | C4, sequências, estados, dados, falhas e impactos |
| [Resultados de testes](09-resultados-de-testes.md) | Carga, resiliência, mutação e defeitos encontrados |
| [Rede](10-rede.md) | Topologia no GCP, sub-redes e regras de tráfego |
| [RACI](11-raci.md) | Responsabilidades por atividade |
| [ADRs](adr/README.md) | Decisões, com radar de trade-offs |
| [Contratos](contratos/README.md) | OpenAPI, AsyncAPI e esquema do evento |

## Implementado e proposto

| Situação | Itens |
|---|---|
| Será implementado e testado localmente | Serviços, outbox, consumidor, saldo, cache, autenticação, telemetria e dashboards |
| Proposto para o GCP, sem deploy | Cloud Run, Cloud SQL, Pub/Sub, Memorystore, Cloud Armor, balanceador e rede |
