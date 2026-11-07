# Plano de frontend: levar os recursos da Ploomes (e do rea-plom) para o AST.Comercial.Web

Base: `ast.comercial/AST.Comercial.Web` (Vue 3.5, TypeScript, Vite, Pinia, vue-router, sem biblioteca de UI).
Insumos: `02-mapeamento-ploomes.md` (o que a Ploomes tem), `03-rea-plom.md` (comportamento observado do funil e dos campos), `01-ast-comercial.md` (estado atual), `04-comparativo-e-plano.md` (plano geral).
Legenda: [O] observado nos relatórios ou no código que li · [I] inferência minha · [V] verificar antes de implementar.

---

## 1. Estado atual do front (lido no código)

| Item | Situação [O] |
|---|---|
| Telas | Login, Resumo (stub), Clientes, Negócios, Propostas, Produtos, Relatórios (reusa o stub), Admin, Editor de modelo |
| Padrão de tela | View fina sobre `TabelaEntidade` (grade OData genérica) com `FormModal`/`FormularioDinamico` |
| Extensão | `TabelaEntidade` aceita `semFormInterno` e emite `novo` / `editar(id)` |
| Estado | Só `auth` e `tema` (Pinia) |
| API | `api.ts`: `odataGet/GetOne/Post/Patch/Delete`, refresh em 401. Sem `$apply`, sem tratamento de erro por status, sem cache |
| Rotas | Sem detalhe (`/negocios/:id` etc.). Admin usa `:painel` com mapa de componentes, sem deep link por seção |
| Componentes base | Não existem. Modal, slide lateral, abas, badge, toast e select com busca não são compartilhados |
| Arquivos grandes | `TabsFiltro` 34 KB, `EditorFormula` 32 KB, `CamposFormularios` 25 KB, `FormulariosAdmin` 24 KB |
| Testes | Nenhum no front |
| Maior ativo | Campos e formulários com editor Monaco de fórmulas, filtros/colunas salvos por usuário |

---

## 2. Princípios

1. **Reaproveitar o que o AST já tem** (grade, filtros salvos, formulário dinâmico, editor de fórmula) e levar para telas de detalhe, em vez de criar um segundo padrão.
2. **Fundação de UI antes das telas grandes.** Sem componentes base, o kanban, o builder de automação e o editor de proposta vão duplicar modal/abas/estilo.
3. **A Ploomes é especificação de comportamento, não de código.** O `mapeamento` diz quais campos, abas e ações cada tela tem. Não copiar templates, textos de i18n, ícones ou visual idêntico.
4. **Contrato é do backend do AST.** O que o rea-plom observou vem da API interna da Ploomes (canary). Serve como regra de comportamento, não como contrato.
5. **Cada fase termina utilizável e testada.**

---

## 3. Fase F0 — Fundação do front (antes das telas novas)

Estimativa [I]: 1 a 2 semanas.

### 3.1 Componentes base (`src/components/ui/`)
Criar e passar a usar nos componentes existentes, em ordem de uso:

| Componente | Para quê | Referência na Ploomes [O] |
|---|---|---|
| `UiModal`, `UiConfirm` | Diálogos | Modal "Automações" com lista de entidades à esquerda |
| `UiSlideOver` | Edição em painel lateral | Slide lateral de edição (automação, tarefa, negócio) |
| `UiTabs` | Abas de detalhe | Detalhe de negócio/cliente |
| `UiBadge`, `UiAvatar`, `UiTag` | Status, responsável, etiqueta | Cartões, tabelas |
| `UiToast` | Feedback de sucesso/erro | Hoje o erro vira `throw new Error` |
| `UiSelect` (busca + assíncrono) | Cliente, usuário, produto, etapa | Seletores em formulários |
| `UiDate`, `UiMoney` | Entrada tipada | Campos de data/valor |
| `UiEmpty`, `UiSkeleton` | Vazio e carregando | Todas as listas |
| `UiStepper` | Etapas do funil no topo do negócio | Cabeçalho do negócio |

Tokens de tema continuam em `App.vue` (dark por padrão, acento ciano). Extrair para `src/styles/tokens.css`.

### 3.2 Camada de dados
- `api.ts`: suporte a `$apply` (groupby/aggregate), `$expand` aninhado, `$count`, cabeçalhos `If-Match` se o backend passar a usar. Erro tipado (`ApiError` com status e corpo) em vez de `throw new Error`.
- Composables: `useOData<T>()` (lista paginada com filtro/busca/ordenação), `useEntidade<T>(endpoint, id)` (carregar/salvar), `useDebounce`.
- Stores Pinia novas: `usuarioAtual` (perfil, equipes, permissões), `funis` (funis e etapas em cache), `esquemaCampos` (`Campos@Esquema` por entidade), `preferencias` (última visualização, funil e filtro por usuário; a Ploomes persiste isso em `Self`, o AST pode usar `VisualizacaoSalva` ou uma entidade nova) [I].
- Interceptor de erro: 403 mostra aviso de permissão, 422 mostra mensagem de regra de negócio, 5xx toast genérico.

### 3.3 Roteamento
- Rotas de detalhe: `/clientes/:id`, `/negocios/:id`, `/propostas/:id`, `/atividades`, `/contatos`.
- Admin com rotas reais: `/admin/campos`, `/admin/automacoes`, `/admin/perfis`... (a Ploomes usa `/administration/automations`, `/administration/teams` etc.), para deep link e botão voltar.
- Rota pública sem layout: `/p/:chave` (aceite de proposta), `/f/:chave` (formulário externo).
- `meta.permissao` por rota e guard usando `usuarioAtual` (depende da Fase 0 do backend).
- Breadcrumb no `AppHeader`.

### 3.4 Qualidade
- Vitest + Vue Test Utils para `api.ts`, composables e componentes base.
- Playwright para os fluxos críticos (login, mover card, criar proposta). Os scripts CDP do rea-plom são úteis como referência do *que* observar, mas têm caminhos fixos e um deles vasculha credenciais; não reaproveitar o código.
- `vue-tsc` já existe; manter no CI.

**Saída da fase:** tela de Clientes e Produtos migradas para os componentes base, testes rodando no CI, rotas de detalhe vazias navegáveis.

---

## 4. Fase F1 — Funil kanban de negócios (maior contribuição do rea-plom)

Estimativa [I]: 2 a 3 semanas. É a entrega de maior impacto: hoje `NegociosView` é só grade.

### 4.1 Comportamento a replicar (observado no rea-plom [O])

| Aspecto | Comportamento da Ploomes | Equivalente no AST |
|---|---|---|
| Carga | Uma consulta por coluna, em paralelo, `$top=15`, rolagem incremental por `$skip` | `odataGet('Negocios', { filter: EtapaId eq X, top: 15, skip })` por coluna |
| Filtro de status | Só negócios abertos (StatusId 1 e 2 na Ploomes) | Status "Aberto" do AST; ganhos/perdidos ficam fora do kanban |
| Ordenação | Negócios com tarefa pendente primeiro, depois data da primeira tarefa, depois mais recentes | Ordenar por tarefas pendentes, vencimento mais próximo, `Id desc` [V: precisa de campo/ordem derivada no backend] |
| Totais da coluna | Consulta separada com agregação por moeda e etapa: soma do valor e contagem | `$apply=groupby((MoedaId,EtapaId),aggregate(Valor with sum as Total,$count as Qtd))` [V: confirmar que os controllers do AST permitem `$apply`] |
| Multimoeda | Total por moeda, com símbolo | Mostrar um total por moeda no cabeçalho |
| Busca | `contains` no título ou no nome do contato, aplicada aos cartões **e** aos totais | Mesmo filtro nas duas consultas |
| Mover cartão | `PATCH` só com `{Id, StageId}` | `odataPatch('Negocios', id, { EtapaId })` |
| Pós-mover | Recarrega totais das colunas afetadas e consulta negócios derivados | Recarregar totais de origem e destino |
| Regras do pipeline | Flags `ForbiddenStageReturn` e `MustPassAllStages` lidas a cada carga | Adicionar ao `Funil` no backend e validar no servidor (422). O front só reflete: bloqueia o drop e explica |
| Preferências | Último funil, modo (funil/tabela), filtro, mostrar meus/outros/abertos, por usuário | Store `preferencias` persistida |
| Troca de funil | Seletor de funil no topo | `FunilSelector` |

Lacunas do rea-plom [O]: os valores das flags, o significado dos status e o erro devolvido ao violar a regra **não foram capturados**. O contrato desses pontos é decisão do AST.

### 4.2 Componentes
- `NegociosView`: alternância **Funil | Tabela** (a tabela atual permanece).
- `FunilKanban`, `ColunaEtapa` (cabeçalho com totais, lista com rolagem incremental), `CartaoNegocio` (título, cliente, valor, responsável, etiquetas, indicador de tarefa atrasada, dias na etapa).
- `FunilSelector`, `BarraBuscaFunil`.
- Drag and drop: HTML5 nativo para começar. Se a experiência em toque/rolagem automática pedir, avaliar SortableJS ou equivalente, com versão fixa [V].
- Atualização otimista com reversão se o servidor responder 422.
- Ações no cartão: Ganhar, Perder (modal com motivo de perda), Reabrir. Os endpoints já existem no backend [O].

### 4.3 Dependências de backend
`Funil` com `ProibirRetorno` e `ExigirTodasEtapas`; validação no `PATCH`; emitir o gatilho `EtapaMudou` (hoje declarado e nunca emitido); `$apply` habilitado; ordenação por tarefas.

### 4.4 Referência visual
`rea-plom/.rea-demos/preview-funil.html` é um protótipo estático do próprio autor (dados fictícios, não vem da Ploomes), com board de colunas e aba de campos. Serve de ponto de partida de UX. Pedir autorização do autor antes de reaproveitar o código [V].

**Saída da fase:** arrastar um cartão persiste no banco, totais atualizam, regra de etapa é respeitada, teste Playwright cobrindo o fluxo.

---

## 5. Fase F2 — Detalhes e relacionamento (negócio, cliente, contato, atividades)

Estimativa [I]: 3 a 4 semanas.

### 5.1 Detalhe do negócio (`/negocios/:id`)
- Cabeçalho: título, `UiStepper` das etapas (clicável, respeitando as regras do funil), valor, responsável, botões Ganhar/Perder/Reabrir.
- Abas: **Dados** (`FormularioDinamico` com os campos customizados do funil), **Contatos**, **Atividades**, **Propostas**, **Histórico** (filtrar `RegistroAlteracao` pela entidade), e depois **Checklists** (a Ploomes consulta `GetApplicableChecklists` ao abrir o negócio).
- Reaproveitar `FormularioDinamico` e `TabelaEntidade` embutida nas abas.

### 5.2 Linha do tempo
`TimelineEntidade` reutilizável (negócio, cliente, contato): alterações, atividades concluídas, propostas, e-mails (quando existirem). Fonte inicial: `RegistroAlteracao` + `Atividade`.

### 5.3 Cliente e contatos
- `/clientes/:id` com abas: Dados, Contatos, Negócios, Propostas, Atividades, Histórico.
- `ContatosView` (hoje não existe) e aba de contatos dentro do cliente.
- Cadastros auxiliares (origem, ramo, porte, status) editáveis em Admin.
- Mesclagem de duplicados fica para a Fase F5.

### 5.4 Atividades e tarefas
- `AtividadesView` com lista (hoje, atrasadas, próximas) e **calendário** (mês/semana/dia). A Ploomes usa FullCalendar; avaliar a versão MIT com versão fixa [V].
- Slide `NovaAtividade`: tipo, vencimento, responsável, vínculo (cliente/negócio/contato), conclusão rápida.
- Repetição e lembretes: a Ploomes tem `Tasks@RepeatIntervalUnits` e `Tasks@EmailReminders` [O]. No AST isso exige backend novo [V].
- Atalhos de teclado para criar tarefa/negócio/cliente (a Ploomes lista atalhos globais) [O].

### 5.5 Shell global
- Busca global no `AppHeader` (clientes, negócios, contatos, propostas) [depende de endpoint de busca].
- Menu "+" de criação rápida.
- Sino de notificações (depende de backend de notificações).

**Saída da fase:** um vendedor percorre cliente → negócio → tarefa → histórico sem sair do front.

---

## 6. Fase F3 — Propostas e documentos

Estimativa [I]: 3 a 5 semanas.

### 6.1 Editor de proposta (`/propostas/:id`)
- Cabeçalho com status, número/revisão e ações (Aprovar, Rejeitar, nova revisão).
- **Itens**: grade editável (produto com busca, quantidade, preço, desconto) com totais recalculados, frete, parcelas e condições.
- **Seções** (capa, corpo, rodapé) e **parcelas**.
- **Pré-visualização**: renderizar o modelo em um `iframe` a partir de `ModelosDocumento(id)/Renderizar(registroId)`. É o mesmo padrão que a Ploomes usa (`preview-ifr`) [O]. Lembrete: não há PDF no backend ainda.
- Aprovação: lista de aprovadores por nível e histórico (depende de o backend criar `SolicitacaoAprovacao`).

### 6.2 Página pública de aceite (`/p/:chave`)
Sem login, layout próprio: documento, valor, botão Aceitar/Recusar com registro de IP e data. Os campos de link e aceite já existem na entidade `Proposta`, sem endpoint [O].

### 6.3 Editor de modelos (evolução do `EditorModeloView`)
A Ploomes organiza o DocGenerator em abas **Opções / Campos / Blocos / Estilos**, seções (Capa, Seção 1..n), barra de formatação e opções do modelo (paginação, nome do PDF, parcelas obrigatórias, visibilidade) [O]. Plano:
- Manter o editor de seções HTML atual e acrescentar: painel de **Campos** (inserir variáveis a partir de `Campos@Esquema`), **Blocos** reutilizáveis, **Estilos** (fontes, cores) e opções do modelo.
- Editor de texto rico: avaliar TipTap ou equivalente, com versão fixa [V]. A Ploomes usa CKEditor, que não precisa ser adotado.
- Condicionais por campo e laço de itens (hoje o `Renderizar` não faz laço) [O].

### 6.4 Vendas/pedidos
A Ploomes separa Proposta, Venda e Documento sobre o mesmo gerador [O]. Decisão pendente: o AST terá Pedido como entidade própria ou como estado da proposta [?].

**Saída da fase:** criar proposta com itens e parcelas, aprovar, enviar o link e receber o aceite.

---

## 7. Fase F4 — Administração completa e builder de automações

Estimativa [I]: 4 a 6 semanas.

### 7.1 Admin com rotas e cards
Reorganizar `AdminView` em grupos como na Ploomes (Geral, Usuários, Propostas e vendas, Integrações, Pagamentos) [O], com rota por seção.

| Seção | Estado no AST | Trabalho de front |
|---|---|---|
| Campos e formulários | Pronta (melhor tela do AST) | Evoluções na seção 7.3 |
| Opções pré-cadastradas | Parcial (`OpcaoCampo`) | Tela de tabelas de opções |
| Dados da empresa | Leitura | Edição (precisa de `PATCH` no backend) |
| Equipes | Grade | Gestão de membros na UI |
| **Perfis e permissões** | Placeholder | Matriz de permissão por perfil × entidade × ação |
| Tabela de usuários | Grade | Convite, redefinir senha, foto, perfil |
| Modelos de documento | Pronta | Ver seção 6.3 |
| Fluxos de aprovação | Ausente | Editor de níveis, aprovadores e filtros |
| **Automações** | Só lista | **Builder** (seção 7.2) |
| Fila de processamento | Ausente | Lista da fila com filtro por status e reprocessar |
| Integrações / usuários de integração | Lista | Criar e mapear fluxos, testar conexão |
| Formulários externos | Ausente | Editor + página pública `/f/:chave` |
| Registro de atividades | Pronta | Filtros por entidade, usuário e período |
| Configurações do sistema | Ausente | Preferências globais |

### 7.2 Builder de automações (maior tela da fase)
Estrutura vista na Ploomes [O], a partir do print do modal "Regras da automação" e dos relatórios:

1. Modal com **lista de entidades à esquerda** (Cliente, Negócio, Proposta, Produto, Tarefa, Usuário, Registro de interação, Documento, Produto de cliente, Histórico).
2. Lista de automações da entidade (ativa/inativa, última execução, criado por).
3. Slide **Regras da automação** em passos numerados: **Descrição**, **Gatilho** (evento ou periódico, com intervalo e início), opção "impedir disparo por outras automações", **Filtro** em **blocos de condição** (campo + operador + valor, grupos E/OU, copiar/colar bloco, aviso de quantos itens serão afetados) e **Ações** em ordem.
4. Botões **Salvar** e **Salvar e executar agora**; aba de **histórico de execução**.

Mapeamento para o AST: gatilhos (`TipoGatilho`, 14) e ações (`TipoAcao`, 16) já têm catálogos em `Automacoes@Gatilhos` e `@Acoes`; o motor avalia grupos (E dentro, OU entre) com os operadores do enum. O bloco de condição pode partir de `ModalFiltro`/`TabsFiltro`, que já têm lógica de filtro [V: ler os dois antes de decidir o reuso].
- Mostrar na UI apenas gatilhos e ações **com implementação real** (hoje 8 de 14 gatilhos e 12 de 16 ações). Os demais ficam desabilitados com aviso.
- "Salvar e executar agora" só entra quando o backend tiver essa ação.
- As opções de gatilho da Ploomes vêm do servidor e não aparecem nos bundles [O]. No AST vêm de `@Gatilhos`.
- Dependência crítica: **o backend não tem criação/edição de automações** (só GET) [O].

### 7.3 Evoluções em Campos e formulários (referência: rea-plom + mapeamento)
O AST já cobre fórmulas de valor, visibilidade, obrigatoriedade e somente leitura. Itens a avaliar contra a entidade `Campo` [V]:
- Permissões por usuário/equipe/perfil (visualizar, editar, tornar obrigatório) [O: listas `Allowed*` no payload da Ploomes].
- Layout: largura, tamanho de coluna, campo múltiplo, dados sensíveis, único por entidade.
- Validação em tempo real de nome único (a Ploomes consulta a cada tecla) [O].
- Prévia ao vivo do resultado da fórmula (o protótipo `preview-funil.html` tem essa ideia) [O].
- Assistente de IA para fórmula: periférico, fora do escopo inicial.
- Chave do campo: a Ploomes usa `entidade_GUID`, gerada no cliente [O]. O AST já gera chave; manter o formato do AST.

**Saída da fase:** um administrador cria uma automação pela UI, ela roda para todas as empresas e o resultado aparece no histórico.

---

## 8. Fase F5 — Resumo, relatórios, metas, dados

Estimativa [I]: 4 a 6 semanas.
- **Resumo (Home):** contadores (negócios abertos, tarefas de hoje e atrasadas, propostas pendentes), funil resumido, últimas atividades. Hoje é "em construção".
- **Relatórios:** painéis com gráficos configuráveis (a Ploomes usa painéis, gráficos, filtros de painel, modo apresentação) [O]. Escolher biblioteca de gráficos com versão fixa [V]. Depende de endpoints de agregação.
- **Metas:** painel por usuário/equipe, definição de meta e acompanhamento.
- **Importação/exportação:** assistente de planilha (mapear colunas → campos, validar, executar, ver resultado).
- **Gestão de dados:** duplicidade e mesclagem, lista de bloqueio.
- **Operação** (painéis por widgets, Beta na Ploomes): só depois, e depende de analisar o bundle React da Ploomes, ainda não feito.

---

## 9. Fase F6 — Periféricos

E-mail integrado, WhatsApp, telefonia, chat, calendário externo, assinatura eletrônica, IA, SSO/MFA, CPQ. Entram por demanda, cada um com seu backend.

---

## 10. Como usar o `mapeamento` e o `rea-plom` durante a construção

1. **Ficha de tela por módulo.** Antes de implementar cada tela, gerar uma ficha a partir de `mapeamento/fontes/templates-ptbr/<Modulo>/` com: campos exibidos, botões, abas, filtros, modais. É uma lista de verificação funcional, não código a copiar. Posso automatizar a extração (rótulos, botões, abas) para os módulos Deals, Contacts, Tasks, Products, Goals, Users e Submodules/Automations.
2. **Regras de comportamento** do funil e dos campos vêm do `03-rea-plom.md` (seção 3 e 7).
3. **Contrato de API de referência:** `fontes/openapi.json` (OData público da Ploomes) para nomear propriedades e parâmetros de forma familiar, se a compatibilidade for decisão sua.
4. **Lacunas de comportamento** (erros ao violar regra de etapa, tipos de campo 2 a 8, flags do pipeline) exigem recaptura em conta de teste própria, só com leituras, guardando o corpo das respostas.

---

## 11. Dependências entre front e backend

| Item de front | Precisa de backend | Pode começar já? |
|---|---|---|
| Componentes base, rotas, stores, testes (F0) | — | **Sim** |
| Kanban sem regras de etapa | `$apply` habilitado | Sim, com totais calculados no cliente como alternativa |
| Regras `ForbiddenStageReturn` / `MustPassAllStages` | Campos no `Funil` + validação | Não |
| Detalhe de negócio/cliente/contato | — (OData já cobre) | **Sim** |
| Atividades + calendário | Repetição e lembretes (opcional) | Sim, sem repetição |
| Perfis e permissões (UI) | Autorização por perfil (Fase 0 backend) | Não |
| Editor de proposta, totais | Cálculo de totais, PDF | Parcial |
| Aceite público | Endpoint público + registro de aceite | Não |
| Builder de automações | CRUD de automações | Não |
| Fila de processamento | Endpoint de reprocesso | Parcial (só leitura) |
| Notificações, busca global | Endpoints novos | Não |
| Resumo / relatórios | Agregações | Parcial |

---

## 12. Riscos de front

1. **Componentes grandes** (`TabsFiltro` 34 KB, `EditorFormula` 32 KB) concentram lógica. Mexer neles sem testes é arriscado; criar testes antes de refatorar.
2. **Sem biblioteca de UI.** Manter componentes próprios dá controle, mas exige disciplina. Alternativa: adotar uma biblioteca para tabelas/calendário/gráficos só onde o custo de construir é alto [?].
3. **Dependências novas** (arrastar-e-soltar, calendário, editor rico, gráficos): fixar versões exatas e conferir licença e manutenção de cada uma.
4. **Dados de teste:** o front hoje não tem seed de negócios/funis para desenvolver kanban. Criar dados de demonstração sem informações reais de clientes.
5. **Cópia indevida.** Replicar função e comportamento é diferente de copiar templates, textos ou o visual da Ploomes. Se o objetivo for comercial, vale revisão jurídica.

---

## 13. Ordem recomendada

F0 (fundação) → F1 (kanban) → F2 (detalhes e tarefas) → F3 (propostas) → F4 (admin e automações) → F5 (resumo e relatórios) → F6 (periféricos).
F0 e as partes de F2 que não dependem de backend podem andar em paralelo com a Fase 0 do backend (autorização e tenant).
