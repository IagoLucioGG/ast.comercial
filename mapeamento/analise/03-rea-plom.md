# 03 - Análise do repositório `rea-plom` (material de análise da Ploomes)

Análise SOMENTE LEITURA de `c:\SistemProject\NovaPloomes\rea-plom`. Nenhum valor de token, chave, cookie ou dado pessoal foi reproduzido aqui; só estrutura, nomes de campos e endpoints.

Legenda: **[OBS]** = observado diretamente em JSON, script ou md. **[INF]** = inferido, sem prova direta nos arquivos.

---

## 1. O que é o `rea-plom` e o que há de específico da Ploomes

**O projeto [OBS]**
- Fork de `REA` ("Reverse Engineer Anything"), pacote npm `rea-agents` v6.8.0, licença MIT. Não é um CRM.
- É um servidor MCP + CLI de engenharia reversa: binários nativos, código gerenciado, Electron/JS, firmware, páginas web e runtime de navegador (CDP, Frida, adb etc.).
- Remoto: `github.com/RoniFebrone/rea-plom`. O `package.json` ainda aponta para `io.github.morluto/rea` (upstream).
- O histórico tem 1 commit (`first commit`, de Roni Febrone de Almeida). Não dá para fazer diff contra o upstream.

**O que foi adicionado/alterado em relação ao upstream [OBS, por busca]**
- `git grep -il ploomes` encontra a palavra em 2 lugares: a pasta `.rea-demos/` (52 arquivos versionados, tudo Ploomes) e `ploomes-demo-scenario.json` na raiz.
- Fora disso (`src/`, `native/`, `bridge/`, `tests/`, `docs/`, `website/` etc.) NÃO há menção a "ploomes". Logo, o fork não alterou o código do REA por causa da Ploomes. Só acrescentou material de coleta.
- `ploomes-demo-scenario.json` é um cenário de execução do REA: abre `https://www.ploomes.com/` num Chrome local, espera 4 s e captura dom, accessibility, url e screenshot. É a coleta do site público.
- Não consigo afirmar se outros arquivos do REA foram mexidos sem relação com a Ploomes, porque não há upstream para comparar. [limitação]

---

## 2. Campos customizados (CAMPOS-MAPA.md, `field-*`, `fields-*`)

**Contexto da captura [OBS]**
- Tela: `app10.ploomes.com/docgenerator/70000296/fieldconfig`, configuração de campos de um modelo de proposta.
- Entidade: Cliente. Campo de teste criado pelo autor, com nome "Teste R re".
- A captura foi feita via CDP (portas 9222/9223) em Chrome e Opera GX logados numa conta real.

**Modelo do campo (payload do save) [OBS: `field-formula-patch.json`, status 200]**

| Grupo | Propriedades vistas |
|---|---|
| Identidade | `Key` (formato `<entidade>_<GUID>`, ex.: `contact_...`), `Name`, `EntityId` (1 = Cliente), `TypeId` (1 = Texto simples), `Multiple` |
| Flags de UI/regra | `Required`, `Disabled`, `Unique`, `Wide`, `ColumnSize`, `FormHidden`, `FormFilterable`, `UseCheckbox`, `RemoveFieldHeightLimit`, `IsSensitiveData` |
| Descrição e i18n | `FieldDescription`; `Languages[]` com `{LanguageId, Name, FieldDescription}`. Ids 1 a 4 existem; só o 1 tinha nome. |
| Origem/integração | `OriginFieldKey`, `ReceiveExternalKey`, `SendExternalKey`, `FilterId`, `ProductGroupId` |
| Valores padrão | `DefaultStringValue`, `DefaultBigStringValue`, `DefaultIntegerValue`, `DefaultDecimalValue`, `DefaultDateTimeValue`, `DefaultBoolValue` |
| Fórmulas | `GeneratedFormula` (bool), `InternalFormula` (string JS), `FieldHideFormula`, `FieldDisableFormula`, `RequiredFieldFormula`, `ExternalFormulaUrl/Method/Headers/RequestBody/ResponsePaths[]`, `FormulaVariables[]` |
| Permissões (listas de ids) | `AllowedUsers/Teams/UserProfiles`, `ExhibitionAllowed*`, `RequiredRulesAffected*`, `CreateOptionsAllowed*` (esta última com `ProfileId: 1` no exemplo) |

- **Fórmula [OBS]:** fica em `InternalFormula`, um IIFE JavaScript em texto que retorna o valor (no teste, a string `"TESTE"`). O tipo do retorno tem de bater com o tipo do campo (comentário do próprio código de teste). Há 3 outros slots de fórmula (ocultar, desabilitar, obrigatório) e um modo de fórmula externa (URL, método, headers, body, paths de resposta).
- **Assistente de IA [OBS]:** `POST ai.ploomes.com/api/generate_formula` é chamado na tela. O payload não foi capturado.
- **Opções [OBS parcial]:** o save expande `Type` e `OptionsTable($expand=Options)`. Isso mostra que campos de lista usam uma tabela de opções separada. O campo capturado era texto simples, então não há exemplo de opção.
- **Tipos de campo [INF/não verificado]:** só `TypeId=1` foi observado. O `preview-funil.html` lista 8 tipos (texto, multilinha, inteiro, moeda, decimal, opções, data, checkbox), mas a lista parece escrita à mão. O corpo de `GET /Fields@Types` NÃO foi capturado. Tratar os ids 2 a 8 como hipótese.
- **Entidades [OBS parcial]:** `EntityId=1` Cliente. Aparecem também `EntityId` 2 (usado em `Filters`, provavelmente Negócio), 7 (modelo de proposta), 24 e 44 (em `Fields@Entities@LastUpdates`). Os nomes de 24 e 44 não aparecem nos arquivos. `Fields@Entities@Paths` liga uma entidade a destinos (`Entity`, `DestinationEntity`), para campos que navegam de uma entidade a outra [INF].
- **Valores nos negócios [OBS]:** o `Deals(id)` expande `OtherProperties` com `FieldKey`, `StringValue`, `BigStringValue`, `IntegerValue`, `DecimalValue`, `DateTimeValue`, `BoolValue`, `ObjectValueId/Name`, `UserValueId/Name`, `ProductValue...`. Ou seja, o valor de um campo customizado é guardado em colunas tipadas por `FieldKey`. É o modelo EAV que o clone precisa.

**O que o "save probe" revelou sobre gravação [OBS]**
- `PATCH https://api2-s08-app-canary.ploomes.com/Fields('{Key}')?$expand=Type,OptionsTable($expand=Options)`, resposta 200. O recurso é endereçado pela **Key** (string), não pelo id numérico.
- Antes de salvar, a UI valida o nome com `GET /Fields?$expand=Languages&$select=Id&$filter=EntityId eq 1 and Languages/any(o: o/Name eq '<nome>' and o/LanguageId eq 1)`, a cada tecla digitada (nome único por entidade e idioma).
- Carregamento do campo: `GET frontend-api-s8.ploomes.com/api/Fields/GetDynamicById/{id}` (id numérico, usa id de 8 dígitos). As tentativas do autor de ler o campo por outras rotas (`/Fields({id})`, `/Fields?$filter=Id eq`) falharam por CORS ou erro de rede (`Failed to fetch`/`xhr_error`). Não houve resposta válida para analisar.
- Chamada auxiliar do app: `POST /Self/LogPage` (log de navegação, body de 34 bytes).
- Criação (POST) de campo: **não foi capturada**. O contrato do md (`PATCH | POST`, `/Fields`) para criar é suposição [INF].
- O corpo das respostas não foi guardado. Só o corpo do PATCH de request foi.

---

## 3. Funil / Kanban (FUNIL-MAPA.md, `funnel-*`)

**Alvo [OBS]:** `https://app10.ploomes.com/Deals/39689/funnel`, pipeline 39689 ("Contato Venda"), com 5 estágios de ids 176810 a 176814. Os nomes mostrados no `preview-funil.html` são Triagem, Prospect, Demonstração, Proposta, Fechamento. O mapa em md não traz esses nomes, então a correspondência id-nome é [INF].

**Arquitetura [OBS]**
- SPA (app10) com web components: `PIPELINE-POCKET-SELECTOR`, `DEAL-FUNNEL`, `DYNAMIC-FUNNEL`, `FUNNEL-LOADER`, `DEAL-SIDE-MENU`.
- API OData em `api2-s08-app-canary.ploomes.com`, com autenticação por header `User-Key` (ver seção 8). Também há `gateway-api2`, `identity-provider`, `central-api`.
- Tempo real: SignalR em `socket-hub.ploomes.com/messageshub/negotiate`. Nenhum evento WebSocket foi capturado (`wsEvents` = 0).
- Cache local: IndexedDB `Ploomes_<conta>_<usuário>` + `Ploomes_utils`, e localStorage com chaves "ai-builder" (cache de menu).

**Chamadas ao carregar [OBS]**
1. `GET /Self?$expand=OtherProperties,PipelinesSettings,PipelineUserUsability,Teams,Profile,Role,...Template...UserUsability,PipelineSelectorViewMode`: usuário, perfil e preferências.
2. `GET /Account?$expand=City,Plan,Module(UserPermissions),...` e `/Account@Integrations?$filter=IntegrationId eq N`: conta, plano e módulos.
3. `GET /Deals@Pipelines?$count=true&$filter=Archived eq false&$top=0`: conta pipelines ativos. `GET /Deals@Pipelines/UserPreferencesLastUpdateDate` serve de cache-busting.
4. `PATCH /Self(0)` com preferências de visualização do usuário. Campos vistos: `DealsViewMode` ("funnel"), `DealsFilterId`, `LastPipelineId`, `LastPipelineTableId`, `ShowMyDeals`, `ShowOthersDeals`, `ShowOpenDeals` e outros (o corpo está truncado na captura). O funil lembra por usuário a última visualização, pipeline e filtro.
5. `GET /Self@Counters`, `/Self@DealsOrdinations`, `/Filters?$filter=Listable eq true and EntityId eq 2`, `/Approvals`, `/Selectors`, `/Operations`, `/RelativeDates`, `/Fields@Entities`, `/Fields@Entities@LastUpdates`.
6. **Por coluna**: `GET /Deals?$top=15&$skip=0&$filter=(StageId eq <id> and (StatusId eq 1 or StatusId eq 2))&$orderby=TasksOrdination,FirstTaskDate,Id desc&$expand=Tags(Tag),LastQuote,Currency,Contact,Stage,Status,Owner,Pipeline($select=Id,ForbiddenStageReturn,MustPassAllStages,Stages;$expand=Stages)&preload=true`. Uma chamada por estágio, em paralelo.
7. **Totais por coluna**: `GET /Deals?$apply=filter((StatusId eq 1 or StatusId eq 2) and PipelineId eq 39689)/groupby((CurrencyId, Currency/Symbol, StageId, PipelineId), aggregate(Amount with sum as AmountSum, $count as Count))`. O contador e o valor do cabeçalho da coluna vêm daqui, não da lista paginada.
8. **Paginação [OBS]:** `$top=15` e `$skip` em múltiplos de 15 (visto `$skip=15` em dois estágios: scroll infinito por coluna).
9. **Busca [OBS]:** a busca livre injeta `contains(Title,'x') or contains(ContactName,'x')` tanto no filtro de cada coluna quanto no `$apply` dos totais. Os contadores acompanham o filtro.

**Drag and drop [OBS: `funnel-drag-capture.json` + md]**
- Mutação única: `PATCH /Deals({dealId})?$expand=<grande>` com corpo `{"Id":<dealId>,"StageId":<novoEstágio>}` → 200.
- Antes do PATCH: `OPTIONS` (preflight CORS). Depois: reconsulta os totais (`$apply`) das colunas afetadas e `GET /Deals?$filter=OriginDealId eq <id>` (negócios derivados).
- Em volta do arraste, a UI também chamou `GET /Deals({id})/GetApplicableChecklists?$expand=Fields(Conditions,FieldPath,Field,Users),DealFilter(...)` e `GET /Deals({id})?$expand=...OtherProperties...DerivatedDeals...ContactsProducts` ao abrir o negócio (painel lateral).
- Houve 8 respostas de `/Approvals` na janela de 120 s, então a UI consulta aprovações com frequência.
- O `$expand` do PATCH é só para a UI receber o deal atualizado. O corpo mínimo é `{Id, StageId}` [OBS no md; o corpo real do PATCH confirma só esses 2 campos].
- `funnel-drag-live.json` está vazio (`events: []`). A captura boa é a `funnel-drag-capture.json`.

**Estados e regras visíveis [OBS]**
- `StatusId`: o funil lista só 1 e 2. O md interpreta como "aberto", mas a semântica exata de 1 e 2 não está provada [INF]. Ganho/perdido não aparece na lista (filtrado fora).
- Pipeline tem `ForbiddenStageReturn` e `MustPassAllStages` (flags lidas pela UI a cada carregamento). **Os valores não foram capturados** (resposta sem corpo). Pela semântica do nome: proibir voltar a estágio anterior / obrigar passar por todos os estágios [INF].
- Checklists por negócio (`GetApplicableChecklists`) com `Fields`, `Conditions` e `DealFilter`: indício de regras "campos obrigatórios por etapa/condição". Conteúdo não capturado.
- Aprovações (`/Approvals`) ligadas a desconto/autorização (hipótese do md; sem corpo).

**DOM [OBS: `funnel-dom-map.json`, `parse-funnel-dom.mjs`]**
- Cards são `link` acessíveis com nome "card selecionável do Contato de Venda: <título>" e levam a `/deal/<id>`.
- Controles com aria-label: "botão de criar novo Contato de Venda", "mudar visualização", "mudar funil", alternância para "funil", rótulos "Novo Negócio", "Propostas", "Funil de vendas".
- Classes CSS e posições dos cards não foram extraídas; só nomes de componentes e rótulos acessíveis. Não há seletor pronto para automação do drag.

**`preview-funil.html` [OBS]**
- Protótipo estático, de um arquivo só (HTML + CSS + 1 `<script>`), sem backend. Usa dados fictícios: 5 estágios com os ids reais, 6 negócios de exemplo (alguns títulos parecem baseados em registros reais; não reproduzidos), 3 entidades (Cliente=1, Negócio, Proposta=7) e 8 tipos de campo.
- Abas: Funil (board com colunas e cards) e Campos (lista, modal de criação/edição, pré-visualização de fórmula). Salva os campos em `localStorage` (`rea-preview-fields-v1`) e gera `Key` com GUID maiúsculo no formato da Ploomes.
- É uma prova de conceito do front/UX do clone, feita pelo autor. Não vem da Ploomes.

---

## 4. Scripts (`.rea-demos/*.mjs`)

Todos são Node ESM usando `ws` (WebSocket) para falar CDP com um Chrome/Opera GX já aberto com depuração remota. Paths dos scripts de parse estão fixos em `f:/rea/.rea-demos/...` (ajustar para reuso).

| Script | O que faz | Reaproveitável |
|---|---|---|
| `cdp-network-capture.mjs` | Anexa à aba Ploomes (porta 9222) e captura requisições de rede | Sim, ajustar porta/filtro |
| `cdp-recent-network.mjs` | Lê o histórico recente de recursos da aba (Performance), sem precisar estar ouvindo | Sim |
| `cdp-reload.mjs` | Recarrega a aba Ploomes | Sim |
| `cdp-listen-drag.mjs` | Escuta 90 a 120 s (`LISTEN_MS`) e grava requests/responses/mutações enquanto o usuário arrasta cards | Sim, é o método da captura de drag |
| `cdp-scan-recent-mutations.mjs` | Procura mutações (POST/PATCH/PUT/DELETE) recentes na aba do funil/deal | Sim |
| `cdp-listen-fields.mjs` | Igual ao de drag, mas para a tela de campos (`CDP_PORT`, `LISTEN_MS`) | Sim |
| `cdp-scan-fields.mjs` | Varre recursos e storage da tela de campos | Sim |
| `cdp-fetch-field.mjs` | Dispara fetch de um campo (`FIELD_ID`) de dentro da página, tentando rotas alternativas | Parcial (falhou por CORS) |
| `cdp-hunt-field-save.mjs` | Varre storage procurando chave de API (padrão `user-key/token`) e a usa como header `User-Key` em GET. **Sensível**: vasculha credenciais | Só com cautela. Não recomendado |
| `extract-field-patch.mjs` | Extrai de `fields-capture.json` o PATCH em `Fields(...)` e grava `field-formula-patch.json` | Sim |
| `extract-demo.mjs`, `extract-dom-text.mjs` | Extraem artefatos e texto do DOM do `ploomes-public-full.json` (site público) | Sim, só para o saída do REA |
| `parse-network.mjs` | Classifica URLs de `funnel-network-raw.json` em deals/funnel/fields/auth/approval etc. (origem de `funnel-api-map.json`) | Sim |
| `parse-deals-detail.mjs` | Filtra chamadas a Deals/Fields/Approvals/Filters/Selectors em `funnel-network-raw.json` (origem de `funnel-deals-requests.json`) | Sim |
| `parse-funnel-dom.mjs`, `parse-funnel-inspect.mjs` | Reduzem o `funnel-inspect.json` (2,5 MB, inspect do REA) em `funnel-dom-map.json` / `funnel-map.json` | Sim |
| `summarize.mjs`, `summarize2.mjs`, `summarize3.mjs` | Resumem `ploomes-public-full.json` (chaves, steps, eventos) em `ploomes-public-summary.json` | Pouco útil |

Para novas coletas: a base é `cdp-listen-*.mjs` + `parse-network.mjs`. Falta um script que grave o **corpo das respostas** (`Network.getResponseBody`), que é a maior lacuna (ver seção 8).

---

## 5. Site público / marketing

Arquivos: `ploomes-public-full.json` (1,2 MB, saída do REA), `ploomes-public-summary.json` (metadados da coleta), `ploomes-demo-highlights.json`, `ploomes-dom.txt` (início do HTML, só `<head>` e scripts), `ploomes-home.png` (screenshot da home, 616 KB).

Observado em `www.ploomes.com` (título "Ploomes: A maior empresa de CRM da América Latina"), site feito em Webflow:
- **Pitch:** "CRM que centraliza toda a jornada do cliente", gerar propostas em minutos, "+3000 empresas", integra "mais de 100 ferramentas", app mobile, time de sucesso.
- **Produtos/módulos anunciados** (títulos H1 a H3):
  - Estúdio IA Ploomes, Construtor de Telas, Integrador Flow, CPQ em Branco, Radar Ploomes.
  - Gestão da carteira de clientes.
  - Consultas externas e API aberta (`developers.ploomes.com`).
  - Integração com ERP.
  - Automação de processos comerciais.
  - Automação de propostas, pedidos e contratos.
- **Segmentos:** Indústria e comércio B2B, Software e Tecnologia (SaaS), Serviços. Solução "visibilidade gerencial".
- **Planos:** não há tabela de preços. O site diz que os planos são definidos pelos recursos desejados (vendas consultivas).
- Há versão em inglês (`/en`) e login em `app10.ploomes.com/login`.
- Ferramentas de terceiros do site (GA/GTM, Clarity, Meta, LinkedIn, ActiveCampaign, reCAPTCHA, StaPe) não interessam ao clone.
- O `ploomes-dom.txt` está truncado no `<head>`. O conteúdo rico está no `-full.json`. O PNG não foi aberto aqui.

---

## 6. Endpoints Ploomes vistos (sem credenciais)

Host principal da API: `api2-s08-app-canary.ploomes.com` (OData v4; `s08` = shard, `canary` = ambiente; o app é `app10.ploomes.com`). `{id}` = id numérico, `{key}` = Key de campo.

**Negócios / funil**

| Método | Path | Tela/ação |
|---|---|---|
| GET | `/Deals@Pipelines?$count&$filter=Archived eq false&$top=0` | Carregar funil: contagem de pipelines ativos |
| GET | `/Deals@Pipelines/UserPreferencesLastUpdateDate` | Invalidação de cache de preferências |
| GET | `/Deals?$top&$skip&$filter&$orderby&$expand&preload` | Cards de uma coluna (paginado, 15 por vez) |
| GET | `/Deals?$apply=filter(...)/groupby(...aggregate(Amount sum, $count))` | Totais e contadores por coluna |
| GET | `/Deals?$filter=Id eq {id}&$expand=...` e `/Deals({id})?$expand=...` | Abrir negócio |
| GET | `/Deals?$filter=OriginDealId eq {id}` | Negócios derivados, após mover |
| GET | `/Deals({id})/GetApplicableChecklists?$expand=...` | Checklists aplicáveis ao negócio |
| **PATCH** | **`/Deals({id})`** corpo `{Id, StageId}` | **Mover card de estágio** |
| GET | `/Approvals` | Aprovações/descontos (sem corpo capturado) |
| GET | `/Filters?$select&$expand&$filter=Listable eq true and EntityId eq 2` | Filtros salvos de negócios |
| GET | `/Contacts?$expand=...` | Dados do cliente ao abrir negócio |

**Campos**

| Método | Path | Tela/ação |
|---|---|---|
| **PATCH** | **`/Fields('{key}')?$expand=Type,OptionsTable($expand=Options)`** | **Salvar campo e fórmula** |
| GET | `/Fields?$expand=Type,OptionsTable($expand=Options)` | Listar campos |
| GET | `/Fields?$expand=Languages&$select=Id&$filter=EntityId eq 1 and Languages/any(...)` | Validar nome único |
| GET | `/Fields@Entities` (`$expand=QuickForm`), `/Fields@Entities@LastUpdates` | Entidades com campos e última atualização |
| GET | `/Fields@Entities@Paths?$expand=...&$filter=EntityId eq {n}` | Caminhos entre entidades |
| GET | `/Fields@Types` | Tipos de campo (URL vista; corpo não capturado) |
| GET | `frontend-api-s8.ploomes.com/api/Fields/GetDynamicById/{id}` | Carregar campo (outro host) |
| POST | `ai.ploomes.com/api/generate_formula` | Assistente de IA para fórmula |

**Usuário / conta / apoio**

| Método | Path |
|---|---|
| GET | `/Self`, `/Self(0)` com `$expand`; `/Self@Counters`; `/Self@DealsOrdinations` |
| PATCH | `/Self(0)` (preferências de visualização) |
| POST | `/Self/ResetDealsCounter`; `/Self/LogPage` |
| GET | `/Account`, `/Account@Integrations?$filter=IntegrationId eq N` |
| GET | `/Operations`, `/RelativeDates`, `/Selectors` (metadados de filtros: operadores, datas relativas, seletores) |
| GET | `/Tasks@Types`, `/Tasks@EmailReminders`, `/Tasks@RepeatIntervalUnits`, `/Timeline@Entities` |

**Outros hosts [OBS]**
- `gateway-api2.ploomes.com`: `GET /Account`, `GET /Self`.
- `identity-provider.ploomes.com`: `GET /api/v1/Self`, `GET /api/v1/Terms/Last`.
- `central-api.ploomes.com`: `GET /api/FeatureFlags`.
- `exporter-api.ploomes.com`: `GET /api/Exportations/StatusByTypeId/{id}`.
- `importation2.ploomes.com`: `GET /api/Importations/GetByUser`.
- `socket-hub.ploomes.com`: `POST /messageshub/negotiate` (SignalR).
- `mailmapie.ploomes.com`: `PUT /api/userpreferences/{x}` e `/api/userslim/{x}` (e-mail).
- `stgploomescrmprd01.blob.core.windows.net`: avatares e logo da conta (Azure Blob).

Todas as chamadas a `api2` usam `OPTIONS` de preflight (204) e `GET/PATCH` com `Content-Type: application/json`. A autenticação é por header `User-Key` (ver seção 8). A API pública documentada está em `developers.ploomes.com`, onde `Deals`, `Fields` e `Contacts` seguem a mesma convenção OData, e esta é uma boa fonte para cruzar [INF].

---

## 7. Regras de negócio e comportamentos que o clone deve replicar

**Observado**
1. **Mover card:** `PATCH /Deals({id})` com `{Id, StageId}` basta. Depois da mudança, recalcular totais das colunas de origem e destino e reconsultar derivados (`OriginDealId`).
2. **Listagem do funil:** só `StatusId` 1 e 2; uma requisição por coluna; ordenação `TasksOrdination, FirstTaskDate, Id desc` (negócios com tarefas pendentes sobem primeiro); página de 15 com carregamento incremental.
3. **Cabeçalho da coluna:** soma de `Amount` e `Count` agrupados por `CurrencyId` + `StageId`, por consulta de agregação separada (multi-moeda: o total é por moeda, com símbolo). Respeita busca e filtros ativos.
4. **Busca:** por título do negócio ou nome do contato (`contains`).
5. **Card:** tags (com cor), último orçamento (`QuoteNumber`), moeda, contato (id, nome, tipo), estágio, status, responsável (nome, avatar). Link para `/deal/{id}`.
6. **Regras do pipeline:** o pipeline carrega `ForbiddenStageReturn` e `MustPassAllStages`; a UI as usa para validar o arraste.
7. **Preferências por usuário:** última visualização (funil/tabela), último pipeline, filtro, mostrar meus/outros/abertos. Persistidas em `Self(0)`.
8. **Pipelines arquivados:** `Archived eq false` na listagem.
9. **Campos:** `Key` gerada no cliente (`entidade_GUID`); nome único por entidade e idioma (validação em tempo real); 4 idiomas; fórmulas em JS; permissões por usuário/equipe/perfil; valores gravados em colunas tipadas por `FieldKey`.
10. **Cache e atualização:** o front usa `...LastUpdates`/`UserPreferencesLastUpdateDate` para decidir o que recarregar, e IndexedDB local. Realtime por SignalR.

**Não capturado (o clone ainda não pode afirmar)**
- O que o servidor responde (erro) ao mover violando `ForbiddenStageReturn`/`MustPassAllStages`, ou com checklist/campo obrigatório pendente. O arraste capturado foi bem-sucedido (200).
- Campos obrigatórios por estágio, regras de ganho/perdido e seus `StatusId`, aprovações, automações.
- Formato dos corpos de resposta (nenhum foi salvo).

---

## 8. Lacunas e riscos

**Dados sensíveis versionados (ALERTA)**
- `fields-capture.json` contém **3 ocorrências de header `User-Key` com valor real** (chave de API do usuário/conta da Ploomes), nos headers das requisições capturadas em `live`. Os valores NÃO foram reproduzidos aqui. O arquivo está commitado e o remoto é público no GitHub. Recomendo: **revogar/rotacionar essa chave na Ploomes** e remover o arquivo do histórico. Não repassar esse arquivo à mega aplicação sem limpeza.
- Não encontrei JWT, cookies nem `Authorization` nos demais arquivos (busca por padrões, sem leitura integral dos 2,5 MB do `funnel-inspect.json`). Os JSONs do REA (`funnel-inspect/observe`) declaram que cookies e corpos não são retidos.
- Dados reais de terceiros presentes (não reproduzidos):
  - Nomes de pessoas e empresas em títulos de cards: `funnel-dom-map.json`, `funnel-map.json`, `funnel-screenshot.json` (87 KB), dados de exemplo do `preview-funil.html`.
  - Ids de contas, usuários e negócios reais: nomes de banco IndexedDB e chaves de storage.
  - URLs com avatares de usuários do Azure Blob.
  - 6 e-mails em `ploomes-public-full.json` (provavelmente do site de marketing; não verifiquei se são de pessoas).
  - O título da aba revela o nome da empresa dona do tenant.
- `cdp-hunt-field-save.mjs` varre storage por chaves de API e as usa em chamadas. Sinal de que o autor tentou extrair credenciais de sessão do navegador.

**Mutações em conta real**
- O PATCH de campo (criação/edição com fórmula, status 200) e o PATCH de mover card (200) foram executados **numa conta real de produção de outro cliente** (tenant com plano e usuários reais; ambiente `canary`), por meio de um navegador logado. Não houve sandbox. Isso tem implicação contratual/ética: a conta e o uso não são do projeto.
- Não há evidência de limpeza (se o campo "Teste R re" e a posição do card foram revertidos).

**Lacunas técnicas**
- **Corpos de resposta não capturados.** Há URLs, query e alguns bodies de request, mas nenhum JSON de resposta (Pipelines, Stages com flags, Deals, Fields, Types, Approvals). Sem eles o esquema de dados é inferido de `$expand`/`$select` e dos nomes das propriedades.
- Valores de `ForbiddenStageReturn`, `MustPassAllStages`, significado de `StatusId` 1 a 5 e de `TypeId` 2+: não observados.
- Criação de campo (POST), exclusão, opções de lista, tipos, automações, aprovações, produtos, propostas: sem captura.
- Apenas um pipeline, 5 estágios, nenhum erro de validação.
- `funnel-drag-live.json` vazio; `funnel-bundle-summary.json` e `funnel-inspect-net.json` são mensagens de erro (CLI `analyze-web-bundle` com argumento errado; `target_changed`). A análise dos bundles JS do front (que revelaria as regras de validação do cliente) **não aconteceu**.
- Scripts com paths fixos (`f:/rea/...`) e portas diferentes (9222/9223); `ploomes-home.png` não foi aberto nesta análise.
- A API é a interna do app (`api2-s08-app-canary`, `frontend-api-s8`), não necessariamente a pública. Pode mudar sem aviso e exige `User-Key` do usuário logado.
- Um clone deve ser cruzado com a documentação pública (`developers.ploomes.com`) antes de assumir contratos.

**Prioridade sugerida:** (1) tratar a `User-Key` vazada; (2) recapturar com corpo de resposta em conta de teste própria; (3) cruzar com os demais mapeamentos.
