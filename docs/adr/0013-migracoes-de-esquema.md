# ADR-0013: Migrações de esquema

- **Status:** aceita
- **Data:** 2026-11-06
- **Registro:** escrito depois da implementação, para documentar uma escolha que já estava no código

## Contexto

O esquema precisa evoluir de forma versionada e repetível. Em nuvem, várias instâncias sobem juntas, e migrar na subida de cada uma disputaria o banco.

## Decisão

Usar DbUp com arquivos SQL embutidos no projeto de infraestrutura. A migração roda uma vez, pela opção `--migrate`, antes de a API subir.

## Opções

<!-- radar:start -->
![Radar de Migrações de esquema](radar/ADR-0013.svg)

| Critério | Peso | **DbUp com SQL** | Flyway | EF Core Migrations |
|---|---:|---:|---:|---:|
| Alinhamento com o negócio | 4 | 5 | 4 | 4 |
| Simplicidade | 3 | 5 | 4 | 3 |
| Aderência ao problema | 2 | 5 | 5 | 2 |
| Operabilidade | 2 | 5 | 4 | 5 |
| Economia | 1 | 5 | 5 | 4 |
| Portabilidade | 1 | 5 | 5 | 5 |
| **Resultado** | | **5,00** | **4,31** | **3,69** |

As perguntas e a evidência de cada nota estão na [avaliação dos radares](../21-avaliacao-dos-radares.md#adr-0013).
<!-- radar:end -->

## Leitura do radar

EF Core Migrations pressupõe o EF Core, descartado no ADR-0012. Flyway é maduro, mas traz outra ferramenta e outro ambiente de execução para uma stack .NET. DbUp mantém o SQL explícito e cabe no próprio binário do serviço.

## Consequências

**Positivas**

- O mesmo artefato do serviço aplica o esquema
- A migração tem sua própria falha, separada da subida da API

**Negativas**

- DbUp só avança: desfazer uma mudança exige uma nova migração
- A compatibilidade entre versões do esquema e do código depende de disciplina

## Gatilho de revisão

Rever se o projeto precisar de migrações com reversão automática ou de relatórios de drift entre ambientes.
