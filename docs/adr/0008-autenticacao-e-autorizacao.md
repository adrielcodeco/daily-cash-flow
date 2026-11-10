# ADR-0008: Autenticação e autorização

- **Status:** aceita
- **Data:** 2026-11-05

## Contexto

Os dados são financeiros e isolados por comerciante. A autorização precisa ser auditável e separar leitura de escrita.

## Decisão

Usar OAuth2 e OIDC com JWT de vida curta. Na borda, o balanceador com Cloud Armor filtra o tráfego, com WAF e limite de taxa, e cada serviço valida o token e os escopos. O `merchantId` vem do token, nunca da rota. Serviços usam identidades próprias no GCP.

## Opções

<!-- radar:start -->
![Radar de Autenticação e autorização](radar/ADR-0008.svg)

| Critério | Peso | **OIDC e JWT, validados em cada serviço** | Chaves de API | mTLS para todo cliente |
|---|---:|---:|---:|---:|
| Alinhamento com o negócio | 4 | 5 | 3 | 3 |
| Segurança | 3 | 4 | 2 | 3 |
| Operabilidade | 2 | 5 | 4 | 3 |
| Simplicidade | 2 | 4 | 4 | 2 |
| Desempenho | 1 | 5 | 4 | 4 |
| Economia | 1 | 4 | 5 | 3 |
| Portabilidade | 1 | 5 | 4 | 4 |
| **Resultado** | | **4,57** | **3,36** | **3,00** |

As perguntas e a evidência de cada nota estão na [avaliação dos radares](../21-avaliacao-dos-radares.md#adr-0008).
<!-- radar:end -->

## Leitura do radar

Chaves de API são simples, mas não expiram sozinhas e têm autorização fraca. mTLS para todo cliente dá identidade forte, mas não traz escopos, fica abaixo do JWT em segurança e é pesado de distribuir e renovar para comerciantes. Ele fica como opção para integrações de sistema a sistema de maior risco.

## Consequências

**Positivas**

- Um comerciante não acessa dados de outro, mesmo se a rota for manipulada
- Tokens curtos e escopos reduzem o dano de um vazamento
- A validação fica em um só lugar, no serviço, sem um componente pago por chamada na borda

**Negativas**

- Depende da disponibilidade do provedor de identidade
- Exige gestão de chaves e de clientes
- Sem gestão de chaves e cotas por cliente na borda: o limite por comerciante fica nos serviços, e o limite global, no Cloud Armor

## Resultado

Implementado com Keycloak no ambiente local. `smoke.sh` prova que token ausente ou inválido responde 401, falta de escopo responde 403, e que o cabeçalho `X-Merchant-Id` é ignorado.

## Revisão de 2026-11-09

A primeira versão desta decisão tinha um API Gateway entre o balanceador e o Cloud Run, fazendo a primeira validação do token. Ele saiu da arquitetura. O API Gateway do GCP chama o Cloud Run pela URL pública `run.app`, e não pelo balanceador, então não alcança um serviço com entrada `internal-and-cloud-load-balancing`, exigida pela D-64. Como cada serviço já valida o JWT e os escopos (D-115), o gateway só repetia uma validação.

A borda passa a ser o balanceador de aplicação externo global com Cloud Armor, ligado ao Cloud Run por NEG sem servidor. O limite de taxa global fica nas regras de taxa do Cloud Armor, e o balanceador remove o `traceparent` enviado pelo cliente. A opção foi renomeada para "OIDC e JWT, validados em cada serviço", e três notas subiram pela régua: Desempenho de 4 para 5, Economia de 3 para 4 e Portabilidade de 4 para 5. O resultado foi de 4,36 para 4,57. Os motivos estão nos [ajustes do arquiteto](../20-metodo-dos-radares.md#ajustes-do-arquiteto), e a decisão é a D-159 do [registro de decisões](../15-registro-de-decisoes.md).

## Gatilho de revisão

Rever se houver integrações reguladas com perfis como FAPI. Nesse caso, mTLS e tokens vinculados ao certificado passam a ser exigidos. Rever também se surgirem clientes que precisem de cotas ou chaves gerenciadas na borda.
