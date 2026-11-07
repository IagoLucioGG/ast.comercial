# Inventário da aplicação base: AST.Comercial

Repositório: `c:\SistemProject\NovaPloomes\ast.comercial` (somente leitura, nada foi alterado).
Legenda de status: **(a)** implementado e funcional, **(b)** esqueleto/stub/TODO, **(c)** apenas declarado (interface/DTO/enum/entidade sem uso).
Onde não li o código com profundidade está marcado como "não verificado". Nenhum segredo foi copiado (só nomes de chaves).

Resultados de build:
- `dotnet build`: **compila, 0 erros**, 3 avisos (pacote `Microsoft.OpenApi 2.0.0` com vulnerabilidade alta conhecida NU1903; parâmetro `logger` não usado em `ExecutorFluxoSaida`). Saída enviada para pasta temporária, repositório intacto.
- `npm run build`: **não executado**. `node_modules` não existe e instalar dependências alteraria o repositório. Compilação do front: não verificado.

---

## 1. Stack e arquitetura

### Backend
| Item | Valor (visto no código) |
|---|---|
| Runtime | .NET `net10.0` (o README diz .NET 8, está desatualizado) |
| Web | ASP.NET Core Web API + **ASP.NET Core OData 9.1.1** (todos os controllers herdam `ODataController`, exceto Autenticação e ImportarModeloPloomes) |
| ORM | **EF Core 9.0.6 + Npgsql (PostgreSQL) 9.0.4**. Sem Dapper |
| Auth | JwtBearer 9.0.6, BCrypt.Net-Next 4.0.3 |
| Docs | `Microsoft.AspNetCore.OpenApi` (`/openapi`, anônimo) e `wwwroot/index.html` (página estática de documentação) |
| Libs ausentes | Sem MediatR, AutoMapper, FluentValidation, Serilog, Hangfire/Quartz, Jint (JS), e-mail (SMTP) |

### Camadas (pasta única, projeto único `AST.Comercial.csproj`, "Clean Architecture simplificada")
- `Domain/Entidades/*` (~60 entidades/enums, todas no namespace `AST.Comercial.Domain.Entidades`).
- `Application/Dtos` (5 DTOs: Atividade, Cliente, Funil, Negocio, PessoaContato), `Interfaces` (25), `Servicos` (22 implementações).
- `Infrastructure/Data` (AppDbContext, 38 configurações Fluent, migrations, seeds), `Infrastructure/Automacoes`, `Infrastructure/Integracoes`, `Infrastructure/Middleware`.
- `Controllers` (27 arquivos).
- Padrão: serviço por agregado, controller fino, `IQueryable` exposto pelo serviço para o OData (`[EnableQuery]`) e `Delta<T>` para POST/PATCH. DI com `AddScoped` manual em `Program.cs`.

### Program.cs (resumo)
- DbContext Npgsql com `ConnectionStrings:DefaultConnection`; JWT validando emissor/audiência/chave (`Jwt:Emissor`, `Jwt:Audiencia`, `Jwt:Chave`).
- **Autorização global**: `AuthorizeFilter` exigindo usuário autenticado em todos os controllers (exceções: `[AllowAnonymous]` em login, refresh, callback de webhook, `/openapi`, `/odata` raiz).
- CORS: `AllowAnyOrigin/AnyHeader/AnyMethod` (o comentário diz "restritivo", mas não é).
- Kestrel: sem header Server, body máx. 10 MB.
- Pipeline: SegurancaHeaders → TratamentoErros → Sanitizacao → Authentication → RateLimit → Authorization → Tenant → Controllers.
- EDM OData: ~55 `EntitySet` registrados em `odata/`.
- **Em Development**: `Database.MigrateAsync()`, `ALTER TABLE`/`CREATE TABLE IF NOT EXISTS` ad-hoc (Campo.Nativo, ModeloDocumento, SecaoModelo), seed de moedas/localidade/empresa/admin e seed de campos nativos e formulários padrão por empresa. Em produção nada disso roda.
- 3 `HostedService`: `ProcessadorAutomacaoService`, `AgendadorAutomacaoService`, `PollingIntegracaoService`.

### Frontend
Vue **3.5** + TypeScript 6 + Vite 8 + **Pinia 3** + **vue-router 5**; ícones `@mdi/font`; `monaco-editor` (editor de fórmulas). **Sem biblioteca de componentes UI** (CSS próprio com variáveis). Proxy do Vite para `localhost:5080` (`/api` e `/odata`). Detalhes na seção 7.

---

## 2. Banco e persistência

- Provider: **PostgreSQL** (Npgsql). `AppDbContext` único, ~62 `DbSet`.
- Convenções do contexto: nome da tabela = nome da classe (singular); **todos os enums gravados como string**; `SaveChangesAsync` sobrescrito para (1) setar `AtualizadoEm` e (2) **gerar auditoria automática** (`RegistroAlteracao`, com `DadosAntes`/`DadosDepois` em JSON, agrupando por entidade raiz).
- **Migrations existem**: 8 migrations (2026-07-01 a 2026-07-05): `InicializacaoBancoDeDados`, `Ajustes`, `AdicionarVisualizacaoSalvaEFotoUsuario`, `AddVisualizacaoSalvaFotoUrl`, `AdicionarPermiteMultiplosValoresCampo`, `AlterarFormulaCampoParaText`, `AdicionarNativoFormulasCampo`, `AdicionarModeloDocumento`. Há remendos em SQL cru no `Program.cs`, o que sugere migrations que ficaram fora de sincronia (`PendingModelChangesWarning` é ignorado).
- `EntidadeBase`: `Id`, `EmpresaId`, `CriadoEm`, `AtualizadoEm`, `Ativo`, `ChaveExterna` (id do sistema externo, citando Bimer). Exclusão é **lógica** (`Ativo=false`) e o filtro global esconde inativos.

### Entidades do Domain (campos principais)
| Grupo | Entidades | Campos principais / relações |
|---|---|---|
| Identidade | Empresa, Usuario, TokenAcesso, Equipe, MembroEquipe, Departamento, Cargo | Empresa: nome, razão social, CNPJ, contato, endereço, logo. Usuario: nome, e-mail, senha hash, Tipo (Normal/Integracao), Perfil (Administrador/Gerente/Vendedor/Visualizador), `TokenIntegracao` (API key), foto, Cargo, Departamento. TokenAcesso: JWT, refresh, expiração, revogado, origem. |
| CRM | Cliente, PessoaContato, Negocio, Funil, EtapaFunil, Atividade, StatusNegocio(+Config), StatusCliente, MotivoPerda, OrigemContato, RamoAtividade, PorteEmpresa, Etiqueta, EtiquetaRegistro, Localidade, Moeda | Cliente: nome, razão, e-mail, telefone, documento, endereço, status, origem, ramo, porte. Negocio: título, valor, previsão, fechamento, Cliente, PessoaContato, Funil, Etapa, Status, MotivoPerda, Moeda. Atividade: título, tipo, vencimento, concluída, vinculada a Cliente/Contato/Negócio. |
| Produtos/Propostas | Produto, FamiliaProduto, GrupoProduto, Proposta, ItemProposta, SecaoProposta, ParcelaProposta | Produto: nome, código, preço, custo, unidade, categoria, família, grupo, moeda. Proposta: número/revisão, valor, desconto, frete, parcelas, status de aprovação, capa/header/footer em HTML, link compartilhável (`Chave`), aceite externo, responsável, template. |
| Campos personalizados | Campo, OpcaoCampo, RegraCampo, ValorCampo, ConfiguracaoCampo, Formulario, SecaoFormulario, CampoFormulario | Campo por EntidadeAlvo, tipo, obrigatório/visível/somente leitura/nativo, **fórmulas (JS em texto)** de valor, visibilidade, obrigatoriedade e somente leitura, máscara, limites. Valores em `ValorCampo` (EAV). Formulários com seções e campos. |
| Automação | Automacao, FiltroAutomacao, AcaoAutomacao, ExecucaoAutomacao, FilaAutomacao | ver seção 5 |
| Aprovação | FluxoAprovacao, FiltroFluxoAprovacao, NivelAprovacao, AprovadorNivel, SolicitacaoAprovacao | ver seção 4 |
| Integração | Integracao, MapeamentoCampoIntegracao, FluxoIntegracao, EtapaFluxoIntegracao, LogIntegracao | ver seção 6 |
| Documentos | ModeloDocumento, SecaoModelo | modelo com seções HTML por entidade alvo |
| Outros | RegistroAlteracao (auditoria), VisualizacaoSalva (abas/colunas/filtros salvos da grid), Pais/Estado/Cidade (**sem DbSet** no contexto; não verificado se ficaram sem uso) | |

Seeds: moedas (BRL/USD/EUR), Brasil, empresa "AST Comercial", usuário administrador padrão (credencial padrão de dev fixa no código `SeedDados.cs`, risco de segurança se rodar fora de dev), status de negócio (Aberto/Ganho/Perdido) e demais dados padrão, campos nativos por entidade (`SeedCamposNativos`), formulários padrão, campos Ploomes de produto.

---

## 3. Autenticação, autorização e multi-tenant

- **Login (a)**: `POST /api/Autenticacao/login` (e-mail + senha, BCrypt, só usuários `Tipo=Normal` e ativos). JWT HS256, validade 60 min, claims `sub`, `empresa_id`, `nome`, `email`, `perfil`, `tipo`. Refresh token de 7 dias, rotacionado (o antigo é revogado), persistido em `TokenAcesso`. `POST /refresh` e `POST /logout` (revoga todos os tokens do usuário).
- **Usuário de integração (a)**: header `X-Api-Key` resolvido no `TenantMiddleware` contra `Usuario.TokenIntegracao` (`Tipo=Integracao`). **Atenção:** o middleware roda *depois* de `UseAuthorization` e o `AuthorizeFilter` global exige usuário autenticado por JWT, então a API key sozinha provavelmente **não passa** nos controllers (só define o tenant). Não testei em execução: *não verificado*, mas o código sugere esse problema.
- **Multi-tenant (a)**: coluna `EmpresaId` em toda `EntidadeBase` e **filtro global do EF** (`EmpresaId == EmpresaIdAtual && Ativo`). `EmpresaIdAtual` é preenchido pelo `TenantMiddleware` a partir da claim `empresa_id`. É tenant por coluna (banco único).
- **Perfis/permissões (c)**: o enum `PerfilUsuario` existe e vai na claim, mas **nenhum controller/serviço usa `[Authorize(Roles=...)]`, políticas ou checagem de perfil**. Qualquer usuário autenticado acessa/altera tudo da sua empresa (inclusive `Usuarios`, `Integracoes`, `Empresas`). Não há conceito de permissão granular, visibilidade por equipe/dono do registro, nem "papéis". No front, os cards "Papéis de usuários", "Perfis de usuários" e "Permissões do sistema" existem no menu admin mas sem componente (mostram "em construção").
- Rate limit por usuário/API key em memória (600/min usuário, 1200 integração, 60 anônimo; **desligado em Development**). Sanitização de input por blacklist de strings (XSS/SQLi "caseiro"), headers de segurança, tratamento global de erros. Todos em `Infrastructure/Middleware` (a).
- Segredos: `appsettings.json` traz connection string e chave JWT em texto (nomes: `ConnectionStrings:DefaultConnection`, `Jwt:Chave`, `Jwt:Emissor`, `Jwt:Audiencia`). Valores não reproduzidos aqui.

---

## 4. API: controllers, endpoints e serviços

Convenção: todos sob `odata/` (OData v4, `[EnableQuery]` nos GET de coleção: `$select/$filter/$orderby/$expand/$count`, `MaxTop=100`). Sub-recursos usam o padrão não-OData `odata/Entidade@SubRecurso` (ex.: `odata/Negocios@Status`). Ações: `POST odata/Entidade(key)/Acao`.
Todo CRUD abaixo é "GET coleção, GET (key), POST (Delta), PATCH (key, Delta), DELETE (key)" salvo quando indicado. DELETE é exclusão lógica.

### Controllers e serviços

| Controller (rota base) | Endpoints além do CRUD | Serviço | O que o serviço realmente faz |
|---|---|---|---|
| **Autenticacao** `api/Autenticacao` | `POST login`, `POST refresh`, `POST logout` | AutenticacaoServico (120 linhas) | **(a)** Login/refresh/logout reais, JWT, BCrypt. |
| **Clientes** | `@Origens`, `@StatusClientes/@Status`, `@RamosAtividade`, `@Portes` (CRUD cada) | ClienteServico (229 l) | **(a)** CRUD real com DTO; publica gatilhos de automação Criado/Editado/Removido; CRUD dos cadastros auxiliares. |
| **PessoasContato** | só CRUD | PessoaContatoServico (101 l) | **(a)** CRUD real + gatilhos Criado/Editado/Removido. |
| **Negocios** | `Ganhar`, `Perder`, `Reabrir`; `@Status`, `@MotivosPerda` (CRUD); `@Funis`, `@Etapas` (GET) | NegocioServico (220 l) | **(a)** CRUD + ganhar/perder/reabrir (muda status, publica gatilho `StatusMudou`); ao ganhar dispara evento de saída de integração `NegocioGanho`. Gatilho `EtapaMudou` **não é publicado** em lugar nenhum. |
| **Funis** | `Funis(key)/Etapas` GET/POST | FunilServico (146 l) | **(a)** CRUD de funil e etapas, gatilhos. Etapa só criação; edição/remoção de etapa não vi endpoint dedicado. |
| **Atividades** | `Concluir`; `@Tipos` | AtividadeServico (164 l) | **(a)** CRUD + concluir (gatilho `Concluido`). |
| **Produtos** | `@Familias`, `@Grupos` (CRUD), `@Categorias` | ProdutoServico (103 l) | **(a)** CRUD simples. |
| **Propostas** | `Aprovar`, `Rejeitar`; `@Produtos`, `@Secoes`, `@Parcelas` (CRUD); `@StatusAprovacao` | PropostaServico (167 l) | **(a)** CRUD de proposta/itens/seções/parcelas; aprovar/rejeitar muda `StatusAprovacao` e publica gatilho. Sem cálculo de totais, sem geração de PDF, sem envio, sem link público de aceite (campos existem na entidade: **(c)**). |
| **Campos** | `@Opcoes`, `@Regras`, `@Configuracoes`, `@Valores` (CRUD); `@Entidades`, `@Tipos`, `@Esquema({entidade})` | CampoServico (164 l) | **(a)** Campos personalizados e valores EAV, geração de chave, esquema de variáveis para o editor de fórmulas (reflection em `ContextoFormula`). **As fórmulas são só armazenadas**; não encontrei motor JS no backend (sem Jint). Avaliação provável no front: *não verificado*. |
| **Formularios / SecoesFormulario / CamposFormulario** | CRUD de seções e campos | FormularioServico (100 l) | **(a)** Layout de formulários dinâmicos por entidade. |
| **ModelosDocumento / SecoesModelo** | `ModelosDocumento(id)/Renderizar(registroId)` | ModeloDocumentoServico (138 l) | **(a)** CRUD de modelos e seções HTML; `Renderizar` substitui tags `<field key="$proposta.Campo">` por valores via reflection para Proposta/Cliente/Empresa e devolve HTML. Sem PDF, sem laço de itens, sem formatação de moeda. |
| **ImportarModeloPloomes** `api/importar-modelo-ploomes` | `POST` | (direto no DbContext) | **(a, pontual)** Lê os 4 HTMLs da pasta `HTML ploomes`, converte tags `<field>` da Ploomes, cria campos que não existem e monta o modelo "Proposta Comercial 3.0". Ferramenta de migração de template. |
| **FluxosAprovacao** | `@Niveis` GET; `@Solicitacoes` GET; `Solicitacoes(key)/Aprovar` e `/Rejeitar`; `@Tipos` | FluxoAprovacaoServico (82 l) | **Parcial.** Cadastro de fluxo e avanço de nível/aprovação/rejeição funcionam, mas **nenhum código cria `SolicitacaoAprovacao`** (nenhum `new SolicitacaoAprovacao`) e não valida se quem aprova é aprovador do nível. Ou seja, o fluxo nunca é acionado automaticamente. Classificação: **(b)**. |
| **Automacoes** | `@Fila` GET, `@Logs` GET, `@Gatilhos`, `@Acoes` | AutomacaoServico (21 l) | **Somente leitura.** Controller **não tem POST/PATCH/DELETE** (só GETs); a criação de automações não é possível pela API (pelo menos neste controller). Ver seção 5. |
| **Integracoes** | `@Mapeamentos`, `@Fluxos`, `@Etapas` (CRUD); `@Logs`; `POST integracoes/callback/{chave}` (anônimo) | IntegracaoServico (147 l) | **(a)** Ver seção 6. |
| **Usuarios** | `RegenerarToken`, `AlterarSenha`; `@Perfis`, `@Tipos` | UsuarioServico (87 l) | **(a)** CRUD, hash de senha, token de integração. |
| **Equipes** | `@Membros` (GET/POST/DELETE) | EquipeServico (37 l) | **(a, simples)** CRUD equipe e membros. Equipe não influencia permissão. |
| **Etiquetas** | só CRUD | EtiquetaServico (43 l) | **(a)** CRUD; `EtiquetaRegistro` (vínculo) é usado só pelo motor de automação. Não vi endpoint para aplicar etiqueta manualmente. |
| **Empresas** | GET coleção/GET key | EmpresaServico (15 l) | **Só leitura**, sem PATCH. A tela "Dados da empresa" não edita. |
| **Departamentos, Cargos** | GET coleção/GET key | DepartamentoServico/CargoServico (15 l) | **Só leitura** (sem criar/editar). |
| **Moedas, Localidades** | GET coleção | MoedaServico/LocalidadeServico (12 l) | **Só leitura** (dados do seed). |
| **Visualizacoes** | CRUD | (direto no DbContext) | **(a)** Abas/colunas/filtros salvos da grid. |

Observações da API:
- Não há endpoints de relatório/dashboard, busca global, importação/exportação genérica (CSV/Excel), upload de arquivos, e-mail, calendário, nem envio/aceite de proposta.
- Não há paginação padronizada além do OData; sem validação (FluentValidation/DataAnnotations não verificado) e sem unidade de trabalho/transação explícita.
- `ClienteDto`, `NegocioDto`, `AtividadeDto`, `FunilDto`, `PessoaContatoDto` são retornados em POST/PATCH; os demais devolvem a entidade direto.

---

## 5. Motor de automação e filas

Arquitetura (código em `Infrastructure/Automacoes`):

1. **Publicação (a)**: `FilaAutomacaoPublicador.PublicarEventoAsync(evento)` é chamado pelos serviços (Cliente, PessoaContato, Negocio, Funil, Atividade, Proposta) após `SaveChanges`. Busca automações ativas da empresa com mesma `EntidadeAlvo` + `Gatilho`, respeita `ImpedirCascata`, insere linhas em `FilaAutomacao` e executa `pg_notify('fila_automacao')`.
2. **Processador (a, com ressalvas)**: `ProcessadorAutomacaoService` (BackgroundService) usa `LISTEN/NOTIFY` do PostgreSQL com fallback de polling a cada 5 s, lote de 50 com `FOR UPDATE SKIP LOCKED`, status `Pendente → EmExecucao → Concluido/Falha/FalhaDefinitiva`, até 3 tentativas com backoff quadrático (10 s × n²).
3. **Agendador (b)**: `AgendadorAutomacaoService` verifica a cada minuto automações `Periodicamente` com `ProximaExecucao <= agora` e enfileira. **Cron não implementado** (TODO: "implementar parser de cron completo (Cronos)"); só repete diariamente no `HorarioExecucao` ou a cada 24 h.
4. **Motor (a/b)**: `MotorAutomacao.ProcessarItemFilaAsync` carrega automação + filtros + ações, avalia filtros (grupos: AND dentro do grupo, OR entre grupos; operadores: igual, diferente, contém, não contém, maior, menor, ≥, ≤, vazio, não vazio, começa/termina com) sobre o JSON `DadosEvento`, executa ações em ordem e grava `ExecucaoAutomacao` (status, duração, erro).

### Gatilhos (enum `TipoGatilho`, 14)
Criado, Editado, CampoAlterado, Removido, EtapaMudou, StatusMudou, Concluido, Vencido, PropostaCriada, PropostaAprovada, PropostaEnviada, PropostaExpirada, EtiquetaAdicionada, Periodicamente.
- **Efetivamente publicados no código**: Criado, Editado, Removido (Cliente, Contato, Negócio, Funil, Atividade), StatusMudou (Negócio ganhar/perder/reabrir e Proposta), Concluido (Atividade), PropostaAprovada. Os demais (CampoAlterado, EtapaMudou, Vencido, PropostaCriada, PropostaEnviada, PropostaExpirada, EtiquetaAdicionada) são **(c) declarados sem emissor**; Periodicamente é (b).

### Ações (enum `TipoAcao`, 16)
| Ação | Estado |
|---|---|
| EditarDados (por reflection, propriedades simples) | (a) |
| CriarTarefa (Atividade com prazo em dias) | (a) |
| AlterarStatus (Negócio) | (a) |
| MoverEtapa (Negócio) | (a) |
| ConcluirTarefa | (a) |
| AdicionarEtiqueta / RemoverEtiqueta | (a) |
| CriarRegistroInteracao (grava em RegistroAlteracao) | (a) |
| ExecutarWebhook (HTTP configurável) | (a), cria `HttpClient` por chamada, sem retry específico, sem bloqueio de SSRF |
| EnviarEmail | **(b) TODO**: só loga "integrar serviço SMTP/SES" |
| NotificarUsuario | **(b) TODO**: só loga |
| CriarContato, CriarProposta, AlterarResponsavel, CriarNegocio, GerarDocumento | **(c)** no enum, caem no `default` ("ação não implementada") |

`FormulaCondicao` (condição JS) e `GatilhoCampoReferencia` existem na entidade mas **não são usados** pelo motor (c).

### Riscos evidentes do motor
- **Provável bug de multi-tenant nos serviços de fundo**: `ProcessadorAutomacaoService`, `AgendadorAutomacaoService` (este usa `IgnoreQueryFilters`) e `PollingIntegracaoService` criam escopo próprio e **nunca setam `AppDbContext.EmpresaIdAtual`** (fica 0). No Processador/Motor, `FilaAutomacao` não herda `EntidadeBase` (sem filtro), mas `Include(Automacao)` e as buscas de `Negocios/Atividades/...` passam pelo filtro global com `EmpresaId == 0`, o que tende a não retornar nada: a execução pode terminar "silenciosamente OK" sem executar ações. **Não executei para confirmar** (leitura apenas); tratar como risco alto a validar.
- Itens em `Falha` (com `ProximaTentativa`) nunca são reprocessados, pois a query seleciona só `Status = 'Pendente'`; o backoff programado não tem efeito.
- Como o motor faz `throw` após gravar a execução, o `finally` salva o log, mas ações já executadas não são revertidas (sem transação).
- Não há API para criar/editar automações (somente GET). O front lista automações em tabela somente leitura.

---

## 6. Integrações

### ODataBimer
- Pasta `ODataBimer/` contém apenas **`metadata.xml` (~165 KB)**: o `$metadata` OData v4 do ERP **Bimer Everest** (namespace `Bimer.Everest.EF.SqlServer.Entities.Geral`). É material de referência do modelo do ERP, **não é código executável nem é referenciado no código** (nenhum uso de "Bimer" além de comentários).
- 23 `EntitySet` (listados): Agendamentos, Configuracoes, Empresas, Pessoas, InformacoesGeraisComplementos, InformacoesGeraisMeiosContato, InformacoesGeraisMotivosEncerramento, InformacoesGeraisMotivosExclusao, Bairros, Cidades, UnidadesFederativas, TitulosAPagar, TitulosAReceber, MovimentosCobranca, ContasCorrentes, FamiliasProduto, GruposProduto, Produtos, Atendimentos, ContatosVenda, Fases, PessoasLicencasCnpjAdicionais, TiposAtendimento.
- Serve de base para mapear Cliente↔Pessoas, Produto/Família/Grupo, Atendimentos/Fases etc. Por isso `EntidadeBase.ChaveExterna` existe.

### Motor genérico de integração (código, não específico do Bimer)
- **Cadastro (a)**: `Integracao` (provedor, URL base, tipo de API Rest/OData/GraphQL/Soap, autenticação Nenhuma/Bearer/Basic/ApiKey/OAuth2 client credentials, headers fixos, `ChaveWebhook`, `WebhookSecret`), `MapeamentoCampoIntegracao` (campo interno ↔ externo, direção, transformação, padrão), `FluxoIntegracao` (entrada/saída; modo Webhook/Polling/Manual; duplicidade Criar/Atualizar/CriarOuAtualizar/Ignorar), `EtapaFluxoIntegracao` (fluxos de saída com etapas Consultar/Enviar, condição, contexto), `LogIntegracao`.
- **Entrada por webhook (a)**: `POST /integracoes/callback/{chave}` (anônimo). Acha a integração pela chave na URL, filtra fluxos pelo campo identificador do payload, mapeia e cria/atualiza o registro (`SalvarEntidadeAsync`, por entidade). **`WebhookSecret` existe só em entidade/configuração e não é validado** em nenhum código: a única proteção é a chave na URL.
- **Entrada por polling (a/risco)**: `PollingIntegracaoService` roda por minuto, chama o endpoint externo, navega no JSON (campo lista), mapeia e grava. Mesmo problema de `EmpresaIdAtual = 0` das automações (consulta de `FluxosIntegracao` passa pelo filtro global), então pode não encontrar fluxos. Não verificado em execução. Paginação (`CampoPaginacaoResponse`, `TamanhoPagina`) declarada, uso não verificado.
- **Saída (a)**: `IntegracaoEventoPublicador.PublicarEventoSaidaAsync` + `ExecutorFluxoSaida` (monta body via `ProcessadorMapeamento`, HTTP com a autenticação configurada). **Só um evento emitido no código: `NegocioGanho`**. Outros eventos não têm emissor.
- Credenciais da integração (token, senha, client secret) ficam **em texto** nas colunas da tabela `Integracao`, e `ODataConventionModelBuilder` expõe a entidade inteira em `odata/Integracoes` (qualquer usuário autenticado lê os segredos).
- **E-mail, ERP específico, WhatsApp, calendário, assinatura eletrônica, etc.: ausentes.** Nenhum conector pronto: o usuário configura manualmente (estilo Postman).
- Usuários de integração (API key): ver seção 3.

---

## 7. Front-end (AST.Comercial.Web)

### Tema/layout
- Tema **dark por padrão** (preto `#000`, superfícies `#111/#1a1a1a`, texto `#fafafa`), alternância light/dark persistida (`stores/tema.ts`, atributo `data-theme`). Acento ciano `#06b6d4` (dark) / `#0891b2` (light), perigo vermelho. Fonte do sistema. Sem Tailwind, Vuetify ou PrimeVue: CSS escopado em cada componente + variáveis em `App.vue`.
- Layout: `AppLayout` = sidebar recolhível (`AppSidebar`: Resumo, Clientes, Negócios, Propostas, Produtos, Relatórios, Administração) + `AppHeader` + `RouterView`. Logo e emblema AST em `src/assets`.
- Auth no front: store Pinia `auth` + `localStorage` (token, refresh, dados do usuário); guarda de rota por existência de token (sem checar perfil); `services/api.ts` com wrapper OData (`odataGet/GetOne/Post/Patch/Delete`) e **refresh automático em 401**. Não há outras stores (somente `auth` e `tema`).

### Rotas e telas
| Rota / tela | O que faz | Ligação |
|---|---|---|
| `/login` LoginView | Login por e-mail/senha | **API real** (`/api/Autenticacao/login`) |
| `/` HomeView ("Resumo") | Texto "Dashboard - em construção" | **Stub** |
| `/relatorios` | Reusa HomeView (mesmo stub) | **Stub** |
| `/clientes` ClientesView | Grade OData de clientes (busca, filtros/abas salvas, colunas) + formulário dinâmico para criar/editar | **API real** |
| `/negocios` NegociosView | Só grade de negócios (título, valor, status). **Sem kanban/funil visual, sem detalhe, sem ganhar/perder na UI** | API real (somente listagem; novo/editar pelo form interno genérico, *não verificado*) |
| `/propostas` PropostasView | Só grade de propostas | API real (somente listagem) |
| `/produtos` ProdutosView | Grade de produtos | API real |
| `/admin` AdminView | Hub de cards por categoria com painel deslizante | misto |
| `/admin/modelo-documento/:id` EditorModeloView | Editor de seções HTML do modelo de documento | API real |

Painéis do hub Admin (`AdminPainel` mapeia slug → componente):
| Card | Componente | Estado |
|---|---|---|
| Campos e formulários | `CamposFormularios` (25 KB) + `FormulariosAdmin` + `EditorFormula` (Monaco, 32 KB) | **API real**, a tela mais completa do front (CRUD de campos, opções, regras, fórmulas, layout de formulário) |
| Equipes e colaboradores | `EquipesAdmin` (grade) | API real, só listagem/form genérico; **gestão de membros na UI: não verificado** |
| Dados da empresa | `DadosEmpresa` | API real, **somente leitura** |
| Automações | `AutomacoesAdmin` | API real, **somente listagem** (sem criar/editar) |
| Funis | `FunisAdmin` | API real, **somente listagem** (sem editar etapas) |
| Tabela de usuários | `TabelaUsuarios` | API real (grade genérica) |
| Papéis / Perfis / Permissões | (sem componente) | **Placeholder "em construção"** |
| Registro de atividades | `RegistroAtividades` | API real (lê `RegistrosAlteracao`) |
| Modelos de documentos | `ModelosDocumentoAdmin` + editor | **API real** |
| Integrações e aplicativos | `IntegracoesAdmin` | API real, **somente listagem** (sem criar/mapear fluxos na UI) |
| Usuários de integração | `UsuariosIntegracao` | API real |
| Importar da Ploomes | `ImportadorPloomes` | API real (chama `POST /api/importar-modelo-ploomes` e afins) |
| (Etiquetas) | `EtiquetasAdmin` existe e está registrado, mas **não há card/slug no menu** | Sem acesso pela UI |

### Componentes compartilhados principais
`TabelaEntidade` (grade OData genérica: busca, paginação, colunas, abas), `TabsFiltro` (abas de filtro salvas, 35 KB), `ModalFiltro`, `ModalColunas`, `FormularioDinamico` (monta formulário a partir de Campos/Formulários da empresa), `FormModal`, `EditorFormula` (Monaco com autocomplete do esquema de `Campos@Esquema`).

### Pontos ausentes no front (versus um CRM completo)
Dashboard, relatórios, kanban de negócios, detalhe de negócio/cliente (timeline, atividades, anexos), agenda/tarefas, editor/visualização de proposta com itens e envio, notificações, busca global, i18n, testes. Nada é mock: o que existe fala com a API real; o que falta é simplesmente "em construção".

---

## 8. Pasta `HTML ploomes`

Conteúdo: 4 arquivos HTML (`Capa.html` ~3 KB, `Secao1.html` ~4 KB, `secao2.html` ~100 KB, `secao3.html` ~1 KB).
- São **exportações do HTML de um modelo de proposta da Ploomes** ("Proposta Comercial 3.0"): páginas A4 posicionadas com `position:absolute`, fonte Roboto, imagens hospedadas no blob storage da Ploomes e **tags proprietárias** `<field key="quote_<GUID>">[Proposta.Campo]</field>` para campos dinâmicos e atributos `condition-field-key / condition-operation / condition-value` para blocos condicionais. O texto da capa menciona uma empresa cliente real (Alterdata), ou seja, é conteúdo de um cliente: tratar como material de referência, não redistribuir.
- Uso no código: lidos por `ImportarModeloPloomesController` para converter para o formato do AST e criar o modelo de documento. Também serve de referência para entender o modelo de documento da Ploomes (campos de Proposta, produtos, parcelas, condicionais).

## 9. `wwwroot`, `img`, outros

- `wwwroot/index.html` (5,6 KB): página estática "AST.Comercial - API" (tema escuro roxo/azul) que documenta a API/endpoints OData; servida por `UseStaticFiles`. O front real é o projeto Vite em `AST.Comercial.Web` (não é servido pelo .NET; sem build copiado para wwwroot).
- `img/`: identidade visual do produto (logos AST.Comercial CRM "Minimal Logo" em versões fundo claro/escuro, emblema, nome, variantes sem fundo e originais). Dois conjuntos de arquivos duplicados, sem relação com código.
- `AST.Comercial.http`: arquivo de requisições do template (aponta para `/weatherforecast`, inexistente): lixo do template.
- `.vscode/settings.json` quase vazio.

---

## 10. Testes, CI, README, qualidade e dívidas

- **Testes: nenhum** (nem projeto de testes .NET, nem Vitest/Jest/Cypress no front).
- **CI/CD: nenhum** (sem `.github`, pipelines, Dockerfile ou compose). Sem containerização.
- **README**: backend desatualizado (diz .NET 8, "apenas o backend está sendo implementado", API listada como OData), README do front é o template padrão do Vite.
- Git: 4 commits; sem branches verificadas.
- Observabilidade: logging padrão do ASP.NET, sem Serilog, métricas, health check, correlação.
- Qualidade geral: código consistente e legível, nomes em português, padrão de serviço repetitivo. Alguns arquivos grandes no front (TabsFiltro 35 KB, EditorFormula 33 KB, CamposFormularios 25 KB, FormulariosAdmin 25 KB) e componentes admin com CSS/JS compactados em uma linha.

### Dívidas técnicas e riscos (priorizados)
1. **Segredos no repositório**: connection string e chave JWT em `appsettings.json` e tokens/credenciais de integração em texto no banco; seed com usuário admin de senha fixa e fácil.
2. **Sem autorização por perfil/permissão**: qualquer usuário autenticado altera usuários, integrações e dados da empresa; sem visibilidade por equipe/responsável.
3. **CORS aberto** (AnyOrigin) apesar do comentário "restritivo".
4. **Entidade `Integracao` exposta integralmente via OData**, incluindo token/senha/secret; `Usuario` exposto (ocultar `SenhaHash`/`TokenIntegracao` não verificado nos controllers).
5. **Serviços de fundo sem tenant** (`EmpresaIdAtual = 0`): automações e polling provavelmente inoperantes em multi-tenant; retry de `Falha` não funciona.
6. **`WebhookSecret` não validado**; callback anônimo sem assinatura/IP allowlist; payload sem limite além dos 10 MB globais.
7. **Pipeline**: `TenantMiddleware` depois de `UseAuthorization` (API key provavelmente não autentica).
8. **Automação/aprovação incompletas**: sem API de criação de automação, sem e-mail/notificação, ações enum sem implementação, cron ausente, solicitações de aprovação nunca criadas.
9. **Migrations frágeis**: SQL manual em `Program.cs` e aviso de modelo pendente ignorado; migrate só em Development.
10. **Sanitização por blacklist** de strings no middleware (falsos positivos/negativos, não substitui validação por DTO).
11. **Dependência vulnerável**: `Microsoft.OpenApi 2.0.0` (NU1903).
12. **Edição de coleções ricas só via OData `Delta<T>`**: expõe todas as propriedades da entidade para PATCH (mass assignment, ex.: `EmpresaId`, `Ativo`); `EmpresaId` é forçado só na criação.
13. Sem testes, sem CI, sem Docker.
14. Front: sem checagem de perfil nas rotas, token em `localStorage`.

---

## 11. Resumo final: módulo × backend × frontend

| Módulo | Backend | Frontend | Observações |
|---|---|---|---|
| Autenticação (login/refresh/logout, JWT) | Implementado | Implementado (login, refresh automático) | Sem 2FA, sem recuperação de senha |
| Multi-tenant (empresa) | Implementado (filtro global por `EmpresaId`) | Implícito | Bug provável em serviços de fundo |
| Perfis/permissões/papéis | Declarado (enum + claim, sem enforcement) | Ausente (cards placeholder) | Lacuna crítica |
| Empresas | Parcial (somente leitura) | Parcial (somente leitura) | Sem edição |
| Usuários | Implementado | Implementado (grade + form genérico) | Alterar senha/regenerar token só via API |
| Equipes/Departamentos/Cargos | Equipes implementado; Dep./Cargos somente leitura | Equipes grade; demais ausentes | Equipe não afeta permissões |
| Clientes (+status/origem/ramo/porte) | Implementado | Implementado (grade + form dinâmico) | Cadastros auxiliares sem tela |
| Contatos (PessoaContato) | Implementado | Ausente (sem view própria) | |
| Negócios | Implementado (ganhar/perder/reabrir) | Parcial (só grade) | Sem kanban, sem detalhe |
| Funis e etapas | Implementado (etapa: só criação) | Parcial (listagem) | |
| Atividades/tarefas | Implementado | Ausente (sem view) | |
| Produtos (+família/grupo) | Implementado | Implementado (grade) | Famílias/grupos sem tela |
| Propostas (itens/seções/parcelas) | Implementado (CRUD + aprovar/rejeitar) | Parcial (só grade) | Sem totais, PDF, envio, aceite externo |
| Modelos de documento | Implementado (render HTML) | Implementado (editor de seções) | Sem PDF; import do modelo Ploomes |
| Campos personalizados + fórmulas | Implementado (armazenamento, esquema) | Implementado (editor Monaco, formulário dinâmico) | Execução de fórmula no backend inexistente |
| Formulários dinâmicos | Implementado | Implementado | Diferencial em relação ao resto |
| Visualizações salvas (abas/colunas/filtros) | Implementado | Implementado | |
| Etiquetas | Implementado (CRUD) | Componente órfão (sem menu) | |
| Moedas/Localidades | Somente leitura (seed) | Ausente | |
| Fluxo de aprovação | Parcial (b) | Ausente | Solicitação nunca é criada |
| Automações (motor) | Parcial: fila/LISTEN-NOTIFY/12 ações ok, e-mail e notificação TODO, cron ausente | Somente listagem | API sem criação; bug de tenant |
| Integrações (webhook/polling/saída) | Parcial: motor genérico completo; um evento de saída | Somente listagem | Segredos expostos; sem conectores prontos |
| ODataBimer | Ausente como código (só `metadata.xml`) | Ausente | Referência do ERP Bimer |
| Auditoria (Registro de alterações) | Implementado (automático no `SaveChanges`) | Implementado (tela) | |
| Dashboard/Relatórios | Ausente | Stub ("em construção") | |
| E-mail, notificações, calendário, WhatsApp | Ausente | Ausente | |
| Importar modelo Ploomes | Implementado (pontual) | Implementado (tela) | Depende da pasta `HTML ploomes` |
| Testes / CI / Docker | Ausente | Ausente | |

## 12. O que não foi verificado
- Execução real (nenhum servidor/banco foi iniciado): comportamento dos serviços de fundo, API key, fluxos de webhook/polling e importador.
- `npm run build` do front (sem `node_modules`).
- Conteúdo detalhado de `CamposFormularios.vue`, `FormulariosAdmin.vue`, `TabsFiltro.vue`, `EditorFormula.vue` e do corpo de `ProcessadorMapeamento`/`SalvarEntidadeAsync` (apenas estrutura e chamadas).
- Se as fórmulas JS são avaliadas no front (procurei só no backend).
