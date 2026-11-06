# Registro de vulnerabilidades

Este registro acompanha as fraquezas conhecidas da solução, do achado à correção. O [documento de segurança](07-seguranca.md) trata do desenho e das ameaças. Aqui ficam os problemas concretos.

**Revisão:** 2026-11-06, por leitura do código e da configuração, com testes na API local. Não houve pentest nem análise de imagens, então a lista não é completa. Ausência de um item não prova ausência do problema.

## Resumo

| Severidade | Sem correção | Riscos aceitos |
|---|---:|---:|
| Crítica | 0 | 0 |
| Alta | 1 | 0 |
| Média | 8 | 0 |
| Baixa | 6 | 2 |

Já foram corrigidos V-01 a V-05. A V-06 está parcial e ainda bloqueia produção.

**Nenhuma vulnerabilidade Crítica ou Alta pode ir para produção.**

## Como classificar

| Severidade | Critério | Prazo proposto |
|---|---|---|
| Crítica | Expõe dados de outro comerciante ou corrompe saldo sem aviso | Bloqueia produção. Corrigir em até 7 dias |
| Alta | Permite fraude, indisponibilidade ou impede investigar um incidente | Bloqueia produção. Corrigir em até 30 dias |
| Média | Exige outra falha ou acesso interno para causar dano | Corrigir em até 90 dias |
| Baixa | Dano pequeno, ou só no ambiente local | Corrigir quando for conveniente |

| Evidência | Significado |
|---|---|
| Testada | Reproduzida contra a API local |
| Código | Confirmada na leitura do código ou da configuração |
| Hipótese | Possível pelo desenho, sem reprodução |

| Situação | Significado |
|---|---|
| Aberta | Conhecida e sem correção |
| Planejada | Com marco definido |
| Parcial | Reduzida, mas ainda com risco |
| Corrigida | Corrigida, com teste que impede a volta |
| Aceita | Risco assumido, com motivo registrado |

## Vulnerabilidades

| ID | Vulnerabilidade | Classe | Severidade | Evidência | Situação | Correção |
|---|---|---|---|---|---|---|
| V-01 | Quem chama escolhe o comerciante por `X-Merchant-Id` | API1, CWE-639 | Crítica | Testada | Corrigida | JWT validado em cada serviço, com `merchantId` vindo do token. Passo 11 de `smoke.sh` |
| V-02 | `entryType` numérico inválido é aceito, e o débito some do saldo | CWE-20 | Crítica | Testada | Corrigida | Número recusado na leitura, tipo validado no domínio e no consumidor. Regras `LEDEN-R09` e `APENT-R07` |
| V-03 | Lançamento sem `entryType` vira crédito | CWE-20 | Alta | Testada | Corrigida | Enum começa em 1, então o valor ausente é recusado. Regra `LEDEN-R09` |
| V-04 | Valor sem limite estoura a soma do saldo | CWE-190 | Alta | Testada | Corrigida | Limite de R$ 1 bilhão no domínio e no banco, e estouro gera erro. Regras `LEDEN-R08` e `DYBAL-R03` |
| V-05 | Não há limite de requisições | API4, CWE-770 | Alta | Testada | Corrigida | 100 requisições por segundo por comerciante, com resposta 429. Passo 12 de `smoke.sh`. Ver a V-19 |
| V-06 | Não há trilha de auditoria | CWE-778 | Alta | Testada | Parcial | Categoria `Audit` nos logs. Falta armazenamento separado, à prova de alteração |
| V-07 | Contêineres rodam como root | CWE-250 | Média | Testada | Aberta | Declarar `USER` sem privilégio nas imagens |
| V-08 | Durante a reconstrução, a API responde 200 com saldo parcial | Integridade | Média | Testada | Aberta | Modo de manutenção, com resposta 503 |
| V-09 | `--reset` apaga o consolidado e está na imagem de produção | CWE-749 | Média | Código | Aberta | Separar a manutenção da imagem de produção e exigir aprovação |
| V-10 | O consumidor confia no conteúdo do evento | API10, CWE-345 | Média | Código | Aberta | Validar o evento pelo esquema, com restrição de publicação por IAM |
| V-11 | CI não verifica pacotes, código, segredos nem imagens | CWE-1395 | Média | Código | Aberta | Dependabot, análise estática e varredura de imagens |
| V-12 | Actions e imagens usam tag, e o CI não restringe `permissions` | CWE-829 | Média | Código | Aberta | Fixar por hash e declarar permissões mínimas |
| V-13 | Descrição livre pode receber dados pessoais, em registro imutável | LGPD | Média | Hipótese | Aberta | Orientar o uso, definir retenção e avaliar com Conformidade |
| V-14 | `outbox`, `processed_event` e `idempotency_key` crescem sem limite | CWE-770 | Baixa | Código | Aberta | Retenção e limpeza |
| V-15 | Mensagens mortas guardam dados financeiros | CWE-200 | Baixa | Hipótese | Aberta | Acesso restrito e retenção curta |
| V-16 | Portas das APIs ficam abertas em todas as interfaces da máquina | CWE-1327 | Baixa | Código | Aberta | Publicar só em `127.0.0.1` |
| V-17 | Senha fixa `local-only` no `docker-compose.yml` | CWE-798 | Baixa | Código | Aceita | Vale só no ambiente local. Nunca reutilizar |
| V-18 | UUID v7 revela o momento da criação | CWE-200 | Baixa | Código | Aceita | Dado de baixo valor, já presente na resposta |
| V-19 | O limite de requisições é por instância, então cresce com a escala | CWE-770 | Média | Código | Aberta | Limite global no Cloud Armor ou no gateway |
| V-20 | Keycloak local em modo de desenvolvimento, com senha fixa de administrador | CWE-798 | Baixa | Código | Aberta | Só no ambiente local. Em produção, usar o provedor de identidade do cliente |
| V-21 | A API aceita `traceparent` de quem chama | CWE-345 | Baixa | Testada | Aberta | Remover o cabeçalho no gateway |
| V-22 | Resultados do k6 guardavam tokens de teste | CWE-312 | Baixa | Testada | Corrigida | `load-test.sh` remove o `setup_data`, e `gitleaks` verifica o histórico |
| V-23 | `sbom.json` gerado entrou no Git, com caminhos locais | CWE-200 | Baixa | Código | Parcial | Fora do versionamento. Falta tirar do histórico antes de publicar |

As classes usam a [OWASP API Security Top 10 2023](https://owasp.org/API-Security/editions/2023/en/0x11-t10/) e a [CWE](https://cwe.mitre.org/).

## Detalhes dos defeitos corrigidos

A descrição abaixo mostra o comportamento antes da correção. Os casos agora estão cobertos por testes e pelo passo 10 de `smoke.sh`.

### V-02: tipo inválido apaga um débito do saldo

**Reprodução:** `POST /v1/entries` com `"entryType": 7`.

| Etapa | Resultado |
|---|---|
| API | Responde 201, com `entryType: 7` |
| Banco de lançamentos | Grava `DEBIT`, porque qualquer valor diferente de crédito vira débito |
| Evento | Sai com `entryType: 7` |
| Consumidor | Processa sem erro e não soma crédito nem débito |
| Saldo | Fica 0, enquanto o lançamento é um débito de 500 |

**Por que é crítica:** o saldo diverge dos lançamentos sem erro, sem mensagem morta e sem alerta. Uma reconstrução corrige o saldo, porque gera o evento de novo a partir do banco, mas nada avisa que ela é necessária. Os testes não pegaram isso, porque só exercitam `CREDIT` e `DEBIT`, e a mutação só avalia os testes existentes.

**Causa:** o conversor de enum aceitava números, nada validava o valor no domínio, e a gravação transformava qualquer valor desconhecido em débito.

### V-03: tipo ausente vira crédito

**Reprodução:** `POST /v1/entries` sem `entryType`. A API responde 201 com `CREDIT`, porque esse é o valor padrão do enum. O OpenAPI declara o campo como obrigatório, mas nada aplica isso. Um cliente com defeito lançaria uma saída como entrada.

### V-04: estouro da soma

**Reprodução:** dois créditos de `9223372036854775807` para o mesmo comerciante e dia. As duas escritas respondem 201. O primeiro evento é aplicado, e o segundo falha com `bigint out of range`. Depois disso, outro crédito de 1.000 também falha. Os eventos seguem para as mensagens mortas, e os créditos daquele dia param de entrar no saldo.

Além disso, o cálculo do fechamento em C# não verifica estouro, então a soma pode virar um número incorreto sem erro. Essa parte foi vista no código, e não testada. Agora o cálculo gera erro.

## Controles verificados

| Controle | Como foi verificado | Resultado |
|---|---|---|
| Injeção de SQL | Leitura dos adaptadores | Todas as entradas são parâmetros |
| Isolamento entre comerciantes | Testes automatizados | Registro de outro comerciante responde 404 |
| Erro interno | Leitura do tratador de erros | Resposta genérica, sem detalhes internos |
| Segredos no Git | `gitleaks` em todo o histórico | Nenhum encontrado. A senha local da V-17 é conhecida e aceita |
| Pacotes .NET vulneráveis | `dotnet list package --vulnerable`, nos 10 projetos | Nenhum, em 2026-11-06 |
| Banco e cache no ambiente local | Leitura do `docker-compose.yml` | Sem porta publicada |

## Ainda não verificado

- Pentest e testes dinâmicos de segurança
- Vulnerabilidades nas imagens de contêiner
- Configuração real do GCP: IAM, rede e criptografia
- Ataques ao token, como troca de algoritmo e reuso de token expirado, que dependem da biblioteca e não foram testados

## Como manter

| Evento | Ação |
|---|---|
| Achado novo | Entra aqui, com ID, evidência e severidade |
| Correção | Muda a situação, com a regra e o teste que impedem a volta |
| Risco aceito | Registra o motivo e quem aceitou |
| Fim de cada marco | Nova revisão do que mudou |
| Antes de produção | Pentest e análise de imagens |

IDs não são reutilizados. Item corrigido continua no registro, porque a história também serve de evidência.
