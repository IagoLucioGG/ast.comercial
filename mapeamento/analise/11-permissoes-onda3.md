# Permissões por perfil (onda 3, agente PERM)

## Como funciona
1. `TenantMiddleware` resolve empresa e usuário. O **perfil vem do banco** (cache de 30 s), não da claim do JWT. Usuário inativo/removido recebe 401 mesmo com token válido.
2. `AutorizacaoMiddleware` (logo depois, registrado em `Program.cs`) converte método + caminho em uma regra (`MapaPermissaoRotas`) e consulta a matriz do perfil (cache de 2 min por empresa+perfil, descartado ao salvar a matriz nesta instância). Sem permissão: **403** `{ "erro": "Sem permissão para <ação> <entidade>.", "status": 403 }`.
3. Escopo (`Proprios`/`Equipe`/`Todos`) de **Negócio** e **Atividade**: filtro global do EF em `AppDbContext` (por `ResponsavelId`), preenchido pelo `TenantMiddleware`. Vale para listagem, `$count`, `/$count`, `$apply`, registro único e escrita (fora do escopo = 404). Serviços de fundo criam o próprio contexto e só têm tenant, então não sofrem o escopo.
4. Equipe = o próprio usuário + quem divide alguma equipe ativa com ele.

## Ações
GET/HEAD = Ver · POST = Criar · PATCH/PUT = Editar · DELETE = Excluir.
Exceções: POST depois da chave (`Negocios(1)/Ganhar`, `Perder`, `Reabrir`, `Atividades(1)/Concluir`, `Propostas(1)/Aprovar|Rejeitar|Compartilhar|Recalcular`, `Funis(1)/Etapas`, `Usuarios(1)/RedefinirSenha|RegenerarToken`, `Negocios(1)/ReordenarItens`…) = **Editar** da entidade. `ModelosDocumento(1)/Renderizar(...)` (GET ou POST) = Ver. Em `api/*`, POST em `/{id}/algo` = Editar e POST terminando em consulta/dados/executar/preview/previa/agregar/query = Ver. Qualquer escrita em dado **filho** (itens, seções, parcelas, membros, opções de campo, etapas, checklists, gráficos…) = **Editar** do pai.

## Mapa rota → entidade do catálogo
| Rota (odata/ ou api/) | Entidade | Observação |
|---|---|---|
| Clientes, PessoasContato, Negocios, Atividades, Propostas, Produtos | própria | |
| ItensNegocio, RespostasChecklistNegocio, api/negocios/{id}/checklist | Negocio | filho |
| ItensProposta, SecoesProposta, ParcelasProposta, Propostas@Produtos/Secoes/Parcelas | Proposta | filho |
| ListasPreco, ItensListaPreco, api/listas-preco, api/produtos/{id}/precos | ListaPreco / Produto | leitura também com Ver Produto/Proposta/Negocio |
| Metas, api/metas | Meta | |
| PaineisRelatorio, GraficosRelatorio, api/relatorios, api/paineis | PainelRelatorio | |
| Importacoes, api/importacoes, api/importacao | Importacao | |
| Etiquetas | Etiqueta | EtiquetasRegistro: leitura Etiqueta; escrita = quem cria/edita algum registro comercial |
| Funis, EtapasFunil, ChecklistsEtapa, ItensChecklistEtapa, api/funis/{id}/checklist-progresso | Funil | **leitura também com Ver Negócio** (kanban) |
| Automacoes (+ filhos), api/automacoes | Automacao | |
| Integracoes, LogsIntegracao | Integracao | |
| Usuarios | Usuario | exceções abaixo |
| Equipes, MembrosEquipe | Equipe | |
| Departamentos, Cargos, Empresas | própria | |
| Campos (+ opções, regras, configurações), Formularios (+ seções, campos) | Campo / Formulario | **leitura aberta** a qualquer autenticado (as telas precisam do esquema); escrita exige a entidade |
| Campos@Valores, ValoresCampo | — | leitura aberta; escrita para quem cria/edita algum registro comercial |
| ModelosDocumento, SecoesModelo | ModeloDocumento | leitura também com Ver Proposta |
| FluxosAprovacao | FluxoAprovacao | `@Solicitacoes` e SolicitacoesAprovacao: **abertas** (o serviço confere o aprovador) |
| StatusNegocios, StatusClientes, MotivosPerda, OrigensContato, RamosAtividade, PortesEmpresa, FamiliasProduto, GruposProduto, Moedas, Localidades, AdminCadastros e as rotas `@` equivalentes (`Clientes@Origens/Status/RamosAtividade/Portes`, `Negocios@Status/MotivosPerda`, `Produtos@Familias/Grupos`) | CadastroAuxiliar | **leitura aberta** (listas de seleção); escrita exige CadastroAuxiliar (AdminCadastros: também leitura) |
| Visualizacoes | — | aberta (preferências do próprio usuário) |
| RegistrosAlteracao | — | leitura para quem tem Ver em alguma entidade comercial |
| api/permissoes | — | catálogo e `minhas`: qualquer usuário; matriz e PUT: só Administrador (o controller confere) |
| api/autenticacao, api/publico, api/aprovacoes | — | livres (login/refresh/proposta pública são anônimos) |
| api/importar-modelo-ploomes, odata/PermissoesPerfil | — | só Administrador |
| Qualquer outra rota de odata/* ou api/* | — | **falha fechada**: 403 "Sem permissão para acessar este recurso." para quem não é Administrador (log de aviso no servidor) |

Rotas fora de `odata/*` e `api/*` (ex.: `integracoes/callback/{chave}`) e endpoints `[AllowAnonymous]` não passam pela matriz. `$metadata` é livre; outras rotas `$` (ex.: `$batch`) caem em "não mapeada".

Exceções em Usuarios: `GET odata/Usuarios?$select=` só com `Id, Nome, Ativo, FotoUrl` (sem `$expand`/`$apply`, sem filtrar/ordenar por e-mail, perfil, tipo ou token) é aberto, porque as telas usam a lista para escolher o responsável. Sem esse `$select` volta 403. `Usuarios(meuId)/AlterarSenha` é livre para o próprio usuário.

## Usuário de integração (X-Api-Key)
Havia um defeito: o `UseAuthentication` não conhece a chave, então o `AuthorizeFilter` global devolvia 401 mesmo com chave válida. Agora o `TenantMiddleware` monta o `ClaimsPrincipal` autenticado da chave. Decisão: integração vale como **Administrador da própria empresa** (acesso total, escopo Todos), como era a intenção original. Chave inválida = 401. Para restringir, troque `db.PerfilAtual = Administrador` no `TenantMiddleware` pelo `Perfil` gravado no usuário.

## API
- `GET api/permissoes/catalogo` → `[{ chave, rotulo, grupo, suportaEscopo }]` (`suportaEscopo` só em Negocio e Atividade, as que o servidor filtra).
- `GET api/permissoes` (Administrador) → `{ permissoes: [{ perfil, entidade, podeVer, podeCriar, podeEditar, podeExcluir, escopo }] }` (grade completa; Administrador sempre total).
- `PUT api/permissoes` (Administrador) corpo `{ permissoes: [...] }` (pode mandar só as linhas alteradas; upsert). 422 se tentar restringir o Administrador; 400 para perfil/entidade/escopo inválidos. Criar/Editar/Excluir ligam Ver automaticamente. Devolve a grade nova.
- `GET api/permissoes/minhas` → `{ usuarioId, perfil, administrador, permissoes: [{ entidade, podeVer, podeCriar, podeEditar, podeExcluir, escopo }] }`.

## Limites conhecidos
- Escopo só é aplicado a Negócio e Atividade. Meta e Relatório têm "Próprios" gravado no seed, mas o filtro dessas entidades é responsabilidade dos agentes METAS/REL.
- Negócio/atividade **sem responsável** ficam invisíveis para Próprios/Equipe.
- Quem tem Criar pode criar registro com outro responsável (fica fora do escopo dele depois).
- Cache em memória por instância: com várias instâncias, a matriz muda em até 2 min nas demais (o contexto do usuário, em 30 s).
- Dados relacionados (valores de campos, histórico, itens de proposta) não herdam o escopo do negócio.

## Rotas da fase B (conferidas pelo agente INT)
Todas as rotas novas já caíam em regras do mapa; a prova HTTP (perfis Administrador, Gerente, Vendedor e Visualizador) confirmou o comportamento da matriz do seed. Regras:

| Rota | Regra |
|---|---|
| `odata/ItensNegocio`, `Negocios({id})/ReordenarItens`, `AtualizarValorPelosItens`, `MarcarItemChecklist`, `Negocios({id})/Checklist` (GET), `ItensNegocio@PrecoSugerido` | Negocio: GET = Ver; escrita (inclusive POST de ação) = Editar |
| `ChecklistsEtapa`, `ItensChecklistEtapa`, `Funis({id})/Etapas(...)`, `ReordenarEtapas` | Funil: escrita = Editar; leitura também com Ver Negócio (kanban) |
| `Funis({id})/ChecklistProgresso` | Funil ou Negócio (Ver) |
| `ListasPreco`, `ItensListaPreco`, `api/listas-preco/{id}/copiar-precos-base` | ListaPreco (leitura também com Ver Produto/Proposta/Negócio); copiar = Editar |
| `api/produtos/{id}/precos` | Produto (Ver) |
| `Metas`, `api/metas/progresso` | Meta |
| `api/relatorios/*` | PainelRelatorio; POST `dados`/`executar`/`preview`/`consulta`… = Ver (só aqui) |
| `api/importacoes/*` | Importacao; **POST `executar` é Criar** (antes era tratado como Ver); o controller ainda confere a entidade de destino |
| `api/fluxos-aprovacao/*`, `FluxosAprovacao`, `@Tipos`, `@Niveis` | FluxoAprovacao |
| `api/aprovacoes/*`, `FluxosAprovacao@Solicitacoes`, `SolicitacoesAprovacao` | livres (o serviço confere o aprovador) |
| `Propostas({id})/SolicitarAprovacao|Aprovar|Rejeitar` | Proposta: Editar |
| `Atividades` com `$expand` | Atividade (Ver); o `$expand` não muda a regra |

Mudança de código: `MapaPermissaoRotas.ResolverApi` só trata POST de consulta (`dados`, `executar`…) como leitura para o recurso de relatórios.
