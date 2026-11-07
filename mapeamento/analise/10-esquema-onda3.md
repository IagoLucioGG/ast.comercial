# Esquema da onda 3 (fonte de verdade da fase B)

Migration única: `20261107140750_Onda3PermissoesItensChecklistsMetasRelatoriosImportacao` (aplicada no banco `ast_comercial`).
Código: entidades em `Domain/Entidades/Onda3/Onda3.cs`; configurações em `Infrastructure/Data/Configurations/Onda3Configuracao.cs`; catálogo em `Domain/Autorizacao/CatalogoEntidades.cs`; seed em `Infrastructure/Data/SeedPermissoes.cs` (chamado no loop por empresa de `Program.cs`, só em Development, idempotente: insere apenas pares Perfil×Entidade que faltam).

Regras gerais: todas herdam `EntidadeBase` (Id, EmpresaId, Ativo, CriadoEm, AtualizadoEm, ChaveExterna), filtro global de tenant/Ativo, enums gravados como string, tabela = nome da classe. **Nenhum controller foi criado**: os EntitySets estão no EDM (`$metadata` os lista), mas `GET odata/<Set>` devolve 404 até a fase B criar o controller de cada um.

## Entidades

| Classe (tabela) | EntitySet OData | DbSet |
|---|---|---|
| PermissaoPerfil | PermissoesPerfil | PermissoesPerfil |
| ItemNegocio | ItensNegocio | ItensNegocio |
| ListaPreco | ListasPreco | ListasPreco |
| ItemListaPreco | ItensListaPreco | ItensListaPreco |
| ChecklistEtapa | ChecklistsEtapa | ChecklistsEtapa |
| ItemChecklistEtapa | ItensChecklistEtapa | ItensChecklistEtapa |
| RespostaChecklistNegocio | RespostasChecklistNegocio | RespostasChecklistNegocio |
| Meta | Metas | Metas |
| PainelRelatorio | PaineisRelatorio | PaineisRelatorio |
| GraficoRelatorio | GraficosRelatorio | GraficosRelatorio |
| Importacao | Importacoes | Importacoes |

### PermissaoPerfil
`Perfil` PerfilUsuario (Administrador, Gerente, Vendedor, Visualizador) · `Entidade` string(60), chave do catálogo · `PodeVer/PodeCriar/PodeEditar/PodeExcluir` bool · `Escopo` EscopoVisibilidade (padrão Todos).
Índice único (EmpresaId, Perfil, Entidade). Atenção: o índice inclui registros inativos; para "remover" uma permissão, edite os booleanos.

### ItemNegocio
`NegocioId` long (FK Negocio, Cascade; nav `Negocio.Itens`) · `ProdutoId` long? (FK Produto, Restrict) · `Descricao` string(300) obrigatório · `Quantidade` decimal(18,4) padrão 1 · `PrecoUnitario` decimal(18,2) · `Desconto` decimal(5,2) em % · `Total` decimal(18,2) · `Ordem` int.
Índices: (NegocioId, Ordem), ProdutoId. O servidor deve calcular `Total`; não há coluna calculada.

### ListaPreco
`Nome` string(200) · `Descricao` string(1000)? · `MoedaId` long? (FK Moeda, Restrict; nav `Moeda`) · `Padrao` bool · `VigenciaInicio` DateTime? · `VigenciaFim` DateTime? (UTC). Nav `Itens`. Índice (EmpresaId, Nome). "Só uma padrão" não é imposto pelo banco.

### ItemListaPreco
`ListaPrecoId` (FK, Cascade) · `ProdutoId` (FK Produto, Restrict; nav `Produto.ItensListaPreco`) · `Preco` decimal(18,2). Índice único (ListaPrecoId, ProdutoId) (vale também para inativos).

### ChecklistEtapa
`EtapaFunilId` (FK EtapaFunil, Cascade; nav `EtapaFunil.Checklists`) · `Nome` string(200) · `Ordem` int. Nav `Itens`. Índice (EtapaFunilId, Ordem).

### ItemChecklistEtapa
`ChecklistEtapaId` (FK, Cascade) · `Texto` string(500) · `Obrigatorio` bool · `Ordem` int. Índice (ChecklistEtapaId, Ordem).

### RespostaChecklistNegocio
`NegocioId` (FK Negocio, Cascade; nav `Negocio.RespostasChecklist`) · `ItemChecklistEtapaId` (FK, Restrict) · `Concluido` bool · `ConcluidoPorUsuarioId` long? (FK Usuario, Restrict; nav `ConcluidoPorUsuario`) · `ConcluidoEm` DateTime?. Índice único (NegocioId, ItemChecklistEtapaId).

### Meta
`Nome` string(200) · `Metrica` MetricaMeta · `Alvo` decimal(18,2) · `MoedaId?` · `UsuarioId?` · `EquipeId?` · `FunilId?` (todas FK Restrict, navs `Moeda`, `Usuario`, `Equipe`, `Funil`) · `Inicio` DateTime · `Fim` DateTime (UTC). Índices (EmpresaId, Inicio, Fim), UsuarioId, EquipeId.

### PainelRelatorio
`Nome` string(200) · `Descricao` string(1000)? · `Compartilhado` bool · `CriadoPorUsuarioId` long? (FK Usuario, Restrict; nav `CriadoPorUsuario`) · `Ordem` int. Nav `Graficos`. Índice (EmpresaId, Ordem).

### GraficoRelatorio
`PainelRelatorioId` (FK, Cascade) · `Titulo` string(200) · `Tipo` TipoGrafico · `Entidade` string(60) (chave do catálogo) · `Agregacao` AgregacaoGrafico · `CampoValor` string(100)? · `CampoAgrupamento` string(100)? · `FiltroJson` text? · `PeriodoCampo` string(100)? · `PeriodoRelativo` string(30)? · `Largura` int (default 2; CHECK 1..4, `CK_GraficoRelatorio_Largura`) · `Ordem` int. Índice (PainelRelatorioId, Ordem).

### Importacao
`Entidade` string(60) · `NomeArquivo` string(300) · `Status` StatusImportacao · `TotalLinhas`, `Sucesso`, `Falhas` int · `MapeamentoJson` text (padrão `{}`) · `ErrosJson` text (padrão `[]`) · `UsuarioId` long? (FK Usuario, Restrict) · `ConcluidaEm` DateTime?. Índice (EmpresaId, CriadoEm).

## Enums (gravados como string)
- `EscopoVisibilidade`: Todos, Equipe, Proprios
- `MetricaMeta`: ValorGanho, QuantidadeGanhos, NegociosCriados, AtividadesConcluidas, PropostasAprovadas
- `TipoGrafico`: Numero, Barras, Linhas, Pizza, Tabela
- `AgregacaoGrafico`: Contagem, Soma, Media
- `StatusImportacao`: Pendente, Processando, Concluida, ConcluidaComErros, Falhou

## Aprovação (o que já existia e o que foi acrescentado)
Já existiam e foram mantidos: `FluxoAprovacao` (Nome, `EntidadeAlvo`, Tipo, Mensagem, `Ordem`, índice EmpresaId+EntidadeAlvo+Ordem; filtros, níveis, aprovadores) e `SolicitacaoAprovacao` (`FluxoAprovacaoId`, `RegistroId` + `EntidadeAlvo` (use `Proposta` = 11), `NivelAtual`, `Status` Pendente/AprovadoNivel/Aprovado/Rejeitado, `AprovadoPorId` (= quem decidiu, aprovando ou rejeitando), `AprovadoEm`, `RejeitadoEm`, `Observacao`). `Proposta` já tem `StatusAprovacao` (Pendente, Aprovada, Rejeitada, EmRevisao) e `AprovadorId`.
Acrescentado em `SolicitacaoAprovacao` (nullable): `SolicitadoPorId` long? (FK Usuario, SetNull; nav `SolicitadoPor`) e `ComentarioDecisao` string(1000)?. Convenção: `Observacao` = nota de quem solicitou; `ComentarioDecisao` = comentário de quem decidiu; data da decisão = `AprovadoEm` ou `RejeitadoEm`; solicitação = `CriadoEm`. `FluxoAprovacao` não precisou de mudança. Observação: `SolicitacoesAprovacao` está no EDM mas não há controller; `NiveisAprovacao`/`AprovadoresNivel`/`FiltrosFluxoAprovacao` não estão no EDM (a fase B registra se precisar, só inserção em `Program.cs`).

## Alterações em tabelas existentes
Somente colunas nullable em `SolicitacaoAprovacao` (`ComentarioDecisao`, `SolicitadoPorId`). Navegações novas (sem coluna): `Negocio.Itens`, `Negocio.RespostasChecklist`, `Produto.ItensListaPreco`, `EtapaFunil.Checklists`. A API antiga continua compatível.

## Catálogo de entidades (`CatalogoEntidades.Todas`, chave → rótulo, grupo)
Comercial: Cliente (Clientes), PessoaContato (Contatos), Negocio (Negócios), Atividade (Atividades), Proposta (Propostas), Produto (Produtos), ListaPreco (Listas de preço), Meta (Metas), PainelRelatorio (Relatórios), Importacao (Importações), Etiqueta (Etiquetas).
Administração: Funil (Funis), Automacao (Automações), Integracao (Integrações), Usuario (Usuários), Equipe (Equipes), Departamento (Departamentos), Cargo (Cargos), Campo (Campos personalizados), Formulario (Formulários), ModeloDocumento (Modelos de documento), FluxoAprovacao (Fluxos de aprovação), Empresa (Empresa), CadastroAuxiliar (Cadastros auxiliares: status, motivos, origens, ramos, portes, famílias, grupos, moedas, localidades).
Constantes: `CatalogoEntidades.Cliente` etc.; `CatalogoEntidades.Existe(chave)`.

## Matriz de permissões padrão (seed; V=ver, C=criar, E=editar, X=excluir)
| Perfil | Regra |
|---|---|
| Administrador | V C E X, escopo Todos, em todas as 24 entidades |
| Gerente | Grupo Comercial (11): V C E X, Todos. Grupo Administração (13): só V, Todos |
| Vendedor | Cliente, PessoaContato, Proposta: V C E, Todos. Negocio, Atividade: V C E, **Proprios**. Produto, ListaPreco, Etiqueta: só V, Todos. Meta, PainelRelatorio: só V, **Proprios**. Importacao e todas de Administração: sem acesso |
| Visualizador | Comercial exceto Importacao: só V, Todos. Resto: sem acesso |

Ninguém exceto Administrador/Gerente exclui. (Decisões minhas fora do pedido literal: ListaPreco e Etiqueta entram como comerciais; Vendedor vê Meta/Relatório próprios, ListaPreco e Etiqueta; Visualizador não vê Importacao.) Contagem gravada no banco (1 empresa): 24 linhas por perfil; ver = 24/24/10/10; criar = 24/11/5/0; excluir = 24/11/0/0 (Admin/Gerente/Vendedor/Visualizador).
