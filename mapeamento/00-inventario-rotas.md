# Inventário de rotas do app10.ploomes.com

Fonte: navegação observada via REA (CDP) na conta logada. Rotas com `{id}` recebem um identificador numérico.

## Navegação principal (menu lateral)
| Rótulo | Rota |
|---|---|
| Operação (Beta) | `/operations` |
| Resumo | `/` |
| Clientes | `/Contacts/table` |
| Contato Venda / Documentos | `/Documents/table` |
| Produtos | `/Products/table` |
| Relatórios | `/reports/panel/` e `/reports/panel/{id}` |
| Administração | `/administration` |
| Ploomes IA | `/ai-chat/new` |

## Negócios / Funil
| Tela | Rota |
|---|---|
| Lista de negócios | `/Deals/table`, `/Deals`, `/Deals/all/table` |
| Funil (kanban) | `/Deals/{id}/funnel`, `/pipeline` |
| Detalhe do negócio | `/deal/{id}` |
| Tarefa dentro do negócio | `/deal/{id}/task/{id}` |

## Vendas
| Tela | Rota |
|---|---|
| Propostas | `/Quotes/table` |
| Pedidos | `/Orders/table` |
| Tarefas | `/Tasks/table` |
| Detalhe de tarefa | `/task/{id}` |
| Documentos | `/Documents/table` |

## Produtos
| Tela | Rota |
|---|---|
| Lista de produtos | `/Products/table` |
| Detalhe de produto | `/Products/table/product/{id}` |

## Usuários / Metas
| Tela | Rota |
|---|---|
| Detalhe de usuário | `/user/{id}` |
| Config. do usuário | `/user/{id}/config` |
| Metas | `/goals`, `/goals/panel/`, `/goals/panel/{id}`, `/goals/panel/{id}/newgoal` |
| Nova mensagem | `/newmessage` |

## Administração (central de cards)
### Geral
| Card | Rota |
|---|---|
| Módulos do sistema | `/system-modules` |
| Campos e formulários | `/formconfig` |
| Opções pré-cadastradas | `/administration/fieldsparameters` |
| Dados da empresa | `/administration/account` |
| Configurações do sistema | `/administration/systemsettings` |
| Automações | `/administration/automations` |
| Ploo Store | `/administration/ploostore` |
| Fila de Processamento | `/administration/processingqueue` |

### Usuários
| Card | Rota |
|---|---|
| Tabela de usuários | `/Users/table` |
| Equipes de usuários | `/administration/teams` |
| Perfis de usuário | `/administration/profiles` |
| Permissão ao Suporte Ploomes | (não confirmado; card presente) |
| Registros (logs) | `/Logs/changes` |

### Propostas e vendas
| Card | Rota |
|---|---|
| Modelos de documento | `/templates`, `/templates/quotes` |
| Fluxos de aprovação | `/administration/businessrules` |

### Integrações
| Card | Rota |
|---|---|
| Integrações plug-and-play | `/administration/integrations` |
| Usuários de integração | `/administration/integrationusers` |
| Formulários externos | `/administration/externalforms` |

### Pagamentos
| Card | Rota |
|---|---|
| Dados de pagamento | `/administration/payment` |
| Histórico de fatura | `/administration/paymenthistory` |
| Créditos de IA | `/administration/aiconsumption` |
