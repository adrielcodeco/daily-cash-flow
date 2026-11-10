# Resumo das decisões

Lista de tudo o que ficou decidido, com 368 itens, sem os trade-offs. Os motivos estão nos [ADRs](adr/README.md) e no [registro de decisões](15-registro-de-decisoes.md).

Este arquivo é gerado. Para atualizar, edite as fontes e rode `node tools/decisions/generate.mjs`.

## Princípios

| ID | Decisão |
|---|---|
| P1 | Registrar um lançamento é a operação mais crítica |
| P2 | Fatos financeiros são imutáveis |
| P3 | Cada serviço é dono dos seus dados |
| P4 | Toda entrega pode repetir |
| P5 | A defasagem deve ser visível |
| P6 | A solução acompanha o tamanho do problema |
| P7 | Afirmações de qualidade precisam de prova |
| P8 | O domínio não conhece a infraestrutura |
| P9 | Documentação em pt-BR e código em en-US |
| P10 | Toda decisão e todo trade-off ficam registrados |

## Premissas

| ID | Decisão |
|---|---|
| PRE-01 | As 50 req/s são consultas ao relatório. A arquitetura também suporta 50 eventos por segundo |
| PRE-02 | Os 5% de perda valem para consultas. Lançamentos confirmados têm perda zero |
| PRE-03 | O sistema atende vários comerciantes |
| PRE-04 | Moeda única, BRL, em centavos inteiros |
| PRE-05 | O dia é a data de competência, no fuso `America/Sao_Paulo` |
| PRE-06 | Lançamentos são imutáveis |
| PRE-07 | Lançamentos retroativos são permitidos |
| PRE-08 | O saldo acumula os dias anteriores |
| PRE-09 | O legado da arquitetura de transição é um cenário assumido: sistema único com banco compartilhado |
| PRE-10 | A arquitetura-alvo roda no GCP, em `southamerica-east1` |
| PRE-11 | A solução considera exigências de uma instituição financeira. A aplicabilidade exata deve ser validada com a área de conformidade |
| PRE-12 | A operação fica com uma equipe reduzida, sem especialista dedicado em infraestrutura. O desafio não define isso; é uma premissa assumida |

## Metas de qualidade

| ID | Atributo | Meta | Origem |
|---|---|---|---|
| RNF-01 | Independência | Lançamentos segue operando com o consolidado fora do ar | PDF |
| RNF-02 | Vazão | 50 req/s no consolidado | PDF |
| RNF-03 | Perda de consultas | No máximo 5%, ou seja, pelo menos 47,5 req/s com sucesso | PDF |
| RNF-04 | Durabilidade | Nenhum lançamento confirmado é perdido, exceto na janela de RPO de um desastre | Proposta |
| RNF-05 | Disponibilidade | 99,9% ao mês em Lançamentos, cerca de 43 minutos de parada | Proposta |
| RNF-06 | Latência de escrita | p95 de até 300 ms | Proposta |
| RNF-07 | Latência de leitura | p95 de até 200 ms | Proposta |
| RNF-08 | Defasagem | p95 de até 5 s em operação normal | Proposta |
| RNF-09 | Recuperação | RTO de 1 hora e RPO de 5 minutos em desastre regional | Proposta |
| RNF-10 | Rastreabilidade | Um lançamento pode ser seguido até o saldo | Proposta |

## Fora de escopo

| Item |
|---|
| Interface web e exportação de relatórios |
| Múltiplas moedas e contas por comerciante |
| Integração bancária e conciliação |
| Deploy real em nuvem |

## Deixado para depois

| Evolução | Quando fazer |
|---|---|
| Infraestrutura como código com Terraform e deploy no GCP | Decisão de ir para produção |
| Interface web | Comerciantes sem integração por API |
| Exportação do relatório em CSV ou PDF | Pedido de contabilidade ou fiscal |
| Fotografias mensais do saldo | Abertura acima da meta de latência |
| Particionamento por comerciante | Banco de lançamentos perto do limite de escrita |
| Kafka no lugar de Pub/Sub | Vários consumidores precisando reprocessar histórico longo |
| CDC no lugar do publicador da outbox | Leitura da outbox virar gargalo |
| GKE no lugar do Cloud Run | Padrão da empresa, ou muitos serviços novos |
| Reconstrução por comerciante | Base grande demais para uma reconstrução completa |
| Reconciliação automática | Antes de produção |
| Limpeza de outbox, chaves e eventos processados | Antes de produção |
| Testes de contrato | Mais de um consumidor do evento |
| Testes de caos | Primeiro serviço em produção |
| Múltiplas moedas e contas | Novo segmento de cliente |
| mTLS e perfil FAPI | Integração regulada de alto risco |
| Conciliação bancária | Assinatura do evento por um novo contexto, sem mudar Lançamentos |

## Arquitetura

| ADR | Tema | Decisão |
|---|---|---|
| [ADR-0001](adr/0001-estilo-arquitetural.md) | Estilo arquitetural | Dois serviços orientados a eventos |
| [ADR-0002](adr/0002-integracao-entre-servicos.md) | Integração entre serviços | Outbox e mensageria |
| [ADR-0003](adr/0003-mensageria.md) | Mensageria | Pub/Sub |
| [ADR-0004](adr/0004-banco-de-dados.md) | Banco de dados | Cloud SQL para PostgreSQL |
| [ADR-0005](adr/0005-modelo-de-leitura.md) | Modelo de leitura do saldo | Agregado diário, com cache opcional por rota ou tabela |
| [ADR-0006](adr/0006-computacao.md) | Plataforma de computação | Cloud Run |
| [ADR-0007](adr/0007-linguagem.md) | Linguagem e plataforma | C# com .NET 10 |
| [ADR-0008](adr/0008-autenticacao-e-autorizacao.md) | Autenticação e autorização | OIDC e JWT, validados em cada serviço |
| [ADR-0009](adr/0009-observabilidade.md) | Observabilidade | OpenTelemetry |
| [ADR-0010](adr/0010-estrategia-de-migracao.md) | Estratégia de migração | Strangler fig |
| [ADR-0011](adr/0011-isolamento-de-comerciantes.md) | Isolamento entre comerciantes | Tabelas compartilhadas com row-level security |
| [ADR-0012](adr/0012-acesso-a-dados.md) | Acesso a dados | Dapper com SQL explícito |
| [ADR-0013](adr/0013-migracoes-de-esquema.md) | Migrações de esquema | DbUp, em execução separada |
| [ADR-0014](adr/0014-separacao-de-processos.md) | Separação de processos | Quatro processos, dois por serviço |
| [ADR-0015](adr/0015-envio-da-outbox.md) | Envio da outbox | Consulta periódica com `SKIP LOCKED` |
| [ADR-0016](adr/0016-idempotencia-da-escrita.md) | Idempotência da escrita | `Idempotency-Key` guardada no banco |
| [ADR-0017](adr/0017-vazao-do-consumidor.md) | Vazão do consumidor | Limite fixo de eventos em processamento |
| [ADR-0018](adr/0018-reconstrucao-do-saldo.md) | Reconstrução do saldo | Reenvio pela outbox |
| [ADR-0019](adr/0019-paginacao.md) | Paginação de lançamentos | Keyset, com cursor |
| [ADR-0020](adr/0020-estilo-da-api-http.md) | Estilo da API HTTP | Minimal APIs com REST |
| [ADR-0021](adr/0021-estrategia-de-testes.md) | Estratégia de testes | Domínio, banco real e mutação |

## Domínio e dados

| ID | Decisão |
|---|---|
| D-01 | Dinheiro em `long`, em centavos |
| D-02 | Uma única moeda: BRL |
| D-03 | Data de competência informada por quem chama; o fuso `America/Sao_Paulo` define o hoje da regra de data futura |
| D-04 | Correção por estorno; lançamento é imutável |
| D-05 | Um lançamento só pode ser estornado uma vez, garantido por índice único |
| D-06 | Lançamento retroativo é permitido |
| D-07 | Abertura calculada na consulta |
| D-08 | Ids em UUID v7 |
| D-09 | `eventId` igual ao id do lançamento |
| D-10 | Evento carrega o necessário para o saldo, sem a descrição |
| D-11 | Tópico versionado no nome: `entries-recorded-v1` |
| D-12 | `processed_event` guarda todo evento aplicado |
| D-13 | Limpeza da outbox fica como proposta |

## Mensageria e fluxo

| ID | Decisão |
|---|---|
| D-14 | Lote de 100 eventos na outbox |
| D-15 | Espera de 200 ms quando o lote vem incompleto ou vazio |
| D-16 | Pausa fixa de 2 s no laço do publicador depois de uma falha; o evento que falhou segue a espera crescente da D-143 |
| D-17 | 5 tentativas antes das mensagens mortas |
| D-18 | Nova entrega com espera de 1 a 60 s |
| D-19 | Provisionar a assinatura antes da primeira publicação |
| D-20 | Lote de 500 lançamentos no reenvio |
| D-21 | 10 s para o consumidor encerrar |

## Cache

| ID | Decisão |
|---|---|
| D-22 | Versão por comerciante na chave do cache |
| D-23 | TTL de 60 s |
| D-24 | Ignorar o cache enquanto estiver desconectado |
| D-25 | Cache desligado por padrão, habilitável por rota ou por tabela |
| D-26 | Redis local sem persistência, com 64 MB e `allkeys-lru` |

## API e contratos

| ID | Decisão |
|---|---|
| D-27 | Versão na rota: `/v1` |
| D-28 | Registro de outro comerciante responde 404 |
| D-29 | `Idempotency-Key` de 8 a 128 caracteres |
| D-30 | Listagem com 50 itens por padrão e 200 no máximo |
| D-31 | Resposta informa `updatedThrough` |
| D-32 | `X-Merchant-Id` provisório até a autenticação |

## Ambiente, teste e entrega

| ID | Decisão |
|---|---|
| D-33 | Emulador do Pub/Sub no ambiente local |
| D-34 | Docker Compose, com build em contêiner |
| D-35 | Sem deploy real no GCP |
| D-36 | Portas locais 18081 e 18082 |
| D-37 | Pool local de 30 conexões, e orçamento menor no GCP |
| D-38 | PostgreSQL 17 e Redis 8 no ambiente local |
| D-39 | No cenário de pico, 50 leituras e 10 escritas por segundo; o teste de resiliência usa 50 escritas por segundo |
| D-40 | Mutação com corte em 70% e meta de 90% |
| D-41 | Mutação fora do CI |
| D-42 | Pacotes com versão central e sem vulnerabilidade conhecida |

## Método e documentação

| ID | Decisão |
|---|---|
| D-43 | Documentação em pt-BR, código em en-US |
| D-44 | Nota de radar sai de 4 perguntas de sim ou não, com critérios, pesos e opções por decisão |
| D-45 | Preços de lista, da região São Paulo quando a página de preços permite, sem desconto |
| D-46 | Volumes de custo são premissas, e só o pico vem do desafio |

## Escopo e método

| ID | Decisão |
|---|---|
| D-47 | Um repositório com os dois serviços |
| D-48 | Portas e adaptadores dentro de cada serviço |
| D-49 | CI no GitHub Actions, com testes e `smoke.sh` |
| D-50 | Cenários Gherkin em pt-BR |
| D-51 | Mensagens de commit em inglês |

## Domínio e contratos

| ID | Decisão |
|---|---|
| D-52 | Lançamentos e Consolidação são domínios principais, com código próprio |
| D-53 | Identidade, mensageria e observabilidade usam produto pronto ou padrão aberto |
| D-54 | Estorno usa o mesmo evento `entry.recorded`, com `reversalOf` |
| D-55 | Saldo de período limitado a 366 dias |
| D-56 | OpenAPI 3.1, AsyncAPI 3.0 e JSON Schema |
| D-57 | Erros no formato Problem Details, da RFC 9457 |
| D-58 | Consumidor ignora campos desconhecidos e aceita repetição e ordem diferente |

## Nuvem e rede

| ID | Decisão |
|---|---|
| D-59 | Uma região, com alta disponibilidade entre zonas |
| D-60 | Réplica fora do Brasil depende de avaliação regulatória |
| D-61 | Início com 6 instâncias sempre ativas de 1 vCPU e 0,5 GiB |
| D-62 | Início com Cloud SQL HA de 2 vCPU e 8 GiB nos dois bancos |
| D-63 | Início com Memorystore Basic, de 1 GiB |
| D-64 | Cloud Run com entrada `internal-and-cloud-load-balancing`: sem acesso direto pela internet |
| D-65 | Bancos e cache sem IP público |
| D-66 | APIs do Google por Private Service Connect |
| D-67 | Saída por Cloud NAT só para o provedor de identidade |
| D-68 | Todo fluxo não listado é negado |
| D-69 | Identidade própria por serviço, com acesso só ao próprio banco |
| D-70 | Limites de 10, 2, 10 e 5 instâncias, conforme o orçamento de conexões |
| D-71 | Faixas de rede `10.20.0.0` são exemplo |

## Segurança

| ID | Decisão |
|---|---|
| D-72 | OAuth2 client credentials, com tokens de até 15 minutos |
| D-73 | Escopos `entries.read`, `entries.write` e `balances.read` |
| D-74 | TLS 1.2 ou superior |
| D-75 | Limite de taxa por comerciante do token (por endereço, sem token), com resposta 429 |
| D-76 | mTLS e perfis como FAPI só em integrações críticas |
| D-77 | Uma assinatura e uma identidade por consumidor de eventos |
| D-78 | Keycloak como identidade local |

## Operação

| ID | Decisão |
|---|---|
| D-79 | Consultas com SLO de 99,5%, mais rígido que os 5% do desafio no pico |
| D-80 | Orçamento de erro esgotado para novas funcionalidades |
| D-81 | Exercício de restauração por trimestre |
| D-82 | Reconciliação automática fica como proposta |
| D-83 | Cada atividade da RACI tem um único aprovador |

## Migração

| ID | Decisão |
|---|---|
| D-84 | Migrar em 5 fases, começando pelas leituras |
| D-85 | CDC alimenta o novo consolidado na fase de espelho |
| D-86 | Avançar só com 30 dias de saldos iguais no espelho e 2 semanas de SLO nas leituras |
| D-87 | Migrar por grupos de comerciantes |
| D-88 | Cada comerciante escreve em um único sistema por vez |

## Recuperação de desastres

| ID | Decisão |
|---|---|
| D-89 | Perda zero não vale na janela de RPO de um desastre |
| D-90 | Erro humano é tratado em uma cópia, com correção pontual |
| D-91 | Depois de restaurar lançamentos, sempre reconstruir o consolidado |
| D-92 | Failover regional exige decisão de uma pessoa nomeada |
| D-93 | A escrita volta primeiro, e o saldo depois |
| D-94 | Cópia isolada dos lançamentos em outro projeto |

## Observabilidade

| ID | Decisão |
|---|---|
| D-95 | Telemetria pode ser descartada, e nunca bloqueia a escrita |
| D-96 | Guardar o contexto do traço na `outbox` |
| D-97 | `merchantId` fica fora dos atributos de métrica |
| D-98 | Amostragem de 10% dos traços na nuvem |
| D-99 | Requisição bem-sucedida não gera log operacional; escritas e estornos vão só para a categoria `Audit` |
| D-100 | `/health/ready` verifica só o próprio banco |
| D-101 | Alertas de orçamento com janela dupla, na proposta para produção; o ambiente local usa limites simples em `infra/prometheus/alerts.yaml` |
| D-102 | Métricas por 6 semanas, e traços e logs por 30 dias |

## Vulnerabilidades e finanças

| ID | Decisão |
|---|---|
| D-103 | Vulnerabilidade Crítica ou Alta bloqueia produção |
| D-104 | Prazos de 7, 30 e 90 dias por severidade |
| D-105 | IDs de vulnerabilidade não são reutilizados, e itens corrigidos ficam no registro |
| D-106 | Senha local do compose (V-17) e tempo no UUID v7 (V-18) são riscos aceitos |
| D-107 | ROI como modelo gerado por script, com premissas explícitas |
| D-108 | Horizonte de 36 meses, sem impostos e sem cancelamento de clientes |
| D-109 | Limite de R$ 1 bilhão por lançamento, no domínio e no banco |
| D-110 | Restrição do banco com `NOT VALID` |
| D-111 | Enum começa em 1, para o valor ausente nunca ser válido |
| D-112 | Consumidor recusa evento com tipo de lançamento desconhecido |
| D-113 | Um cliente por comerciante, com `merchant_id` fixo no token |
| D-114 | Limite de 100 requisições por segundo por comerciante, em cada instância |
| D-115 | Cada serviço valida o JWT, com tolerância de 30 s no horário |
| D-116 | Falta de escopo responde 403, e token inválido responde 401 |
| D-117 | Issuer fixo no Keycloak local |
| D-118 | Cada teste ponta a ponta e de carga cria seu comerciante no Keycloak |
| D-119 | Configuração de telemetria e segurança repetida em cada serviço |
| D-120 | Tempo para traços locais |
| D-121 | Logs na saída padrão, fora do coletor |
| D-122 | Auditoria como categoria de log |
| D-123 | Publicador lê o tamanho da outbox a cada 5 s |
| D-124 | Nível de log do ASP.NET definido no código |
| D-125 | Métricas expiram em 1 minuto no coletor |
| D-126 | `sbom.json` é gerado e não entra no Git |
| D-127 | Resultados de carga ficam no Git, sem `setup_data` |
| D-128 | README em pt-BR, com limites explícitos para produção |
| D-129 | Cache como camada na frente do repositório |
| D-130 | Sem alvos de cache, o sistema não conecta ao Redis |
| D-131 | Valor gravado na versão lida antes da consulta |
| D-132 | Papéis separados por processo: migração, API e worker |
| D-133 | O publicador de Lançamentos ignora o RLS, porque lê todos os comerciantes |
| D-134 | Leitura da API em transação, para definir o comerciante |
| D-135 | Opção com veto é eliminada |
| D-136 | C# é preferência de negócio, e não restrição |
| D-137 | Alinhamento com o negócio tem peso 4 em toda decisão |
| D-138 | Avaliação feita às cegas, com ajustes do arquiteto registrados |
| D-139 | Manter Cloud SQL, com AlloyDB 5 para 4 em Alinhamento |

## Revisão antes da publicação

| ID | Decisão |
|---|---|
| D-140 | Cada serviço só considera, da lista de alvos de cache, os nomes que conhece. A API e o consumidor do consolidado precisam receber os mesmos alvos, porque o consumidor invalida o cache do saldo; o compose passa um único `CACHE_TARGETS` a todos |
| D-141 | O reset do consolidado invalida o cache por uma época global, e falha se o Redis estiver fora |
| D-142 | Redis com `volatile-lru` |
| D-143 | O evento que falha espera 1, 2, 4… segundos, até 1 minuto, somados à pausa de 2 s do laço (D-16), antes de nova tentativa |
| D-144 | O que a mensageria confirmou é marcado em um único comando, mesmo com o publicador sendo encerrado |
| D-145 | A reconstrução recoloca um lote inteiro na outbox com um comando, por `unnest` |
| D-146 | O consumidor do consolidado grava sob row-level security, com o comerciante do evento em cada transação |
| D-147 | O hash de idempotência inclui o tamanho da descrição |
| D-148 | A resposta 404 entra na auditoria |
| D-149 | Token sem `merchant_id` recebe 403, e com valor inválido ou todo em zeros recebe 401 |
| D-150 | Partições do limite de requisições com prefixo de tipo; sem `merchant_id`, o limite é por endereço, e health não tem limite |
| D-151 | Toda recusa responde ProblemDetails, inclusive 401, 403 e corpo inválido |
| D-152 | Testes na fronteira HTTP com tokens assinados no teste e o handler JWT real, em ambiente de produção e com armazenamento em memória |
| D-153 | Reconciliação por script, chamada pelo smoke e ao fim da reconstrução |
| D-154 | Carga com dois anos de saldos diários por comerciante, inseridos direto na tabela |
| D-155 | A CI verifica a formatação e se os documentos gerados estão em dia |
| D-156 | Memorystore fora do cenário Início, porque o cache vem desligado. Standard estimado em 2 vezes o Basic, até confirmar na calculadora |
| D-157 | AsyncAPI com `occurredAt` em UTC e os atributos do Pub/Sub descritos como cabeçalhos |
| D-158 | Nos contratos, nomes de campos, operações, rotas e eventos em inglês; descrições, títulos e tags em português |
| D-159 | Sem API Gateway: balanceador com Cloud Armor direto no Cloud Run, com entrada restrita ao balanceador |
| D-160 | Segurança pesa no mínimo 2 em toda decisão em que é critério, e entra em toda decisão cujas opções mudam a exposição dos dados: ADR-0002, ADR-0003, ADR-0012, ADR-0013 e ADR-0016, com peso 2. A equipe reduzida passa a ser a premissa PRE-12 e pesa em Operabilidade |

## Regras de negócio

| ID | Regra |
|---|---|
| RN-01 | O valor é um inteiro maior que zero, em centavos, até R$ 1 bilhão |
| RN-02 | O tipo é `CREDIT` ou `DEBIT`, sem valor padrão |
| RN-03 | A data de competência não pode estar no futuro |
| RN-04 | Lançamentos nunca são alterados nem excluídos |
| RN-05 | Um lançamento só pode ser estornado uma vez |
| RN-06 | Um estorno não pode ser estornado |
| RN-07 | O estorno tem a natureza inversa, o mesmo valor e a mesma data de competência do original |
| RN-08 | O comerciante só acessa os próprios dados |

## Doutrina do produto

| ID | Regra |
|---|---|
| LDIMM-R01 | um lançamento registrado nunca é alterado nem excluído |
| LDIMM-R03 | a API não tem permissão de alterar nem excluir lançamentos |
| LDIMM-R02 | todo valor em dinheiro é um número inteiro de centavos |
| MRISO-R01 | todo registro carrega o comerciante dono |
| MRISO-R02 | toda consulta é limitada ao comerciante da requisição |
| MRISO-R03 | o banco também aplica o isolamento |
| WRIND-R01 | registrar um lançamento depende apenas do banco de lançamentos |
| WRIND-R02 | todo lançamento confirmado tem seu evento gravado na mesma transação |
| WRIND-R03 | a assinatura do consolidado existe antes de qualquer publicação |

## Regras dos casos de uso

| ID | Regra |
|---|---|
| CNSCP-R01 | sem um token válido para esta API, o pedido é recusado com 401 |
| CNSCP-R02 | as consultas exigem o escopo balances.read, e sem ele o pedido é recusado com 403 |
| CNSCP-R03 | um token sem o comerciante é recusado com 403 |
| CNSCP-R04 | a cota de pedidos é de cada comerciante, e nenhum comerciante escapa dela |
| CNADS-R01 | todo acesso recusado fica na trilha de auditoria |
| CNADS-R02 | uma consulta bem-sucedida fica fora da trilha |
| CNHTP-R01 | um comerciante que não pode ser identificado é recusado com 401 |
| CNHTP-R02 | o saldo é sempre o do comerciante do token |
| CNHTP-R03 | a recusa da consulta responde no formato de problema, com o código |
| CNHTP-R04 | a recusa sem corpo próprio também responde no formato de problema |
| APENT-R01 | um crédito soma nos créditos do dia, e um débito soma nos débitos do dia |
| APENT-R02 | um evento repetido não altera o saldo |
| APENT-R03 | a ordem de chegada não altera o resultado |
| APENT-R04 | o agregado pertence ao comerciante do evento |
| APENT-R07 | um evento de tipo desconhecido é recusado, e não marcado como aplicado |
| CBSTR-R01 | sem alvo ligado, a consulta vai ao armazenamento e não toca no cache |
| CBSTR-R02 | um alvo de rota liga o cache só naquela rota |
| CBSTR-R03 | um alvo de tabela liga o cache em todas as rotas dela |
| CBSTR-R04 | um valor em cache é devolvido sem consultar o armazenamento |
| CBSTR-R05 | uma falha do cache não derruba a consulta |
| CBSTR-R06 | um evento aplicado invalida o cache do comerciante |
| CBSTR-R07 | uma falha do cache não impede a aplicação do evento |
| CBSTR-R08 | um valor é gravado na versão lida antes da consulta |
| CBSTR-R09 | o reset descarta o cache de todos os comerciantes, ou falha |
| DYBAL-R01 | o fechamento é a abertura mais os créditos menos os débitos |
| DYBAL-R02 | um dia sem movimento mantém a abertura como fechamento |
| DYBAL-R03 | um estouro no fechamento gera erro, e nunca um saldo errado |
| GTBAL-R01 | a consulta devolve apenas dados do comerciante da requisição |
| GTBAL-R02 | a abertura é o acumulado de todos os dias anteriores |
| GTBAL-R03 | a resposta informa o momento do último evento aplicado |
| GTBAL-R04 | um comerciante sem movimento recebe saldo zerado e sem momento de atualização |
| GTBAL-R07 | um lançamento retroativo muda a abertura dos dias seguintes |
| GTBAL-R08 | o banco só devolve linhas do comerciante definido na transação |
| GTPRD-R01 | o início não pode ser depois do fim |
| GTPRD-R02 | o período tem no máximo 366 dias |
| GTPRD-R03 | todo dia do período aparece, mesmo sem movimento |
| GTPRD-R04 | o fechamento de um dia é a abertura do seguinte |
| GTPRD-R05 | a consulta devolve apenas dados do comerciante da requisição |
| RSBAL-R01 | depois da limpeza, nenhum saldo nem evento processado permanece |
| RSBAL-R02 | os eventos reenviados recompõem o mesmo saldo |
| LDSCP-R01 | sem um token válido para esta API, o pedido é recusado com 401 |
| LDSCP-R02 | cada operação exige o seu escopo, e sem ele o pedido é recusado com 403 |
| LDSCP-R03 | um token sem o comerciante é recusado com 403 |
| LDSCP-R04 | a cota de pedidos é de cada comerciante, e nenhum comerciante escapa dela |
| LDADS-R01 | toda escrita fica na trilha de auditoria |
| LDADS-R02 | todo acesso recusado fica na trilha de auditoria |
| LDADS-R03 | uma leitura bem-sucedida fica fora da trilha |
| LDEPS-R01 | a escrita exige uma Idempotency-Key de 8 a 128 caracteres |
| LDEPS-R02 | o tipo do lançamento enviado como número é recusado com 400 |
| LDEPS-R03 | o lançamento de outro comerciante responde como inexistente |
| LDEPS-R04 | um comerciante que não pode ser identificado é recusado com 401 |
| LDMRC-R01 | o comerciante é lido do token |
| LDMRC-R02 | um comerciante que não é um identificador válido não é lido |
| LDMRC-R03 | um comerciante escolhido pelo cliente é ignorado |
| LDHTP-R01 | a recusa de uma regra de domínio responde no formato de problema, com o código da regra |
| LDHTP-R02 | a recusa sem corpo próprio também responde no formato de problema |
| CESTR-R01 | sem alvo ligado, a consulta vai ao armazenamento e não toca no cache |
| CESTR-R02 | um alvo de rota liga o cache só naquela rota |
| CESTR-R03 | um alvo de tabela liga o cache em todas as rotas dela |
| CESTR-R04 | um valor em cache é devolvido sem consultar o armazenamento |
| CESTR-R05 | uma falha do cache não derruba a consulta |
| CESTR-R06 | um lançamento registrado invalida o cache do comerciante |
| CESTR-R07 | uma falha do cache não impede o registro do lançamento |
| CESTR-R08 | um valor é gravado na versão lida antes da consulta |
| LEDEN-R01 | o lançamento pertence a um comerciante |
| LEDEN-R02 | o valor é um inteiro de centavos maior que zero |
| LEDEN-R03 | a data de competência não pode estar no futuro, no fuso de São Paulo |
| LEDEN-R04 | a descrição tem no máximo 200 caracteres |
| LEDEN-R05 | um lançamento válido nasce com identidade e momento de criação |
| LEDEN-R06 | o estorno tem o tipo inverso, o mesmo valor e a mesma data de competência do original |
| LEDEN-R07 | um estorno não pode ser estornado |
| LEDEN-R08 | o valor de um lançamento não passa de R$ 1 bilhão |
| LEDEN-R09 | o tipo é crédito ou débito, sem valor padrão |
| LEDEN-R10 | a API não consegue alterar nem excluir um lançamento |
| LSTEN-R01 | a lista traz apenas lançamentos do comerciante, na data pedida |
| LSTEN-R02 | o limite vai de 1 a 200 |
| LSTEN-R03 | a página informa a próxima posição só quando há mais lançamentos |
| LSTEN-R04 | o banco só devolve linhas do comerciante definido na transação |
| OBRLY-R01 | um evento só é marcado como publicado depois da confirmação da mensageria |
| OBRLY-R02 | ao falhar, o lote para e o restante continua pendente |
| OBRLY-R03 | o que foi confirmado antes da falha continua marcado |
| OBRLY-R04 | o evento que falhou espera mais a cada falha antes de nova tentativa |
| RCENT-R01 | o lançamento e o evento são gravados juntos ou nenhum é gravado |
| RCENT-R02 | o registro não depende do consolidado nem da mensageria |
| RCENT-R03 | o evento leva os dados do lançamento e usa a identidade dele |
| RCENT-R04 | a mesma chave com o mesmo conteúdo devolve o lançamento original |
| RCENT-R05 | a mesma chave com outro conteúdo é recusada |
| RCENT-R06 | duas chamadas simultâneas com a mesma chave criam um único lançamento |
| RPENT-R01 | todo lançamento volta a ter seu evento pendente |
| RPENT-R02 | o evento reenviado mantém a identidade original |
| RVENT-R01 | lançamento inexistente ou de outro comerciante não é encontrado |
| RVENT-R02 | um lançamento só pode ser estornado uma vez |
| RVENT-R03 | o estorno e seu evento são gravados juntos |
| RVENT-R04 | a mesma chave devolve o estorno original |

## Versionamento dos contratos

| Regra |
|---|
| Rotas usam a versão principal no caminho, como `/v1` |
| Campos novos e opcionais não mudam a versão |
| Remover ou mudar um campo cria uma nova versão, e as duas convivem durante a migração |
| Eventos levam o campo `version`, e consumidores ignoram campos desconhecidos |
| Erros seguem o formato Problem Details, da RFC 9457 |

## Decisões revistas

| Data | Antes | Depois |
|---|---|---|
| 2026-11-05 | RabbitMQ como plano B no ambiente local | Descartado |
| 2026-11-05 | Pool padrão e consumo sem limite | Limite de 16 eventos e pool definido |
| 2026-11-05 | Esperar o tempo limite do Redis | Ignorar o cache desconectado |
| 2026-11-05 | Cache como ganho de latência | Cache como proteção do banco |
| 2026-11-06 | Cache sempre ligado | Cache opcional, por rota ou tabela |
| 2026-11-06 | Isolamento só no código | Row-level security como segunda barreira |
| 2026-11-06 | Npgsql direto | Dapper, com SQL explícito |
| 2026-11-06 | `X-Merchant-Id` provisório | JWT, com o comerciante vindo do token |
| 2026-11-06 | Recuperação de desastres dentro de Operação | Documento próprio |
| 2026-11-06 | RTO de 1 hora para a perda do consolidado | RTO condicionado ao volume |
| 2026-11-09 | D-63: Início com Memorystore Basic | Início sem Memorystore |
| 2026-11-09 | D-133: worker do consolidado ignora o RLS | Consumidor sob row-level security; só o publicador de Lançamentos mantém `BYPASSRLS` |
| 2026-11-09 | Evento que falha volta ao início do lote | Espera crescente: ver D-143 |
| 2026-11-09 | D-26: Redis com `allkeys-lru` | `volatile-lru`: ver D-142 |
| 2026-11-09 | D-130: sem alvos de cache, não conecta ao Redis | Sem alvos do próprio serviço, não conecta |
| 2026-11-09 | D-16: espera de 2 s depois de falha ao publicar | A pausa vale para o laço; o evento que falhou segue a espera crescente da D-143 |
| 2026-11-09 | API Gateway na borda | Sem API Gateway: ver D-159 |
| 2026-11-09 | Segurança com peso 1 no ADR-0009, e fora dos ADRs 0002, 0003, 0012, 0013 e 0016 | Peso mínimo 2, e critério em toda decisão que muda a exposição: ver D-160 |
| 2026-11-09 | Equipe pequena citada nos pesos como restrição do negócio | Premissa assumida PRE-12, que pesa em Operabilidade: ver D-160 |
