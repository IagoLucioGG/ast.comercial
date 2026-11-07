# 02 - Mapeamento de recursos da Ploomes (CRM)

Escopo: inventário de funcionalidades por módulo, a partir da engenharia reversa em `mapeamento/`. Somente leitura; este é o único arquivo escrito.

Legenda de confiança:
- **[O]** Observado: visto em template AngularJS, i18n pt-BR, OpenAPI, captura de rede ou screenshot.
- **[I]** Inferido: dedução razoável a partir de nomes, rotas ou textos.
- **[D]** Desconhecido: não há evidência nas fontes.

Fontes: docs 00..04 e `_rodada4.md`, `fontes/openapi.json`, `fontes/templates-ptbr/` (464 templates, 40 módulos), `fontes/i18n/pt-BR.json` (94 grupos), `telas/` (apenas layout; nenhum dado de cliente foi copiado), `scripts/`.

Nota de método: os templates dão a estrutura das telas (rótulos, abas, botões). Opções que vêm do servidor (tipos de campo, gatilhos de automação, tipos de ação, widgets) não aparecem nos bundles e ficam marcadas como [D] ou [I].

---

## 0. Arquitetura (resumo)

- [O] Front híbrido: AngularJS (`angular-app.min.js`, 7,4 MB) + React (`react-app.min.js`, 10,1 MB) + 6 chunks. Libs: CKEditor, FullCalendar, SignalR, Google Maps/Charts, Intercom, Birdie, PostHog (visto em rede).
- [O] API do front = mesmo contrato OData da API pública (`api2-s08-app-canary...`), mais serviços por domínio: `reports-commands-api`, `operation-screen-br`, `ai-builder`, `flows-api-s08`, `identity-provider`, `logs-api`, `exporter-api`, `importation2`, `mailmapie` (e-mail), `service-channel-api`, `sse-subscriptions`, `socket-hub` (SignalR), `pricing`, `central-api` (feature flags), `gateway-api2`.
- [O] Nomes de entidades são personalizáveis por conta: os templates usam tokens como `(name:deal::s)`, `(name:pipeline:aa:s)`, `(name:contact_product:...)` com gênero e número. "Negócio", "Funil" e "Produto de cliente" são rótulos configuráveis (a API `/Account` fala em "alterações nos nomes das entidades").
- [O] Módulos habilitáveis por conta (Administração > Módulos do sistema): Clientes, Produtos, Tarefas, E-mail, Formulários Externos, Gestão de Dados (MDM), Propostas e Documentos, CPQ, Workflow (automação e obrigatoriedades), Analytics, Acesso à API, White Label, Radar Ploomes (transcrição IA), Biblioteca (com IA), Produto de cliente, Negócios.
- [O] Ordenação das telas e regras vale "da primeira para a última" em automações e fluxos de aprovação.

---

## 1. Mapa de módulos

Formato: finalidade | telas/rotas | entidades/campos | ações | automações/regras | API | confiança.

### 1.1 Resumo / Home
- Finalidade: painel inicial. [O]
- Rota `/`. Widgets: negócios criados, leads criados, vendas criadas, propostas criadas, negócios atuais (por funil/estágio, com situação "em andamento/ganho/perdido" e "meus/de outros"), tarefas do dia, mural de mensagens (publicar para "todos da empresa" ou "limitado a equipes", com anexos), atividades. [O]
- API: `Messages`, `Deals`, `Tasks`, `Self@Counters`. [O] (rede); endpoints de widget [D].
- Confiança: alta para estrutura, média para métricas exatas.

### 1.2 Clientes (Contacts)
- Finalidade: base de empresas e pessoas, com filiais e vínculos. [O]
- Rotas: `/Contacts/table`, `/contact/{id}`. [O]
- Telas: tabela dinâmica com abas e filtros, modo "Mapa", segmentação por modo de visualização, "Novo cliente" (Pessoa ou Empresa), página do cliente com menu lateral (Editar, Mesclar, Excluir, foto, exibir informações de filiadas) e abas: Linha do tempo, Filiais, Pessoas, Negócios, Propostas, Documentos, Vendas, Produtos do cliente, Anexos, Formulários externos. Pré-visualização lateral (entity preview). [O]
- Entidades: Contacts (Empresa/Pessoa, `Type`, `Company`, `Tags`, `Contacts` relacionados), `Contacts@Origins`, `@LinesOfBusiness` (segmento), `@NumbersOfEmployees`, `@Relationships`, `@Status`, `@Types`, `Contacts@Products` (produto do cliente). Campos de API: Name, LegalName, Register, Email, StreetAddress, CityId, Phones (com `PhoneTypes`). [O]
- Formulários separados: Empresas, Pessoas, mini de ambas, Produto do cliente (e mini). [O]
- Ações: criar, editar, excluir, mesclar (com regras por perfil), importar planilha, edição em massa, exportar (exporter-api), filtros salvos, marcadores (tags), consentimento (campo `contactHasConsent`), integração com Receita (IRS/"integração de CNPJ", beta). [O]
- Regras: detecção de duplicidade via Gestão de Dados (CPF, nome, e-mail, telefone, endereço, razão social). [O]
- Confiança: alta.

### 1.3 Leads
- Finalidade: pré-venda antes de virar cliente. [O]
- Telas: lista com menu lateral, novo lead, modal de lead com histórico e notas, status (Agendado para, Descartado com motivo, Convertido), "levar ao topo", conversão para Empresa ou Pessoa (+ negócio). Importar leads por Excel; configurar campos de leads; formulário próprio `LEAD_FORM`. Busca por nome da empresa, e-mail, CNPJ, telefone. [O]
- Automação: ação de automação com "cadência de leads" (início imediato ou aguardar início) e integração Meetime (cadência). [O]
- API: não há tag `Leads` no OpenAPI público. [O] Endpoint interno [D].
- Confiança: média.

### 1.4 Negócios (Deals) e Funil
- Finalidade: oportunidades em funis (pipelines) com estágios. [O]
- Rotas: `/Deals/table`, `/Deals/all/table`, `/Deals/{id}/funnel`, `/pipeline`, `/deal/{id}`, `/deal/{id}/task/{id}`. [O]
- Telas: lista com modo Tabela ou Funil/Kanban; filtros "Exibir" (situação: em andamento/ganho/perdido; responsáveis: meus/de outros; ordenação), seletor de funil, busca por título, "Novo negócio" (modal e página cheia). Página do negócio: cabeçalho (valor, moeda, cliente, funil de estágios clicável), opções (marcar lido/não lido, editar, duplicar, mesclar, excluir, remanejar para outro funil), abas com contador, linha do tempo, widgets de informações, anexos, documentos/propostas/vendas, formulários externos, tarefas, checklists por estágio, "negócio de origem". [O]
- Config. do funil (`pipelinePage`): nome, cor do ícone, etapas (criar/reordenar), automações e checklists por etapa, formulário de novo negócio e mini, visualizações permitidas (Tabela, Funil), para quais funis o negócio pode ser remanejado, modelos de documento permitidos, nomenclatura (singular, plural, gênero), Status do negócio (para acompanhar atendimento via registro de interação), ações de ganhar/perder (cor e verbo do botão), motivos de perda. [O]
- Campos de API: Id, Title, StageId, ContactId, PersonId, OwnerId, Amount; `$expand` Contact, Stage, Status, Tags, Tasks, Products, Quotes, Origin, InteractionRecords, Orders, OtherProperties, Documents. Ações `POST /Deals({Id})/Win|Lose|Reopen` (respeitam checklists e regras de funil). `Deals@Stages` (expande Checklists, Automations), `Deals@Status`, `Deals@LossReasons`, `Deals@Pipelines` (expande Form, Stages, AllowedUsers, AllowedTeams, LossReasons, AllowedDocumentTemplates). [O]
- Rede observada: `Deals({id})/GetApplicableChecklists`, `ValidateChecklist`, `Self/ResetDealsCounter`, `Self@DealsOrdinations`, `PublicForms@Deals`. [O]
- Kanban (screenshot): colunas por estágio com nome, ícone, soma em R$ e contagem; cartões com título, subtítulo (cliente), valor, avatar do responsável, marcadores coloridos, ícone de tarefa e selo de idade em dias. Coluna carrega isoladamente e tem estado de erro com "tentar novamente". [O]
- Confiança: alta.

### 1.5 Tarefas e Agenda
- Rotas: `/Tasks/table`, `/task/{id}`. [O]
- Telas: modos Calendário (FullCalendar), Lista e Tabela, mais Mapa; filtros (abertas/finalizadas, minhas/de outros), nova tarefa, modal completo. [O]
- Campos: título, descrição, tipo (`Tasks@Types`: Simples, Reunião, Telefone, E-mail, Conferência, Visita, WhatsApp, com e sem horário), cliente, negócio, contatos relacionados, usuários, responsáveis, outros participantes (e-mails não cadastrados), marcadores, anexos, localização, repetição (`RepeatIntervalUnits`, "repetir a cada"), lembrete por e-mail (`EmailReminders`), data relativa, convite de calendário. [O]
- Ações: finalizar (`POST /Tasks({Id})/Finish`), finalizar com observações (cria registro de interação), duplicar, excluir, tarefas por transcrição automática com IA e modelo de resumo. [O]
- Integrações: Google Calendar (3 passos) e Outlook, com compartilhamento de contatos. [O]
- Confiança: alta.

### 1.6 Registros de Interação
- Finalidade: histórico de contatos (ligação, visita, etc.), vinculados obrigatoriamente a cliente ou negócio. [O]
- Telas: lista, calendário, mapa, modal (abas de dados do cliente, localização, e-mail, telefone, pessoas da empresa), check-in e check-out com endereço. [O]
- API: `InteractionRecords` CRUD (ContactId, DealId, Date, TypeId, Content, Title, NotifiedUsers, Comments). [O]
- Origem automática: telefonia (Zenvia/TotalVoice), WhatsApp, finalização de tarefa. [O]
- Confiança: alta.

### 1.7 Produtos
- Rota: `/Products/table`, `/Products/table/product/{id}`. [O]
- Entidades: Produto, Grupo, Família, Lista, Vínculo/Parte (`Products@Parts`, com tipos de vínculo e blocos "apenas um"), Tabela de estoque, produtos serializados, imagens. Modos de visualização: Produtos, Grupos, Famílias, Vínculos. [O]
- API: `Products`, `@Families`, `@Groups`, `@Parts` (CRUD); campos Id, Name, Code, GroupId, FixedPrice, FamilyId; `$expand` Group, Family, Currency, Parts. [O]
- Campos customizados podem ser ligados a um grupo; flag "disponibilizar como filtro" na inserção de produtos em documentos. [O]
- Importação com "importar vínculos de produto (pai/filho)". [O]
- Confiança: alta.

### 1.8 Propostas, Vendas (Pedidos) e Documentos
Ver seção 5. Rotas: `/Quotes/table`, `/Orders/table`, `/Documents/table`. [O]

### 1.9 Metas (Goals)
- Rotas: `/goals`, `/goals/panel/{id}`, `/goals/panel/{id}/newgoal`. [O]
- Assistente de meta (11 passos): nome; entidade e campo a metrificar; agregação; filtro (data, região, equipe); usuários ou equipes responsáveis; papel; quem pode ver; início e fim; campo de data de referência; divisão do intervalo; valor por intervalo; cor; exibir só em %; duplicar. [O]
- Painéis de metas, navegação por intervalos, visão por situação, responsabilidade, intervalo; filtros por usuário ou equipe. [O]
- API: `Goals`, `Panels@Goals` (rede). [O]
- Confiança: alta.

### 1.10 Relatórios
- Rotas: `/reports/panel/`, `/reports/panel/{id}`. [O]
- Painéis com gráficos configuráveis: tipos Área, Barra (vertical/horizontal), Donut, Pizza, Funil, Indicador, Linha, Mapa (mundo/países/estados), Tabela; tamanhos P/M/G. Configuração: métrica (objeto, propriedade, agregação), métrica calculada, dimensão, intervalo, filtros, cor, drill-down, filtros de painel, permissões, modo quiosque, duplicar/mover gráfico, "analisar com IA". Gráfico especial de histórico de funil (avançados, ganhos, perdidos, tempo médio, taxa de conversão). Há "Reports Pro" com limites. [O]
- Backend: `reports-commands-api`; front React (`REACT_PLOOMES_CHARTS`). [O]
- Confiança: média-alta.

### 1.11 Operação (Beta)
- Rota: `/operations`. Telas de operação por painéis; serviços `operation-screen-br` (`operationPanels`, `widgetMasterTypes`, `widgetTypes`). [O]
- Widgets padrão: Atividades, Checklist, Dados do cliente, Dados do negócio, Documentos, Metas, Registro de interação, Publicações, Lista de tarefas. Painéis têm permissões Administrador, Editor, Leitor e modo de customização. Tela focada em tarefa com "tarefas atrasadas/próximas". [O]
- Confiança: média (React).

### 1.12 E-mail
- Telas: caixa em threads, pastas e subpastas, filtros/regras de e-mail, modelos de e-mail, novo e-mail (Para/Cc/Cco, assunto, modelo, link de visualização com rastreio de leitura), rascunho, agrupar por conversa. [O]
- Integrações: Gmail (OAuth), Microsoft, SMTP/IMAP manual (servidor, porta, SSL), cópia dos enviados. Serviço `mailmapie`. [O]
- Confiança: média-alta.

### 1.13 Chats internos
- Chat entre usuários e grupos (criar grupo, admins, foto, arquivar, não lidas, menções, GIFs, anexos, silenciar alerta), mini-chat, tela cheia, tempo real via SignalR (`socket-hub`). [O]

### 1.14 Telefonia
- "Ploomes Fone": painel de discagem (teclado, chamando, em ligação, finalizada) com criação de registro de interação ao fim da ligação. [O]
- Integrações de telefonia: TotalVoice/Zenvia (ramais por usuário, relatório de chamadas, recarga), Callix. [O]

### 1.15 Usuários
- Rotas: `/Users/table`, `/user/{id}`, `/user/{id}/config`. [O]
- Página do usuário: negócios, clientes, linha do tempo, log de navegação, perfil, configuração (senha, MFA Google). [O]
- API: `Users` (expande Role, Profile, Teams), `Users@Profiles`, `Roles` (cargos), `Teams`, `Departments`. [O]

### 1.16 Logs
- Rota `/Logs/changes`. Três abas: alterações (usuário, ação, entidade base, id, data), API (método, endpoint, início, fim) e navegação. Serviço `logs-api` (`ChangeLog`, `ChangeLog/Count`). [O]

### 1.17 Biblioteca de anexos (AttachmentsLibrary)
- Coleções, pastas e subpastas (limite de profundidade), itens, tabela, vínculo a entidades, download, armazenamento, **RAG** (biblioteca com IA, módulo habilitável). [O]

### 1.18 Aprovações
- Painel de pendências ("uma proposta/venda/documento requer sua aprovação") e ações aprovar/recusar nos documentos. [O] Ver 5.3.

### 1.19 Ploomes IA / Builder
- Rota `/ai-chat/new`. Serviço `ai-builder` (`projects`, `templates`, `menu`, `account/usage`) e `flows-api` (contagem de fluxos). Telas de CRM geradas por IA ("construções") com status rascunho/publicado, ícone, quem pode usar (usuários ou equipes), tela inicial. Consumo em créditos (`/administration/aiconsumption`, `pricing/api/ai-credits`). [O]
- Detalhe do gerador, prompts e modelos [D] (React não analisado).

---

## 2. Catálogo de componentes de UI reutilizáveis

| Componente | O que faz | Evidência |
|---|---|---|
| **Tabela dinâmica** (`DynamicTable`) | Colunas configuráveis por aba (entidade + coluna + "destacar"), paginação, itens por página, edição de colunas condicionada à permissão da aba | [O] templates |
| **Abas dinâmicas** (`DynamicTabs`) | Aba = filtro salvo; nome, itens por página, "criar aba sem filtro", ações da aba (ex.: integrar ao MailChimp) | [O] |
| **Filtros** | Simples e avançado (E/OU, operadores igual, diferente, maior, menor, vazio, datas fixas e relativas, "dias/meses/minutos"), filtros salvos com visibilidade por quem pode ver, alerta de filtro de alta complexidade | [O] |
| **Funil/Kanban dinâmico** | Colunas por estágio, totais, cartões com avatar, tags, idade; carga por coluna | [O] + screenshot |
| **Slide lateral** (`modalSideSlide`) | Painel que desliza para edição (automações, checklists, perfis, formulários externos, fórmulas, campos) | [O] |
| **Modal** (`modal`, `modalConfirm`, `modalFooter`, `section`) | Confirmações com texto de reversibilidade | [O] |
| **Formulário dinâmico** (`Forms`) | Renderiza campos por configuração; edição inline; validação de unicidade; campos obrigatórios por regra | [O] |
| **Campos customizados** | select, select FK (referência a entidade), tag, endereço (e CEP estrangeiro), anexo, imagem, moeda, duração, telefone, e-mails múltiplos, data relativa, repetição, lembrete por e-mail, convite de calendário, consentimento, ações de negócio, assinatura, cor, fórmula, acumulador, HTML/JS ("campo dev") | [O] |
| **Timeline** | Atividades, modificações, modificações de automação, registros de interação (check-in/out), filtro por tipo, "próximos passos" | [O] |
| **Checklist** | Bloco no negócio por estágio; itens ligados a campos; condição de aplicação; exigência para avançar | [O] |
| **Tags (marcadores)** | Descrição e cor, por entidade | [O] |
| **Edição em massa** | Campo + operação + valor, ação especial, excluir itens, aviso de cascata | [O] |
| **Busca rápida** | Global: empresas, pessoas, negócios, propostas, vendas, usuários, com "mais relevantes" | [O] |
| **Importação** | Wizard Upload, Mapeamento, Configurações, Resumo (planilha de erros) | [O] |
| **Mesclar** | Modal de merge de clientes e negócios, painel de duplicadas | [O] |
| **Mapa** | Google Maps para clientes, tarefas, registros | [O] |
| **Seletor de pessoas permitidas** (`allowedUsers`) | Usuários e equipes para visibilidade | [O] |
| **Seletores** | ícone, idioma, funil, estágio, moeda | [O] |

### Padrão visual (screenshots)
- Menu lateral esquerdo largo, azul-marinho escuro; item ativo em destaque azul-ciano com texto branco; ícones de linha; rodapé com logo e botões de recolher. Entradas: Ploomes IA (selo "Novo"), Operação (selo "Beta"), Resumo, Clientes, Contato Venda (rótulo do módulo de negócios), Documentos, Produtos, Relatórios, Administração. [O]
- Barra superior clara: botão "+", busca global, botão "Assistente" (IA, contorno roxo), ícones de e-mail, notificações e chat com contadores amarelos, perfil. [O]
- Barra de contexto da lista: seletor de funil, busca, "Exibir", "Filtros", alternador Funil/Tabela, "Menu", botão primário ciano "Novo Negócio". [O]
- Fundo de conteúdo cinza muito claro, cartões brancos com borda fina e cantos pouco arredondados; tipografia sem serifa compacta (aparenta ser da família Source Sans, [I]); texto secundário em cinza. [O/I]
- Modal central com lista à esquerda (entidades) e conteúdo à direita (automações), contador "9/200" e botão primário ciano; linhas com ícone de lâmpada (ativa) e engrenagem. [O]
- Configuração de formulários: coluna de acordeões à esquerda (Cliente, Produto, Sua Empresa, Usuário, Registro de Interação) e estado vazio "Selecione um formulário para editar". [O]
- Lib de ícones FontAwesome (classes `fal`/`fa`) nos templates. [O]

---

## 3. Motor de automações

- Local: Administração > Automações (modal "Automações"), também embutido no funil (por estágio) e fila em `/administration/processingqueue`. [O]
- **Entidades suportadas** (lista da tela): Cliente, Contato de Venda (negócio), Proposta, Produto, Tarefa, Usuário, Registro de Interação, Documento, Produto de cliente, mais "Histórico de automações". Vendas e Leads também aparecem por cadência/ação [I]. `GET Automations@AllowedEntities` fornece a lista. [O]
- **Estrutura de uma automação** (`automationContent`): 1 Descrição (máx. 150 caracteres); 2 Gatilho ("Quando a automação deve ser disparada?"); filtro (bloco de condição, com alerta de complexidade e limite de nós); lista ordenada de ações; opção "impedir disparo por outras automações"; criado por / atualizado por; salvar, habilitar/desabilitar, duplicar, excluir. [O]
- **Gatilhos**: o texto mostra que existem gatilhos de evento (ex.: "quando o cliente for CRIADO") e gatilhos temporais/periódicos ("Executar automação a cada", "Iniciar em", "às", "Salvar e executar agora", com contagem de itens afetados e aviso de que pode levar mais de 1 hora). Também há gatilho por campo editado ("campo já usado como gatilho"). A lista exata de gatilhos vem do servidor. [O] parcial / [D] lista completa.
- **Condições**: o mesmo editor de filtro avançado (E/OU, campos por caminho entre entidades); limite de nós. [O]
- **Ações observadas** (campos dos templates): editar campos (valor estático, valor puxado de outro campo, fórmula, limpar valor; tipo de edição; datas "dias após a automação"); mover estágio de negócio; perder negócio com motivo; enviar e-mail (remetente fixo, de campo ou "sem remetente"/no-reply; destinatários por campo ou lista; modelo; usuário a avisar se falhar); enviar WhatsApp (via Neppo, com cobrança por envio; destinatários por campo, primeiro ou todos os telefones; grupo de atendimento; modelo de mensagem); criar tarefa (dias depois, compartilhar com relacionados); finalizar tarefas (todas, específicas, "com observações"); adicionar usuários a partir de campo; adicionar marcador; criar/atualizar Produto de cliente por campos-chave; enviar lead em cadência (imediata ou aguardar início). [O]
- **Execução**: ordem do primeiro ao último; se duas alteram o mesmo dado, a de baixo prevalece; limite por entidade (`MAX_AUTOMATIONS_PER_ENTITY_REACHED`; a tela mostra "9/200"). Duplicar em outro estágio. [O]
- **Logs**: "Histórico de automações" (filtros por data, descrição, identificação; colunas automação, entidade, identificação, data/hora, resultado Sucesso/Erro; navegar até ação, automação ou entidade). Timeline do registro mostra "modificações de automação". [O]
- **Fila de Processamento**: acompanha automações e webhooks em tempo real (status Na fila/Processando, Automação, Ação, Entidade, URL para webhooks, "atualizado há ..."). API `Automations@Status`. [O]
- **Webhooks** (API pública): entidade/ação (`Webhooks@Actions`), callback com ValidationKey; até 10 retentativas com intervalo crescente. [O]
- **Automações de documentos**: módulo "Propostas e Documentos" e "Workflow" ativam automações de documentos (necessário para CPQ). [O]
- **Flows** (`flows-api-s08`): serviço separado de fluxos com autenticação própria; relação com automações [D].
- Também existem Fluxos de aprovação (seção 5.3) e regras de campo por fórmula (seção 4), que funcionam como regras de negócio adicionais.

---

## 4. Campos customizados, formulários e opções pré-cadastradas

- **Campos e formulários** (`/formconfig`): construtor com seções, campos arrastáveis, "Nova seção", "Adicionar campo", "Criar campo", seção com quem pode ver, maximizar/minimizar seções. Formulários editáveis: Empresas, Pessoas (e minis), Produto do cliente (e mini, e filtro), Leads, Produtos (e mini, filtro), Grupos, Famílias, Listas, Vínculos (e mini), Registro de interação, Usuários, Equipes, Sua empresa; negócios, propostas, pedidos e documentos têm formulários próprios por funil ou modelo. [O]
- **Configuração de campo** (`fieldConfig`): tipo; título (por idioma, até 50 caracteres); texto informativo (HTML); valor padrão e gatilho; campo múltiplo; campo chave (unicidade, ligada ao MDM); 1 a 4 colunas; fórmula; limitar itens listados (filtro simples ou por fórmula); marcar como dado de pessoa física (exclusão definitiva ao apagar conta); associar a grupo de produtos; usar como filtro; exibir como interruptor (checkbox); obrigatório (por padrão, por fórmula, por usuário); bloqueado/edição por fórmula; oculto/visibilidade por fórmula; chaves de integração externa (envio/recebimento); quem pode criar opções durante o preenchimento; campo "dev" com HTML/CSS/JS; tradução por idioma. [O]
- **Tipos de campo**: lista vem de `Fields@Types` (servidor). Pelos componentes: texto, número, moeda, data, data relativa, seleção única e múltipla, opções pré-cadastradas, referência (FK) a cliente/produto/usuário/etc., tag, telefone, e-mails, endereço, anexo, imagem, duração, repetição, assinatura (só mobile), consentimento, cor, fórmula, acumulador, dev field. [O]/[I] para tipos exatos.
- **Fórmulas** (4 modos): simples, condicional (SE/ENTÃO/SENÃO, OU SE), integrada (chama URL externa: URL, headers, método, JSONPath, body) e Excel (planilha, aba, célula de resultado, células de entrada); variáveis com campo relacionado e propriedade; teste com valores; snippets; "fórmulas geradas por IA" com aviso para testar. [O]
- **Gatilho de campo** (simples ou condicional): ao preencher/alterar, com variáveis e teste. [O]
- **Campo acumulador**: soma/agrega dados de outra entidade com filtros de refinamento, data de referência, limite por conta, proteção contra referência circular, "forçar atualização". [O]
- **Opções pré-cadastradas** (`/administration/fieldsparameters`): tabelas de opções (`Fields@OptionsTables` + `@Options`: Id, TableId, Name), usadas por campos de seleção; CRUD via API. Também listas fixas como origens, segmentos, número de funcionários. [O]
- **Busca pela FieldKey**: campos customizados são acessados na API por `OtherProperties` e `Fields({Key})`. [O]
- **Regras de negócio por campo**: obrigatório, bloqueio, visibilidade, cada uma com regra por usuário e por fórmula. [O]
- API: `Fields` (CRUD), `Fields@Entities`, `@Types`, `@OptionsTables`, `@OptionsTables@Options`; rede: `Fields@Entities@LastUpdates` (cache por entidade). [O]

---

## 5. Documentos

### 5.1 Propostas (Quotes), Pedidos/Vendas (Orders) e Documentos
- Três tipos que compartilham o mesmo gerador: Proposta (`Quotes`), Venda (`Orders`, com estágios próprios em `Orders@Stages`) e Documento genérico (`Documents`, para contratos, OS, etc.). [O]
- Existem versões "legado" (modais e página cheia) e novas "BETA" (CPQ em página cheia: "Nova proposta BETA", "Editar valores preenchidos BETA", "Duplicar BETA", "Revisar BETA"). [O]
- Telas: tabela por tipo, nova (modal, página cheia, "fora do modal" a partir do cliente/negócio), página do documento com iframe de pré-visualização (`preview-ifr`) e side slide com abas: Informações, Atualizações, Proposta online, Comentários, Chat com cliente, Aprovações, Revisões (lista numerada com valor e criador). [O]
- Ações: compartilhar (PDF, link web, e-mail, WhatsApp, rascunho), desligar compartilhamento, atualizar dados e modelo, editar valores preenchidos, modificação visual, duplicar (com negócio), revisar (nova revisão: `POST /Quotes({Id})/Review`), ir à última revisão, excluir, enviar para aprovação. Status de link: ligado, aceito, recusado via link web. [O]
- Campos de API: Quotes (DealId, QuoteNumber; expande Template, ApprovalStatus, Approvals, Pages, Comments, ExternalComments, Sections, Products); Orders (OrderNumber, Date, ContactId, StageId, Amount); Documents (Name, Date, DocumentNumber, TemplateId, ContactId, DealId; Installments, RelatedPeople). [O]
- Produtos no documento: seções de produtos (por famílias, grupos, listas, ou só produtos do cliente), itens, parcelas (quantidade, atualizar valores, total deve bater com valor), opcionais, ações em lote (duplicar/excluir), troca de produto. [O]

### 5.2 Modelos e DocGenerator (editor)
- Rotas `/templates`, `/templates/quotes`, `/docgenerator`. Lista de modelos por tipo: nome, padrão, editor, duplicar, excluir (`DocumentTemplates`: Id, Name, EntityId, Default; expande Form, Pages, AllowedUsers, AllowedTeams). [O]
- Editor: cabeçalho/rodapé, capa, seções (nova, renomear, duplicar, excluir), blocos padrão, campos/tokens do documento (criar campo), estilos, margens (superior, inferior, lateral), paginação, nome do PDF personalizado, parcelas obrigatórias e campo do total das parcelas, quem pode ver o modelo, "modificação visual" (edição de HTML com CKEditor), preenchimento externo (formulário com URL). [O]
- Integração de assinatura por modelo: Clicksign, DocuSign, D4Sign, com mensagem personalizada, assunto. [O]
- IA: gerar documento/formulário/modelo com IA a partir de descrição ou arquivos (limite de 200 mil caracteres), identificar campos, alterar com IA por chat. [O]

### 5.3 Aprovações
- Admin > Fluxos de aprovação (`/administration/businessrules`): por entidade (proposta, venda, documento), nome, filtros (condições), aviso opcional, mensagens de aprovação e bloqueio, níveis de aprovação ("1ª, 2ª aprovação", um ou todos os aprovadores), notificação por e-mail, duplicar, ordem de verificação (primeiro que dispara vale). Remover nível aprova automaticamente pendências. [O]
- Uso: botão "Enviar para aprovação"; painel de aprovações na home; aprovar/recusar no documento; `Approvals` e `Quotes@ApprovalStatus`. [O]

### 5.4 CPQ
- Módulo "Configure, Price, Quote": fórmulas, impostos, margem, descontos, consulta externa (preços, estoque, cotação), kits e pacotes, variáveis e opcionais com regras de exclusão, recomendação e obrigatoriedade entre opcionais, comissão. Exige o módulo de automações de propostas. [O texto de marketing] A implementação do cálculo [D].

### 5.5 Assinatura eletrônica
- D4Sign (cofre, pasta, signatários, ordem, reenvio, cancelar), DocuSign, Clicksign, Lexdocs (sidebar), LetSign (aviso). Campo de assinatura só no app mobile. [O]

---

## 6. Administração (`/administration`)

Central de cards em 6 grupos. [O]

**Geral**
- Módulos do sistema (`/system-modules`): habilitar/desabilitar módulos, ícones, plano "Ploomes Básico", contato com gerente de conta. [O]
- Campos e formulários (`/formconfig`). [O]
- Opções pré-cadastradas (`/administration/fieldsparameters`). [O]
- Dados da empresa (`/administration/account`): logo, e (White Label) cores de marca/ação, logos de login, menu, ícone, e-mail automático. [O]
- Configurações do sistema (`/administration/systemsettings`): salvar cópia de e-mails enviados, padrão regional/máscaras por país, moedas (CRUD, `Currencies`), fuso horário, segurança (senha forte, expiração e histórico de senhas, MFA com tempo de expiração, política de acesso de suporte, login só por SSO, provedor SSO Microsoft ou Sankhya Pass). [O]
- Automações. [O]
- Ploo Store (`/administration/ploostore`): loja de extensões [I]; conteúdo [D].
- Fila de Processamento. [O]
- Ambiente Sandbox: configuração de acesso, migração, recorrência, logs, status, visão geral (muitos textos em `SANDBOX`). [O]
- Gestão de Dados (MDM): lista de bloqueio (e-mail e WhatsApp), tratamento de dados, varreduras de duplicatas, regras de vínculo e varredura de vínculos, pendências; regras de detecção por campo com normalização (CPF sem caracteres, nome minúsculo sem acento, e-mail principal, telefone principal, endereço, razão social); comportamento por origem: criação manual (aviso impede salvar), integração (impedir criação e atualizar o mais antigo), planilha (regras do próprio fluxo). [O]

**Usuários**
- Tabela de usuários, equipes, perfis, permissão ao Suporte Ploomes (solicitações de acesso: aprovar, recusar, revogar, data limite), Registros (logs). [O]
- Perfis (`profilesAdm`): permissões por módulo — clientes (ver/editar/excluir por responsabilidade ou equipe, criar empresa/pessoa, mesclar), produtos do cliente, negócios (criar, editar, excluir, reabrir, ganhar e perder, ver), propostas, vendas, documentos, produtos, leads, metas, interações, formulários externos, campos, integrações, biblioteca, usuários e "outros". [O]
- Usuário: novo, cargo, perfil, contato, último login, ativar/inativar (cobrança pro rata), chave da API (resetar), trocar senha, foto. [O]

**Propostas e vendas**
- Modelos de documento, Fluxos de aprovação, Modelos de e-mail. [O]

**Integrações**
- Integrações plug-and-play (`/administration/integrations`): lista com status (Ligado, Desligado, Pausada, Pendente, Sincronizando), busca. Observadas: RD Station, MailChimp, Zapier, Google Calendar, Outlook, Callix, Dataplace, Obras Online, TotalVoice/Zenvia, Meetime (cadência), Omie (ERP, sob contato), Neppo (WhatsApp), WhatsApp Coex (Meta), Flowbiz, Emites, Sankhya (registros e SSO), Clicksign, D4Sign, DocuSign, Lexdocs. [O]
- Usuários de integração (`/administration/integrationusers`): nome, e-mail, perfil, User-Key. [O]
- Formulários externos (`/administration/externalforms`): logo, nome, descrição, cor, finalidade (criar, atualizar existentes, criar ou atualizar), funil e estágio de destino, campos, valor padrão mapeado, idioma, URL pública e código embed, duplicar em outro funil. [O]
- Registros do Sankhya. [O]

**Pagamentos**
- Dados de pagamento, Histórico de fatura, Créditos de IA (consumo). [O]

---

## 7. Integrações e serviços auxiliares

| Área | O que existe | Conf. |
|---|---|---|
| API pública | OData v4, `User-Key`, webhooks | [O] |
| E-mail | Gmail, Microsoft, SMTP/IMAP, modelos, rastreio de leitura | [O] |
| Chat | Chat interno por SignalR; WhatsApp via Neppo e WhatsApp Coex (Meta Cloud: webhook, resumo por IA, mapeamento de campo de telefone) | [O] |
| Telefonia | Ploomes Fone (webphone), TotalVoice/Zenvia, Callix | [O] |
| Calendário | Google Calendar, Outlook | [O] |
| Marketing | RD Station, MailChimp, Meetime | [O] |
| ERP | Omie, Sankhya, Emites (notas) | [O/I] |
| Assinatura | Clicksign, D4Sign, DocuSign, Lexdocs, LetSign | [O] |
| Importação | `importation2`: upload, mapeamento, configurações, resumo, planilha de erros, uma por vez, detecção de duplicado por campo; entidades clientes, produtos, registros de interação, leads | [O] |
| Exportação | `exporter-api` (status por tipo, "suas exportações") | [O] |
| IA | Assistente (botão superior), Ploomes IA (builder), Radar Ploomes (transcrição de reuniões e tarefas com modelos de prompt), DocGenerator IA, resumo de WhatsApp, analisar relatório, biblioteca RAG, créditos de IA | [O] |
| Metas e relatórios | Seções 1.9 e 1.10 | [O] |
| Operação | Seção 1.11 | [O] |
| Segurança | MFA, SSO, política de senha, auditoria (logs) | [O] |
| Suporte | Canais de atendimento (`service-channel-api`: gerente de conta, financeiro, premium, especializado), Centro de conhecimento, Universidade Ploomes, NPS | [O] |
| Outros | Notificações, lista de bloqueio, mesclagem, sandbox, white label, Intercom | [O] |

---

## 8. Lacunas

1. **Bundle React** (`react-app.min.js`, 10 MB) não analisado: Operação, Relatórios (novos), Ploomes IA/Builder, documentos CPQ BETA, Metas novas, Mesclagem e Sandbox provavelmente são React. Só há chaves i18n e chamadas de rede.
2. **Opções vindas do servidor**: gatilhos de automação (ex.: "Periodicamente"), tipos de ação completos, `Fields@Types`, `Automations@AllowedEntities`, `Webhooks@Actions`, widgets (`widgetTypes`).
3. **Modelos de dados**: o OpenAPI tem `schemas` com 1 item; corpos de request/response e lista completa de campos nativos não estão documentados.
4. **Iframes de proposta** (`preview-ifr`): o corpo do documento não aparece nas capturas; o layout depende do modelo.
5. **Telas de captura antigas** podem estar incompletas (espera fixa); só foram abertas 3 capturas.
6. **Permissões por perfil**: lista completa de flags não extraída campo a campo.
7. **Regras de CPQ** (fórmula de impostos, margem, comissão) e **Flows** não têm evidência de implementação.
8. **Ploo Store**, **Sandbox** e **Lead** (API interna) sem detalhe.
9. **Autenticação, billing e limites por plano** só visíveis em textos; valores [D].
10. **Webhooks**: eventos exatos e payload [D].
11. Source maps indisponíveis (403): lógica real do front está minificada.

---

## 9. Priorização

### Núcleo de CRM (replicar primeiro)
1. Clientes (empresa, pessoa, filiais, tags, origem/segmento) + tabela dinâmica com abas, filtros salvos e busca global.
2. Negócios com funis, estágios, kanban, ganhar/perder/reabrir, motivos de perda, remanejar, linha do tempo.
3. Tarefas e agenda (calendário, tipos, repetição, lembretes) e registros de interação.
4. Campos customizados, formulários por entidade, opções pré-cadastradas, regras de obrigatório/bloqueio/oculto.
5. Usuários, equipes, cargos, perfis e permissões por entidade; logs de alteração.
6. Produtos (família, grupo, produto, vínculos) e itens em propostas e vendas.
7. Propostas e vendas com modelo, PDF, link público com aceite, parcelas, revisões.
8. API REST/OData com chave de integração e webhooks; importação e exportação por planilha.

### Avançado
1. Automações (evento + temporal, condições, ações, fila e histórico).
2. Checklists por estágio e validação para avançar.
3. Fluxos de aprovação multiníveis.
4. DocGenerator (editor de modelos) e assinatura eletrônica.
5. Relatórios em painéis com métricas calculadas e metas.
6. Gestão de dados: duplicidade, mesclagem, lista de bloqueio.
7. Fórmulas de campo (simples, condicional, integrada, acumulador).
8. Formulários externos para captura de leads; leads com cadência.
9. Operação (painéis por widgets) e dashboards home.
10. E-mail integrado e SMTP.

### Periférico
CPQ completo, WhatsApp (Neppo/Coex), telefonia, chat interno, Google/Outlook Calendar, RD/MailChimp/Meetime/Omie/Sankhya, IA (assistente, builder, transcrição, RAG), biblioteca de anexos, white label, SSO/MFA, sandbox, Ploo Store, mural de mensagens, NPS, centro de conhecimento, créditos e cobrança.

---

## Apêndice - Referência rápida da API pública (OpenAPI)

- 79 paths, 129 operações, 22 tags: Account, Cities, Contacts, Currencies, Deals, Documents, DocumentTemplates, Departments, Fields, InteractionRecords, Operations, Orders, PhoneTypes, Products, Quotes, RelativeDates, Roles, Tags, Tasks, Teams, Users, Webhooks. [O]
- Servidor `https://public-api2.ploomes.com`; header `User-Key` (gerada em Administração > Usuários de integração; só admins criam). [O]
- Parâmetros OData em todas as listagens: `$top`, `$skip`, `$select`, `$expand`, `$filter`, `$orderby`. `$expand=OtherProperties` traz campos customizados. [O]
- Convenção `Entidade@Subentidade` para tabelas de apoio (ex.: `Deals@Stages`, `Contacts@Origins`). [O]
- Operações de ação: Deals `Win|Lose|Reopen`, Tasks `Finish`, Quotes `Review`. [O]
- Leitura apenas (sem POST/PATCH/DELETE) em: Cities (3), Currencies, Deals@Stages/Status/LossReasons, DocumentTemplates, Fields@Entities/Types, Operations, PhoneTypes, RelativeDates, Contacts@Relationships/Status/Types, Quotes@ApprovalStatus, Tasks@Types/RepeatIntervalUnits/EmailReminders, Users@Profiles, Webhooks@Actions. [O]
- CRUD completo: Contacts, Contacts@Origins/LinesOfBusiness/NumbersOfEmployees/Products, Deals, Deals@Pipelines, Documents, Departments, Fields, Fields@OptionsTables@Options, InteractionRecords, Orders, Orders@Stages, Products (+Families/Groups/Parts), Quotes, Tags, Tasks, Teams, Users, Webhooks. Roles só GET e POST. [O]
- Ausentes da API pública (existem no app): Leads, Goals, Automations, Checklists, Filters, Approvals, Messages, Timeline, Library, Reports, Integrations. [O]
