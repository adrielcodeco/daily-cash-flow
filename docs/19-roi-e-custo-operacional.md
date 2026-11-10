# ROI, ponto de equilíbrio e custo operacional

Este documento estima quanto custa construir e operar a solução, quantos comerciantes pagam essa conta e em quanto tempo o investimento volta.

**Os resultados são um modelo, e não uma previsão.** Só o custo de infraestrutura vem de preços pesquisados, nos [custos](05-custos.md). Equipe, câmbio, preço por comerciante e adoção são premissas sem dados do cliente, escritas para serem trocadas. Este arquivo é gerado: edite as premissas em `tools/roi/generate.mjs` e rode `node tools/roi/generate.mjs`.

## Conclusões

- **A equipe é o principal custo, e não a nuvem.** No cenário Início, ela representa 75% do custo mensal. Otimizar a infraestrutura muda pouco o resultado.
- **Ponto de equilíbrio mensal:** cerca de 3.953 comerciantes, a R$ 19,90 por mês.
- **Custo adicional por comerciante:** cerca de R$ 0,12 por mês. Depois do ponto de equilíbrio, quase toda a receita vira margem.
- **Retorno no cenário Base:** mês 21, com ROI de 75% em 36 meses.
- **O cenário Conservador não se paga em 36 meses.** O projeto depende de adoção e preço, e não de tecnologia.

## Premissas

| Premissa | Valor | Base |
|---|---|---|
| Câmbio | R$ 5,50 por dólar | Premissa. Atualizar com a cotação do dia |
| Requisições por comerciante por mês | 1.000 | Premissa. Liga os cenários de custo ao número de comerciantes |
| Custo mensal por pessoa, com encargos | R$ 30.000 | Premissa |
| Equipe de construção | 5,5 pessoas por 4 meses | Premissa, para levar o projeto a produção |
| Lançamento | R$ 100.000 | Premissa: pentest, conformidade e migração |
| Ambiente de não produção | 40% do cenário Início | Premissa |
| Suporte do provedor | 10% da infraestrutura | Premissa. Confirmar o plano contratado |
| Equipe de operação | 2 pessoas | Premissa |
| Plantão | R$ 10.000 por mês | Premissa |
| Adoção | Linear por 24 meses, depois estável | Premissa |
| Taxa de desconto | 15% ao ano | Premissa. Usar a taxa do cliente |

A receita pode ser uma mensalidade ou um valor equivalente. Se o serviço for gratuito, o mesmo cálculo vale com o benefício mensal por comerciante, como retenção de clientes ou redução de atendimento.

## Investimento inicial

| Item | Valor |
|---|---:|
| Equipe de construção | R$ 660.000 |
| Ambiente durante a construção | R$ 10.188 |
| Lançamento | R$ 100.000 |
| **Total** | **R$ 770.188** |

## Custo operacional mensal

| Item | Início: 13.000 comerciantes | Crescimento: 130.000 comerciantes | Peso no Início |
|---|---:|---:|---:|
| Infraestrutura de produção | R$ 6.367 | R$ 19.855 | 8% |
| Ambiente de não produção | R$ 2.547 | R$ 2.547 | 3% |
| Suporte do provedor | R$ 891 | R$ 2.240 | 1% |
| Equipe de operação | R$ 60.000 | R$ 60.000 | 75% |
| Plantão | R$ 10.000 | R$ 10.000 | 13% |
| **Total** | **R$ 79.806** | **R$ 94.643** | **100%** |

| Custo unitário | Início | Crescimento |
|---|---:|---:|
| Por comerciante, por mês | R$ 6,14 | R$ 0,73 |
| Só infraestrutura, por mil requisições | R$ 0,49 | R$ 0,15 |

O volume cresce 10 vezes, e o custo total cresce bem menos, porque a equipe fica igual. Esse modelo supõe que a mesma equipe sustente o cenário Crescimento, o que precisa ser validado.

## Ponto de equilíbrio

O ponto de equilíbrio mensal é o número de comerciantes em que a receita cobre o custo do mês:

```text
comerciantes = custo fixo mensal / (preço - custo variável por comerciante)
```

Custo fixo mensal: R$ 78.157. Custo variável por comerciante, com suporte: R$ 0,13.

| Preço mensal | Comerciantes no equilíbrio |
|---|---:|
| R$ 9,90 | 7.997 |
| R$ 19,90 | 3.953 |
| R$ 29,90 | 2.625 |
| R$ 49,90 | 1.570 |

### Sensibilidade

Comerciantes necessários a R$ 19,90, mudando uma premissa de cada vez:

| Mudança | Comerciantes no equilíbrio |
|---|---:|
| Modelo base | 3.953 |
| Equipe de operação com 1 pessoa | 2.435 |
| Equipe de operação com 4 pessoas | 6.987 |
| Custo por pessoa de R$ 20.000 | 2.941 |
| Custo por pessoa de R$ 45.000 | 5.470 |
| Infraestrutura com o dobro do custo | 4.365 |

Dobrar a infraestrutura pesa menos que acrescentar pessoas à operação.

## ROI

```text
ROI = (receita acumulada - custo acumulado) / custo acumulado
```

O custo acumulado inclui o investimento inicial. O período é de 36 meses depois do lançamento.

| Cenário | Preço | Comerciantes ao fim da adoção | Mês com lucro | Retorno do investimento | Saldo em 36 meses | VPL | ROI |
|---|---:|---:|---|---|---:|---:|---:|
| Conservador | R$ 9,90 | 6.000 | Não ocorre em 36 meses | Não ocorre em 36 meses | R$ -2.147.184 | R$ -1.954.022 | -60% |
| Base | R$ 19,90 | 13.000 | Mês 8 | Mês 21 | R$ 2.713.918 | R$ 1.771.599 | 75% |
| Otimista | R$ 29,90 | 30.000 | Mês 3 | Mês 9 | R$ 18.299.451 | R$ 13.716.581 | 498% |

## O que fora do modelo pode mudar o resultado

| Item | Efeito |
|---|---|
| Impostos e custo de aquisição de clientes | Reduzem a margem |
| Cancelamento de clientes | A adoção linear ignora a saída de comerciantes |
| Descontos por compromisso no GCP | Reduzem a infraestrutura |
| Incidentes e multas | Não estão no custo. As vulnerabilidades abertas aumentam esse risco |
| Fim do legado, em uma migração | Pode gerar economia, se a solução substituir um sistema |

## Como reduzir o custo

| Ação | Efeito estimado | Trade-off |
|---|---|---|
| Automatizar a operação | Maior alavanca, porque ataca a equipe | Exige investimento em plataforma |
| Reduzir o Cloud SQL do consolidado | Menor custo de infraestrutura | Recuperação mais lenta |
| Descontos por compromisso | Menor custo de infraestrutura | Compromisso de uso |
| Manter o Redis desligado também no Crescimento | Cerca de US$ 635 por mês no Crescimento. O Início já não usa Redis | Menos proteção do banco em volumes maiores |
