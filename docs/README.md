# Documentação de arquitetura

| Documento | Conteúdo |
|---|---|
| [Princípios e glossário](00-principios-e-glossario.md) | Regras que guiam as decisões e linguagem do domínio |
| [Domínios e capacidades](01-dominios-e-capacidades.md) | Capacidades, mapa de contextos e eventos |
| [Requisitos](02-requisitos.md) | Requisitos, premissas e cenários de qualidade |
| [Arquitetura-alvo](03-arquitetura-alvo.md) | C4, sequências, estados, dados, falhas e impactos |
| [Arquitetura de transição](04-arquitetura-transicao.md) | Migração de um legado, em fases |
| [Custos](05-custos.md) | Estimativa de infraestrutura e licenças no GCP |
| [Observabilidade](06-observabilidade.md) | SLOs, métricas, alertas e capacidade |
| [Segurança](07-seguranca.md) | Ameaças, dados, integração e regulação |
| [Evoluções futuras](08-evolucoes-futuras.md) | Melhorias e seus gatilhos |
| [Resultados de testes](09-resultados-de-testes.md) | Carga, resiliência, mutação e defeitos encontrados |
| [Rede](10-rede.md) | Topologia no GCP, sub-redes e regras de tráfego |
| [RACI](11-raci.md) | Responsabilidades por atividade |
| [Operação](12-operacao.md) | Recuperação de desastres e runbooks |
| [Revisão Well-Architected](13-revisao-well-architected.md) | Autoavaliação pelos pilares do GCP |
| [Riscos](14-riscos.md) | Registro e mapa de exposição |
| [Registro de decisões](15-registro-de-decisoes.md) | Decisões menores, parâmetros e o que foi revisto |
| [ADRs](adr/README.md) | Decisões, com radar de trade-offs |
| [Contratos](contratos/README.md) | OpenAPI, AsyncAPI e esquema do evento |

## Implementado e proposto

| Situação | Itens |
|---|---|
| Implementado e testado localmente | Serviços, outbox, consumidor, saldo e cache |
| Previsto para o M6 | Autenticação, limite de taxa, telemetria e dashboards |
| Proposto para o GCP, sem deploy | Cloud Run, Cloud SQL, Pub/Sub, Memorystore, Cloud Armor, balanceador e rede |
