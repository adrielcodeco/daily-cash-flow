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

O registro entra no mesmo commit da decisão, com o resumo gerado de novo por `node tools/decisions/generate.mjs`. Antes de fechar um marco, o código é conferido contra este documento.

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
| D-03 | Dia pela data de competência, em `America/Sao_Paulo` | UTC, fuso por comerciante | Comerciante em outro fuso segue o dia de São Paulo | Julgamento | Houver comerciante fora do fuso |
| D-04 | Correção por estorno; lançamento é imutável | Atualizar ou excluir | Corrigir um erro gera dois registros a mais no histórico | Requisito | Não previsto: é doutrina do produto |
| D-05 | Um lançamento só pode ser estornado uma vez, garantido por índice único | Regra só no código, estorno parcial | Não há estorno parcial | Julgamento | O negócio pedir estorno parcial |
| D-06 | Lançamento retroativo é permitido | Bloquear dias fechados | Um dia passado pode mudar, sem fechamento contábil | Julgamento | Houver fechamento de período |
| D-07 | Abertura calculada na consulta | Gravar o acumulado por dia | A consulta soma o histórico do comerciante | Julgamento | A soma ficar lenta: ver ADR-0005 |
| D-08 | Ids em UUID v7 | UUID v4, sequência do banco | Ids mais longos que uma sequência, e revelam o momento da criação | Julgamento | O tamanho dos índices pesar |
| D-09 | `eventId` igual ao id do lançamento | Id próprio do evento | Um lançamento só pode gerar um evento desse tipo | Julgamento | Surgirem novos eventos por lançamento |
| D-10 | Evento carrega o necessário para o saldo, sem a descrição | Evento só com o id, evento com todos os campos | Quem precisar da descrição terá de obtê-la de outra forma | Julgamento | Um consumidor precisar da descrição |
| D-11 | Tópico versionado no nome: `entries-recorded-v1` | Versão só no conteúdo | Mudança incompatível exige novo tópico e publicação dupla | Julgamento | Houver mudança incompatível |
| D-12 | `processed_event` guarda todo evento aplicado | Janela de retenção | A tabela cresce sem limite | Julgamento | O volume pedir retenção, maior que a retenção de reenvio |
| D-13 | Limpeza da outbox fica como proposta | Limpar desde o início | A tabela cresce até a limpeza existir | Julgamento | Antes de produção |

## Mensageria e fluxo

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-14 | Lote de 100 eventos na outbox | Lote maior ou menor | Um lote grande prende a transação por mais tempo | Valor inicial | A vazão exigir: ver ADR-0015 |
| D-15 | Espera de 200 ms com a fila vazia | Espera maior, aviso do banco | Consultas contínuas sem eventos | Valor inicial | A carga no banco pesar |
| D-16 | Espera de 2 s depois de falha ao publicar | Espera exponencial | Recuperação simples, mas não adaptativa | Valor inicial | Falhas longas gerarem ruído |
| D-17 | 5 tentativas antes das mensagens mortas | Mais tentativas, sem desvio | Uma falha longa do banco pode desviar eventos válidos, que exigem reenvio | Valor inicial | O runbook for acionado por falhas transitórias |
| D-18 | Nova entrega com espera de 1 a 60 s | Espera fixa | A recuperação pode atrasar até 60 s por evento | Valor inicial | A defasagem passar do SLO |
| D-19 | Provisionar a assinatura antes da primeira publicação | Criar na subida do consumidor | Um passo a mais na implantação | Medição | Não previsto: mensagem sem assinatura é descartada |
| D-20 | Lote de 500 lançamentos no reenvio | Lote maior ou menor | Mais idas ao banco que um lote maior | Valor inicial | A reconstrução ficar lenta |
| D-21 | 10 s para o consumidor encerrar | Encerrar na hora | Eventos em andamento além desse prazo são entregues de novo | Valor inicial | O encerramento gerar repetição demais |

## Cache

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-22 | Versão por comerciante na chave do cache | Invalidar dia a dia, invalidar a tabela inteira | Toda mudança descarta todo o cache daquele comerciante | Julgamento | A taxa de acerto cair demais |
| D-23 | TTL de 60 s | Sem TTL, TTL maior | Se a invalidação falhar com o Redis fora, o saldo pode ficar até 60 s desatualizado | Valor inicial | O negócio exigir menos defasagem |
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
| D-32 | `X-Merchant-Id` provisório até o M6 | Autenticação desde o início | Até o M6, quem chama escolhe o comerciante. Não pode ir a produção | Julgamento | No M6, com a troca por JWT |

## Ambiente, teste e entrega

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-33 | Emulador do Pub/Sub no ambiente local | RabbitMQ, projeto real no GCP | Ele pode diferir do serviço real: risco R09 | Medição | Antes de produção, com teste no GCP |
| D-34 | Docker Compose, com build em contêiner | Instalar o SDK na máquina | Primeira subida mais lenta | Julgamento | Não previsto |
| D-35 | Sem deploy real no GCP | Implantar | Rede, IAM e serviços gerenciados não foram exercitados | Julgamento | Antes de produção |
| D-36 | Portas locais 18081 e 18082 | 8081 e 8082 | Portas menos usuais | Julgamento | Não previsto: evitam conflito com serviços que costumam usar a faixa 8080 |
| D-37 | Pool local de 30 conexões, e orçamento menor no GCP | Mesmo valor nos dois | O teste local não valida o pool proposto para a nuvem | Valor inicial | No teste de carga no GCP |
| D-38 | PostgreSQL 17 e Redis 8 no ambiente local | Outras versões | Falta confirmar as versões oferecidas pelo Cloud SQL e pelo Memorystore | Julgamento | Antes de produção |
| D-39 | Carga com 50 leituras e 10 escritas por segundo | Só leituras | A proporção de escritas é premissa | Julgamento | Houver dados reais de uso |
| D-40 | Mutação com corte em 70% e meta de 90% | Sem corte | Um corte baixo aceita regressão | Valor inicial | A pontuação estabilizar |
| D-41 | Mutação fora do CI | Rodar a cada commit | A pontuação pode ficar defasada | Julgamento | Ver ADR-0021 |
| D-42 | Pacotes com versão central e sem vulnerabilidade conhecida | Versão por projeto | Atualização exige verificação manual | Julgamento | Houver atualização automática |

## Método e documentação

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-43 | Documentação em pt-BR, código em en-US | Um idioma só | O glossário precisa ligar os termos | Autor | Não previsto |
| D-45 | Nota de radar sai de 4 perguntas de sim ou não, com critérios, pesos e opções por decisão | Sete pilares fixos com nota por julgamento | Mais trabalho por decisão, e a nota ainda depende de interpretação | Autor | Houver dados de operação |
| D-46 | Preços de lista em São Paulo, sem desconto | Preços com desconto | O custo real tende a ser menor | Julgamento | Houver contrato com o provedor |
| D-47 | Volumes de custo são premissas, e só o pico vem do desafio | Só o pico | As médias podem estar erradas | Julgamento | Houver dados reais |

## Escopo e método

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-54 | Um repositório com os dois serviços | Um repositório por serviço | Os serviços não têm ciclo de versão separado | Julgamento | Equipes diferentes assumirem cada serviço |
| D-55 | Portas e adaptadores dentro de cada serviço | Camadas simples | Mais projetos e interfaces | Julgamento | O custo superar o ganho em testes |
| D-56 | CI no GitHub Actions, com testes e `smoke.sh` | Outro CI | Depende do GitHub | Julgamento | O repositório mudar de lugar |
| D-57 | Cenários Gherkin em pt-BR | Em inglês | Features e testes ficam em idiomas diferentes | Julgamento | A equipe não falar português |
| D-58 | Mensagens de commit em inglês | Em pt-BR | Histórico em idioma diferente da documentação | Autor | Não previsto |

## Domínio e contratos

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-59 | Lançamentos e Consolidação são domínios principais, com código próprio | Tratar o relatório como suporte | Duas bases de código | Julgamento | Não previsto |
| D-60 | Identidade, mensageria e observabilidade usam produto pronto ou padrão aberto | Construir | Dependência de fornecedores | Julgamento | Não previsto |
| D-61 | Estorno usa o mesmo evento `entry.recorded`, com `reversalOf` | Evento `entry.reversed` | O evento não nomeia o estorno, e o consumidor precisa olhar o campo | Julgamento | Um consumidor tratar estornos de forma diferente |
| D-62 | Saldo de período limitado a 366 dias | Sem limite | Períodos maiores exigem mais de uma consulta | Valor inicial | O uso real pedir |
| D-63 | OpenAPI 3.1, AsyncAPI 3.0 e JSON Schema | Só texto | Contratos para manter junto do código | Julgamento | Não previsto |
| D-64 | Erros no formato Problem Details, da RFC 9457 | Formato próprio | Formato mais verboso | Julgamento | Não previsto |
| D-65 | Consumidor ignora campos desconhecidos e aceita repetição e ordem diferente | Contrato rígido | Erros de campo passam sem aviso | Julgamento | Não previsto |

## Nuvem e rede

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-66 | Uma região, com alta disponibilidade entre zonas | Duas regiões ativas | Perder a região exige recuperação, com RTO de 1 hora | Julgamento | Conformidade aprovar região fora do Brasil |
| D-67 | Réplica fora do Brasil depende de avaliação regulatória | Assumir Santiago | A recuperação regional fica condicionada | Julgamento | Houver parecer de conformidade |
| D-68 | Início com 6 instâncias sempre ativas de 1 vCPU e 0,5 GiB | Escala a zero | Custo fixo maior, sem inicialização a frio | Julgamento | O teste no GCP mostrar outro tamanho |
| D-69 | Início com Cloud SQL HA de 2 vCPU e 8 GiB nos dois bancos | Consolidado sem HA | Maior custo do cenário, embora o consolidado possa ser reconstruído | Julgamento | Houver pressão de custo: é a primeira economia |
| D-70 | Início com Memorystore Basic, de 1 GiB | Standard, com réplica | O cache pode cair com a zona, e a consulta vai ao banco | Julgamento | O banco não suportar a leitura sem cache |
| D-71 | Cloud Run aceita tráfego só do balanceador | Entrada pública | Mais um componente no caminho | Julgamento | Não previsto |
| D-72 | Bancos e cache sem IP público | IP público com lista de permissão | Exige rede privada e acesso controlado para operar | Julgamento | Não previsto |
| D-73 | APIs do Google por Private Service Connect | Saída pela internet | Mais configuração de rede | Julgamento | Não previsto |
| D-74 | Saída por Cloud NAT só para o provedor de identidade | Saída livre | Cada novo destino precisa de liberação | Julgamento | Surgirem novas integrações externas |
| D-75 | Todo fluxo não listado é negado | Permitir por padrão | Mudanças exigem nova regra | Julgamento | Não previsto |
| D-76 | Identidade própria por serviço, com acesso só ao próprio banco | Conta compartilhada | Mais identidades para administrar | Julgamento | Não previsto |
| D-77 | Limites de 10, 2, 10 e 5 instâncias, conforme o orçamento de conexões | Sem limite | A escala para antes de esgotar o banco | Valor inicial | Houver medição no GCP |
| D-78 | Faixas de rede `10.20.0.0` são exemplo | Definir agora | Falta alinhar com o plano de endereços do cliente | Julgamento | Antes de produção |

## Segurança

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-79 | OAuth2 client credentials, com tokens de até 15 minutos | Tokens longos | Mais renovações de token | Valor inicial | O provedor de identidade pesar |
| D-80 | Escopos `entries.read`, `entries.write` e `balances.read` | Escopo único | Mais configuração por cliente | Julgamento | Não previsto |
| D-81 | TLS 1.2 ou superior | Só TLS 1.3 | Aceita clientes mais antigos | Julgamento | A política do cliente exigir 1.3 |
| D-82 | Limite de taxa por cliente, com resposta 429 | Limite global | Um estado a mais por cliente | Julgamento | Valores definidos no M6 |
| D-83 | mTLS e perfis como FAPI só em integrações críticas | Para todo cliente | Clientes comuns têm proteção menor | Julgamento | A regulação exigir |
| D-84 | Uma assinatura e uma identidade por consumidor de eventos | Assinatura compartilhada | Mais recursos para administrar | Julgamento | Não previsto |
| D-85 | Keycloak como identidade local | Provedor na nuvem | O ambiente local difere do provedor real | Julgamento | Definição do provedor do cliente |

## Operação

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-86 | Consultas com SLO de 99,5%, mais rígido que os 5% do desafio no pico | Usar 95% | Mais esforço que o exigido | Julgamento | Houver dados de operação |
| D-87 | Orçamento de erro esgotado para novas funcionalidades | Só alertar | Entregas podem atrasar | Julgamento | Não previsto |
| D-88 | Exercício de restauração por trimestre | Sem exercício | Tempo da equipe | Julgamento | Não previsto |
| D-89 | Reconciliação automática fica como proposta | Implementar agora | A divergência só aparece com verificação manual ou reconstrução | Julgamento | Antes de produção |
| D-90 | Cada atividade da RACI tem um único aprovador | Aprovação compartilhada | Papéis podem se acumular em equipes pequenas | Julgamento | Não previsto |

## Migração

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-91 | Migrar em 5 fases, começando pelas leituras | Começar pelas escritas | Mais tempo com dois sistemas | Julgamento | Houver um legado real |
| D-92 | CDC alimenta o novo consolidado na fase de espelho | Escrita dupla | Componente temporário a construir | Julgamento | Houver um legado real |
| D-93 | Avançar só com 30 dias de saldos iguais no espelho e 2 semanas de SLO nas leituras | Prazos menores | A migração demora mais | Valor inicial | Houver um legado real |
| D-94 | Migrar por grupos de comerciantes | Todos juntos | Convivência mais longa | Julgamento | Não previsto |
| D-95 | Cada comerciante escreve em um único sistema por vez | Escrever nos dois | Exige um corte controlado por grupo | Julgamento | Não previsto |

## Recuperação de desastres

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-96 | Perda zero não vale na janela de RPO de um desastre | Replicação síncrona entre regiões | Até 5 minutos de lançamentos confirmados, em eventos raros | Julgamento | O negócio não aceitar essa janela |
| D-97 | Erro humano é tratado em uma cópia, com correção pontual | Substituir o banco inteiro | Recuperação mais trabalhosa, em troca de preservar lançamentos novos | Julgamento | Não previsto |
| D-98 | Depois de restaurar lançamentos, sempre reconstruir o consolidado | Ajustar só a diferença | Mais tempo com saldo indisponível | Julgamento | Houver reenvio parcial |
| D-99 | Failover regional exige decisão de uma pessoa nomeada | Failover automático | Mais tempo até reagir, em troca de evitar dois históricos | Julgamento | Não previsto |
| D-100 | A escrita volta primeiro, e o saldo depois | Recuperar tudo junto | O saldo demora mais a voltar | Requisito | Não previsto: princípio P1 |
| D-101 | Cópia isolada dos lançamentos em outro projeto | Só o backup do projeto | Custo adicional, ainda não estimado | Julgamento | Definição de segurança do cliente |

## Observabilidade

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-102 | Telemetria pode ser descartada, e nunca bloqueia a escrita | Garantir toda a telemetria | Dados podem faltar durante um incidente | Requisito | Não previsto: princípio P1 |
| D-103 | Guardar o contexto do traço na `outbox` | Traços separados | Uma coluna a mais e uma migração | Julgamento | Não previsto |
| D-104 | `merchantId` fica fora dos atributos de métrica | Métrica por comerciante | Problema de um comerciante só aparece em logs e traços | Julgamento | Houver grupos de comerciantes de baixa cardinalidade |
| D-105 | Amostragem de 10% dos traços na nuvem | Todos os traços | Traços de casos raros podem faltar | Valor inicial | Houver dados de uso |
| D-106 | Requisição bem-sucedida não gera log | Log de toda requisição | Menos detalhe, em troca de custo menor | Julgamento | A auditoria exigir, na trilha própria |
| D-107 | `/health/ready` verifica só o próprio banco | Verificar todas as dependências | A verificação não mostra falha em dependências | Requisito | Não previsto: princípio P1 |
| D-108 | Alertas de orçamento com janela dupla | Limite simples | Regras mais complexas | Julgamento | Não previsto |
| D-109 | Métricas por 6 semanas, e traços e logs por 30 dias | Retenção maior | Menos histórico para comparar | Valor inicial | Definição de conformidade |

## Vulnerabilidades e finanças

| ID | Decisão | Alternativas | Do que abrimos mão | Base | Rever quando |
|---|---|---|---|---|---|
| D-110 | Vulnerabilidade Crítica ou Alta bloqueia produção | Decidir caso a caso | Lançamentos podem atrasar | Julgamento | Não previsto |
| D-111 | Prazos de 7, 30 e 90 dias por severidade | Sem prazo | Prazos ainda não validados pela segurança do cliente | Valor inicial | Política do cliente |
| D-112 | IDs de vulnerabilidade não são reutilizados, e itens corrigidos ficam no registro | Apagar ao corrigir | Registro mais longo | Julgamento | Não previsto |
| D-113 | Senha local e tempo no UUID v7 são riscos aceitos | Corrigir | Dois achados baixos ficam abertos | Julgamento | Mudança de uso |
| D-114 | ROI como modelo gerado por script, com premissas explícitas | Número fechado em documento | Sem dados do cliente, o resultado é só um exemplo | Julgamento | Houver dados reais |
| D-116 | Limite de R$ 1 bilhão por lançamento, no domínio e no banco | Sem limite, limite por comerciante | Um lançamento legítimo acima disso precisa ser dividido | Autor | O negócio definir outro limite ou limite por comerciante |
| D-117 | Restrição do banco com `NOT VALID` | Validar linhas antigas | Linhas antigas fora do limite não são verificadas | Julgamento | Antes de produção, validar a restrição |
| D-118 | Enum começa em 1, para o valor ausente nunca ser válido | Campo obrigatório só na API | Os números do enum mudaram, sem efeito externo: o tipo é gravado por nome | Julgamento | Não previsto |
| D-119 | Consumidor recusa evento de tipo desconhecido | Ignorar o evento | Um evento ruim vai para as mensagens mortas e exige ação | Julgamento | Não previsto |
| D-120 | Um cliente por comerciante, com `merchant_id` fixo no token | Comerciante como usuário, token de usuário | Cada comerciante exige um cliente no provedor | Julgamento | Houver integração por usuário final |
| D-121 | Limite de 100 requisições por segundo por comerciante, em cada instância | Limite global com Redis | O limite real cresce com as instâncias: V-19 | Valor inicial | Houver dados de uso |
| D-122 | Cada serviço valida o JWT, com tolerância de 30 s no horário | Confiar só no gateway | Validação repetida, em troca de defesa em profundidade | Julgamento | Não previsto |
| D-123 | Falta de escopo responde 403, e token inválido responde 401 | Responder sempre 401 | Revela que o token é válido | Julgamento | Não previsto |
| D-124 | Issuer fixo no Keycloak local | Dois endereços aceitos | O endereço interno aparece no token local | Julgamento | Não previsto: vale só no ambiente local |
| D-125 | Cada teste cria seu comerciante no Keycloak | Clientes fixos no realm | Testes dependem da API de administração | Julgamento | Não previsto |
| D-126 | Configuração de telemetria e segurança repetida em cada serviço | Biblioteca compartilhada | Código repetido, em troca de serviços sem dependência entre si | Julgamento | Surgirem mais serviços |
| D-127 | Tempo para traços locais | Jaeger | Mais um componente da família Grafana | Julgamento | Não previsto |
| D-128 | Logs na saída padrão, fora do coletor | Enviar por OTLP | Sem logs no Grafana local | Julgamento | Precisar correlacionar logs no ambiente local |
| D-129 | Auditoria como categoria de log | Armazenamento próprio agora | Sem proteção contra alteração: V-06 | Julgamento | Antes de produção |
| D-130 | Publicador lê o tamanho da outbox a cada 5 s | Ler a cada exportação | A métrica para se o publicador morrer | Valor inicial | Não previsto: `TelemetryMissing` cobre esse caso |
| D-131 | Nível de log do ASP.NET definido no código | Variável de ambiente | Mudar exige novo build. O `sh` do contêiner descarta variáveis com ponto | Medição | Trocar o ponto de entrada da imagem |
| D-132 | Métricas expiram em 1 minuto no coletor | Padrão de 5 minutos | Queda curta some mais rápido dos gráficos | Valor inicial | Não previsto |
| D-133 | `sbom.json` é gerado e não entra no Git | Versionar o inventário | Sem histórico do inventário | Julgamento | Publicar o SBOM como artefato do CI |
| D-134 | Resultados de carga ficam no Git, sem `setup_data` | Não versionar resultados | Arquivos grandes no repositório, em troca de prova rastreável | Julgamento | Não previsto |
| D-135 | README em pt-BR, com limites explícitos para produção | Só instruções de execução | Texto mais longo | Julgamento | Não previsto |
| D-140 | Cache como camada na frente do repositório | Cache de segundo nível de um ORM | Código próprio, em troca de invalidar só o comerciante e funcionar entre processos | Julgamento | Adotar um ORM |
| D-141 | Sem alvos de cache, o sistema não conecta ao Redis | Conectar sempre | Mudar a configuração exige reiniciar | Julgamento | Precisar ligar em execução |
| D-142 | Valor gravado na versão lida antes da consulta | Ler a versão ao gravar | Um valor a mais para carregar | Medição | Não previsto: corrigiu uma corrida |
| D-143 | Papéis separados por processo: migração, API e worker | Usuário único | Mais papéis para administrar | Julgamento | Não previsto |
| D-144 | Worker ignora o RLS, porque atende todos os comerciantes | Definir o comerciante por evento | O isolamento do worker depende do código | Julgamento | Houver exigência de isolamento no worker |
| D-145 | Leitura da API em transação, para definir o comerciante | Definir na conexão | Mais idas ao banco. Em conexões reaproveitadas, a transação evita vazar o comerciante | Julgamento | A latência pesar |
| D-146 | Opção com veto é eliminada | Nota 1 e média normal | Uma interpretação rígida de veto pode tirar uma boa opção | Autor | Não previsto |
| D-147 | C# é preferência de negócio, e não restrição | Tratar como restrição | Outras linguagens seguem na comparação, com nota menor em Alinhamento | Autor | Não previsto |
| D-148 | Alinhamento com o negócio tem peso 4 em toda decisão | Peso igual aos demais | O critério pode contar de novo méritos de outros critérios | Autor | Não previsto |
| D-149 | Avaliação feita às cegas, com ajustes do arquiteto registrados | Notas do próprio autor | O resultado depende de modelos de IA, com uma rodada | Autor | Houver revisão por pessoas |
| D-150 | Manter Cloud SQL, com AlloyDB 5 para 4 em Alinhamento | Adotar AlloyDB | Decisão por julgamento, com preço ainda não confirmado | Autor | O preço do AlloyDB não for maior |
| D-115 | Horizonte de 36 meses, sem impostos e sem cancelamento de clientes | Modelo completo | O resultado tende a ser otimista | Julgamento | Houver dados reais |

## Decisões revistas

| Data | Decisão original | O que mudou | Motivo |
|---|---|---|---|
| 2026-11-05 | RabbitMQ como plano B no ambiente local | Descartado | O emulador validou entrega e mensagens mortas |
| 2026-11-05 | Pool padrão e consumo sem limite | Limite de 16 eventos e pool definido | O teste de resiliência esgotou as conexões: ver ADR-0017 |
| 2026-11-05 | Esperar o tempo limite do Redis | Ignorar o cache desconectado | A carga mostrou p95 de 2,2 s |
| 2026-11-06 | Cache sempre ligado | Cache opcional, por rota ou tabela | O recálculo e a carga não mostraram ganho no volume atual |
| 2026-11-06 | Isolamento só no código | Row-level security como segunda barreira | O recálculo apontou fraqueza para uma instituição financeira |
| 2026-11-06 | Npgsql direto | Dapper, com SQL explícito | O recálculo favoreceu menos mapeamento manual |
| 2026-11-06 | `X-Merchant-Id` provisório | JWT, com o comerciante vindo do token | Previsto para o M6 |
| 2026-11-06 | Recuperação de desastres dentro de Operação | Documento próprio | O plano precisa de escopo, papéis e procedimentos próprios |
| 2026-11-06 | RTO de 1 hora para a perda do consolidado | RTO condicionado ao volume | A medição mostrou cerca de 266 mil lançamentos por hora |
| 2026-11-05 | Cache como ganho de latência | Cache como proteção do banco | A carga não mostrou ganho de latência: ver ADR-0005 |
