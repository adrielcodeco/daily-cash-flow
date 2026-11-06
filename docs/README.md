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
| [ROI e custo operacional](19-roi-e-custo-operacional.md) | Investimento, ponto de equilíbrio e retorno, com premissas editáveis |
| [Evoluções futuras](08-evolucoes-futuras.md) | Melhorias e seus gatilhos |
| [Resultados de testes](09-resultados-de-testes.md) | Carga, resiliência, mutação e defeitos encontrados |
| [Rede](10-rede.md) | Topologia no GCP, sub-redes e regras de tráfego |
| [RACI](11-raci.md) | Responsabilidades por atividade |
| [Operação](12-operacao.md) | Runbooks, implantação e manutenção |
| [Recuperação de desastres](17-recuperacao-de-desastres.md) | Plano, procedimentos, exercícios e lacunas |
| [Vulnerabilidades](18-vulnerabilidades.md) | Registro de fraquezas, evidências e correções |
| [Revisão Well-Architected](13-revisao-well-architected.md) | Autoavaliação pelos pilares do GCP |
| [Riscos](14-riscos.md) | Registro e mapa de exposição |
| [Resumo das decisões](16-resumo-de-decisoes.md) | Tudo o que ficou decidido, de todas as fontes, sem os trade-offs |
| [Registro de decisões](15-registro-de-decisoes.md) | Decisões menores, parâmetros e o que foi revisto |
| [ADRs](adr/README.md) | Decisões, com radar de trade-offs |
| [Método dos radares](20-metodo-dos-radares.md) | O que as notas significam, como foram dadas e seus limites |
| [Contratos](contratos/README.md) | OpenAPI, AsyncAPI e esquema do evento |

## Implementado e proposto

| Situação | Itens |
|---|---|
| Implementado e testado localmente | Serviços, outbox, consumidor, saldo, cache, autenticação, limite de taxa, telemetria, dashboards e alertas |
| Proposto para o GCP, sem deploy | Cloud Run, Cloud SQL, Pub/Sub, Memorystore, Cloud Armor, balanceador e rede |
