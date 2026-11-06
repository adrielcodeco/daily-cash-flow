# Evoluções futuras

Cada item traz o que motivaria a mudança. Sem esse gatilho, o custo de fazer agora supera o benefício.

| Evolução | Gatilho |
|---|---|
| Infraestrutura como código com Terraform e deploy no GCP | Decisão de ir para produção |
| Interface web | Comerciantes sem integração por API |
| Exportação do relatório em CSV ou PDF | Pedido de contabilidade ou fiscal |
| Fotografias mensais do saldo | Abertura acima da meta de latência |
| Isolamento por segurança no nível da linha | Exigência de auditoria ou aumento do número de equipes |
| Particionamento por comerciante | Banco de lançamentos perto do limite de escrita |
| Kafka no lugar de Pub/Sub | Vários consumidores precisando reprocessar histórico longo |
| CDC no lugar do publicador da outbox | Leitura da outbox virar gargalo |
| GKE no lugar do Cloud Run | Padrão da empresa, ou muitos serviços novos |
| Reconstrução por comerciante | Base grande demais para uma reconstrução completa |
| Reconciliação automática | Antes de produção |
| Limpeza de outbox, chaves e eventos processados | Antes de produção |
| Testes de contrato | Mais de um consumidor do evento |
| Testes de caos | Primeiro serviço em produção |
| Múltiplas moedas e contas | Novo segmento de cliente |
| mTLS e perfil FAPI | Integração regulada de alto risco |
| Conciliação bancária | Assinatura do evento por um novo contexto, sem mudar Lançamentos |
