# Revisão pelo Google Cloud Well-Architected Framework

Autoavaliação da arquitetura proposta pelos pilares do framework. As situações refletem esta entrega, sem uso em produção.

| Pilar | Situação | Pontos fortes | Lacunas |
|---|---|---|---|
| Excelência operacional | Parcial | Runbooks, SLOs e serviços gerenciados | Infraestrutura como código e automação de alertas não foram implementadas |
| Segurança, privacidade e conformidade | Parcial | Isolamento por comerciante, modelo de ameaças e rede privada | Autenticação real entra no M6, e a conformidade precisa de validação |
| Confiabilidade | Bom | Falhas isoladas, outbox, retentativas e testes de queda | Exercício de recuperação e resposta regional não foram testados |
| Otimização de custos | Bom | Escala automática e serviços dimensionados à carga | Custo real depende de uso medido em produção |
| Otimização de desempenho | Bom | Saldo pré-calculado, com p95 abaixo de 12 ms localmente | Sem medição no GCP e com histórico grande |
| Sustentabilidade | Parcial | Escala conforme a demanda | A região é definida por residência de dados, e a pegada não foi medida |

## Prioridades antes de produção

1. Autenticação e autorização reais
2. Validação de conformidade e retenção
3. Infraestrutura como código
4. Teste de carga no GCP, com histórico realista
5. Exercício de recuperação de desastres
