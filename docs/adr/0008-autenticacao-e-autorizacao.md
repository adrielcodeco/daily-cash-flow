# ADR-0008: Autenticação e autorização

- **Status:** aceita
- **Data:** 2026-11-05

## Contexto

Os dados são financeiros e isolados por comerciante. A autorização precisa ser auditável e separar leitura de escrita.

## Decisão

Usar OAuth2 e OIDC com JWT de vida curta. O gateway faz a primeira validação, e cada serviço valida o token novamente. O `merchantId` vem do token, nunca da rota. Serviços usam identidades próprias no GCP.

## Opções

<!-- radar:start -->
![Radar de Autenticação e autorização](radar/ADR-0008.svg)

| Critério | Peso | **Gateway, OIDC e JWT** | Chaves de API | mTLS para todo cliente |
|---|---:|---:|---:|---:|
| Alinhamento com o negócio | 4 | 5 | 3 | 3 |
| Segurança | 3 | 4 | 2 | 3 |
| Operabilidade | 2 | 5 | 4 | 3 |
| Simplicidade | 2 | 4 | 4 | 2 |
| Desempenho | 1 | 4 | 4 | 4 |
| Economia | 1 | 3 | 5 | 3 |
| Portabilidade | 1 | 4 | 4 | 4 |
| **Resultado** | | **4,36** | **3,36** | **3,00** |

As perguntas e a evidência de cada nota estão na [avaliação dos radares](../21-avaliacao-dos-radares.md#adr-0008).
<!-- radar:end -->

## Leitura do radar

Chaves de API são simples, mas não expiram sozinhas e têm autorização fraca. mTLS para todo cliente é forte, mas pesado de distribuir e renovar para comerciantes. Ele fica como opção para integrações de sistema a sistema de maior risco.

## Consequências

**Positivas**

- Um comerciante não acessa dados de outro, mesmo se a rota for manipulada
- Tokens curtos e escopos reduzem o dano de um vazamento

**Negativas**

- Depende da disponibilidade do provedor de identidade
- Exige gestão de chaves e de clientes

## Resultado

Implementado no M6, com Keycloak no ambiente local. `smoke.sh` prova que token ausente ou inválido responde 401, falta de escopo responde 403, e que o cabeçalho `X-Merchant-Id` é ignorado.

## Gatilho de revisão

Rever se houver integrações reguladas com perfis como FAPI. Nesse caso, mTLS e tokens vinculados ao certificado passam a ser exigidos.
