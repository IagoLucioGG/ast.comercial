# Aprovações de propostas (onda 3, agente APROV)

Sem migration: usa `FluxoAprovacao`, `FiltroFluxoAprovacao`, `NivelAprovacao`, `AprovadorNivel` e `SolicitacaoAprovacao` (ver 10-esquema-onda3.md). Código: `Application/Servicos/FluxoAprovacaoServico.cs` (cadastro + motor), `PropostaServico.cs` (ganchos), `Controllers/FluxosAprovacaoController.cs`.

## Regras do motor
- Fluxos ativos (`EntidadeAlvo = Proposta`) são avaliados por `Ordem`, depois `Id`; **o primeiro que se enquadra vale**. Filtros do mesmo `Grupo` = E; grupos diferentes = OU; sem filtros = toda proposta.
- `Tipo`: `Alerta` nunca gera solicitação; `Bloqueio` e `ControleAlcada` geram e precisam de ao menos 1 nível com aprovador.
- Quando avalia: ao **criar** a proposta; ao **editar** `Valor, Desconto, CustoFrete, MoedaId, ClienteId, ResponsavelId, NumeroParcelas` (só se o valor mudou); ao criar/remover item ou mudar produto, quantidade, preço ou desconto de um item (`Propostas@Produtos`); no `Recalcular` se o valor mudou; e no pedido explícito `POST odata/Propostas(id)/SolicitarAprovacao`.
- Enquadrou: cria `SolicitacaoAprovacao` (nível 1, `Pendente`, `SolicitadoPorId` = usuário) e a proposta fica `StatusAprovacao = Pendente`.
- Solicitação em andamento e a proposta é editada: sem nenhuma decisão ainda, a mesma segue; com decisão feita, é cancelada e nasce outra no nível 1 (a trilha recomeça). Se deixou de se enquadrar, é cancelada e o `Pendente` é limpo (`StatusAprovacao = null`).
- Proposta `Aprovada` (ou `Rejeitada`) que é editada e se enquadra de novo: nova solicitação no nível 1 (reabre a avaliação). `Rejeitada` por fluxo que deixou de se enquadrar volta a `null` (libera o compartilhamento).
- **Cancelada** = solicitação em andamento com `Ativo = false` (não há valor de enum para isso). Desativar um fluxo cancela as solicitações em andamento dele.
- `PATCH` não muda `StatusAprovacao`/`AprovadorId` quando há fluxo em jogo (solicitação em andamento ou fluxo aplicável); só `Aprovar`, `Rejeitar` e `SolicitarAprovacao` mudam.
- Aprovador de um nível: `AprovadorNivel.UsuarioId = eu` **ou** `Perfil = meu perfil` (o modelo existente só tem usuário ou perfil; não há equipe). **Administrador** pode decidir qualquer nível e fica marcado `foraDaAlcada` na trilha. Não aprovador comum: **403** `{ erro, status }`.
- Aprovar avança de nível até o último (`AprovadoNivel` → `Aprovado`; proposta `Aprovada`, evento `PropostaAprovada`). Rejeitar encerra (`Rejeitado`; proposta `Rejeitada`, evento `StatusMudou`) e exige comentário (**422** sem ele).
- Trilha por nível: cada decisão grava um `RegistroAlteracao` com `Acao = "AprovacaoDecisao"` e `DadosDepois = { solicitacaoId, nivel, decisao, comentario, foraDaAlcada }` (a solicitação guarda só a última decisão).
- **Compartilhar** (mudança de contrato): `POST odata/Propostas(id)/Compartilhar` responde **422** se `StatusAprovacao = Pendente`, ou `Rejeitada` sem ser recusa do cliente (`StatusResposta <> Recusada`). Sem status (nenhum fluxo) ou `Aprovada`, segue como antes.
- Sem fluxo aplicável: `Propostas(id)/Aprovar` e `Rejeitar` mantêm o comportamento antigo (grava o status direto). Com fluxo aplicável e nenhuma solicitação aberta, o servidor cria a solicitação e aplica a decisão do usuário ao nível 1.

## Campos de filtro (`GET api/fluxos-aprovacao/campos`)
`DescontoPercentual` (desconto total dos itens + desconto da proposta, em % do bruto), `DescontoMaximoItem` (%), `Valor`, `CustoFrete`, `NumeroParcelas` (numéricos: Igual, Diferente, Maior, Menor, MaiorOuIgual, MenorOuIgual, Vazio, NaoVazio); `ResponsavelId`, `ClienteId`, `MoedaId`, `ProdutoId` (qualquer item) com Igual/Diferente/Vazio/NaoVazio; valor = id ou ids separados por vírgula. `Valor` não converte moedas: combine com `MoedaId`.

## Rotas (JSON camelCase; erros `{ erro, status }`)
| Rota | O que faz |
|---|---|
| `GET/POST api/fluxos-aprovacao`, `GET/PUT/DELETE api/fluxos-aprovacao/{id}`, `GET api/fluxos-aprovacao/campos` | CRUD. Corpo: `{ nome, tipo, mensagem, ordem, ativo, filtros:[{grupo,campoReferencia,operador,valor}], niveis:[{descricao, aprovadores:[{usuarioId}\|{perfil}]}] }`. Os níveis são numerados 1..n pela ordem do array; PUT substitui filtros e níveis. DELETE apaga de vez só fluxo sem histórico (senão 422: desative). Lista inclui inativos. |
| `POST odata/Propostas(id)/SolicitarAprovacao` `{ observacao? }` | 200 com a solicitação; 422 se nenhum fluxo se aplica, já está pendente ou já aprovada. |
| `POST odata/Propostas(id)/Aprovar` / `Rejeitar` `{ comentario? }` | 204; 403 não aprovador; 422 rejeição sem comentário. |
| `POST api/aprovacoes/{solicitacaoId}/aprovar` / `rejeitar` `{ comentario? }` | Igual, devolve a solicitação. O OData `FluxosAprovacao@Solicitacoes({id})/Aprovar` e `Rejeitar` também passam pela checagem. |
| `GET api/aprovacoes/minhas` | Em andamento em que sou aprovador do nível atual. |
| `GET api/aprovacoes/solicitadas` | As que eu pedi (qualquer situação). |
| `GET api/aprovacoes/historico?entidade=Proposta&id=N` | Todas as solicitações da proposta (mais recente primeiro), com `passos` (trilha) e `podeDecidir`. Sem `id`: concluídas e canceladas (Administrador vê todas; os demais as que pediram ou decidiram). |

Solicitação: `{ id, fluxoId, fluxoNome, registroId, status, situacao (EmAndamento|Aprovada|Rejeitada|Cancelada), nivelAtual, totalNiveis, solicitadoPorNome, solicitadoEm, observacao, decididoPorNome, decididoEm, comentarioDecisao, podeDecidir, proposta{numero,titulo,clienteNome,valor,moedaSimbolo,moedaCodigo,descontoPercentual,statusAprovacao}, passos:[{nivel,descricao,aprovadores[],estado,decididoPorNome,decididoEm,comentario,foraDaAlcada}] }`.

## Permissões
`api/aprovacoes/*` é livre para autenticados (o serviço confere o aprovador); `api/fluxos-aprovacao` segue a entidade `FluxoAprovacao`; as ações de proposta seguem `Proposta` (editar).

## Limites conhecidos
- Aceite/recusa pelo link público continua gravando `StatusAprovacao` (Aprovada/Rejeitada); por isso a recusa do cliente não bloqueia novo compartilhamento.
- Não há notificação por e-mail; o aprovador vê a caixa em `/aprovacoes`.
- Remover um nível de um fluxo não aprova pendências automaticamente (a Ploomes faz); solicitação com `nivelAtual` além do último nível conclui no próximo "aprovar".
