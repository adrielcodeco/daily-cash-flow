# ADR-0017: Vazão do consumidor

- **Status:** aceita
- **Data:** 2026-11-06
- **Registro:** escrito depois da implementação, para documentar uma escolha que já estava no código

## Contexto

No teste de resiliência, a fila acumulada durante uma queda esgotou as conexões do banco quando o consumidor voltou, e a API de saldo passou a falhar com "too many clients". A recuperação de um componente derrubou outro.

## Decisão

Limitar o consumidor a 16 eventos em processamento, com um único cliente de assinatura por processo e pool de conexões definido.

## Opções

<!-- radar:start -->
![Radar de Vazão do consumidor](radar/ADR-0017.svg)

| Pilar | **Limite por processo** | Sem limite, pool maior | Pooler de conexões |
|---|---:|---:|---:|
| Segurança | 4 | 4 | 4 |
| Desempenho | 3 | 4 | 4 |
| Escalabilidade | 3 | 2 | 5 |
| Confiabilidade | 5 | 1 | 4 |
| Custo | 5 | 4 | 3 |
| Simplicidade | 4 | 5 | 2 |
| Operabilidade | 4 | 2 | 3 |
<!-- radar:end -->

## Leitura do radar

Aumentar o pool só adia o problema e foi o que falhou no teste. Um pooler resolve melhor em escala, mas é mais um componente para um risco que o limite já trata. O limite deixa a fila esvaziar mais devagar, o que é aceitável: o teste terminou com saldo exato 3 segundos depois do fim da carga, e sem nenhuma leitura com falha.

## Consequências

**Positivas**

- A leitura segue atendendo enquanto a fila é consumida
- O total de conexões fica previsível, o que permite o orçamento do documento de observabilidade

**Negativas**

- A recuperação de uma fila grande demora mais
- 16 resolveu o caso medido, mas não é um valor otimizado

## Gatilho de revisão

Rever se o tempo de recuperação da fila passar do aceitável. Aumente instâncias dentro do orçamento de conexões e, depois, adote um pooler.
