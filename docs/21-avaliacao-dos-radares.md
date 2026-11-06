# Avaliação dos radares

Este documento guarda a evidência de cada nota. O método está em [método dos radares](20-metodo-dos-radares.md). Este arquivo é gerado.

## ADR-0001

**Estilo arquitetural.** [Ler o ADR](adr/0001-estilo-arquitetural.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. O estilo define se lançamentos sobrevive à queda do consolidado. |
| Confiabilidade | 3 | Afeta diretamente a restrição de lançamentos não cair com o consolidado e a de não perder lançamento confirmado. |
| Operabilidade | 3 | Afeta diretamente a restrição de equipe pequena sem especialista em infraestrutura. |
| Desempenho | 2 | Relevante: o estilo define saltos de rede e partida a frio no caminho das metas de p95. |
| Escalabilidade | 2 | Relevante: o estilo define se leitura e escrita crescem separadas até 500 req/s. |
| Simplicidade | 2 | Relevante: a quantidade de partes pesa para equipe pequena. |
| Economia | 1 | Secundário: o estilo muda o custo fixo, mas em valores pequenos diante do banco. |

### Dois serviços orientados a eventos (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Lançamentos segue confirmando com o consolidado fora do ar, e novos consumidores como conciliação assinam o mesmo evento sem mudar quem publica. |
| Confiabilidade | 5 | A falha do consolidado só atrasa o saldo; o evento fica retido e é reprocessado, desde que seja gravado com o lançamento e consumido de forma idempotente. |
| Operabilidade | 4 | Contêineres e broker gerenciados, com troca gradual de versão e telemetria padrão; não há informação de que a equipe domine o broker. |
| Desempenho | 4 | A escrita não espera o consolidado e a leitura usa saldo já calculado; cada lançamento custa trabalho extra de publicação e consumo. Não medido. |
| Escalabilidade | 5 | Cada lado escala sozinho e o broker absorve picos, sem coordenação manual. |
| Simplicidade | 2 | Acrescenta serviço e broker, consistência eventual e consumo idempotente, que exigem código próprio; roda localmente com contêineres. |
| Economia | 3 | Acrescenta broker pago, com custo por uso baixo no volume inicial, que cresce em linha com a carga. Valor exato não verificado. |

### Funções serverless

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Funções separadas isolam a falha, e o Cloud Run functions tem runtime dotnet10 em GA desde 18/02/2026 (docs.cloud.google.com/functions/docs/runtime-support e release notes), compatível com a stack e o provedor. |
| Confiabilidade | 5 | Cada função falha sozinha e a plataforma reinicia; com gatilho durável, a entrega é repetida, e repetir é seguro só com consumo idempotente, como em qualquer opção por eventos. |
| Operabilidade | 4 | Totalmente gerenciado, com logs e métricas nativos; não há informação de que a equipe domine o modelo de funções no GCP. |
| Desempenho | 2 | Partida a frio de .NET exige ajuste de instâncias mínimas para sustentar o p95, o que é ajuste especial; duração de partida a frio não verificada, por isso sem veto. |
| Escalabilidade | 4 | Escala automática sem coordenação, mas a abertura rápida de instâncias pressiona o limite de conexões do banco. |
| Simplicidade | 3 | Cada função é uma unidade a mais de implantação e a cola entre elas é código próprio; há framework local para rodar. |
| Economia | 4 | Substitui o cômputo em vez de somar componente e cobra por uso, então o custo fixo inicial é baixo, mas cresce em linha com a carga. Valor exato não verificado. |

### SOA com barramento central

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Separa os serviços e aceita novos consumidores, mas exige montar e governar o barramento antes do primeiro uso. |
| Confiabilidade | 2 | Todo tráfego passa pelo barramento, que vira ponto único de falha compartilhado entre lançamentos e consolidado; filas persistentes protegem o dado, se configuradas. |
| Operabilidade | 2 | Roteamento e transformação centrais precisam de dono; um ESB gerenciado equivalente no GCP não foi verificado, por isso não marquei o veto de especialista, embora o risco exista. |
| Desempenho | 2 | Acrescenta salto e processamento no caminho de cada mensagem; a meta tende a caber, mas não foi medida. |
| Escalabilidade | 2 | O barramento central é um gargalo novo e crescer exige dimensionar esse ponto único; capacidade numérica não verificada. |
| Simplicidade | 1 | Acrescenta um componente pesado, conceitos próprios de integração e configuração, e é difícil de reproduzir na máquina local. |
| Economia | 1 | Componente novo com custo fixo e manutenção contínua; preço não verificado, por isso sem veto. |

### Monolito modular (eliminada)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | Veto | Os módulos dividem o mesmo processo e a mesma implantação, então uma falha ou sobrecarga do consolidado derruba lançamentos, o que contraria a primeira restrição; separar depois exige refazer a implantação. |
| Confiabilidade | Veto | Não há chamada direta entre os módulos, mas o domínio de falha é o mesmo processo, e assim a escrita depende da saúde do consolidado; transação local protege o dado confirmado. |
| Operabilidade | 5 | Uma única imagem em contêiner gerenciado, com troca gradual de versão, telemetria padrão e tecnologia que a equipe conhece. |
| Desempenho | 4 | Sem salto de rede entre módulos; a latência de escrita fica sujeita à carga de leitura no mesmo processo. Não medido. |
| Escalabilidade | 3 | Réplicas sem estado alcançam a carga, mas leitura e escrita escalam juntas e disputam os mesmos recursos. |
| Simplicidade | 5 | Um só serviço, sem broker, fácil de rodar e testar na máquina local. |
| Economia | 4 | Sem componente adicional; o custo de réplicas cresce junto com a carga. Valor exato não verificado. |

## ADR-0002

**Integração entre serviços.** [Ler o ADR](adr/0002-integracao-entre-servicos.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Aderência ao problema | 3 | Há dois bancos, e a garantia exigida é que commit e aviso não divirjam. |
| Confiabilidade | 3 | A decisão define se a escrita depende do consolidado e se um lançamento confirmado chega a ele. |
| Operabilidade | 3 | A equipe é pequena e não tem especialista de infraestrutura, e a integração roda continuamente. |
| Desempenho | 2 | A opção pode acrescentar chamada ao caminho da escrita, com meta de p95 de 300 ms. |
| Escalabilidade | 2 | O fluxo de eventos acompanha a escrita no cenário de 500 req/s. |
| Simplicidade | 2 | As opções variam muito em componentes e conceitos novos. |
| Economia | 1 | O volume é baixo e o custo pesa pouco frente à garantia. |

### Outbox na transação e mensageria (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | O evento nasce na mesma transação do lançamento, a escrita não depende do consolidado e novos consumidores entram por assinatura, com construção de dias, não meses. |
| Aderência ao problema | 5 | Usa a transação do PostgreSQL para a garantia central, sem compensação, e a escala é adequada a 50 a 500 eventos por segundo. |
| Confiabilidade | 5 | Falha do relay ou da mensageria só atrasa o envio, a linha fica no banco até sair, e a entrega repetida é segura com consumidor idempotente. |
| Operabilidade | 4 | O relay roda no Cloud Run e a mensageria é gerenciada, mas a régua não informa que a equipe a domine. |
| Desempenho | 5 | Acrescenta só um INSERT na transação existente, sem chamada de rede; a estabilidade depende de limpar a tabela de saída. |
| Escalabilidade | 5 | Vários relays podem dividir o trabalho com bloqueio de linhas e lotes; 500 eventos por segundo não foi medido, mas é carga modesta para inserts em PostgreSQL. |
| Simplicidade | 2 | Acrescenta relay e mensageria, exige entender entrega ao menos uma vez e idempotência, e pede código próprio ou biblioteca; roda localmente. |
| Economia | 3 | A mensageria é paga por volume, com custo inicial baixo (não verificado), e o relay é pequeno. |

### CDC do banco

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 3 | Captura todo commit sem tocar a escrita e aceita novos consumidores, mas o Datastream não entrega em Pub/Sub (só BigQuery, Cloud Storage e Iceberg, https://docs.cloud.google.com/datastream/docs/destinations), o que leva a um conector como o Debezium, em JVM e fora da stack. |
| Aderência ao problema | 4 | Lê só o que foi commitado, sem escrita dupla, mas é ferramenta de replicação em volume muito acima do necessário e acopla o evento ao formato da tabela. |
| Confiabilidade | 3 | O WAL guarda o dado confirmado e o conector retoma da última posição, mas um slot parado retém WAL no banco de lançamentos e o conector é um novo ponto único; o comportamento do slot no failover do Cloud SQL não foi verificado. |
| Operabilidade | 3 | Sem rota gerenciada até a mensageria no GCP (https://docs.cloud.google.com/datastream/docs/destinations), o conector é operado pela equipe e não é tecnologia .NET; atualizar o conector não para a escrita, e upgrade maior do banco com slot não foi verificado; não chega ao veto, mas é a opção mais próxima dele aqui. |
| Desempenho | 5 | A leitura do log ocorre fora da requisição, sem chamada nova na escrita. |
| Escalabilidade | 3 | Atende 500 req/s, mas um slot tem um único leitor, o que torna o conector um gargalo que não escala sozinho. |
| Simplicidade | 3 | Exige conector, replicação lógica e slots, conceitos fora do dia a dia .NET, com pouco código próprio e execução local possível em contêiner. |
| Economia | 3 | Soma conector e mensageria; o Datastream cobra por GiB (https://cloud.google.com/datastream/pricing) e o custo do conector próprio é estimativa não verificada; operar slots e conector tende a passar de 1 pessoa-mês por ano (não verificado). |

### Publicar na mensageria durante a requisição (eliminada)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Desacopla a escrita do consolidado e é rápida de entregar, com novos consumidores por assinatura; o lançamento continua salvo no banco de origem, por isso não aplico o veto aqui. |
| Aderência ao problema | Veto | Não há transação entre banco e mensageria, logo a opção não consegue garantir que todo lançamento confirmado seja avisado e exigiria reconciliação na aplicação. |
| Confiabilidade | 1 | Uma queda entre o commit e o envio deixa o lançamento fora do consolidado sem nenhum registro para reenviar, e a falha da mensageria chega à requisição; o registro segue no banco de origem, então leio o veto como não atingido, com ressalva. |
| Operabilidade | 4 | A mensageria gerenciada não exige operação, mas a régua não informa que a equipe a domine. |
| Desempenho | 4 | Acrescenta uma chamada de rede à escrita; a latência de publicação não foi medida, e a folga da meta de 300 ms sugere que cabe. |
| Escalabilidade | 5 | Publicação direta em mensageria gerenciada escala com as instâncias da API, sem etapa intermediária. |
| Simplicidade | 4 | Acrescenta a mensageria, mas é conceito conhecido, com pouco código e emulador local. |
| Economia | 3 | A mensageria é paga por volume; o custo inicial fica baixo (não verificado) e o esforço não inclui a reconciliação de divergências. |

### HTTP síncrono entre os serviços (eliminada)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | Veto | Se o consolidado cair, a escrita falha ou perde o aviso, o que contraria a restrição central do negócio, e cada novo consumidor seria mais uma chamada. |
| Aderência ao problema | Veto | Não há transação entre dois bancos pela rede, e a garantia exigida só viria com retentativa e compensação na aplicação. |
| Confiabilidade | Veto | A escrita passa a depender do consolidado, exatamente o caso do veto. |
| Operabilidade | 5 | Não traz nada novo para operar e a equipe conhece HTTP. |
| Desempenho | 2 | Soma a latência do consolidado à escrita, e a escrita herda seus picos; deve caber em 300 ms em condição normal. |
| Escalabilidade | 3 | O consolidado vira gargalo da escrita, e o esforço cresce a cada consumidor. |
| Simplicidade | 5 | Sem componente novo, com conceito conhecido e execução local simples. |
| Economia | 4 | Sem componente pago, mas o consolidado precisa escalar junto com as escritas. |

## ADR-0003

**Mensageria.** [Ler o ADR](adr/0003-mensageria.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Confiabilidade | 3 | O transporte separa lançamentos do consolidado e carrega lançamentos confirmados: afeta duas restrições diretamente. |
| Operabilidade | 3 | A equipe não tem especialista em infraestrutura, e um broker é o item mais pesado de operar nesta decisão. |
| Aderência ao problema | 2 | O volume é de dezenas de eventos por segundo, então importa se a ferramenta tem o tamanho certo. |
| Economia | 2 | O custo fixo mínimo varia muito entre as opções. |
| Escalabilidade | 2 | Há cenário de 500 req/s e novos consumidores, mas não é a restrição imediata. |
| Simplicidade | 2 | A equipe é pequena e o conceito novo pesa. |
| Portabilidade | 1 | Trocar de transporte é possível, mas não há exigência de sair do GCP. |

### Pub/Sub (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Serviço do próprio GCP, sem cluster para montar, e cada novo consumidor, como conciliação, é só uma nova assinatura (https://docs.cloud.google.com/pubsub/docs/subscription-overview). |
| Confiabilidade | 5 | Serviço gerenciado com entrega at-least-once e reentrega sem ação manual; a repetição é segura desde que o assinante seja idempotente, como a documentação pede (https://docs.cloud.google.com/pubsub/docs/subscription-overview). |
| Operabilidade | 4 | É gerenciado e sem janela de manutenção conduzida pelo cliente; o domínio da equipe nesta tecnologia não foi informado, logo não verificado. |
| Aderência ao problema | 3 | Não é modelo relacional e, sendo at-least-once sem ordem por padrão, exige idempotência no consumidor, mas cobre durabilidade e cabe em dezenas de eventos por segundo sem capacidade reservada (https://docs.cloud.google.com/pubsub/docs/subscription-overview). |
| Economia | 3 | É componente pago novo e cobrado por volume, portanto cresce com a carga; o valor mensal em dezenas de eventos por segundo parece baixo, mas a tabela de preços não pôde ser lida e o valor é não verificado (https://cloud.google.com/pubsub/pricing). |
| Escalabilidade | 5 | Não há capacidade a dimensionar pelo cliente, e 500 mensagens por segundo está muito abaixo do uso típico do serviço; limite exato por projeto não verificado. |
| Simplicidade | 4 | Acrescenta um serviço, mas tópico, assinatura e confirmação são conceitos simples, e há emulador local, com limitações documentadas (https://docs.cloud.google.com/pubsub/docs/emulator). |
| Portabilidade | 2 | A API é proprietária e o emulador pode diferir do serviço real, mas as mensagens são transitórias e não prendem dados (https://docs.cloud.google.com/pubsub/docs/emulator). |

### Kafka gerenciado

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Está disponível em southamerica-east1 e aceita novos consumidores por grupo de consumo (https://docs.cloud.google.com/managed-service-for-apache-kafka/docs/locations). |
| Confiabilidade | 5 | Os clusters ficam distribuídos em três zonas com réplicas, e o log retido permite reler mensagens (https://docs.cloud.google.com/managed-service-for-apache-kafka/docs/overview). |
| Operabilidade | 4 | O serviço é gerenciado, mas a equipe ainda decide partições e tamanho do cluster; janela de atualização não verificada em detalhe e domínio da equipe não informado (https://docs.cloud.google.com/managed-service-for-apache-kafka/docs/overview). |
| Aderência ao problema | 2 | Tem garantias fortes, mas sobra capacidade: o mínimo é 3 vCPU e a própria Google estima cerca de 20 MiB/s de publicação por vCPU, muito acima de dezenas de eventos por segundo (https://cloud.google.com/managed-service-for-apache-kafka/pricing). |
| Economia | 3 | O mínimo de 3 vCPU e 3 GiB equivale a 2,1 DCU, cerca de US$ 138 por mês em us-central1 a US$ 0,09 por DCU-hora, cálculo próprio sem armazenamento, e o preço de São Paulo é não verificado (https://cloud.google.com/managed-service-for-apache-kafka/pricing). |
| Escalabilidade | 4 | Atende 500 mensagens por segundo com folga, mas partições e vCPUs são dimensionados pela equipe (https://cloud.google.com/managed-service-for-apache-kafka/pricing). |
| Simplicidade | 3 | Partições, offsets e rebalanceamento de grupos não são conceitos de uma equipe .NET típica, embora rode localmente em contêiner. |
| Portabilidade | 5 | Usa o protocolo Apache Kafka aberto, que roda igual em contêiner local e pode migrar de fornecedor (https://docs.cloud.google.com/managed-service-for-apache-kafka/docs/overview). |

### RabbitMQ autogerenciado

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Desacopla os serviços e aceita novas filas, mas exige montar VMs, cluster e correções antes do primeiro uso, o que pesa contra a equipe sem especialista. |
| Confiabilidade | 4 | Filas quorum em 3 nós com confirmação de publicação preservam mensagens, mas a recuperação de nó ou de partição de rede fica com a equipe (https://www.rabbitmq.com/docs/quorum-queues). |
| Operabilidade | 2 | A equipe opera VMs, correções, atualizações e discos sem especialista, e só a telemetria é padrão, por Prometheus (https://www.rabbitmq.com/docs/prometheus); o veto de especialista dedicado não foi marcado porque não há fato que o prove. |
| Aderência ao problema | 3 | Oferece confirmação de publicação e filas duráveis e cabe no volume, mas não é relacional e também exige idempotência no consumidor. |
| Economia | 2 | Três VMs com disco em São Paulo devem ficar perto ou acima de US$ 100, valor não verificado, e a manutenção contínua tende a passar de 1 pessoa-mês por ano. |
| Escalabilidade | 2 | 500 mensagens por segundo cabem em um cluster pequeno, mas cada fila tem um líder, o que cria gargalo, e crescer exige ação manual. |
| Simplicidade | 4 | A biblioteca é conhecida em .NET e roda em contêiner local, mas acrescenta um serviço a executar. |
| Portabilidade | 5 | AMQP é padrão aberto, e o mesmo broker roda no local e em qualquer nuvem. |

## ADR-0004

**Banco de dados.** [Ler o ADR](adr/0004-banco-de-dados.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Aderência ao problema | 3 | Afeta diretamente a restrição de não perder lançamento: o contexto exige transação entre lançamento e evento e agregação por período. |
| Confiabilidade | 3 | Afeta diretamente a restrição de não perder lançamento confirmado. |
| Operabilidade | 3 | Afeta diretamente a restrição de equipe sem especialista em infraestrutura. |
| Segurança | 3 | Afeta diretamente a restrição de dados isolados entre comerciantes. |
| Desempenho | 2 | Relevante: o banco está no caminho das metas de p95. |
| Economia | 2 | Relevante: o banco tende a ser o maior custo fixo. |
| Escalabilidade | 2 | Relevante: o banco precisa acompanhar o cenário de 500 req/s. |
| Simplicidade | 2 | Relevante: modelo novo pesa para equipe pequena. |
| Portabilidade | 1 | Secundário: o provedor está definido, mas dados financeiros têm vida longa. |

### Cloud SQL para PostgreSQL (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Oferece transação entre lançamento e evento, agregação em SQL e tem tudo gerenciado no GCP, com Npgsql no .NET. |
| Aderência ao problema | 5 | Relacional, com ACID, restrição de unicidade e agregação nativas, e dimensionado para este volume. |
| Confiabilidade | 5 | Na alta disponibilidade, a escrita é replicada de forma síncrona nos discos das duas zonas antes do commit, e o failover é automático em cerca de 60 s (docs.cloud.google.com/sql/docs/mysql/high-availability; o texto vem da página de MySQL e o mecanismo é o mesmo). |
| Operabilidade | 3 | Gerenciado, mas a edição Enterprise para menos de 30 s na manutenção e só a Enterprise Plus fica abaixo de 1 s (docs.cloud.google.com/sql/docs/postgres/editions-intro); não há informação de que a equipe domine PostgreSQL. |
| Segurança | 5 | Autenticação IAM com token de curta duração (cloud.google.com/sql) e row level security do PostgreSQL para isolar comerciantes no banco. |
| Desempenho | 5 | Folga para 50 a 500 req/s com índices adequados, embora o disco regional custe alguma latência. Não medido. |
| Economia | 3 | O custo é por instância, em degraus; o preço exato de 2 vCPU com alta disponibilidade em São Paulo não foi encontrado, e considerei acima de US$ 100 por mês, não verificado. |
| Escalabilidade | 2 | Comporta 500 req/s em instância modesta, mas a escrita fica em um primário único, que cresce na vertical. Não medido. |
| Simplicidade | 4 | SQL e Npgsql comuns, e o mesmo PostgreSQL roda em contêiner local. |
| Portabilidade | 5 | PostgreSQL padrão, com o mesmo motor local e em qualquer nuvem, e dados exportáveis por pg_dump. |

### AlloyDB

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Compatível com PostgreSQL, com São Paulo na tabela de preços (cloud.google.com/alloydb/pricing), e atende transação e agregação sem mudar o modelo. |
| Aderência ao problema | 4 | Relacional com transação ACID e SQL completo, mas seus recursos analíticos e de alta escala sobram muito para este volume. |
| Confiabilidade | 5 | O primário de alta disponibilidade tem dois nós, com failover automático, e SLA de 99,99% (páginas de produto e de preços do AlloyDB). |
| Operabilidade | 4 | Serviço gerenciado com SLA de 99,99%; manutenção sem parada relevante não verificada em detalhe; não há informação de que a equipe domine PostgreSQL. |
| Segurança | 5 | Herda row level security do PostgreSQL para o isolamento; a autenticação IAM do AlloyDB não foi confirmada na pesquisa e foi respondida por conhecimento prévio, não verificado. |
| Desempenho | 5 | Folga grande para 50 a 500 req/s com índices adequados. Não medido. |
| Economia | 3 | Nó de 2 vCPU e 16 GiB em São Paulo custa cerca de US$ 361 por mês segundo a Bytebase (terceiro), e o primário de alta disponibilidade usa dois nós; fica abaixo do veto, mas muito acima de US$ 100. |
| Escalabilidade | 3 | Pools de leitura crescem na horizontal, mas a escrita fica em um primário único, que cresce na vertical. |
| Simplicidade | 4 | Usa Npgsql e SQL comuns, e localmente roda com PostgreSQL ou AlloyDB Omni em contêiner (docs.cloud.google.com/alloydb/omni/docs/overview). |
| Portabilidade | 4 | Fala o protocolo PostgreSQL e exporta dados para ele, mas o motor é fechado, e o Omni não traz recursos que dependem do Google Cloud. |

### Spanner

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Atende transação e escala sem mudar a solução; instância de 100 PU permite começar pequeno (cloud.google.com/spanner/pricing). Disponibilidade em São Paulo não verificada nesta pesquisa. |
| Aderência ao problema | 4 | Relacional com transação forte, mas a escala global sobra muito para 50 a 500 req/s, e a interface PostgreSQL cobre só um subconjunto (página de produto do Spanner). |
| Confiabilidade | 5 | Replicação síncrona por quórum, com recuperação automática e sem failover a cargo da equipe; SLA não verificado nesta pesquisa. |
| Operabilidade | 4 | Gerenciado e sem janela de parada a cargo do cliente; não há informação de que a equipe domine o produto. |
| Segurança | 4 | Acesso nativo por IAM; suporte a isolamento por linha não foi confirmado na pesquisa, então considerei o isolamento dependente de código. |
| Desempenho | 4 | Latência estável com a carga, mas cada commit paga quórum entre réplicas. Não medido. |
| Economia | 2 | Google cita início em US$ 65 por mês e US$ 0,09 por 100 PU por hora (cloud.google.com/spanner/pricing), mas o preço de São Paulo não foi verificado; o custo cresce com as PUs e o modelo custa manutenção. |
| Escalabilidade | 5 | Escala horizontal da escrita aumentando processing units, sem primário único. |
| Simplicidade | 2 | Exige conceitos próprios, como chaves que evitam hotspot, e o emulador tem limitações e dados só em memória (cloud.google.com/spanner/docs/emulator). |
| Portabilidade | 1 | O emulador difere do serviço (sem IAM, sem persistência) e a interface PostgreSQL é um subconjunto, então trocar exige adaptar a aplicação. |

### Firestore

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Tem transação e é gerenciado no GCP, mas conciliação e relatórios por período exigem modelagem nova a cada consulta. |
| Aderência ao problema | 3 | Oferece transações serializáveis e sum/count/avg (firebase.google.com/docs/firestore/transaction-data-contention e /pricing), mas o modelo é de documentos e saldos pedem contadores desnormalizados mantidos pela aplicação. |
| Confiabilidade | 5 | Serviço replicado e gerenciado, com transação atômica que grava lançamento e evento juntos; repetição automática de transação é feita pelas bibliotecas. |
| Operabilidade | 4 | Sem servidor para operar e sem janela de manutenção a cargo do cliente; não há informação de que a equipe domine o produto. |
| Segurança | 4 | O acesso é por IAM, mas as bibliotecas de servidor ignoram as security rules (docs.cloud.google.com/firestore/native/docs/security/iam), então o isolamento entre comerciantes fica só no código. |
| Desempenho | 3 | Leitura por chave é rápida, mas o documento de saldo concorrido sofre contenção, e as bibliotecas falham com ABORTED após as repetições (firebase.google.com/docs/firestore/transaction-data-contention). |
| Economia | 2 | Cobra por operação, sem custo fixo relevante, mas o custo acompanha a carga, e agregações cobram por entradas de índice lidas (cloud.google.com/firestore/pricing); a modelagem compensatória custa manutenção. |
| Escalabilidade | 4 | Escala automaticamente, porém o saldo do comerciante em um documento único é um gargalo novo; limite de escrita por documento não verificado. |
| Simplicidade | 2 | Exige modelagem de documentos e desnormalização, e usa emulador localmente; paridade do emulador não verificada. |
| Portabilidade | 1 | API e modelo proprietários; trocar de fornecedor exige reescrever a camada de dados. |

## ADR-0005

**Modelo de leitura do saldo.** [Ler o ADR](adr/0005-modelo-de-leitura.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Aderência ao problema | 3 | Um lançamento retroativo muda dias seguintes, e o saldo precisa continuar correto. |
| Desempenho | 3 | A decisão afeta diretamente a meta de leitura p95 de 200 ms a 50 req/s. |
| Confiabilidade | 2 | O modelo define se a leitura pesa na escrita e como o saldo se recompõe. |
| Escalabilidade | 2 | O crescimento previsto é 500 req/s, e o custo de leitura muda conforme o histórico. |
| Operabilidade | 2 | Cache e event store acrescentam operação a uma equipe pequena. |
| Simplicidade | 2 | As opções vão de uma consulta até um novo paradigma. |
| Economia | 1 | O custo importa, mas o valor em jogo é pequeno. |

### Agregado diário sem cache (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Atende a meta com folga e mantém a leitura separada da escrita, usando só o banco já existente. |
| Aderência ao problema | 4 | Combina com dados relacionais e transação, mas o retroativo exige lógica no consumidor para refletir nos dias seguintes. |
| Desempenho | 5 | Medido: p95 de 6,54 ms a 50 leituras por segundo, 0% de falha, sem chamada adicional. |
| Confiabilidade | 5 | O agregado fica isolado da escrita e pode ser recomposto a partir dos eventos. |
| Escalabilidade | 4 | Leitura indexada por dia deve atender 500 req/s (não medido); a API escala sozinha, mas o banco é dimensionado à mão. |
| Operabilidade | 5 | Usa o PostgreSQL gerenciado, sem nada novo. |
| Simplicidade | 4 | Sem componente novo, mas o consumidor e a regra do retroativo são código próprio. |
| Economia | 5 | Sem componente pago novo, e o volume de linhas cresce por dia e comerciante, não por lançamento. |

### Agregado diário com cache Redis

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Atende a meta e existe como serviço gerenciado no GCP, mas sem ganho medido no volume atual. |
| Aderência ao problema | 2 | Cache e banco não têm transação comum, o retroativo exige invalidar dias seguintes, e o ganho não aparece neste volume. |
| Desempenho | 3 | Medido: p95 de 7,55 ms, 0% de falha, pior que 6,54 ms sem cache; acrescenta chamada e uma operação a mais por leitura. |
| Confiabilidade | 4 | O dado continua no banco e a leitura pode cair nele, mas a invalidação pode falhar e servir saldo antigo depois de um retroativo. |
| Escalabilidade | 3 | Reduz carga no banco em volumes maiores (não medido), mas o Redis é novo nó a dimensionar. |
| Operabilidade | 4 | O Memorystore é gerenciado, sem domínio informado na equipe. |
| Simplicidade | 3 | Acrescenta um serviço e o código de invalidação, com execução local simples. |
| Economia | 4 | É cobrado por GiB provisionado (https://cloud.google.com/memorystore/docs/redis/pricing); o preço em São Paulo não foi verificado, mas uma instância pequena deve ficar abaixo de US$ 100 por mês. |

### Consulta direta aos lançamentos

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 3 | É a entrega mais rápida, mas a leitura passa a disputar com a escrita e o custo cresce com o histórico, sem atender à separação pedida. |
| Aderência ao problema | 4 | Lançamento retroativo sai correto sem nenhuma lógica, mas recalcular a cada leitura não combina com 50 a 500 req/s. |
| Desempenho | 2 | Não há medição desta opção; somar os lançamentos a cada leitura custa mais conforme o volume cresce. |
| Confiabilidade | 4 | Sem cópia, nada se perde e nada diverge, mas leitura pesada atinge o banco da escrita. |
| Escalabilidade | 1 | Cada leitura cresce com o histórico e pesa no banco de lançamentos; 50 req/s é provável, mas não foi medido. |
| Operabilidade | 5 | Não acrescenta nada para operar. |
| Simplicidade | 5 | Uma consulta SQL, sem componente novo. |
| Economia | 4 | Sem componente novo, mas o banco precisa crescer com leituras e histórico. |

### Event sourcing completo

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Dá histórico completo e suporta novos consumidores, mas exige construção longa antes do primeiro uso. |
| Aderência ao problema | 1 | A leitura ainda precisa de projeção relacional, a concorrência fica na aplicação ou em biblioteca, e o modelo sobra para este volume. |
| Desempenho | 4 | A leitura viria de uma projeção igual ao agregado, porém com eventos e replay consumindo mais recursos; não medido. |
| Confiabilidade | 5 | Eventos imutáveis permitem recompor qualquer projeção. |
| Escalabilidade | 4 | Projeções atendem 500 req/s (não medido), com banco dimensionado à mão. |
| Operabilidade | 2 | Replay, versionamento de eventos e troca de projeções são operação própria, sem domínio informado na equipe. |
| Simplicidade | 2 | Exige conceito novo e bastante código, mesmo sobre PostgreSQL. |
| Economia | 3 | Pode rodar no banco existente, mas os eventos crescem sem fim e construir tende a passar de 1 pessoa-mês por ano (não verificado). |

## ADR-0006

**Plataforma de computação.** [Ler o ADR](adr/0006-computacao.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Confiabilidade | 3 | A plataforma define se a queda do consolidado fica isolada de lançamentos. |
| Operabilidade | 3 | Sem especialista em infraestrutura, a plataforma define quase toda a carga de operação. |
| Desempenho | 2 | Partida a frio pode afetar o p95 das metas. |
| Economia | 2 | São processos pequenos, então o custo fixo importa. |
| Escalabilidade | 2 | O crescimento a 500 req/s passa pela plataforma, mas 50 req/s não é o limite aqui. |
| Simplicidade | 2 | Equipe pequena, e o modelo da plataforma entra no dia a dia. |
| Portabilidade | 1 | Todas rodam contêineres, então pesa pouco. |
| Segurança | 1 | A identidade da carga muda pouco entre as opções, mas a superfície de acesso muda. |

### Cloud Run (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Está em southamerica-east1 e cada processo vira um serviço ou worker pool separado, embora a disponibilidade de worker pool em São Paulo não tenha sido verificada (https://docs.cloud.google.com/run/docs/locations). |
| Confiabilidade | 5 | Cada serviço escala e falha separado, e instâncias são recriadas pela plataforma, sem guardar dado confirmado (https://docs.cloud.google.com/run/docs/overview/what-is-cloud-run.md.txt). |
| Operabilidade | 4 | Não há nó nem cluster, e as revisões trocam sem parada; o domínio da equipe não foi informado (https://docs.cloud.google.com/run/docs/overview/what-is-cloud-run.md.txt). |
| Desempenho | 4 | Sem instância mínima há partida a frio, que exige ajuste para proteger o p95, mas com ela a latência fica estável; valor da partida em .NET não verificado. |
| Economia | 4 | Não há taxa de cluster e os serviços podem ir a zero, mas processos em segundo plano sempre ativos pagam o tempo todo; São Paulo é Tier 2 e o total não foi verificado (https://docs.cloud.google.com/run/docs/locations). |
| Escalabilidade | 5 | Escala por requisição e concorrência sem coordenação manual; o limite real passa a ser conexões do banco, ponto a cuidar, mas não novo gargalo da plataforma. |
| Simplicidade | 4 | Basta uma imagem e um serviço, sem conceito de orquestrador, e o contêiner roda localmente. |
| Portabilidade | 5 | Roda imagens OCI comuns, sem API proprietária na aplicação (https://docs.cloud.google.com/run/docs/overview/what-is-cloud-run.md.txt). |
| Segurança | 4 | Cada serviço usa conta de serviço própria e acesso por IAM ao Cloud SQL, sem SSH nem cluster, mas o isolamento entre comerciantes não é tratado aqui (https://docs.cloud.google.com/sql/docs/postgres/connect-run). |

### GKE Autopilot

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Separa os 4 processos, roda contêineres no GCP e aceita novos consumidores como novas cargas. |
| Confiabilidade | 5 | Pods reiniciam sozinhos e os processos são separados, com réplicas entre zonas, sem guardar dado confirmado. |
| Operabilidade | 4 | O Google gerencia nós e atualizações, mas a equipe ainda mantém objetos Kubernetes, e o domínio dela nessa tecnologia não foi informado. |
| Desempenho | 5 | Pods ficam sempre ativos, sem partida a frio por requisição, e a plataforma não acrescenta salto além do balanceador. |
| Economia | 3 | O crédito de US$ 74,40 por mês cobre a taxa de um cluster, mas não os pods; com réplicas para os 4 processos o valor pode passar de US$ 100 em São Paulo, número não verificado (https://cloud.google.com/anthos/pricing). |
| Escalabilidade | 5 | O autoescalonamento horizontal atende 500 req/s sem mudar a arquitetura. |
| Simplicidade | 3 | Pods, Deployments e Services não são conceitos que uma equipe .NET típica domina, embora os contêineres rodem localmente. |
| Portabilidade | 5 | Kubernetes e imagens OCI são abertos e movem-se entre fornecedores. |
| Segurança | 3 | Workload Identity evita chaves, mas o cluster acrescenta uma API de controle a proteger, e o isolamento entre comerciantes não é tratado aqui. |

### Compute Engine

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 3 | Cabe no GCP e isola processos em grupos de VMs, mas exige imagem, implantação e correção antes do primeiro uso, e cada novo consumidor repete esse trabalho. |
| Confiabilidade | 5 | Avaliado com um grupo gerenciado por processo e autorrecuperação, que isola falhas; sem isso, processos na mesma VM cairiam juntos. |
| Operabilidade | 3 | Grupos gerenciados fazem atualização gradual, mas sistema operacional, correções e imagem ficam com a equipe, o que pesa em uma instituição financeira. |
| Desempenho | 5 | VMs ficam sempre ativas, sem partida a frio, e sem salto extra além do balanceador. |
| Economia | 2 | Isolar 4 processos com 2 VMs cada dá 8 VMs mais balanceador, provavelmente acima de US$ 100 por mês, valor não verificado, e a manutenção das VMs é contínua. |
| Escalabilidade | 4 | O autoescalonamento de grupos gerenciados atende 500 req/s, mas cada processo novo exige outro grupo e outra imagem. |
| Simplicidade | 3 | VMs são conhecidas, mas a implantação, a autorrecuperação e os scripts de inicialização são código próprio. |
| Portabilidade | 5 | Os mesmos contêineres rodam em qualquer VM, sem API proprietária na aplicação. |
| Segurança | 3 | A conta de serviço anexada evita chaves, mas SSH e o sistema operacional são novos caminhos de acesso a proteger. |

## ADR-0007

**Linguagem e plataforma.** [Ler o ADR](adr/0007-linguagem.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. A stack do cliente está na lista de restrições. |
| Operabilidade | 3 | Afeta diretamente a restrição de equipe pequena: quem mantém precisa dominar a tecnologia. |
| Desempenho | 2 | Relevante: o runtime influi em latência e consumo, embora o trabalho seja dominado por E/S. |
| Simplicidade | 2 | Relevante: curva de aprendizado e código próprio mudam conforme a linguagem. |
| Economia | 1 | Secundário: as linguagens são gratuitas, e o custo vem da manutenção. |

### C# com .NET 10 (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | É a stack do cliente, e o .NET 10 é LTS com suporte até 14/11/2028 (dotnet.microsoft.com/platform/support/policy). |
| Operabilidade | 5 | Roda em contêiner gerenciado, com OpenTelemetry, e é a tecnologia que a equipe domina. |
| Desempenho | 5 | Runtime compilado e assíncrono, com folga para a meta nesta carga. Não medido. |
| Simplicidade | 5 | Tipo decimal nativo, ASP.NET no próprio framework e execução local direta, sem curva de aprendizado. |
| Economia | 4 | Sem custo de licença e sem treinamento; o custo de execução acompanha a carga como nas demais. |

### Go

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 2 | A stack principal do cliente, listada como restrição, é C# e .NET, e a equipe que vai manter domina C#; Go contraria isso. |
| Operabilidade | 4 | Roda em contêiner gerenciado com OpenTelemetry, mas a equipe domina C#, não Go. |
| Desempenho | 5 | Binário nativo, com partida rápida e pouca memória; a meta é folgada para esta carga. Não medido. |
| Simplicidade | 3 | Goroutines e tratamento de erro são novos para equipe .NET, e não há tipo decimal na biblioteca padrão, o que pede dependência ou código próprio. |
| Economia | 3 | Sem custo de licença, mas treinar ou contratar fora da stack custa mais que 1 pessoa-mês. Estimativa, não verificada. |

### Java com Spring Boot

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 2 | Comum em instituições financeiras, mas contraria a stack listada como restrição e a equipe que vai manter domina C#. |
| Operabilidade | 4 | Roda em contêiner com telemetria madura, mas a equipe domina C#, não JVM. |
| Desempenho | 4 | Cumpre a meta nesta carga, com memória maior por instância na JVM. Não medido. |
| Simplicidade | 4 | BigDecimal e ecossistema resolvem o problema, porém o ajuste de JVM e as convenções do Spring são novos para a equipe. |
| Economia | 3 | Sem custo de licença obrigatório, mas treinar ou contratar fora da stack custa mais que 1 pessoa-mês. Estimativa, não verificada. |

### TypeScript com Node.js

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 2 | Contraria a stack listada como restrição, e a equipe que vai manter domina C#. |
| Operabilidade | 4 | Roda em contêiner com telemetria padrão, mas a equipe domina C#, não Node.js. |
| Desempenho | 4 | Atende carga de E/S, mas usa uma thread de execução por processo, o que pede mais processos sob CPU. Não medido. |
| Simplicidade | 4 | Sintaxe próxima de C#, mas o tipo number é ponto flutuante e dinheiro exato pede biblioteca e cuidado próprios. |
| Economia | 3 | Sem custo de licença, mas treinar ou contratar fora da stack custa mais que 1 pessoa-mês. Estimativa, não verificada. |

## ADR-0008

**Autenticação e autorização.** [Ler o ADR](adr/0008-autenticacao-e-autorizacao.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Segurança | 3 | A identidade do cliente sustenta o isolamento de dados exigido por uma instituição financeira. |
| Operabilidade | 2 | Credenciais e certificados precisam de rotação e operação contínuas. |
| Simplicidade | 2 | As opções variam em componentes e no esforço de cada cliente. |
| Desempenho | 1 | A validação pesa pouco diante da folga de latência. |
| Economia | 1 | Há diferença de custo, mas ela é secundária. |
| Portabilidade | 1 | O padrão importa para clientes externos, mas é secundário. |

### Gateway, OIDC e JWT (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Dá identidade e escopos por padrões conhecidos, e o ASP.NET Core valida JWT; o provedor de identidade ainda precisa ser escolhido. |
| Segurança | 4 | Tokens curtos e assinados trazem a identidade e facilitam auditar por cliente, mas o client secret segue como credencial do cliente, e o isolamento dos dados depende de outra camada. |
| Operabilidade | 5 | Gateway gerenciado e validação JWT nativa no .NET; o provedor de identidade pode ser gerenciado. |
| Simplicidade | 4 | Acrescenta gateway e provedor de identidade, mas o conceito é conhecido e a API testa localmente com tokens de desenvolvimento. |
| Desempenho | 4 | O gateway acrescenta um salto, mas a validação do JWT é local, sem chamada por requisição. |
| Economia | 3 | O API Gateway tem 2 milhões de chamadas grátis por mês e depois US$ 3 por milhão (https://cloud.google.com/api-gateway/pricing), logo o custo acompanha a carga, e 500 req/s contínuos passariam de US$ 3 mil por mês; o provedor de identidade não foi precificado. |
| Portabilidade | 4 | OIDC e JWT são abertos, mas o gateway não existe no ambiente local. |

### Chaves de API

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 3 | Identifica o comerciante e sai rápido, mas é credencial longa, fraca para uma instituição financeira, e sem escopos para evoluir. |
| Segurança | 2 | É segredo longo enviado em cada chamada, sujeito a vazar em log; identifica o cliente, mas não quem usou; não expõe dados por si, então não há veto. |
| Operabilidade | 4 | Armazenar, rotacionar e revogar chaves fica com a equipe, com tecnologia conhecida. |
| Simplicidade | 4 | Sem componente novo, mas exige código para guardar e rotacionar chaves. |
| Desempenho | 4 | Exige consultar a chave, em banco ou cache, salvo se validada em outra camada. |
| Economia | 5 | Sem componente pago, e o custo não cresce com a carga. |
| Portabilidade | 4 | Funciona igual em todo lugar, mas não há padrão aberto para o ciclo de vida das chaves. |

### mTLS para todo cliente

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 3 | Dá identidade forte e é viável no balanceador do GCP (https://docs.cloud.google.com/load-balancing/docs/mtls), mas exige certificado por comerciante antes do primeiro uso e não traz escopos de autorização. |
| Segurança | 3 | A chave privada do certificado é credencial longa, com rotação própria; o certificado identifica o cliente, e o isolamento dos dados continua dependendo de outra camada. |
| Operabilidade | 3 | O balanceador é gerenciado, mas emitir, renovar e revogar certificados de cada comerciante fica com a equipe; pesado, sem chegar a especialista dedicado. |
| Simplicidade | 2 | Exige balanceador, trust config e cadeia de certificados, e é difícil reproduzir localmente. |
| Desempenho | 4 | Acrescenta o salto do balanceador; a validação do certificado ocorre no handshake. |
| Economia | 3 | Soma o balanceador e o esforço de certificados; o valor exato não foi verificado. |
| Portabilidade | 4 | É padrão aberto, mas a terminação no balanceador difere do ambiente local. |

## ADR-0009

**Observabilidade.** [Ler o ADR](adr/0009-observabilidade.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Operabilidade | 3 | Sem especialista, a telemetria é o meio de operar e de provar as metas de p95. |
| Economia | 2 | Licença e volume de dados variam bastante entre as opções. |
| Portabilidade | 2 | O fato exige que a instrumentação rode no local e no GCP, mas isso não é restrição do negócio. |
| Simplicidade | 2 | A instrumentação entra em todos os serviços. |
| Segurança | 1 | Dados de telemetria podem sair para terceiros, o que é secundário mas real em uma instituição financeira. |

### OpenTelemetry (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Mede as metas, é compatível com .NET e com o GCP, e novos consumidores usam a mesma instrumentação (https://docs.cloud.google.com/stackdriver/docs/reference/telemetry/overview). |
| Operabilidade | 4 | O GCP recebe OTLP em telemetry.googleapis.com, e exportar direto dispensa coletor, embora a Google recomende um; o domínio da equipe não foi informado (https://docs.cloud.google.com/stackdriver/docs/reference/telemetry/overview). |
| Economia | 5 | A biblioteca é gratuita e a amostragem limita o volume, sem coletor; tarifas de ingestão não verificadas. |
| Portabilidade | 5 | É padrão aberto, e o mesmo código exporta OTLP para ferramenta local ou para o GCP (https://docs.cloud.google.com/stackdriver/docs/reference/telemetry/overview). |
| Simplicidade | 4 | É biblioteca e não serviço, mas spans, exportadores e convenções semânticas são conceitos novos para parte das equipes; roda no local com um console ou painel. |
| Segurança | 4 | A API OTLP do GCP usa credenciais padrão do Google e os dados ficam no provedor (https://docs.cloud.google.com/stackdriver/docs/reference/telemetry/overview). |

### SDK do provedor de nuvem

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Mede as metas e é rápido no GCP, mas fica preso a um backend e trocar exige refazer a instrumentação. |
| Operabilidade | 4 | O backend é gerenciado e a telemetria é padrão no GCP; o domínio da equipe não foi informado. |
| Economia | 5 | Usa serviços já presentes no GCP e a amostragem limita o volume; tarifas de ingestão não verificadas. |
| Portabilidade | 1 | Sem credenciais do GCP a instrumentação não tem paridade no local, e a API e os dados seguem o fornecedor, o que contraria o fato da decisão. |
| Simplicidade | 4 | Não acrescenta serviço e usa biblioteca pronta, mas rodar no local exige credenciais ou desligar o envio. |
| Segurança | 4 | Autentica por credenciais padrão do Google e os dados ficam no GCP, sem efeito sobre o isolamento entre comerciantes. |

### APM comercial

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Entrega painéis rápido e evolui, mas enviar dados a um terceiro exige aprovação em instituição financeira, e essa aprovação não foi verificada. |
| Operabilidade | 4 | O produto é gerenciado e pronto, mas a equipe precisaria conhecê-lo; domínio não informado. |
| Economia | 2 | Acrescenta licença paga cobrada por host ou por volume; preço não verificado, pois não há produto específico. |
| Portabilidade | 1 | O agente e o backend são do fornecedor, e o local depende de conta nele; suporte a OTLP varia por produto e não foi verificado. |
| Simplicidade | 3 | Acrescenta agente em cada serviço, embora a instrumentação automática exija pouco código. |
| Segurança | 2 | Costuma usar chave de API de longa duração e envia dados para fora do GCP, o que abre novo caminho de exposição. |

## ADR-0010

**Estratégia de migração.** [Ler o ADR](adr/0010-estrategia-de-migracao.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Aderência ao problema | 3 | Afeta diretamente a mesma restrição: sistema financeiro exige garantia sem compensação frágil. |
| Confiabilidade | 3 | Afeta diretamente a restrição de não perder lançamento confirmado durante a migração. |
| Operabilidade | 2 | Relevante: a migração precisa ser conduzida por equipe pequena. |
| Simplicidade | 2 | Relevante: código temporário de migração pesa para equipe pequena. |
| Economia | 1 | Secundário: é custo temporário. |

### Strangler fig, por fases (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Entrega por fases, mantém o sistema em uso e cada fase prepara a seguinte. |
| Aderência ao problema | 4 | Mantém transação dentro de cada dono, mas a coexistência com banco compartilhado pede sincronização temporária. |
| Confiabilidade | 4 | Cada fase tem um dono do dado e volta atrás pela rota, mas a fachada de roteamento é um ponto único novo. |
| Operabilidade | 5 | O roteamento pode usar balanceador gerenciado, sem parada, e a mudança é gradual, com tecnologia da equipe. |
| Simplicidade | 3 | Exige fachada e código temporário de sincronização, e é um padrão conhecido. |
| Economia | 2 | A coexistência dura mais e pede fachada e esforço por fase, acima de 1 pessoa-mês. Estimativa, não verificada. |

### Migração de uma vez

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 2 | Só entrega valor no fim e concentra o risco em uma virada; nenhuma restrição listada proíbe parada planejada, por isso sem veto. |
| Aderência ao problema | 4 | Sem coexistência não há compensação, mas o porte de uma virada única não combina com sistema financeiro em uso. |
| Confiabilidade | 2 | Há uma só fonte de verdade por vez, mas a virada é ponto único de risco, e voltar atrás é manual e perde o que foi escrito depois dela. |
| Operabilidade | 3 | Exige janela de parada e operação manual da virada. |
| Simplicidade | 4 | Não há roteamento nem sincronização, porém ensaiar a virada completa é difícil na máquina local. |
| Economia | 4 | Sem componente novo e sem coexistência longa, mas a construção é grande antes do uso. Valores não verificados. |

### Escrita dupla nos dois sistemas (eliminada)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | Veto | Sem transação entre os dois bancos, um lançamento pode ser confirmado em um só; ao trocar para o outro, ele some, o que contraria a restrição de não perder lançamento confirmado. |
| Aderência ao problema | Veto | Dois bancos independentes não oferecem atomicidade, e a garantia só vem com lógica compensatória na aplicação. |
| Confiabilidade | Veto | Falha parcial entre as duas gravações é comum e deixa os sistemas divergentes, e recuperar exige reconciliação. |
| Operabilidade | 4 | Não exige parada, mas a equipe opera dois sistemas e acompanha divergências. |
| Simplicidade | 3 | Parece simples, mas exige código de gravação dupla e de reconciliação. |
| Economia | 2 | Dois sistemas ativos, com esforço de reconciliação. Valores não verificados. |

## ADR-0011

**Isolamento entre comerciantes.** [Ler o ADR](adr/0011-isolamento-de-comerciantes.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Operabilidade | 3 | Muitos comerciantes multiplicam migrações e operação para uma equipe pequena. |
| Segurança | 3 | A decisão define diretamente o isolamento de dados exigido. |
| Aderência ao problema | 2 | A garantia de isolamento pode ser nativa ou ficar na aplicação. |
| Economia | 2 | Comerciantes pequenos não pagam um custo fixo por unidade. |
| Escalabilidade | 2 | O número de comerciantes e de conexões cresce com o negócio. |
| Simplicidade | 2 | A complexidade de migrações e de acesso muda muito entre opções. |
| Desempenho | 1 | As diferenças são pequenas nas leituras simples. |

### Tabelas compartilhadas com row-level security (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | O banco aplica o isolamento, comerciante novo é só uma linha, e o modelo aceita novos consumidores com papel próprio. |
| Operabilidade | 4 | É recurso do PostgreSQL gerenciado, mas nem toda equipe .NET conhece RLS. |
| Segurança | 3 | A política fica no banco, com negação por padrão, mas o dono da tabela escapa sem FORCE ROW LEVEL SECURITY, e superusuários e BYPASSRLS sempre escapam (https://www.postgresql.org/docs/current/ddl-rowsecurity.html); a identidade vem da sessão definida pela aplicação. |
| Aderência ao problema | 5 | O PostgreSQL oferece a garantia nativamente, sem lógica na aplicação além de definir o comerciante da sessão. |
| Economia | 5 | Sem custo novo por comerciante. |
| Escalabilidade | 4 | Comerciante novo não muda a estrutura; o banco é dimensionado à mão. |
| Simplicidade | 4 | Sem componente novo, mas exige políticas e contexto por transação, conceitos que uma equipe .NET típica pode desconhecer. |
| Desempenho | 5 | Acrescenta um predicado com índice; o custo não foi medido aqui. |

### Tabelas compartilhadas com filtro no código

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 3 | É rápida e barata, mas o isolamento depende só de cada consulta, fraco para uma instituição financeira. |
| Operabilidade | 5 | Um banco e um esquema só. |
| Segurança | 2 | Um filtro esquecido expõe dados de outro comerciante, sem barreira no banco; é risco, não exposição certa, por isso não há veto. |
| Aderência ao problema | 3 | Modelo relacional comum, mas o isolamento não é garantia nativa e fica na aplicação. |
| Economia | 5 | Sem custo novo por comerciante. |
| Escalabilidade | 4 | Índice por comerciante atende o crescimento, com banco dimensionado à mão. |
| Simplicidade | 5 | Filtro global no ORM, sem conceito novo. |
| Desempenho | 5 | Um predicado indexado, sem chamada nova. |

### Esquema por comerciante

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 3 | Separa com GRANT, mas cada comerciante novo exige criar esquema e migrar, e consultas entre comerciantes ficam difíceis. |
| Operabilidade | 3 | As migrações se repetem por esquema; o efeito de milhares de esquemas no catálogo não foi verificado. |
| Segurança | 3 | Permissões por esquema dão barreira no banco, desde que cada comerciante tenha papel próprio. |
| Aderência ao problema | 3 | Tem transação nativa, mas o roteamento fica na aplicação e o modelo sobra para comerciantes pequenos. |
| Economia | 4 | Sem custo de infraestrutura novo, mas manter migrações por esquema custa esforço (não verificado). |
| Escalabilidade | 2 | Req/s não é o limite, mas o trabalho cresce por comerciante, e o efeito no catálogo não foi verificado. |
| Simplicidade | 4 | É conceito conhecido, com migração e roteamento próprios. |
| Desempenho | 3 | Leitura simples não muda, mas o cache de catálogo por conexão tende a crescer; não verificado. |

### Banco por comerciante

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 3 | Isola com força, mas cada comerciante novo pede provisionamento, e uma visão entre comerciantes, como conciliação, fica difícil. |
| Operabilidade | 3 | O banco é gerenciado, mas migrações e monitoração se repetem por comerciante; pesado para muitos comerciantes pequenos, sem chegar a especialista dedicado. |
| Segurança | 3 | O isolamento é físico, mas multiplica credenciais a gerir. |
| Aderência ao problema | 3 | Tem transação nativa, mas o roteamento fica na aplicação e o modelo sobra para comerciantes pequenos. |
| Economia | 3 | Na mesma instância não há custo novo, mas o esforço cresce por comerciante. |
| Escalabilidade | 1 | Cada banco tem pool próprio, e o limite de conexões é da instância; com muitos comerciantes, conexões viram gargalo. |
| Simplicidade | 4 | É conceito simples, com roteamento e migrações próprias. |
| Desempenho | 2 | Pools fragmentados aumentam conexões e consumo de memória; não medido. |

## ADR-0012

**Acesso a dados.** [Ler o ADR](adr/0012-acesso-a-dados.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Aderência ao problema | 3 | As consultas críticas sustentam a garantia de lançamento confirmado por meio de FOR UPDATE SKIP LOCKED e ON CONFLICT. |
| Desempenho | 2 | A biblioteca fica no caminho das metas de p95, embora o banco domine o tempo. |
| Simplicidade | 2 | Há poucas consultas, então o peso da ferramenta precisa ser proporcional. |

### Dapper (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Biblioteca .NET sobre Npgsql, que aceita o SQL crítico sem contorno. |
| Aderência ao problema | 5 | Envia o SQL escrito tal como está, com transação do Npgsql, e o tamanho combina com poucas consultas. |
| Desempenho | 5 | A camada é fina e o mapeamento é leve; não acrescenta chamada de rede. |
| Simplicidade | 5 | É uma biblioteca pequena e conhecida, que evita o mapeamento manual de resultados. |

### Npgsql com SQL explícito

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | É o driver oficial de PostgreSQL para .NET, necessário nas outras opções também, e atende às consultas críticas. |
| Aderência ao problema | 5 | Dá acesso direto a todo o SQL do PostgreSQL e às transações, sem camada extra, e cabe em poucas consultas. |
| Desempenho | 5 | É a camada mais fina, sem mapeador, e não acrescenta chamada de rede. |
| Simplicidade | 4 | Não acrescenta dependência, mas o mapeamento de leitura é código próprio, ainda que pequeno por haver poucas consultas. |

### EF Core

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | É a escolha padrão em .NET e atende às consultas críticas por SQL bruto, com FromSql para o lock (https://milanjovanovic.tech/blog/pessimistic-locking-ef-core-postgresql). |
| Aderência ao problema | 3 | Tem transações, mas não tem API própria de lock, então a consulta crítica vai em SQL escrito à mão, e o ORM sobra para poucas consultas (https://milanjovanovic.tech/blog/pessimistic-locking-ef-core-postgresql). |
| Desempenho | 4 | Cumpre as metas, mas rastreamento de mudanças e tradução de consultas consomem mais por operação; números não verificados. |
| Simplicidade | 5 | É biblioteca conhecida, com pouco código de mapeamento, e roda no local com PostgreSQL. |

## ADR-0013

**Migrações de esquema.** [Ler o ADR](adr/0013-migracoes-de-esquema.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Simplicidade | 3 | Afeta diretamente a restrição de equipe pequena: a ferramenta não deve trazer runtime ou modelo a manter. |
| Aderência ao problema | 2 | Relevante: a aplicação não usa ORM, e a ferramenta precisa combinar com isso. |
| Operabilidade | 2 | Relevante: a ferramenta roda em toda implantação. |
| Economia | 1 | Secundário: as opções têm versão gratuita. |
| Portabilidade | 1 | Secundário: scripts SQL sobrevivem à troca da ferramenta. |

### DbUp com SQL (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Biblioteca .NET com provedor dbup-postgresql (github.com/DbUp/DbUp), dentro da stack do cliente. |
| Simplicidade | 5 | Um pacote NuGet e scripts SQL com tabela de journal; roda como programa .NET comum, inclusive localmente (dbup.readthedocs.io). |
| Aderência ao problema | 5 | Trabalha com SQL puro, sem modelo de ORM; o modo transacional não foi confirmado na página do projeto e foi respondido por conhecimento prévio, não verificado. |
| Operabilidade | 5 | Não há serviço a operar, e usa a linguagem que a equipe domina; a parada depende do script, não da ferramenta. |
| Economia | 5 | Licença MIT (github.com/DbUp/DbUp), sem custo e com manutenção pequena. |
| Portabilidade | 5 | Os scripts são SQL padrão do PostgreSQL e valem com outra ferramenta. |

### Flyway

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Cumpre a função com SQL, mas é ferramenta de JVM fora da stack .NET do cliente; roda por CLI ou contêiner. |
| Simplicidade | 4 | Scripts SQL simples, com um executável ou contêiner a mais no fluxo. |
| Aderência ao problema | 5 | Foi feito para migração em SQL, sem depender de ORM. |
| Operabilidade | 4 | Não há serviço permanente, mas a equipe .NET não domina o runtime da ferramenta. |
| Economia | 5 | A edição Community é gratuita; a Redgate já moveu recursos entre edições (github.com/flyway/flyway/issues/3881), o que é risco a acompanhar, sem custo hoje. |
| Portabilidade | 5 | Scripts SQL padrão e edição de código aberto. |

### EF Core Migrations

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Pertence à stack .NET, mas a aplicação não usa ORM, e adotá-lo só para o esquema cria dependência que não apoia o desenho. |
| Simplicidade | 3 | Conhecido por equipes .NET, mas exige manter entidades e DbContext sem uso em tempo de execução. |
| Aderência ao problema | 2 | Cada migração inclui um snapshot do modelo, e não é só SQL (learn.microsoft.com/ef/core/managing-schemas/migrations/teams), o que não combina com aplicação sem ORM; gera script idempotente. |
| Operabilidade | 5 | Bundles e scripts idempotentes rodam sem serviço novo (learn.microsoft.com/ef/core/managing-schemas/migrations/applying), e a equipe conhece a ferramenta. |
| Economia | 4 | Sem custo de licença, mas manter um modelo só para o esquema é trabalho contínuo. Esforço não medido. |
| Portabilidade | 5 | Código aberto, igual em local e nuvem, e o esquema resultante é PostgreSQL comum. |

## ADR-0014

**Separação de processos.** [Ler o ADR](adr/0014-separacao-de-processos.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Confiabilidade | 3 | Envio e consumo de eventos sustentam a garantia de não perder lançamento e o isolamento de falhas. |
| Desempenho | 2 | O trabalho contínuo pode disputar recursos com a API. |
| Economia | 2 | Instância sempre ativa tem custo fixo. |
| Escalabilidade | 2 | API e trabalho contínuo têm cargas diferentes. |
| Operabilidade | 2 | O modo de execução no Cloud Run muda o que a equipe opera. |
| Simplicidade | 2 | O número de recursos a publicar muda entre as opções. |

### Processos separados (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | O Cloud Run tem worker pools para trabalho contínuo (https://docs.cloud.google.com/run/docs/deploy-worker-pools), e a API fica isolada dos picos do trabalho de fundo. |
| Confiabilidade | 5 | A queda do worker não atinge a API e vice-versa, e o trabalho retoma do ponto pendente. |
| Desempenho | 5 | O trabalho de fundo não disputa CPU com a API. |
| Economia | 4 | É um recurso a mais, sempre ativo, pago; uma instância pequena deve ficar abaixo de US$ 100 por mês (não verificado). |
| Escalabilidade | 4 | Cada parte cresce por si, mas worker pools não têm autoscaling (https://docs.cloud.google.com/run/docs/deploy-worker-pools), e o número de instâncias é definido à mão. |
| Operabilidade | 5 | Ambos são Cloud Run gerenciado, com código .NET comum. |
| Simplicidade | 4 | Dois processos para publicar, com serviço de fundo comum no .NET. |

### Job agendado

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Serve ao saldo diário com atraso, mas não é trabalho contínuo, e novos consumidores herdam o atraso do agendamento. |
| Confiabilidade | 5 | Falha afeta só a execução, e a seguinte retoma do pendente, desde que execuções sobrepostas sejam evitadas. |
| Desempenho | 5 | Roda fora da API, sem disputar recursos; a meta da régua não cobre atraso de propagação, que chega a cerca de 1 minuto (inferência da resolução do cron, não verificada). |
| Economia | 4 | Soma Scheduler e execuções, pagas só enquanto rodam; o preço atual não foi verificado. |
| Escalabilidade | 4 | Lotes podem atender a carga, mas o paralelismo é configurado à mão. |
| Operabilidade | 5 | Cloud Run Jobs com Cloud Scheduler são gerenciados (https://docs.cloud.google.com/run/docs/execute/jobs-on-schedule). |
| Simplicidade | 4 | Soma job e agendamento, com loop em lote simples. |

### Worker dentro da API

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Sai rápido, mas exige cobrança por instância, e API e worker não podem crescer separadamente. |
| Confiabilidade | 4 | A falha de um derruba o outro; sem cobrança por instância, a CPU existe só durante requisições, e instâncias ociosas podem ser encerradas (https://docs.cloud.google.com/run/docs/configuring/billing-settings); o trabalho pendente é retomado depois. |
| Desempenho | 3 | Drenar uma fila disputa CPU com a API e prejudica a latência em picos. |
| Economia | 4 | Sem recurso novo, mas a cobrança por instância paga o ciclo inteiro, mesmo ocioso (https://docs.cloud.google.com/run/docs/configuring/billing-settings). |
| Escalabilidade | 2 | O Cloud Run escala a API por requisições, não pela fila de trabalho, logo o worker escala junto com a carga errada. |
| Operabilidade | 5 | Um só recurso para operar. |
| Simplicidade | 5 | Um processo só, simples de rodar localmente. |

## ADR-0015

**Envio da outbox.** [Ler o ADR](adr/0015-envio-da-outbox.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Confiabilidade | 3 | O envio é o caminho do lançamento confirmado até o consolidado e precisa resistir à queda da mensageria. |
| Operabilidade | 3 | Sem especialista, um componente de replicação pesa diretamente na restrição da equipe. |
| Aderência ao problema | 2 | O volume é de dezenas de eventos por segundo, e a opção precisa ter esse tamanho. |
| Desempenho | 2 | A opção pode tocar o commit da escrita, que tem meta de p95. |
| Escalabilidade | 2 | O envio precisa acompanhar o cenário de 500 req/s. |
| Simplicidade | 2 | Equipe pequena, e o código de envio é próprio. |
| Economia | 1 | O custo só muda de forma relevante com um conector à parte. |

### Consulta periódica com SKIP LOCKED (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Medido: fechou o saldo 4 s depois de 50 escritas por segundo com queda da mensageria, sem nenhum componente novo. |
| Confiabilidade | 5 | Medido com queda da mensageria, sem perda; a outbox retoma sozinha e várias instâncias podem ler sem disputa por causa do SKIP LOCKED. |
| Operabilidade | 5 | É SQL e C# dentro de um processo existente, com telemetria comum, e não há nada novo para operar. |
| Aderência ao problema | 5 | Usa transação e SKIP LOCKED nativos do PostgreSQL, e o intervalo de consulta cabe no volume, como mostra o fato medido. |
| Desempenho | 5 | A consulta roda fora do caminho da escrita e por lotes, com índice sobre pendentes; a espera adicional só atinge o saldo, que é assíncrono. |
| Escalabilidade | 5 | Lotes e instâncias paralelas com SKIP LOCKED crescem sem coordenação; 500 eventos por segundo não foi medido. |
| Simplicidade | 5 | É um laço simples com uma consulta, e roda no local com o próprio PostgreSQL. |
| Economia | 5 | Não acrescenta componente nem custo fixo. |

### LISTEN/NOTIFY com consulta de segurança

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Reduz a espera até publicar, mantém a consulta como rede de segurança e usa apenas o PostgreSQL. |
| Confiabilidade | 5 | A notificação pode se perder, mas a consulta de segurança recupera o evento pendente, e a outbox guarda o dado. |
| Operabilidade | 4 | Não há serviço novo, mas o LISTEN exige conexão direta ou em modo sessão, e falha silenciosa em pool de transação é um risco a monitorar (https://pigsty.io/docs/pgbouncer/features/). |
| Aderência ao problema | 3 | O PostgreSQL oferece o recurso, mas a consulta de segurança compensa notificações perdidas, e a latência extra ganha não é exigida: o fato medido mostra 4 s com a consulta simples. |
| Desempenho | 4 | Não há chamada de rede nova, mas a trava de NOTIFY no commit pode pesar na escrita com carga alta; o efeito a 500 req/s não foi medido (https://recall.ai/blog/postgres-listen-notify-does-not-scale). |
| Escalabilidade | 4 | Atende o volume, mas NOTIFY toma trava global no commit, o que serializa commits com muitos escritores; a correção em versões novas não foi verificada (https://recall.ai/blog/postgres-listen-notify-does-not-scale). |
| Simplicidade | 3 | Mantém dois caminhos de código, com conexão dedicada, e o conceito é pouco comum em equipes .NET. |
| Economia | 5 | Não acrescenta componente pago, e o código extra é pequeno. |

### CDC

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Publica sem laço próprio, mas exige um conector antes do primeiro uso, e o Datastream não publica no Pub/Sub, então a saída seria Debezium (https://docs.cloud.google.com/datastream/docs/destinations). |
| Confiabilidade | 3 | Um slot de replicação retém WAL se o conector parar, o que pode encher o disco do banco de escrita, e o conector é um novo ponto único (https://debezium.io/documentation/reference/stable/operations/debezium-server.html). |
| Operabilidade | 2 | Debezium Server tem sink de Pub/Sub, mas é um serviço que a equipe executa, e o slot precisa de cuidado em failover e atualização (https://debezium.io/documentation/reference/stable/operations/debezium-server.html). |
| Aderência ao problema | 4 | Lê o WAL com transação preservada e não precisa de laço próprio, mas é mais ferramenta do que dezenas de eventos por segundo pedem. |
| Desempenho | 5 | A leitura do WAL fica fora da transação de escrita e o custo por evento é baixo. |
| Escalabilidade | 3 | O fluxo do WAL aguenta 500 eventos por segundo, mas um slot por conector é um gargalo único, e não escala horizontalmente sozinho. |
| Simplicidade | 2 | Acrescenta serviço, replicação lógica e formato de eventos do conector; o ambiente local fica mais pesado. |
| Economia | 3 | Acrescenta um processo sempre ativo, e o custo fixo deve ficar abaixo de US$ 100, valor não verificado, mas manter slot e conector custa mais tempo que as outras opções. |

## ADR-0016

**Idempotência da escrita.** [Ler o ADR](adr/0016-idempotencia-da-escrita.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Aderência ao problema | 3 | Afeta diretamente a mesma restrição: a garantia de idempotência é o próprio objeto da decisão. |
| Confiabilidade | 3 | Afeta diretamente a restrição de não perder lançamento: repetir a operação precisa ser seguro. |
| Desempenho | 2 | Relevante: a verificação fica no caminho da escrita. |
| Operabilidade | 2 | Relevante: um novo armazenamento precisa ser operado. |
| Simplicidade | 2 | Relevante: componente e código a mais pesam para equipe pequena. |
| Economia | 1 | Secundário: os valores são pequenos. |

### Idempotency-Key guardada no banco (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | A chave distingue repetição de lançamento legítimo igual e não traz dependência nova para a escrita. |
| Aderência ao problema | 5 | A restrição de unicidade do banco resolve chamadas simultâneas, sem lógica compensatória. |
| Confiabilidade | 5 | Chave e lançamento são gravados na mesma transação, então ou existem os dois, ou nenhum. |
| Desempenho | 4 | Usa a mesma conexão e transação, com o custo de um índice a mais por escrita; a tabela de chaves precisa de expurgo. Não medido. |
| Operabilidade | 5 | Usa o banco que a equipe já opera, sem peça nova. |
| Simplicidade | 5 | Uma tabela ou coluna e uma restrição única, e o teste roda no mesmo banco local. |
| Economia | 5 | Sem componente novo, só um pouco de armazenamento. |

### Idempotency-Key guardada no Redis (eliminada)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | A chave distingue os lançamentos legítimos e trata chamadas simultâneas, mas não participa da transação do lançamento. |
| Aderência ao problema | Veto | O Google avisa que a aplicação precisa tolerar perda de gravações confirmadas no failover, e chave e lançamento não ficam na mesma transação, então a garantia contra duplicidade não existe nesses casos. |
| Confiabilidade | 3 | Replicação assíncrona pode perder gravações confirmadas no failover (docs.cloud.google.com/memorystore/docs/redis/high-availability-for-memorystore-for-redis): o lançamento fica no banco, mas a chave some e a repetição duplica; sem veto porque o efeito é duplicar, não perder. |
| Desempenho | 4 | A consulta é rápida, mas acrescenta uma chamada de rede ao caminho da escrita. |
| Operabilidade | 3 | Gerenciado, mas o failover deixa o serviço indisponível por cerca de 30 s (FAQ do Memorystore); não há informação de que a equipe domine Redis. |
| Simplicidade | 3 | Acrescenta um componente e código para tratar o intervalo entre a chave e o commit. |
| Economia | 3 | Componente pago novo; com US$ 0,064 por GiB-hora em Iowa no Standard, 1 GiB fica perto de US$ 47 por mês (fitgap, terceiro); preço de São Paulo não verificado. |

### Deduplicar por conteúdo do lançamento (eliminada)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | Veto | Dois lançamentos legítimos e iguais no mesmo dia podem existir, e o segundo seria descartado como repetido, o que perde lançamento real. |
| Aderência ao problema | Veto | O conteúdo não distingue repetição de lançamento legítimo igual, portanto não oferece a garantia exigida. |
| Confiabilidade | Veto | O segundo lançamento legítimo receberia resposta de sucesso sem ser gravado, e esse caso é comum, não exceção. |
| Desempenho | 5 | Índice único no próprio banco, sem chamada adicional. |
| Operabilidade | 5 | Não há nada novo a operar. |
| Simplicidade | 5 | Só um hash e um índice único, sem contrato novo com o cliente. |
| Economia | 5 | Não há componente novo. |

## ADR-0017

**Vazão do consumidor.** [Ler o ADR](adr/0017-vazao-do-consumidor.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Confiabilidade | 3 | Drenar a fila não pode derrubar a API de saldo, que tem limite de 5% de perda. |
| Desempenho | 3 | A leitura precisa continuar dentro da meta enquanto o consumidor drena a fila. |
| Escalabilidade | 2 | A vazão de recuperação cresce com o volume de eventos. |
| Operabilidade | 2 | A opção pode exigir componente novo ou mudança na configuração do banco. |
| Simplicidade | 2 | As opções vão de uma configuração até um componente novo. |
| Economia | 1 | O custo é secundário nesta decisão. |

### Limite fixo de eventos em processamento (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Medido: com limite de 16 eventos, 0 leituras com falha, e o saldo fechou em 4 s. |
| Confiabilidade | 5 | Medido: a drenagem não afetou a API, e eventos acima do limite ficam na fila, sem perda. |
| Desempenho | 5 | Medido: 0 leituras com falha durante a drenagem; a latência da leitura nesse teste não foi informada, e a resposta supõe que ficou na meta. |
| Escalabilidade | 3 | O limite vale por instância e precisa de revisão ao escalar; a vazão a 500 req/s não foi medida. |
| Operabilidade | 5 | É uma configuração do consumidor, sem nada novo para operar. |
| Simplicidade | 5 | Uma configuração ou um semáforo, testável localmente. |
| Economia | 5 | Sem custo novo. |

### Pooler de conexões

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Limita conexões no banco, mas o pooler gerenciado do Cloud SQL exige edição Enterprise Plus (https://docs.cloud.google.com/sql/docs/postgres/managed-connection-pooling), e o efeito neste caso não foi medido. |
| Confiabilidade | 3 | Não limita o trabalho do consumidor, que ainda compete com a API nas filas do pooler (não medido); PgBouncer próprio seria um novo ponto único. |
| Desempenho | 2 | Não foi medido; um pooler próprio acrescenta um salto, e a fila no pooler pode aumentar a latência da API. |
| Escalabilidade | 4 | Sustenta mais clientes por conexão, mas o pooler é mais um ponto de contenção. |
| Operabilidade | 3 | Existe versão gerenciada, mas ativá-la em instância existente reinicia o banco (https://docs.cloud.google.com/sql/docs/postgres/managed-connection-pooling). |
| Simplicidade | 3 | É componente novo, com restrições do modo transação, como SET e LISTEN (https://docs.cloud.google.com/sql/docs/postgres/managed-connection-pooling). |
| Economia | 3 | Exige Enterprise Plus ou um serviço próprio; a diferença de preço não foi verificada. |

### Sem limite, com pool de conexões maior (eliminada)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | Veto | Medido: sem limite, o consumidor esgotou as conexões e a API de saldo falhou, o que contraria o limite de 5% de perda; um pool maior só adia o esgotamento. |
| Confiabilidade | 3 | A falha do consumidor atinge a API de saldo, conforme medido; a recuperação vem quando a fila acaba. |
| Desempenho | 2 | A API falhou durante a drenagem, conforme medido; o caso com pool maior não foi medido, e mais conexões disputam o mesmo banco. |
| Escalabilidade | Veto | Medido: a API de saldo falhou durante a drenagem, e a pressão cresce com o tamanho da fila. |
| Operabilidade | 4 | Sem componente novo, mas mudar max_connections no Cloud SQL reinicia a instância, segundo fonte secundária, sem confirmação em doc oficial. |
| Simplicidade | 5 | É só configuração. |
| Economia | 4 | Sem componente novo, mas mais conexões podem exigir instância maior (não medido). |

## ADR-0018

**Reconstrução do saldo.** [Ler o ADR](adr/0018-reconstrucao-do-saldo.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Aderência ao problema | 3 | A opção precisa cobrar a garantia de refazer tudo, e a retenção limitada e o saldo parcial tocam isso diretamente. |
| Confiabilidade | 3 | A reconstrução precisa recuperar todo lançamento confirmado sem afetar a escrita. |
| Escalabilidade | 2 | O fato medido de 74 por segundo define o tempo de reconstrução à medida que o histórico cresce. |
| Operabilidade | 2 | Reconstruir é operação rara, feita por equipe sem especialista. |
| Segurança | 2 | Ler o banco do outro serviço abre caminho novo de acesso. |
| Simplicidade | 2 | Equipe pequena, e o mecanismo raramente roda. |
| Economia | 1 | O custo é esporádico. |

### Reenvio dos lançamentos pela outbox (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Respeita a fronteira entre serviços, parte da fonte da verdade e também serve para alimentar um novo consumidor, como conciliação. |
| Aderência ao problema | 3 | Cobre todo o histórico, mas, medido, a API respondeu saldo parcial durante o processo, o que exige marcar reconstrução na aplicação, e 74 por segundo fica curto para histórico grande. |
| Confiabilidade | 4 | Refaz tudo a partir dos lançamentos, com repetição idempotente, mas precisa ser acionado manualmente; medido: 8.809 lançamentos em 126 s. |
| Escalabilidade | 2 | Medido: cerca de 74 por segundo, abaixo do cenário de 500 req/s, e o reenvio divide o canal com os eventos em tempo real; paralelismo com SKIP LOCKED não foi medido. |
| Operabilidade | 4 | Não há componente novo, mas a reconstrução não é transparente para o leitor: medido, a API respondeu saldo parcial durante o processo. |
| Segurança | 4 | Os dados seguem pelo canal já autorizado, sem novo acesso, e o isolamento entre comerciantes não muda. |
| Simplicidade | 4 | Reaproveita a outbox e a idempotência, com um comando de reenvio e o aviso de saldo parcial como código próprio. |
| Economia | 5 | Usa o que já existe, e o custo de mensagens é esporádico. |

### Restaurar backup do consolidado e reprocessar o que faltou

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 4 | Volta rápido com Cloud SQL, mas depende do reenvio para o trecho faltante e não serve para alimentar um novo consumidor sem histórico. |
| Aderência ao problema | 4 | Combina dois mecanismos e precisa definir com exatidão o ponto de corte, mas o reprocessamento é pequeno e proporcional à perda. |
| Confiabilidade | 4 | A fonte continua nos lançamentos e o backup reduz o reenvio, mas a restauração é manual, e um defeito de lógica gravado no backup voltaria com ele. |
| Escalabilidade | 4 | O reenvio limita-se ao trecho faltante, então o tempo não cresce com todo o histórico; tempo de restauração não medido. |
| Operabilidade | 3 | O backup é gerenciado, mas a restauração tem janela de indisponibilidade do consolidado, e o procedimento precisa de treino; tempo não verificado. |
| Segurança | 4 | O backup fica dentro da fronteira do próprio serviço, sem acesso novo. |
| Simplicidade | 3 | Exige restaurar e reenviar por ponto de corte, o que é difícil de ensaiar no computador de quem desenvolve. |
| Economia | 5 | Backups já fazem parte do banco gerenciado, e o custo adicional é pequeno; valor não verificado. |

### Leitura direta do banco de lançamentos (eliminada)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | Veto | Contraria a regra dada na decisão, de que um serviço não pode ler o banco do outro, e acopla o consolidado ao esquema de lançamentos. |
| Aderência ao problema | 5 | Tecnicamente é a via mais direta, com SQL e transação sobre os dados relacionais, embora a regra da decisão a vete no alinhamento. |
| Confiabilidade | 3 | Uma consulta pesada de reconstrução atinge o banco de escrita, e a mudança de esquema passa a quebrar o consolidado. |
| Escalabilidade | 3 | SQL em lote é rápido, mas todo o peso cai sobre o banco de lançamentos, que vira gargalo. |
| Operabilidade | 5 | Não exige componente novo, e é só SQL. |
| Segurança | 3 | Abre um caminho novo de acesso aos dados de lançamentos, ainda que por identidade. |
| Simplicidade | 5 | É a opção mais curta de escrever e testar, por ser um comando SQL. |
| Economia | 5 | Não acrescenta custo, a não ser o peso no banco de escrita. |

### Replay na mensageria (eliminada)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 3 | A retenção limitada impede refazer o histórico completo, então o replay só serve a perdas recentes e a evolução com novos consumidores também fica parcial. |
| Aderência ao problema | Veto | Não oferece a garantia de refazer o consolidado depois da retenção, fato dado pela decisão; o lançamento não se perde, mas o saldo fica incompleto (https://docs.cloud.google.com/pubsub/docs/replay-overview). |
| Confiabilidade | 3 | Só recupera o que ainda está retido: no Pub/Sub, um snapshot dura até 7 dias, e eventos mais antigos não voltam pela mensageria (https://docs.cloud.google.com/pubsub/docs/replay-overview). |
| Escalabilidade | 4 | O transporte aguenta a taxa de replay, mas ampliar a janela de retenção cresce em custo junto com o volume. |
| Operabilidade | 4 | O recurso de seek é gerenciado, mas o domínio da equipe não foi informado (https://docs.cloud.google.com/pubsub/docs/replay-overview). |
| Segurança | 4 | Usa o canal já autorizado, sem novo acesso. |
| Simplicidade | 4 | Usa recurso pronto, mas o emulador não aceita configurar retenção, o que dificulta testar no local (https://docs.cloud.google.com/pubsub/docs/emulator). |
| Economia | 3 | Reter mensagens por mais tempo é cobrado à parte e cresce com o volume; valor não verificado. |

## ADR-0019

**Paginação de lançamentos.** [Ler o ADR](adr/0019-paginacao.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Aderência ao problema | 3 | Afeta diretamente a correção de dados financeiros: novos lançamentos chegam durante a listagem. |
| Desempenho | 3 | Afeta diretamente a meta de leitura p95 até 200 ms. |
| Escalabilidade | 2 | Relevante: o custo por página precisa ficar estável com o volume do dia. |
| Simplicidade | 2 | Relevante: a API muda conforme a opção. |

### Keyset com cursor (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Mantém a leitura estável em dias com muitos lançamentos, e o cursor serve a novos consumidores, como conciliação. |
| Aderência ao problema | 5 | Com ordem única por chave composta, novos lançamentos não deslocam a posição já lida. |
| Desempenho | 5 | A consulta parte da posição no índice, então o custo não depende da profundidade da página. Não medido. |
| Escalabilidade | 5 | O trabalho por página é constante, e a API segue sem estado. |
| Simplicidade | 4 | Exige codificar o cursor e a comparação por chave composta, e não permite saltar para uma página qualquer. |

### Offset e limite

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 3 | É fácil de entregar, mas piora justamente nos dias com muitos lançamentos, e a listagem completa por um consumidor pode saltar ou repetir itens. |
| Aderência ao problema | 2 | Inserções durante a listagem deslocam as posições, e as páginas repetem ou pulam itens, conforme a ordem; uma garantia de listagem exata não consta das restrições, por isso sem veto. |
| Desempenho | 2 | O PostgreSQL ainda calcula as linhas puladas, e um OFFSET grande pode ser ineficiente (postgresql.org/docs/current/queries-limit.html); sem medição, não marquei veto de latência. |
| Escalabilidade | 2 | O custo por página cresce com a profundidade, e o esforço acompanha o volume. |
| Simplicidade | 5 | Só LIMIT e OFFSET, conhecidos por qualquer equipe e fáceis de testar. |

## ADR-0020

**Estilo da API HTTP.** [Ler o ADR](adr/0020-estilo-da-api-http.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Operabilidade | 2 | A equipe .NET mantém a API, e o gateway exige contrato conhecido. |
| Portabilidade | 2 | Clientes externos usam a API, e o padrão do contrato importa. |
| Simplicidade | 2 | Poucas rotas pedem pouca estrutura. |
| Desempenho | 1 | Medido: a latência não é gargalo, com p95 abaixo de 12 ms. |

### Minimal APIs (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | A Microsoft a recomenda para projetos novos (https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis), com OpenAPI embutido desde o .NET 9 (fonte secundária). |
| Operabilidade | 4 | Roda como qualquer ASP.NET Core, mas a régua não informa que a equipe domine o estilo. |
| Portabilidade | 5 | HTTP e OpenAPI são abertos. |
| Simplicidade | 5 | Menos código e testes mais simples, segundo a Microsoft (https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis). |
| Desempenho | 5 | Medido: p95 abaixo de 12 ms, e a Microsoft indica sobrecarga menor (https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis). |

### Controllers MVC

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Entrega HTTP com OpenAPI na stack do cliente, e a Microsoft a mantém como alternativa suportada (https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis). |
| Operabilidade | 5 | É o estilo tradicional, indicado pela Microsoft a equipes familiares com MVC (https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis). |
| Portabilidade | 5 | HTTP e OpenAPI são abertos. |
| Simplicidade | 4 | Sem componente novo, mas tem mais código repetitivo que Minimal APIs (https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis). |
| Desempenho | 4 | Medido: p95 abaixo de 12 ms; a Microsoft indica mais sobrecarga que em Minimal APIs (https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis). |

### gRPC

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 2 | É suportado no .NET, mas o contrato pedido é OpenAPI e clientes externos precisariam de transcodificação; não chega ao veto porque OpenAPI não é restrição listada do negócio. |
| Operabilidade | 4 | Roda no Cloud Run, com telemetria padrão, sem domínio informado na equipe. |
| Portabilidade | 5 | gRPC e Protobuf são abertos e funcionam igual em todo ambiente. |
| Simplicidade | 3 | Exige arquivos .proto e transcodificação JSON para clientes que não falam gRPC (https://learn.microsoft.com/en-us/aspnet/core/grpc/browser). |
| Desempenho | 5 | Medido: a latência não é gargalo; o formato binário não traz ganho que importe aqui. |

## ADR-0021

**Estratégia de testes.** [Ler o ADR](adr/0021-estrategia-de-testes.md).

| Critério | Peso | Por que este peso |
|---|---|---|
| Alinhamento com o negócio | 4 | Peso fixo da régua. |
| Aderência ao problema | 3 | Parte das garantias mora no SQL, e a estratégia precisa exercitar exatamente isso. |
| Eficácia de detecção | 3 | Critério próprio: o catálogo não mede se a estratégia acha defeitos, e os fatos medidos giram em torno disso, ligados à restrição de não perder lançamento. |
| Simplicidade | 2 | Equipe pequena mantém essa estrutura. |
| Economia | 1 | O custo de CI é secundário aqui. |

Perguntas de Eficácia de detecção: ["Exercita as garantias onde elas moram, inclusive unicidade, travas e ON CONFLICT em banco real?","Detecta regressão de domínio com retorno rápido?","Exercita a validação de entrada na fronteira da API?","Aponta a causa da falha sem investigação longa?","Veto: deixa sem exercício uma garantia exigida."]

### Domínio, banco real e mutação (escolhida)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 5 | Verifica as garantias no SQL e cresce por camada, sem depender de ambiente completo. |
| Aderência ao problema | 5 | Testa cada garantia onde ela mora, sem imitar o banco. |
| Eficácia de detecção | 4 | Medido: o banco real achou um defeito, mas testes e mutação acima de 97% não acharam uma entrada inválida aceita pela API, então a fronteira da API continua descoberta. |
| Simplicidade | 2 | Contêiner de banco e ferramenta de mutação são peças e conceitos a mais, embora rodem no local. |
| Economia | 4 | Usa ferramentas gratuitas, mas manter três camadas e rodar mutação custa tempo; valor não verificado. |

### Só testes de ponta a ponta

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 3 | Cobre o fluxo real, mas demora a dar retorno e fica caro de manter conforme crescem os consumidores. |
| Aderência ao problema | 4 | Exercita o SQL real, mas sobra custo para verificar regras simples de domínio. |
| Eficácia de detecção | 3 | Passa pelo banco e pela API, mas é lento e localiza mal a causa, e casos de concorrência são difíceis de provocar assim. |
| Simplicidade | 2 | Exige todo o ambiente para rodar e montar cada cenário. |
| Economia | 4 | Roda em CI existente, mas testes lentos e instáveis custam tempo de manutenção; valor não verificado. |

### Só testes com falsos em memória (eliminada)

| Critério | Nota | Evidência |
|---|---|---|
| Alinhamento com o negócio | 2 | Não verifica a garantia de não perder lançamento, que está no SQL, e não dá evidência adequada a uma instituição financeira. |
| Aderência ao problema | Veto | Um falso não reproduz travas nem ON CONFLICT, logo não consegue oferecer a garantia exigida. |
| Eficácia de detecção | Veto | Medido: um teste com banco real achou um defeito que um falso não acharia; a estratégia deixa unicidade, travas e ON CONFLICT sem exercício. |
| Simplicidade | 4 | Roda rápido e em qualquer máquina, mas cada falso é código que imita o banco. |
| Economia | 5 | Roda barato, e a manutenção dos falsos é pequena em poucas consultas. |
