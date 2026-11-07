# Briefing comum dos agentes de implementação

Leia este arquivo inteiro antes de começar. Ele vale para todos os agentes que trabalham ao mesmo tempo no repositório `c:\SistemProject\NovaPloomes\ast.comercial`.
Contexto de produto: `mapeamento/analise/04-comparativo-e-plano.md` e `05-plano-frontend.md`. Comportamento da Ploomes (referência funcional): `02-mapeamento-ploomes.md` e `03-rea-plom.md`.

## 1. Ambiente que já está rodando (NÃO mexa)

| Item | Estado |
|---|---|
| Backend | `dotnet run` em Development, `http://localhost:5080`. Banco PostgreSQL `ast_comercial` já migrado e populado. |
| Front | Vite com HMR em `http://localhost:5173` (proxy de `/api` e `/odata` para o 5080). O usuário está olhando esta tela agora. |
| Login | Usuário admin do seed. E-mail e senha estão em `Infrastructure/Data/SeedDados.cs` (leia de lá em tempo de execução). Nunca escreva a senha em relatório, log ou arquivo novo. |

Proibido: iniciar outro `dotnet run`/`vite`, matar processos, `npm install`/`npm ci`, instalar pacote (npm ou NuGet), mexer em `package.json`, `tsconfig*`, `vite.config.ts`, `appsettings*.json`, docker, git (nada de commit, branch, stash, reset, checkout). Nunca conecte ferramentas REA/CDP na porta 9222: é o navegador do usuário logado na Ploomes.

## 2. Regras de convivência (outros agentes editam ao mesmo tempo)

1. **Só edite os arquivos do seu escopo** (seção 5). Para arquivo compartilhado (`Program.cs`, `AdminPainel.vue`): releia o arquivo imediatamente antes de editar e faça edições pequenas e pontuais (`str_replace`), nunca reescreva o arquivo inteiro.
2. Precisa de um componente genérico novo? Crie dentro da **sua** pasta. Não crie em `components/ui/` nem altere os componentes de `ui/` existentes.
3. Erros de compilação em arquivos de outros agentes não são seus. Ignore e siga.
4. Não apague nem renomeie arquivos que não criou.
5. Sem dependências novas. Arrastar-e-soltar = HTML5 nativo; calendário e gráficos = próprios (CSS/SVG).

## 3. Convenções de front (Vue 3 + TypeScript)

- `<script setup lang="ts">`, CSS `scoped`, alias `@/`. Textos e comentários em **português do Brasil**.
- Tema por variáveis CSS (já existem): `--bg-primary --bg-secondary --bg-surface --bg-elevated --border --text-primary --text-secondary --text-muted --accent --accent-hover --danger`. Dark por padrão; precisa funcionar também no tema claro. Ícones: `<i class="mdi mdi-...">`.
- Olhe os componentes existentes (`components/shared/*`, `views/ClientesView.vue`, `components/admin/*`) e **siga o mesmo visual**.
- Base compartilhada já pronta: `components/ui/UiModal`, `UiSlideOver`, `UiTabs`, `UiBadge`, `UiToastHost`; `composables/useToast`; em `services/api.ts`: `odataGet/odataGetOne/odataPost/odataPatch/odataDelete` (antigas) e as novas `odataRequest`, `odataQuery`, `odataAction`, `montarQuery`, `ApiError`. **Telas novas usam as novas.** Valores de `$filter` precisam de `encodeURIComponent`.
- Todo fluxo tem estados de carregando, vazio e erro (erro → `useToast().error(mensagem)`). Botões com `aria-label` quando só têm ícone; foco e teclado funcionando (Esc fecha modal).
- Sem dados reais de pessoas/empresas no código. Sem copiar texto, template, ícone ou visual idêntico da Ploomes: implemente o **comportamento**, com a identidade visual do AST.
- Rotas já registradas (não edite `router/index.ts` nem `AppSidebar.vue`): `/clientes/:id`, `/contatos`, `/negocios/:id`, `/atividades`, `/propostas/:id`, público `/p/:chave`. As views correspondentes existem como esqueleto; substitua o conteúdo.
- Navegar da grade para o detalhe: use a prop `semFormInterno` da `TabelaEntidade` e trate `@editar="id => router.push(...)"`. **Não altere `TabelaEntidade.vue`.**

## 4. Convenções de backend (.NET 10, EF Core + Npgsql, OData)

- Siga o padrão dos controllers/serviços existentes (`Controllers/ClientesController.cs`, `Application/Servicos/ClienteServico.cs`): OData com `[EnableQuery]`, serviço por agregado, DI em `Program.cs`, exclusão lógica (`Ativo=false`), multi-tenant por `EmpresaId` (filtro global do EF).
- Entradas validadas; nada de SQL por concatenação; nada de segredo no código; não exponha campos sensíveis.
- **Compilar sem travar o backend em execução**: o `bin/` está em uso. Use sempre
  `dotnet build --no-restore -o "$env:TEMP\astb-<seu-nome>"` e não rode a aplicação. Se o build falhar por arquivo travado ou por colisão com outro agente (`obj/` é compartilhado), espere alguns segundos e tente de novo.
- **Migrations: somente o agente B1 cria** (`dotnet ef migrations add`). Qualquer outro agente que precisar de coluna/tabela nova **não cria**: descreve a necessidade no relatório final.
- O lead reinicia o backend ao final e a migração é aplicada sozinha em Development.

## 5. Dono de cada arquivo

| Agente | Escopo (somente estes caminhos) |
|---|---|
| **A** Kanban | `views/NegociosView.vue`, `components/funil/**`, `components/negocio/**`, `stores/funis.ts`, `composables/useFunil*.ts`, `composables/useAcoesNegocio.ts` |
| **B1** Backend funil/negócios | `Controllers/FunisController.cs`, `NegociosController.cs`, `Application/Servicos/{Funil,Negocio}Servico.cs`, `Application/Interfaces/I{Funil,Negocio}Servico.cs`, `Application/Dtos/{Funil,Negocio}Dto.cs`, `Domain/Entidades/{Funil,EtapaFunil}.cs`, `Infrastructure/Data/**` (inclui migrations), e `Program.cs` (mínimo) |
| **B2** Backend automações/propostas | `Controllers/{Automacoes,Propostas,PropostaPublica}Controller.cs`, `Application/Servicos/{Automacao,Proposta}Servico.cs` e interfaces correspondentes, `Infrastructure/Automacoes/**`, `Infrastructure/Integracoes/**`, `Program.cs` (mínimo) |
| **C** Detalhes | `views/{NegocioDetalhe,ClienteDetalhe,Contatos,Clientes}View.vue`, `components/detalhe/**`, `composables/useEntidade*.ts` |
| **D** Atividades e shell | `views/AtividadesView.vue`, `components/atividades/**`, `components/AppHeader.vue`, `components/busca/**`, `composables/useAtalhos.ts` |
| **E** Propostas | `views/{Propostas,PropostaDetalhe,PropostaPublica}View.vue`, `components/propostas/**`, `composables/useProposta*.ts` |
| **F1** Admin geral | `views/AdminView.vue`, `components/admin/**` **exceto** `AutomacoesAdmin.vue` e `components/admin/automacoes/**`; backend: `Controllers/{Empresas,Cargos,Departamentos,Equipes,Usuarios}Controller.cs` e serviços/interfaces correspondentes |
| **F2** Builder de automações | `components/admin/AutomacoesAdmin.vue`, `components/admin/automacoes/**` |
| **G** Resumo | `views/HomeView.vue`, `components/dashboard/**` |
| **S** Dados demo | `ast.comercial/scripts/**` (novo) |

`AdminPainel.vue` (mapa slug → componente) é compartilhado entre F1 e F2: edição pontual, relendo antes.

## 6. Contratos entre front e backend (para trabalharem em paralelo)

### 6.1 Funil e negócios (A ↔ B1)
- `GET odata/Funis?$expand=Etapas&$top=..` deve passar a funcionar (hoje dá 500 com qualquer opção de consulta: o controller devolve `FunilDto` mas o EDM é `Funil`). Enquanto isso não for corrigido, A pode usar `odata/Negocios@Funis` e `odata/Negocios@Etapas`, que já funcionam.
- `Funil` ganha `ProibirRetornoEtapa` (bool) e `ExigirTodasEtapas` (bool). A regra é validada no servidor ao mudar a etapa de um negócio (`PATCH odata/Negocios(id)` com `EtapaFunilId`/campo de etapa existente): violação devolve **HTTP 422** com `{ "erro": "mensagem em português", "status": 422 }`.
- `$apply=groupby((MoedaId,EtapaFunilId),aggregate(Valor with sum as Total,$count as Qtd))` deve funcionar em `odata/Negocios`.
- O evento de automação `EtapaMudou` passa a ser emitido ao mudar de etapa.
- Nomes reais de propriedade (`EtapaFunilId`, `StatusNegocioId` etc.): leia `Domain/Entidades/Negocio.cs`. O front deve consultar a entidade, não adivinhar.

### 6.2 Propostas (E ↔ B2)
- `POST odata/Propostas({id})/Compartilhar` → 200 `{ "chave": "...", "url": "/p/{chave}" }` (gera a chave se faltar, marca compartilhada).
- `POST odata/Propostas({id})/Recalcular` → 200 com a proposta (campo de valor recalculado a partir de itens, desconto e frete; B2 documenta a fórmula no relatório).
- Público, anônimo, sem tenant (resolver por chave com `IgnoreQueryFilters`, respeitar expiração e `Compartilhada`):
  - `GET /api/publico/propostas/{chave}` → 200 `{ numero, titulo, empresaNome, clienteNome, valor, moeda, validadeAte, status, itens:[{descricao,quantidade,precoUnitario,desconto,total}], parcelas:[{numero,vencimento,valor}], documentoHtml, podeResponder }`; 404 se inexistente, não compartilhada ou expirada.
  - `POST /api/publico/propostas/{chave}/aceitar` `{ nome, email? }` → 200 `{ status }`.
  - `POST /api/publico/propostas/{chave}/recusar` `{ motivo? }` → 200 `{ status }`.
  - Registrar data e IP do aceite. Se faltar coluna, B2 não cria migration: descreve no relatório.
- Pré-visualização do documento na tela interna: já existe `POST/GET odata/ModelosDocumento({id})/Renderizar(...)`; E lê o controller para o formato exato.

### 6.3 Automações (F2 ↔ B2)
- B2 escreve **logo no início** o contrato final em `mapeamento/analise/07-contrato-automacoes.md` (rotas, corpos, catálogos de gatilhos/ações/operadores **realmente implementados**, validações). Depois implementa.
- F2 começa montando a UI com tipos locais e, quando o arquivo existir, alinha os tipos ao contrato (consulte o arquivo de tempos em tempos).
- Mínimo esperado: listar (com filtros e ações), criar, editar, ativar/desativar, excluir, histórico de execuções, catálogo.

## 7. Como validar o seu trabalho

- Front, tipos: `cd AST.Comercial.Web; npx vue-tsc --noEmit -p tsconfig.app.json`. A linha de base já tem **24 erros pré-existentes em 7 arquivos** (`FormulariosAdmin.vue`, `ImportadorPloomes.vue`, `FormModal.vue`, `ModalFiltro.vue`, `TabelaEntidade.vue`, `stores/auth.ts`, `EditorModeloView.vue`; vêm de `noUncheckedIndexedAccess`). Seu critério: **zero erros nos arquivos que você criou ou alterou**. Quem é dono de um desses 7 arquivos deve corrigi-lo; os demais ignoram.
- Front, compilação real: `Invoke-WebRequest http://localhost:5173/src/views/<Arquivo>.vue -UseBasicParsing` deve dar 200 (erro de template aparece como 500 com a mensagem).
- Backend: build conforme a seção 4 e chamadas HTTP de leitura ao backend em execução (token via `POST /api/Autenticacao/login`). Código novo só passa a responder depois que o lead reiniciar a API; teste isso no relatório como "não verificado em execução" quando for o caso.
- Nunca declare algo "funcionando" sem ter rodado a checagem. Diga claramente o que **não** conseguiu verificar.

## 8. Entrega

Responda com um resumo curto (máx. 20 linhas): o que ficou pronto, arquivos criados/alterados, o que foi verificado e como, o que ficou pendente ou depende de outro agente, decisões que o usuário precisa tomar. Não repita código na resposta.


---

# Adendo da segunda rodada (valores reais, atualizados)

O backend foi corrigido e reiniciado; há dados de demonstração. **O que mudou desde a primeira rodada:**

1. **Limite do servidor: `$top` máximo = 100.** Pedir mais devolve 400 ("The limit of '100' for Top query has been exceeded") e derruba a tela inteira. Para carregar mais de 100 itens use `odataTodos<T>(path)` de `services/api.ts` (pagina de 100 em 100; o `path` não pode ter `$top`/`$skip` e precisa de `$orderby`).
2. **Formato das respostas em PascalCase.** `odata/Atividades`, `Clientes`, `PessoasContato`, `Negocios`, `Funis` agora devolvem a entidade (`Titulo`, `DataVencimento`, `Concluida`, `ClienteId`...), não o DTO em camelCase. As rotas `@` (`Negocios@Etapas`, `Propostas@Produtos`...) devolvem **array puro**, sem `{ value }`.
3. **Moedas**: `odata/Moedas` agora devolve BRL (`R$`), USD (`$`) e EUR (`€`) com `Id`, `Codigo`, `Simbolo`. Negócios usam `MoedaId` 1 (BRL) e 2 (USD). **Todo valor monetário deve usar o símbolo/código da moeda do registro**, nunca "R$" fixo. Valor em moedas diferentes nunca se soma.
4. **Responsável**: `Negocio.ResponsavelId` e `Atividade.ResponsavelId` existem, com navegação `Responsavel` (`$expand=Responsavel($select=Id,Nome)`) e filtro `ResponsavelId eq N`. Registros novos recebem o usuário do token; os demos recriados já têm. Usuário logado: `localStorage.usuario` (JSON com `usuarioId`) ou `useAuthStore().usuario`.
5. **Status do negócio**: `Ganhar` e `Perder` agora gravam `StatusId` (TipoBase Ganho/Perdido; Aberto = 1). `Perder` aceita `{ "MotivoPerdaId": n }` no corpo. `Reabrir` volta a Aberto. Um negócio "aberto" continua sendo `FechadoEm eq null`.
6. **Auditoria**: `odata/RegistrosAlteracao` existe (somente leitura, filtrada pela empresa, máx. 100 por página) com `EntidadeAlvo`, `RegistroId`, `Acao`, `DadosAntes`, `DadosDepois` (JSON), `UsuarioId`, `UsuarioNome`, `CriadoEm`. Leia `Domain/Entidades/**/RegistroAlteracao.cs` para os nomes exatos.
7. **Datas**: o servidor aceita data-hora só com fuso (`2026-12-05T10:00:00Z` ou `toISOString()`); sem fuso devolve 400. Filtros de data com `Z` funcionam.
8. **Aceite de proposta**: `Proposta.StatusResposta` (Aguardando/Aceita/Recusada), `RespondidaEm`, `RespondidaPorNome`, `RespondidaPorEmail`, `MotivoRecusa`. Automações: `criadoPor` no DTO e `Excluida` (ver `07-contrato-automacoes.md`).
9. **Erros do servidor** vêm como `{ "erro": "mensagem", "status": N }` (422 regra de negócio, 409 conflito, 400 corpo inválido). `ApiError.message` já traz a mensagem.

**Como o QA é feito**: o lead roda o front num Chrome headless logado e olha screenshots reais de cada tela após cada rodada. Por isso: nada de "provavelmente funciona". Se uma chamada de dados real falhar, a tela deve mostrar o erro de forma legível e não quebrar.

**Fixtures para testar**: negócio `176` (ganho, funil padrão), cliente `120`, proposta `34` (compartilhada; chave pública em `GET odata/Propostas(34)?$select=Chave`). Há 45 negócios, 33 atividades, 25 clientes, 20 contatos, 8 propostas, todos com prefixo `[Demo]`.

**Servidores**: backend em 5080 e front em 5173 continuam rodando. Backend novo só vale depois que o lead reinicia (peça no relatório). Vale a proibição de git, de instalar pacotes e de mexer em arquivos fora do seu escopo.
