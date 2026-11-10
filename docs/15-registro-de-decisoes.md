# Registro de decisões

Toda decisão e todo trade-off ficam registrados, conforme o princípio P10. Este documento guarda as decisões menores e os parâmetros. As escolhas com alternativas reais ficam nos [ADRs](adr/README.md), com radar. A lista de tudo o que foi decidido, de todas as fontes, está no [resumo das decisões](16-resumo-de-decisoes.md).

## Como registrar

| Tipo de decisão | Onde fica |
|---|---|
| Escolha entre alternativas reais, com efeito na arquitetura | ADR com radar |
| Parâmetro, convenção ou escolha menor | Este registro |
| Regra de negócio | Spec ao lado do código e doutrina em `product/` |
| Premissa, meta ou item fora de escopo | [Requisitos](02-requisitos.md) |
| Decisão revista | Seção "Decisões revistas", sem apagar a original |

O registro entra no mesmo commit da decisão, com o resumo gerado de novo por `node tools/decisions/generate.mjs`. Antes de cada entrega, o código é conferido contra este documento.

A coluna **Base** diz de onde veio cada escolha:

| Base | Significado |
|---|---|
| Requisito | Vem do desafio ou de um requisito aceito |
| Autor | Definida por quem conduz o projeto |
| Medição | Foi definida ou confirmada por um teste |
| Julgamento | Escolha fundamentada, sem medição |
| Valor inicial | Número razoável para começar, ainda não medido nem ajustado |

## Domínio e dados

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-01 | Dinheiro em `long`, em centavos | `decimal`, tipo monetário do banco | Só BRL, e toda exibição precisa formatar | Julgamento | Houver outra moeda ou fração de centavo |
| D-02 | Uma única moeda: BRL | Moeda por lançamento | Sem suporte a outras moedas | Julgamento | O produto atender outro país |
| D-03 | Data de competência informada por quem chama; o fuso `America/Sao_Paulo` define o hoje da regra de data futura | UTC, fuso por comerciante | Um comerciante adiantado em relação a São Paulo não lança no próprio dia perto da meia-noite | Julgamento | Houver comerciante fora do fuso |
| D-04 | Correção por estorno; lançamento é imutável | Atualizar ou excluir | Corrigir um erro gera dois registros a mais no histórico | Requisito | Não previsto: é doutrina do produto |
| D-05 | Um lançamento só pode ser estornado uma vez, garantido por índice único | Regra só no código, estorno parcial | Não há estorno parcial | Julgamento | O negócio pedir estorno parcial |
| D-06 | Lançamento retroativo é permitido | Bloquear dias fechados | Um dia passado pode mudar, sem fechamento contábil | Julgamento | Houver fechamento de período |
| D-07 | Abertura calculada na consulta | Gravar o acumulado por dia | A consulta soma o histórico do comerciante | Julgamento | A soma ficar lenta: ver ADR-0005 |
| D-08 | Ids em UUID v7 | UUID v4, sequência do banco | Ids mais longos que uma sequência, e revelam o momento da criação | Julgamento | O tamanho dos índices pesar |
| D-09 | `eventId` igual ao id do lançamento | Id próprio do evento | Um lançamento só pode gerar um evento desse tipo | Julgamento | Surgirem novos eventos por lançamento |
| D-10 | Evento carrega o necessário para o saldo, sem a descrição | Evento só com o id, evento com todos os campos | Quem precisar da descrição terá de obtê-la de outra forma | Julgamento | Um consumidor precisar da descrição |
| D-11 | Tópico versionado no nome: `entries-recorded-v1` | Versão só no conteúdo | Mudança incompatível exige novo tópico e publicação dupla | Julgamento | Houver mudança incompatível |
| D-12 | `processed_event` guarda todo evento aplicado | Janela de retenção | A tabela cresce sem limite entre reconstruções | Julgamento | O volume pedir retenção, maior que a retenção de reenvio |
| D-13 | Limpeza da outbox fica como proposta | Limpar desde o início | A tabela cresce até a limpeza existir | Julgamento | Antes de produção |

## Mensageria e fluxo

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-14 | Lote de 100 eventos na outbox | Lote maior ou menor | Um lote grande prende a transação por mais tempo | Valor inicial | A vazão exigir: ver ADR-0015 |
| D-15 | Espera de 200 ms quando o lote vem incompleto ou vazio | Espera maior, aviso do banco | Consultas contínuas sem eventos | Valor inicial | A carga no banco pesar |
| D-16 | Pausa fixa de 2 s no laço do publicador depois de uma falha; o evento que falhou segue a espera crescente da D-143 | Pausa exponencial no laço | Com a mensageria fora, o publicador consulta o banco a cada 2 s | Valor inicial | Falhas longas gerarem ruído |
| D-17 | 5 tentativas antes das mensagens mortas | Mais tentativas, sem desvio | Uma falha longa do banco pode desviar eventos válidos, que exigem reenvio | Valor inicial | O runbook for acionado por falhas transitórias |
| D-18 | Nova entrega com espera de 1 a 60 s | Espera fixa | A recuperação pode atrasar até 60 s por evento | Valor inicial | A defasagem passar do SLO |
| D-19 | Provisionar a assinatura antes da primeira publicação | Criar na subida do consumidor | Um passo a mais na implantação | Medição | Não previsto: mensagem sem assinatura é descartada |
| D-20 | Lote de 500 lançamentos no reenvio | Lote maior ou menor | Mais idas ao banco que um lote maior | Valor inicial | A reconstrução ficar lenta |
| D-21 | 10 s para o consumidor encerrar | Encerrar na hora | Eventos em andamento além desse prazo são entregues de novo | Valor inicial | O encerramento gerar repetição demais |

## Cache

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-22 | Versão por comerciante na chave do cache | Invalidar dia a dia, invalidar a tabela inteira | Toda mudança descarta todo o cache daquele comerciante | Julgamento | A taxa de acerto cair demais |
| D-23 | TTL de 60 s | Sem TTL, TTL maior | Se a invalidação falhar enquanto o Redis segue disponível para leitura, um saldo ou uma lista de lançamentos pode ficar até 60 s desatualizado | Valor inicial | O negócio exigir menos defasagem |
| D-24 | Ignorar o cache enquanto estiver desconectado | Aguardar o tempo limite | Sem o cache, toda leitura vai ao banco | Medição | Não previsto: esperar elevou o p95 a 2,2 s |
| D-25 | Cache desligado por padrão, habilitável por rota ou por tabela | Sempre ligado, remover | Mais código para manter, em troca de pagar pelo Redis só onde há ganho | Autor | Não previsto |
| D-26 | Redis local sem persistência, com 64 MB e `allkeys-lru` | Persistir | Reiniciar perde o cache | Julgamento | Não previsto: o dado é descartável |

## API e contratos

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-27 | Versão na rota: `/v1` | Versão em cabeçalho | URLs mudam quando a versão muda | Julgamento | Houver `/v2` |
| D-28 | Registro de outro comerciante responde 404 | Responder 403 | O cliente não diferencia inexistente de proibido | Julgamento | Não previsto: evita revelar dados |
| D-29 | `Idempotency-Key` de 8 a 128 caracteres | Formato fixo | Aceita chaves fracas de quem chama | Valor inicial | Houver colisão por chave ruim |
| D-30 | Listagem com 50 itens por padrão e 200 no máximo | Outros limites | Mais chamadas para percorrer dias grandes | Valor inicial | O uso real pedir |
| D-31 | Resposta informa `updatedThrough` | Esconder a defasagem | Mais um campo que o cliente precisa entender | Requisito | Não previsto: princípio P5 |
| D-32 | `X-Merchant-Id` provisório até a autenticação | Autenticação desde o início | Até a autenticação entrar, quem chama escolhe o comerciante. Não pode ir a produção | Julgamento | Com a troca por JWT |

## Ambiente, teste e entrega

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-33 | Emulador do Pub/Sub no ambiente local | RabbitMQ, projeto real no GCP | Ele pode diferir do serviço real: risco R09 | Medição | Antes de produção, com teste no GCP |
| D-34 | Docker Compose, com build em contêiner | Instalar o SDK na máquina | Primeira subida mais lenta | Julgamento | Não previsto |
| D-35 | Sem deploy real no GCP | Implantar | Rede, IAM e serviços gerenciados não foram exercitados | Julgamento | Antes de produção |
| D-36 | Portas locais 18081 e 18082 | 8081 e 8082 | Portas menos usuais | Julgamento | Não previsto: evitam conflito com serviços que costumam usar a faixa 8080 |
| D-37 | Pool local de 30 conexões, e orçamento menor no GCP | Mesmo valor nos dois | O teste local não valida o pool proposto para a nuvem | Valor inicial | No teste de carga no GCP |
| D-38 | PostgreSQL 17 e Redis 8 no ambiente local | Outras versões | Falta confirmar as versões oferecidas pelo Cloud SQL e pelo Memorystore | Julgamento | Antes de produção |
| D-39 | No cenário de pico, 50 leituras e 10 escritas por segundo; o teste de resiliência usa 50 escritas por segundo | Só leituras | A proporção de escritas é premissa | Julgamento | Houver dados reais de uso |
| D-40 | Mutação com corte em 70% e meta de 90% | Sem corte | Um corte baixo aceita regressão | Valor inicial | A pontuação estabilizar |
| D-41 | Mutação fora do CI | Rodar a cada commit | A pontuação pode ficar defasada | Julgamento | Ver ADR-0021 |
| D-42 | Pacotes com versão central e sem vulnerabilidade conhecida | Versão por projeto | Atualização exige verificação manual | Julgamento | Houver atualização automática |

## Método e documentação

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-43 | Documentação em pt-BR, código em en-US | Um idioma só | O glossário precisa ligar os termos | Autor | Não previsto |
| D-44 | Nota de radar sai de 4 perguntas de sim ou não, com critérios, pesos e opções por decisão | Sete pilares fixos com nota por julgamento | Mais trabalho por decisão, e a nota ainda depende de interpretação | Autor | Houver dados de operação |
| D-45 | Preços de lista, da região São Paulo quando a página de preços permite, sem desconto | Preços com desconto | O custo real tende a ser menor | Julgamento | Houver contrato com o provedor |
| D-46 | Volumes de custo são premissas, e só o pico vem do desafio | Só o pico | As médias podem estar erradas | Julgamento | Houver dados reais |

## Escopo e método

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-47 | Um repositório com os dois serviços | Um repositório por serviço | Os serviços não têm ciclo de versão separado | Julgamento | Equipes diferentes assumirem cada serviço |
| D-48 | Portas e adaptadores dentro de cada serviço | Camadas simples | Mais projetos e interfaces | Julgamento | O custo superar o ganho em testes |
| D-49 | CI no GitHub Actions, com testes e `smoke.sh` | Outro CI | Depende do GitHub | Julgamento | O repositório mudar de lugar |
| D-50 | Cenários Gherkin em pt-BR | Em inglês | Features e testes ficam em idiomas diferentes | Julgamento | A equipe não falar português |
| D-51 | Mensagens de commit em inglês | Em pt-BR | Histórico em idioma diferente da documentação | Autor | Não previsto |

## Domínio e contratos

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-52 | Lançamentos e Consolidação são domínios principais, com código próprio | Tratar o relatório como suporte | Duas bases de código | Julgamento | Não previsto |
| D-53 | Identidade, mensageria e observabilidade usam produto pronto ou padrão aberto | Construir | Dependência de fornecedores | Julgamento | Não previsto |
| D-54 | Estorno usa o mesmo evento `entry.recorded`, com `reversalOf` | Evento `entry.reversed` | O evento não nomeia o estorno, e o consumidor precisa olhar o campo | Julgamento | Um consumidor tratar estornos de forma diferente |
| D-55 | Saldo de período limitado a 366 dias | Sem limite | Períodos maiores exigem mais de uma consulta | Valor inicial | O uso real pedir |
| D-56 | OpenAPI 3.1, AsyncAPI 3.0 e JSON Schema | Só texto | Contratos para manter junto do código | Julgamento | Não previsto |
| D-57 | Erros no formato Problem Details, da RFC 9457 | Formato próprio | Formato mais verboso | Julgamento | Não previsto |
| D-58 | Consumidor ignora campos desconhecidos e aceita repetição e ordem diferente | Contrato rígido | Erros de campo passam sem aviso | Julgamento | Não previsto |

## Nuvem e rede

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-59 | Uma região, com alta disponibilidade entre zonas | Duas regiões ativas | Perder a região exige recuperação em outra. O RTO de 1 hora é proposto e depende da região secundária (D-60), da infraestrutura como código e do tempo de reconstrução do consolidado | Julgamento | Conformidade aprovar região fora do Brasil |
| D-60 | Réplica fora do Brasil depende de avaliação regulatória | Assumir Santiago | A recuperação regional fica condicionada | Julgamento | Houver parecer de conformidade |
| D-61 | Início com 6 instâncias sempre ativas de 1 vCPU e 0,5 GiB | Escala a zero | Custo fixo maior, sem inicialização a frio | Julgamento | O teste no GCP mostrar outro tamanho |
| D-62 | Início com Cloud SQL HA de 2 vCPU e 8 GiB nos dois bancos | Consolidado sem HA | Maior custo do cenário, embora o consolidado possa ser reconstruído | Julgamento | Houver pressão de custo: é a primeira economia |
| D-63 | Início com Memorystore Basic, de 1 GiB | Standard, com réplica | O cache pode cair com a zona, e a consulta vai ao banco | Julgamento | O banco não suportar a leitura sem cache |
| D-64 | Cloud Run com entrada `internal-and-cloud-load-balancing`: sem acesso direto pela internet | Entrada pública | Mais um componente no caminho | Julgamento | Não previsto |
| D-65 | Bancos e cache sem IP público | IP público com lista de permissão | Exige rede privada e acesso controlado para operar | Julgamento | Não previsto |
| D-66 | APIs do Google por Private Service Connect | Saída pela internet | Mais configuração de rede | Julgamento | Não previsto |
| D-67 | Saída por Cloud NAT só para o provedor de identidade | Saída livre | Cada novo destino precisa de liberação | Julgamento | Surgirem novas integrações externas |
| D-68 | Todo fluxo não listado é negado | Permitir por padrão | Mudanças exigem nova regra | Julgamento | Não previsto |
| D-69 | Identidade própria por serviço, com acesso só ao próprio banco | Conta compartilhada | Mais identidades para administrar | Julgamento | Não previsto |
| D-70 | Limites de 10, 2, 10 e 5 instâncias, conforme o orçamento de conexões | Sem limite | A escala para antes de esgotar o banco | Valor inicial | Houver medição no GCP |
| D-71 | Faixas de rede `10.20.0.0` são exemplo | Definir agora | Falta alinhar com o plano de endereços do cliente | Julgamento | Antes de produção |

## Segurança

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-72 | OAuth2 client credentials, com tokens de até 15 minutos | Tokens longos | Mais renovações de token | Valor inicial | O provedor de identidade pesar |
| D-73 | Escopos `entries.read`, `entries.write` e `balances.read` | Escopo único | Mais configuração por cliente | Julgamento | Não previsto |
| D-74 | TLS 1.2 ou superior | Só TLS 1.3 | Aceita clientes mais antigos | Julgamento | A política do cliente exigir 1.3 |
| D-75 | Limite de taxa por comerciante do token (por endereço, sem token), com resposta 429 | Limite global | Um estado a mais por comerciante | Julgamento | Valores definidos com a autenticação |
| D-76 | mTLS e perfis como FAPI só em integrações críticas | Para todo cliente | Clientes comuns têm proteção menor | Julgamento | A regulação exigir |
| D-77 | Uma assinatura e uma identidade por consumidor de eventos | Assinatura compartilhada | Mais recursos para administrar | Julgamento | Não previsto |
| D-78 | Keycloak como identidade local | Provedor na nuvem | O ambiente local difere do provedor real | Julgamento | Definição do provedor do cliente |

## Operação

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-79 | Consultas com SLO de 99,5%, mais rígido que os 5% do desafio no pico | Usar 95% | Mais esforço que o exigido | Julgamento | Houver dados de operação |
| D-80 | Orçamento de erro esgotado para novas funcionalidades | Só alertar | Entregas podem atrasar | Julgamento | Não previsto |
| D-81 | Exercício de restauração por trimestre | Sem exercício | Tempo da equipe | Julgamento | Não previsto |
| D-82 | Reconciliação automática fica como proposta | Implementar agora | A divergência só aparece com verificação manual ou reconstrução | Julgamento | Antes de produção |
| D-83 | Cada atividade da RACI tem um único aprovador | Aprovação compartilhada | Papéis podem se acumular em equipes pequenas | Julgamento | Não previsto |

## Migração

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-84 | Migrar em 5 fases, começando pelas leituras | Começar pelas escritas | Mais tempo com dois sistemas | Julgamento | Houver um legado real |
| D-85 | CDC alimenta o novo consolidado na fase de espelho | Escrita dupla | Componente temporário a construir | Julgamento | Houver um legado real |
| D-86 | Avançar só com 30 dias de saldos iguais no espelho e 2 semanas de SLO nas leituras | Prazos menores | A migração demora mais | Valor inicial | Houver um legado real |
| D-87 | Migrar por grupos de comerciantes | Todos juntos | Convivência mais longa | Julgamento | Não previsto |
| D-88 | Cada comerciante escreve em um único sistema por vez | Escrever nos dois | Exige um corte controlado por grupo | Julgamento | Não previsto |

## Recuperação de desastres

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-89 | Perda zero não vale na janela de RPO de um desastre | Replicação síncrona entre regiões | Até 5 minutos de lançamentos confirmados, em eventos raros | Julgamento | O negócio não aceitar essa janela |
| D-90 | Erro humano é tratado em uma cópia, com correção pontual | Substituir o banco inteiro | Recuperação mais trabalhosa, em troca de preservar lançamentos novos | Julgamento | Não previsto |
| D-91 | Depois de restaurar lançamentos, sempre reconstruir o consolidado | Ajustar só a diferença | Mais tempo com saldo em manutenção, hoje manual (V-08) | Julgamento | Houver reenvio parcial |
| D-92 | Failover regional exige decisão de uma pessoa nomeada | Failover automático | Mais tempo até reagir, em troca de evitar dois históricos | Julgamento | Não previsto |
| D-93 | A escrita volta primeiro, e o saldo depois | Recuperar tudo junto | O saldo demora mais a voltar | Requisito | Não previsto: princípio P1 |
| D-94 | Cópia isolada dos lançamentos em outro projeto | Só o backup do projeto | Custo adicional, ainda não estimado | Julgamento | Definição de segurança do cliente |

## Observabilidade

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-95 | Telemetria pode ser descartada, e nunca bloqueia a escrita | Garantir toda a telemetria | Dados podem faltar durante um incidente | Requisito | Não previsto: princípio P1 |
| D-96 | Guardar o contexto do traço na `outbox` | Traços separados | Uma coluna a mais e uma migração | Julgamento | Não previsto |
| D-97 | `merchantId` fica fora dos atributos de métrica | Métrica por comerciante | Problema de um comerciante só aparece em logs e traços | Julgamento | Houver grupos de comerciantes de baixa cardinalidade |
| D-98 | Amostragem de 10% dos traços na nuvem | Todos os traços | Traços de casos raros podem faltar | Valor inicial | Houver dados de uso |
| D-99 | Requisição bem-sucedida não gera log operacional; escritas e estornos vão só para a categoria `Audit` | Log de toda requisição | Menos detalhe, em troca de custo menor | Julgamento | A operação precisar de log de cada requisição |
| D-100 | `/health/ready` verifica só o próprio banco | Verificar todas as dependências | A verificação não mostra falha em dependências | Requisito | Não previsto: princípio P1 |
| D-101 | Alertas de orçamento com janela dupla, na proposta para produção; o ambiente local usa limites simples em `infra/prometheus/alerts.yaml` | Limite simples | Regras mais complexas | Julgamento | Não previsto |
| D-102 | Métricas por 6 semanas, e traços e logs por 30 dias | Retenção maior | Menos histórico para comparar | Valor inicial | Definição de conformidade |

## Vulnerabilidades e finanças

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-103 | Vulnerabilidade Crítica ou Alta bloqueia produção | Decidir caso a caso | Entregas podem atrasar | Julgamento | Não previsto |
| D-104 | Prazos de 7, 30 e 90 dias por severidade | Sem prazo | Prazos ainda não validados pela segurança do cliente | Valor inicial | Política do cliente |
| D-105 | IDs de vulnerabilidade não são reutilizados, e itens corrigidos ficam no registro | Apagar ao corrigir | Registro mais longo | Julgamento | Não previsto |
| D-106 | Senha local do compose (V-17) e tempo no UUID v7 (V-18) são riscos aceitos | Corrigir | Dois achados baixos ficam como riscos aceitos, sem correção | Julgamento | Mudança de uso |
| D-107 | ROI como modelo gerado por script, com premissas explícitas | Número fechado em documento | Sem dados do cliente, o resultado é só um exemplo | Julgamento | Houver dados reais |
| D-108 | Horizonte de 36 meses, sem impostos e sem cancelamento de clientes | Modelo completo | O resultado tende a ser otimista | Julgamento | Houver dados reais |
| D-109 | Limite de R$ 1 bilhão por lançamento, no domínio e no banco | Sem limite, limite por comerciante | Um lançamento legítimo acima disso precisa ser dividido | Autor | O negócio definir outro limite ou limite por comerciante |
| D-110 | Restrição do banco com `NOT VALID` | Validar linhas antigas | Linhas antigas fora do limite não são verificadas | Julgamento | Antes de produção, validar a restrição |
| D-111 | Enum começa em 1, para o valor ausente nunca ser válido | Campo obrigatório só na API | Os números do enum mudaram, sem efeito externo: o tipo é gravado por nome | Julgamento | Não previsto |
| D-112 | Consumidor recusa evento com tipo de lançamento desconhecido | Ignorar o evento | Um evento ruim vai para as mensagens mortas e exige ação | Julgamento | Não previsto |
| D-113 | Um cliente por comerciante, com `merchant_id` fixo no token | Comerciante como usuário, token de usuário | Cada comerciante exige um cliente no provedor | Julgamento | Houver integração por usuário final |
| D-114 | Limite de 100 requisições por segundo por comerciante, em cada instância | Limite global com Redis | O limite real cresce com as instâncias: V-19 | Valor inicial | Houver dados de uso |
| D-115 | Cada serviço valida o JWT, com tolerância de 30 s no horário | Confiar só na borda | Validação repetida, em troca de defesa em profundidade | Julgamento | Não previsto |
| D-116 | Falta de escopo responde 403, e token inválido responde 401 | Responder sempre 401 | Revela que o token é válido | Julgamento | Não previsto |
| D-117 | Issuer fixo no Keycloak local | Dois endereços aceitos | O endereço interno aparece no token local | Julgamento | Não previsto: vale só no ambiente local |
| D-118 | Cada teste ponta a ponta e de carga cria seu comerciante no Keycloak | Clientes fixos no realm | Testes dependem da API de administração | Julgamento | Não previsto |
| D-119 | Configuração de telemetria e segurança repetida em cada serviço | Biblioteca compartilhada | Código repetido, em troca de serviços sem dependência entre si | Julgamento | Surgirem mais serviços |
| D-120 | Tempo para traços locais | Jaeger | Mais um componente da família Grafana | Julgamento | Não previsto |
| D-121 | Logs na saída padrão, fora do coletor | Enviar por OTLP | Sem logs no Grafana local | Julgamento | Precisar correlacionar logs no ambiente local |
| D-122 | Auditoria como categoria de log | Armazenamento próprio agora | Sem proteção contra alteração: V-06 | Julgamento | Antes de produção |
| D-123 | Publicador lê o tamanho da outbox a cada 5 s | Ler a cada exportação | A métrica para se o publicador morrer | Valor inicial | Não previsto: `TelemetryMissing` cobre esse caso |
| D-124 | Nível de log do ASP.NET definido no código | Variável de ambiente | Mudar exige novo build. O `sh` do contêiner descarta variáveis com ponto | Medição | Trocar o ponto de entrada da imagem |
| D-125 | Métricas expiram em 1 minuto no coletor | Padrão de 5 minutos | Queda curta some mais rápido dos gráficos | Valor inicial | Não previsto |
| D-126 | `sbom.json` é gerado e não entra no Git | Versionar o inventário | Sem histórico do inventário | Julgamento | Publicar o SBOM como artefato do CI |
| D-127 | Resultados de carga ficam no Git, sem `setup_data` | Não versionar resultados | Arquivos grandes no repositório, em troca de prova rastreável | Julgamento | Não previsto |
| D-128 | README em pt-BR, com limites explícitos para produção | Só instruções de execução | Texto mais longo | Julgamento | Não previsto |
| D-129 | Cache como camada na frente do repositório | Cache de segundo nível de um ORM | Código próprio, em troca de invalidar só o comerciante e funcionar entre processos | Julgamento | Adotar um ORM |
| D-130 | Sem alvos de cache, o sistema não conecta ao Redis | Conectar sempre | Mudar a configuração exige reiniciar | Julgamento | Precisar ligar em execução |
| D-131 | Valor gravado na versão lida antes da consulta | Ler a versão ao gravar | Um valor a mais para carregar | Medição | Não previsto: corrigiu uma corrida |
| D-132 | Papéis separados por processo: migração, API e worker | Usuário único | Mais papéis para administrar | Julgamento | Não previsto |
| D-133 | O publicador de Lançamentos ignora o RLS, porque lê todos os comerciantes | Definir o comerciante por evento | O isolamento do publicador depende do código | Julgamento | Houver exigência de isolamento no publicador |
| D-134 | Leitura da API em transação, para definir o comerciante | Definir na conexão | Mais idas ao banco. Em conexões reaproveitadas, a transação evita vazar o comerciante | Julgamento | A latência pesar |
| D-135 | Opção com veto é eliminada | Nota 1 e média normal | Uma interpretação rígida de veto pode tirar uma boa opção | Autor | Não previsto |
| D-136 | C# é preferência de negócio, e não restrição | Tratar como restrição | Outras linguagens seguem na comparação, com nota menor em Alinhamento | Autor | Não previsto |
| D-137 | Alinhamento com o negócio tem peso 4 em toda decisão | Peso igual aos demais | O critério pode contar de novo méritos de outros critérios | Autor | Não previsto |
| D-138 | Avaliação feita às cegas, com ajustes do arquiteto registrados | Notas do próprio autor | O resultado depende de modelos de IA, com uma rodada | Autor | Houver revisão por pessoas |
| D-139 | Manter Cloud SQL, com AlloyDB 5 para 4 em Alinhamento | Adotar AlloyDB | Decisão por julgamento, com preço ainda não confirmado | Autor | O preço do AlloyDB não for maior |

## Revisão antes da publicação

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-140 | Cada serviço só considera, da lista de alvos de cache, os nomes que conhece. A API e o consumidor do consolidado precisam receber os mesmos alvos, porque o consumidor invalida o cache do saldo; o compose passa um único `CACHE_TARGETS` a todos | Ligar o cache em todos os serviços com qualquer alvo | Um alvo escrito no serviço errado é ignorado sem aviso | Julgamento | Os alvos passarem a vir de um lugar central |
| D-141 | O reset do consolidado invalida o cache por uma época global, e falha se o Redis estiver fora | Apagar as chaves por padrão, ou ignorar a falha | Se o Redis estiver fora, o reset falha depois de limpar o consolidado; o script de reconstrução para, e o operador repete a reconstrução. O processo do reset precisa receber os mesmos alvos de cache da API | Julgamento | O reset passar a ser automático |
| D-142 | Redis com `volatile-lru` | `allkeys-lru` com prazo nas chaves de versão | As chaves de versão e a época nunca são despejadas e ocupam memória até o Redis reiniciar | Julgamento | As chaves de versão pesarem na memória |
| D-143 | O evento que falha espera 1, 2, 4… segundos, até 1 minuto, somados à pausa de 2 s do laço (D-16), antes de nova tentativa | Fila de mensagens mortas na outbox, ou ordem estrita | A ordem de publicação deixa de seguir a de criação, o que o consolidado tolera porque soma valores | Julgamento | Surgir um consumidor que dependa da ordem |
| D-144 | O que a mensageria confirmou é marcado em um único comando, mesmo com o publicador sendo encerrado | Marcar evento por evento, com o cancelamento valendo também para a marcação | O encerramento espera a marcação terminar | Julgamento | Não previsto |
| D-145 | A reconstrução recoloca um lote inteiro na outbox com um comando, por `unnest` | Um comando por lançamento | O SQL fica específico do PostgreSQL | Julgamento | O banco mudar |
| D-146 | O consumidor do consolidado grava sob row-level security, com o comerciante do evento em cada transação | Manter `BYPASSRLS` | Uma chamada a mais por evento. O publicador de Lançamentos ainda lê todos os comerciantes e mantém `BYPASSRLS` | Julgamento | O publicador puder trabalhar por comerciante |
| D-147 | O hash de idempotência inclui o tamanho da descrição | Tratar descrição vazia como ausente | Repetir uma chave gravada antes da mudança responde 409; o risco fica restrito à janela de nova tentativa dos clientes, porque as chaves não expiram | Julgamento | Não previsto |
| D-148 | A resposta 404 entra na auditoria | Auditar só 401, 403 e 429 | Registra todo 404, inclusive rotas inexistentes e ids inexistentes do próprio comerciante | Requisito | O volume da auditoria pesar |
| D-149 | Token sem `merchant_id` recebe 403, e com valor inválido ou todo em zeros recebe 401 | Responder 401 em todos os casos | A política de autorização responde 403 a quem já se autenticou, e o contrato documenta os dois casos | Medição | Não previsto |
| D-150 | Partições do limite de requisições com prefixo de tipo; sem `merchant_id`, o limite é por endereço, e health não tem limite | Chave sem prefixo | Atrás do balanceador, o tráfego sem token divide a mesma partição, porque os cabeçalhos encaminhados não são tratados. Esse tráfego só recebe 401, e o Cloud Armor limita por endereço antes (D-159) | Julgamento | Requisições sem token passarem a ter uso legítimo |
| D-151 | Toda recusa responde ProblemDetails, inclusive 401, 403 e corpo inválido | Deixar o corpo vazio nessas respostas | Nenhum | Medição | Não previsto |
| D-152 | Testes na fronteira HTTP com tokens assinados no teste e o handler JWT real, em ambiente de produção e com armazenamento em memória | Esquema de autenticação falso, ambiente de desenvolvimento, ou banco real | O banco não participa desses testes; ele já é coberto pelos testes de integração | Julgamento | Surgir regra que dependa do banco na fronteira |
| D-153 | Reconciliação por script, chamada pelo smoke e ao fim da reconstrução | Worker periódico com alerta | Só roda quando chamado e não alerta sozinho | Julgamento | Houver ambiente de produção |
| D-154 | Carga com dois anos de saldos diários por comerciante, inseridos direto na tabela | Gerar o histórico pela API | O histórico não passa pela mensageria, o que não importa para a consulta medida. O consolidado fica diferente dos lançamentos até a próxima reconstrução, e a reconciliação acusa essas linhas | Julgamento | Não previsto |
| D-155 | A CI verifica a formatação e se os documentos gerados estão em dia | Confiar no hook local | A CI fica alguns segundos mais lenta | Julgamento | Não previsto |
| D-156 | Memorystore fora do cenário Início, porque o cache vem desligado. Standard estimado em 2 vezes o Basic, até confirmar na calculadora | Manter o Basic no Início | Os preços do Memorystore ficam como estimativa | Julgamento | Os preços forem confirmados |
| D-157 | AsyncAPI com `occurredAt` em UTC e os atributos do Pub/Sub descritos como cabeçalhos | Exemplo no fuso local | Nenhum | Julgamento | Não previsto |
| D-158 | Nos contratos, nomes de campos, operações, rotas e eventos em inglês; descrições, títulos e tags em português | Contratos inteiros em inglês | Dois idiomas no mesmo arquivo | Julgamento | O contrato for publicado para fora |
| D-159 | Sem API Gateway: balanceador com Cloud Armor direto no Cloud Run, com entrada restrita ao balanceador | API Gateway com entrada aberta e invocação só pela identidade do gateway | Sem gestão de chaves e cotas por cliente na borda; o limite por comerciante fica nos serviços e o global no Cloud Armor | Julgamento | Surgirem clientes que precisem de cotas ou chaves gerenciadas na borda |
| D-160 | Segurança pesa no mínimo 2 em toda decisão em que é critério, e entra em toda decisão cujas opções mudam a exposição dos dados: ADR-0002, ADR-0003, ADR-0012, ADR-0013 e ADR-0016, com peso 2. A equipe reduzida passa a ser a premissa PRE-12 e pesa em Operabilidade | Manter Segurança só onde já estava, com peso 1 no ADR-0009; ou dar peso 3 em toda decisão | Decisões que não tratam de dados ganham um critério a mais, e o mínimo de 2 pode contar de novo um mérito já medido em Alinhamento. Nenhuma escolhida deixou de vencer; as margens mudaram nos ADRs 0002, 0003, 0009, 0012 e 0013 | Autor | Houver revisão de segurança por pessoas, ou o cliente informar o tamanho da equipe |

## Decisões revistas

| Data | Decisão original | O que mudou | Motivo |
|---|---|---|---|
| 2026-11-05 | RabbitMQ como plano B no ambiente local | Descartado | O emulador validou entrega e mensagens mortas |
| 2026-11-05 | Pool padrão e consumo sem limite | Limite de 16 eventos e pool definido | O teste de resiliência esgotou as conexões: ver ADR-0017 |
| 2026-11-05 | Esperar o tempo limite do Redis | Ignorar o cache desconectado | A carga mostrou p95 de 2,2 s |
| 2026-11-05 | Cache como ganho de latência | Cache como proteção do banco | A carga não mostrou ganho de latência: ver ADR-0005 |
| 2026-11-06 | Cache sempre ligado | Cache opcional, por rota ou tabela | O recálculo e a carga não mostraram ganho no volume atual |
| 2026-11-06 | Isolamento só no código | Row-level security como segunda barreira | O recálculo apontou fraqueza para uma instituição financeira |
| 2026-11-06 | Npgsql direto | Dapper, com SQL explícito | O recálculo favoreceu menos mapeamento manual |
| 2026-11-06 | `X-Merchant-Id` provisório | JWT, com o comerciante vindo do token | Previsto desde o início |
| 2026-11-06 | Recuperação de desastres dentro de Operação | Documento próprio | O plano precisa de escopo, papéis e procedimentos próprios |
| 2026-11-06 | RTO de 1 hora para a perda do consolidado | RTO condicionado ao volume | A medição mostrou cerca de 266 mil lançamentos por hora |
| 2026-11-09 | D-63: Início com Memorystore Basic | Início sem Memorystore | O cache vem desligado: ver D-156 |
| 2026-11-09 | D-133: worker do consolidado ignora o RLS | Consumidor sob row-level security; só o publicador de Lançamentos mantém `BYPASSRLS` | O comerciante do evento já é conhecido: ver D-146 |
| 2026-11-09 | Evento que falha volta ao início do lote | Espera crescente: ver D-143 | Um evento sempre recusado travava a outbox |
| 2026-11-09 | D-26: Redis com `allkeys-lru` | `volatile-lru`: ver D-142 | A chave de versão podia ser despejada e ressuscitar um valor antigo |
| 2026-11-09 | D-130: sem alvos de cache, não conecta ao Redis | Sem alvos do próprio serviço, não conecta | Um alvo de outro serviço abria conexão sem uso: ver D-140 |
| 2026-11-09 | D-16: espera de 2 s depois de falha ao publicar | A pausa vale para o laço; o evento que falhou segue a espera crescente da D-143 | A pausa fixa não protegia contra um evento sempre recusado |
| 2026-11-09 | API Gateway na borda | Sem API Gateway: ver D-159 | O API Gateway chama o Cloud Run pela URL pública `run.app` e não alcança um serviço com entrada restrita ao balanceador (D-64). Cada serviço já valida o JWT (D-115) |
| 2026-11-09 | Segurança com peso 1 no ADR-0009, e fora dos ADRs 0002, 0003, 0012, 0013 e 0016 | Peso mínimo 2, e critério em toda decisão que muda a exposição: ver D-160 | Em um sistema financeiro, a exposição dos dados não é secundária |
| 2026-11-09 | Equipe pequena citada nos pesos como restrição do negócio | Premissa assumida PRE-12, que pesa em Operabilidade: ver D-160 | O desafio não define o tamanho da equipe |
