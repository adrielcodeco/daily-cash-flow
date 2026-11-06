# Matriz RACI

Cada atividade tem um único responsável final (A) e pelo menos um executor (R). Os papéis são funções e podem ser acumulados, conforme a estrutura do cliente.

| Sigla | Significado |
|---|---|
| R | Responsável: executa o trabalho |
| A | Aprovador: responde pelo resultado |
| C | Consultado: contribui antes da decisão |
| I | Informado: recebe o resultado |

| Papel | Sigla |
|---|---|
| Patrocinador do negócio | PN |
| Dono do produto | DP |
| Arquiteto de soluções | AS |
| Líder técnico | LT |
| Desenvolvimento | DV |
| Qualidade | QA |
| SRE e plataforma | SRE |
| Segurança da informação | SI |
| Dados e DBA | DBA |
| Conformidade e jurídico | CJ |

| Atividade | PN | DP | AS | LT | DV | QA | SRE | SI | DBA | CJ |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| Priorizar requisitos de negócio | A | R | C | C | I | I | I | I | I | C |
| Mapear domínios e capacidades | I | C | A/R | C | I | I | I | I | C | I |
| Definir requisitos não funcionais | I | C | A/R | C | I | C | C | C | C | C |
| Desenhar a arquitetura-alvo | I | C | A/R | C | C | I | C | C | C | I |
| Aprovar ADRs | I | I | A | R | C | I | C | C | C | I |
| Definir contratos de API e eventos | I | C | A | R | R | C | I | C | I | I |
| Implementar os serviços | I | I | C | A | R | C | I | I | I | I |
| Definir a estratégia de testes | I | I | C | A | R | R | C | I | I | I |
| Executar testes de carga e resiliência | I | I | C | A | C | R | R | I | I | I |
| Provisionar infraestrutura e rede | I | I | C | C | I | I | A/R | C | C | I |
| Definir controles de segurança | I | I | C | C | I | I | C | A/R | I | C |
| Modelar e manter os bancos | I | I | C | C | C | I | I | I | A/R | I |
| Definir retenção de dados | I | C | C | I | I | I | I | C | R | A |
| Avaliar conformidade regulatória | C | C | C | I | I | I | I | C | I | A/R |
| Liberar para produção | I | A | C | R | C | C | R | C | I | I |
| Operar e responder a incidentes | I | I | C | C | C | I | A/R | C | C | I |
| Planejar a recuperação de desastres | I | I | C | C | I | I | A/R | C | R | C |
| Estimar e acompanhar custos | A | C | R | I | I | I | R | I | I | I |
