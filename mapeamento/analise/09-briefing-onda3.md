# Briefing da onda 3 (vale junto com o 06-briefing-agentes.md)

Leia ANTES, inteiros: `mapeamento/analise/06-briefing-agentes.md` (regras, convenções, proibições e o ADENDO com os valores reais do backend), `08-lacunas-e-onda3.md` (o que a onda entrega) e, para a fase B, `10-esquema-onda3.md` (tabelas criadas pela fase A). Todos os caminhos de arquivo abaixo são relativos a `c:\SistemProject\NovaPloomes\ast.comercial`.

## 1. Ambiente

- Backend principal: `http://localhost:5080` (não encerre nem reinicie). Front com HMR: `http://localhost:5173`. Banco PostgreSQL no container Docker `postgres-local` (não pare o container).
- Repositório git na branch `feature/frontend-recursos-ploomes`. **Proibido qualquer comando git** (o lead faz commit e push).
- Login de teste: admin do seed (`Infrastructure/Data/SeedDados.cs`, leia em tempo de execução; nunca grave a senha). Fixtures: negócio `176`, cliente `120`, proposta `34` (não aceite nem recuse a 34), 45 negócios / 33 atividades / 25 clientes / 12 produtos / 8 propostas `[Demo]`. Há 3 vendedores `[Demo]` (perfil Vendedor).
- Dados que você criar para teste: prefixo `[teste-<seu-nome>]` e exclusão lógica ao final.

## 2. Backend: como testar sem colidir

- Compile com `dotnet build --no-restore -o "$env:TEMP\astb-<seu-nome>"` (o `bin/` está em uso). Se falhar por arquivo travado em `obj/` (outros agentes compilam ao mesmo tempo), espere 10 s e tente de novo.
- Suba **uma** instância temporária do seu build na **sua porta** (tabela da seção 4), com `$env:ASPNETCORE_ENVIRONMENT='Development'; $env:Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command='Warning'` e `dotnet <dir>\AST.Comercial.dll --urls http://localhost:<porta>`. Encerre **só** o processo que você iniciou, logo após testar (as instâncias rodam os serviços de fundo contra o mesmo banco).
- **Migrations: só o agente ESQ (fase A) cria.** Na fase B ninguém cria migration nem altera entidade/configuração existente. Se faltar coluna, descreva no relatório.
- Arquivos compartilhados (`Program.cs`, `views/AdminView.vue`, `components/admin/AdminPainel.vue`): releia imediatamente antes de editar e faça **só inserções pontuais** com `str_replace` (registro de DI, card, item do mapa). Nunca reescreva o arquivo.
- Novas rotas de API: OData quando for CRUD simples de entidade (registre o `EntitySet` só se a fase A não tiver registrado); `api/<recurso>` para operações compostas. Toda rota respeita o tenant (`EmpresaId`) e devolve erros como `{ "erro", "status" }` (422 regra de negócio).

## 3. Front

- Use a base pronta: `components/ui/*`, `useToast`, `services/api.ts` (`odataRequest`, `odataQuery`, `odataTodos`, `odataAction`, `montarQuery`, `ApiError`). `$top` máximo 100. Respostas em PascalCase; rotas `@` devolvem array puro.
- Rotas já registradas pelo lead (não edite `router/index.ts` nem `AppSidebar.vue`, exceto o agente PERM): `/produtos/:id`, `/relatorios`, `/relatorios/:id`, `/metas`, `/aprovacoes`. As views existem como esqueleto.
- Dinheiro sempre com o símbolo da moeda do registro (`odata/Moedas`). Iniciais de avatar pela primeira LETRA, ignorando prefixo entre colchetes.
- Sem dependências novas. Gráficos em SVG/CSS próprios. Sem `v-html` com dado da API.
- Validação: `npx vue-tsc --noEmit -p tsconfig.app.json` com **zero erros no projeto** (está limpo; se aparecer erro em arquivo de outro agente, só relate) e GET 200 no dev server para cada arquivo seu.

## 4. Donos de arquivo (fase B)

| Agente | Porta | Escopo exclusivo |
|---|---|---|
| **PERM** | 5095 | Backend: `Infrastructure/Autorizacao/**` (novo), `Controllers/PermissoesController.cs` (novo), serviço/interface de permissões (novos), `Infrastructure/Data/AppDbContext.cs` (somente filtros de visibilidade), `Infrastructure/Middleware/TenantMiddleware.cs`, registro em `Program.cs`. Front: `composables/usePermissoes.ts`, `stores/auth.ts`, `router/index.ts` (guardas), `components/AppSidebar.vue`, `components/admin/permissoes/**`, `components/admin/PerfisEmBreve.vue` (substituir) |
| **NEG** | 5096 | Backend: `Controllers/{Negocios,Funis}Controller.cs`, `Application/Servicos/{Negocio,Funil}Servico.cs` e interfaces, controllers/serviços novos de itens do negócio e checklists. Front: `views/NegocioDetalheView.vue`, `components/negocio/**`, `components/funil/**`, `components/admin/FunisAdmin.vue`, `components/admin/funis/**` (novo) |
| **PROD** | 5097 | Backend: `Controllers/ProdutosController.cs`, `Application/Servicos/ProdutoServico.cs` e interface, controllers/serviços novos de listas de preço. Front: `views/ProdutosView.vue`, `views/ProdutoDetalheView.vue`, `components/produtos/**` |
| **REL** | 5098 | Backend: controllers/serviços novos de painéis, gráficos e agregação. Front: `views/RelatoriosView.vue`, `components/relatorios/**` |
| **METAS** | 5099 | Backend: controllers/serviços novos de metas. Front: `views/MetasView.vue`, `components/metas/**` |
| **IMP** | 5100 | Backend: controllers/serviços novos de importação. Front: `components/admin/importacao/**` (novo) e um card em `AdminView.vue`/`AdminPainel.vue` |
| **APROV** | 5101 | Backend: `Controllers/FluxosAprovacaoController.cs`, `Application/Servicos/{FluxoAprovacao,Proposta}Servico.cs` e interfaces, `Controllers/PropostasController.cs`. Front: `views/AprovacoesView.vue`, `views/PropostaDetalheView.vue`, `components/aprovacoes/**`, `components/propostas/**`, `components/admin/aprovacoes/**` (novo) e um card no admin |
| **POLIR** | 5102 | Backend: `Controllers/AtividadesController.cs`, `Application/Servicos/AtividadeServico.cs` e interface. Front: `components/shared/TabelaEntidade.vue`, `components/admin/TabelaUsuarios.vue`, `views/PropostasView.vue`, `views/ClientesView.vue`, `views/ContatosView.vue`, `components/shared/exportacao/**` (novo) |

Arquivos de outros agentes: só leitura. Se a sua entrega depende de uma mudança num arquivo alheio, descreva no relatório em vez de editar.

## 5. Contratos entre agentes da fase B

- **PERM → todos**: o servidor passa a responder **403** `{ "erro": "Sem permissão para <ação> <entidade>.", "status": 403 }` quando o perfil não tem a permissão. O `api.ts` já transforma isso em `ApiError` (status 403): mostre a mensagem com `useToast().error`, sem quebrar a tela. O perfil **Administrador** sempre pode tudo. PERM expõe `GET api/permissoes/minhas` (permissões do usuário logado) e o composable `usePermissoes()` com `pode(entidade, acao)`; os outros agentes **podem** usá-lo para esconder botões, mas a tela não pode depender dele para funcionar (se o composable ainda não existir quando você testar, siga sem ele).
- **NEG**: o PATCH de etapa passa a validar checklists obrigatórios da etapa atual (422 com a lista do que falta). O kanban (`components/funil/**`, do próprio NEG) já trata 422.
- **APROV**: proposta que se enquadra num fluxo ativo fica com aprovação **Pendente** e só pode ser compartilhada/enviada depois de aprovada (422 caso contrário). Isso altera `Compartilhar`; documente.
- **POLIR**: a exportação CSV fica num botão da `TabelaEntidade` (todas as grades ganham), gerada no navegador a partir do filtro atual, com `odataTodos`, sem endpoint novo.

## 6. Entrega

Resposta curta (máx. 25 linhas): o que ficou pronto, arquivos criados/alterados, o que foi verificado (como), o que não foi verificado, dependências e decisões para o usuário. Não repita código.


## Adendo APROV (onda 3)
O contrato de POST odata/Propostas(id)/Compartilhar mudou: responde **422** { erro, status } quando a proposta está com aprovação Pendente, ou Rejeitada por fluxo (não por recusa do cliente). Aprovar e Rejeitar passam a checar o aprovador do nível (403) e Rejeitar exige comentário quando há fluxo (422). Novas rotas e regras: 12-aprovacoes-onda3.md.
