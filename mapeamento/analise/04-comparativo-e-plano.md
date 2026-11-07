# Comparativo dos 3 repositórios e plano de implementação

Base: `ast.comercial`. Fontes de conhecimento: `mapeamento` (engenharia reversa da Ploomes) e `rea-plom` (análise da Ploomes feita por um amigo).
Relatórios de origem: `01-ast-comercial.md`, `02-mapeamento-ploomes.md`, `03-rea-plom.md` (mesma pasta).
Legenda: [O] observado nos relatórios · [I] inferência minha · [?] depende de decisão sua.

---

## 1. O que muda em relação ao que se imaginava

1. **O `rea-plom` não é uma aplicação.** É um fork do REA (`rea-agents` 6.8.0, a ferramenta de engenharia reversa). O valor dele está só em `.rea-demos/`: captura de campos, funil, drag-and-drop, 17 scripts CDP e um protótipo estático do funil (`preview-funil.html`). Não há código de CRM para aproveitar. [O]
2. **O `ast.comercial` é uma base real e boa**, mas está longe de um CRM completo. Compila (0 erros), tem multi-tenant, auditoria, motor de automação com fila e formulários dinâmicos com editor de fórmula. Faltam kanban, detalhe de negócio, tarefas, dashboard, e-mail, PDF de proposta, permissões. [O]
3. **Há bugs e falhas de segurança que precisam vir antes de qualquer funcionalidade nova** (seção 4, Fase 0). [O]
4. **O `mapeamento` é uma especificação funcional**, não código reaproveitável. Ele diz *o que* a Ploomes faz e *como se comporta*. O front dela (AngularJS + React, minificado) não serve de base técnica. [I]

---

## 2. Comparativo por módulo

Classificação Ploomes (do relatório 02): **N** = núcleo · **A** = avançado · **P** = periférico.
Estado no AST: **Ok** funcional · **Parc** parcial · **Stub** só placeholder · **Aus** ausente.

| Módulo | Ploomes | AST backend | AST frontend | Contribuição do rea-plom | Lacuna principal |
|---|---|---|---|---|---|
| Clientes e contatos | N | Ok | Ok (clientes) / Aus (contatos) | — | Tela de contatos, filiais, mesclagem |
| Negócios e funil kanban | N | Ok (ganhar/perder/reabrir) | Parc (só grade) | **Alta**: carga por coluna (`$top=15`), totais por `$apply=groupby`, mover card = `PATCH {Id,StageId}`, flags `ForbiddenStageReturn` / `MustPassAllStages` | Kanban, detalhe, timeline, motivo de perda na UI |
| Tarefas, agenda, interações | N | Ok (atividades) | Aus | Média: `Tasks@Types`, lembretes, repetição | Telas, calendário, lembretes |
| Campos customizados e formulários | N | Ok | Ok (diferencial do AST) | **Alta**: formato do campo (`Key = entidade_GUID`, 4 idiomas, 4 slots de fórmula, EAV tipado) | Execução de fórmula (hoje só armazena), tipos de fórmula, acumulador |
| Usuários, equipes, perfis, permissões | N | Parc (**sem enforcement**) | Stub | Baixa | Autorização por perfil e por entidade, visibilidade por equipe |
| Produtos, famílias, grupos | N | Ok | Ok (grade) | — | Telas de família/grupo, listas de preço |
| Propostas e vendas | N | Parc (CRUD) | Parc (só grade) | — | Totais, PDF, link público com aceite, revisões, envio |
| Modelos de documento / DocGenerator | A | Ok (render HTML) | Ok (editor de seções) | — | PDF, laços de itens, formatação, assinatura eletrônica |
| Automações | A | Parc (fila ok; e-mail, cron, 5 ações faltam) | Parc (só lista) | Nenhuma | **API de criação/edição**, builder na UI, emissores de gatilho, tenant dos serviços de fundo |
| Fluxos de aprovação | A | Parc (nunca cria solicitação) | Aus | Baixa (`/Approvals` consultado no funil) | Criar solicitações, validar aprovador |
| Checklists por estágio | A | Aus | Aus | Média (`GetApplicableChecklists`) | Tudo |
| Integrações / webhooks | A | Parc (motor genérico) | Parc (lista) | — | Validar `WebhookSecret`, esconder segredos, conectores prontos |
| Relatórios, painéis, metas | A | Aus | Stub | — | Tudo |
| Gestão de dados (duplicidade, bloqueio) | A | Aus | Aus | — | Tudo |
| Importação / exportação | N | Parc (só modelo Ploomes) | Parc | — | Planilha genérica |
| E-mail, WhatsApp, telefonia, chat, IA, CPQ, SSO | P | Aus | Aus | — | Só sob demanda |
| Auditoria | N | Ok | Ok | — | — |
| Testes, CI, Docker | — | Aus | Aus | Scripts CDP servem para testes de comportamento | Tudo |

---

## 3. Como usar cada fonte (para não copiar o que não deve)

| Fonte | Uso recomendado | Não fazer |
|---|---|---|
| `mapeamento/analise/02` + `00-inventario-rotas` + `fontes/openapi.json` | Especificação funcional: módulos, campos, regras, fluxos de tela, contrato OData público como referência de compatibilidade | Copiar templates, textos de i18n, ícones, imagens ou layout pixel a pixel |
| `mapeamento/fontes/templates*`, `bundles/`, `i18n/` | Consulta para entender estrutura de telas e nomes de campo | Levar para o repositório do produto; é código e texto da Ploomes |
| `mapeamento/telas/` | Referência visual interna | Versionar ou compartilhar: tem dados reais de clientes |
| `rea-plom/.rea-demos` | Comportamento do funil e dos campos; scripts CDP como base de testes | Reaproveitar `cdp-hunt-field-save.mjs` (vasculha credenciais) ou `fields-capture.json` |
| `ast.comercial` | Base de código | — |

---

## 4. Plano de implementação

Princípio: **corrigir a fundação, depois entregar o núcleo de CRM de ponta a ponta, depois o avançado.** Cada fase termina com algo utilizável e testado.

### Fase 0 — Fundação e segurança (antes de qualquer feature)
Estimativa [I]: 1 a 2 semanas.
- Autorização: aplicar o perfil (`Administrador/Gerente/Vendedor/Visualizador`) nos controllers; política por entidade; visibilidade por equipe/responsável. Hoje qualquer logado altera tudo.
- Tenant nos serviços de fundo: definir `EmpresaIdAtual` em `ProcessadorAutomacaoService`, `AgendadorAutomacaoService` e `PollingIntegracaoService`. **Validar o bug antes** (ver seção 6), porque o relatório não executou o sistema.
- Retry de itens em `Falha` (hoje a query só pega `Pendente`).
- Ordem do pipeline: `TenantMiddleware` antes da autorização, para a API key funcionar.
- Segredos: tirar de `appsettings.json` (user-secrets/variáveis de ambiente); remover senha fixa do seed; restringir CORS.
- Parar de expor `Integracao` e `Usuario` inteiros no OData (segredos, `SenhaHash`, `TokenIntegracao`); criptografar credenciais de integração; validar `WebhookSecret`.
- Atualizar `Microsoft.OpenApi` (vulnerabilidade alta).
- Qualidade: projeto de testes .NET (xUnit) + Vitest no front, CI, Dockerfile e docker-compose (API + Postgres), README correto (.NET 10).
- **Saída da fase:** build e testes verdes no CI; teste automatizado provando isolamento entre empresas e bloqueio por perfil.

### Fase 1 — Núcleo de CRM completo
Estimativa [I]: 4 a 6 semanas.
- Funil kanban (Vue): uma consulta por coluna, rolagem incremental de 15 em 15, totais por moeda via agregação, busca que afeta cartões e totais, arrastar com `PATCH {Id, StageId}` e atualização dos totais. Adicionar `ForbiddenStageReturn` e `MustPassAllStages` ao `Funil` e validar no servidor. **Mover o evento `EtapaMudou`** para ser emitido (hoje não é).
- Detalhe de negócio e de cliente: abas (dados, contatos, tarefas, propostas, histórico) e linha do tempo.
- Contatos e Atividades: telas, calendário, tipos, repetição, lembretes.
- Busca global; cadastros auxiliares editáveis (cargos, departamentos, famílias, grupos, etiquetas no menu).
- Perfis e permissões: tela de gestão ligada à Fase 0.
- Dashboard "Resumo" básico (contadores e funil).
- **Saída:** um vendedor consegue operar o dia a dia só pela UI.

### Fase 2 — Propostas e documentos
Estimativa [I]: 3 a 5 semanas.
- Totais, descontos, parcelas e revisões na proposta; editor de proposta com itens.
- Geração de PDF a partir do modelo; link público com aceite externo (campos já existem na entidade).
- Fluxo de aprovação: criar `SolicitacaoAprovacao` por regra e validar o aprovador do nível.
- Modelos: laços de itens, formatação de moeda/data, condicionais.
- **Saída:** proposta criada, aprovada, enviada por link e aceita.

### Fase 3 — Automações e integrações
Estimativa [I]: 4 a 6 semanas.
- API de criação/edição de automações e **builder na UI** (entidade, gatilho, bloco de condição, ações em ordem, histórico). Estrutura vem do relatório 02 (seção 3).
- Emissores dos gatilhos que faltam; ações `EnviarEmail` e `NotificarUsuario` (SMTP); cron real; ações do enum sem código.
- Tela da fila de processamento (reprocessar, ver falhas).
- Integrações: criação e mapeamento de fluxos na UI; mais eventos de saída além de `NegocioGanho`.
- **Saída:** uma automação criada pela UI executa de ponta a ponta em ambiente multi-empresa.

### Fase 4 — Campos avançados e regras por estágio
- Execução de fórmulas (decidir front sandbox vs. backend; ver seção 5), os quatro tipos (simples, condicional, integrada por URL, Excel), campo acumulador, obrigatório/oculto/bloqueado por usuário ou fórmula.
- Checklists por estágio e validação para avançar de etapa.

### Fase 5 — Relatórios, metas, dados
- Painéis e gráficos com métricas; metas; importação/exportação por planilha; duplicidade e mesclagem; lista de bloqueio.

### Fase 6 — Periféricos (sob demanda)
- CPQ, WhatsApp, e-mail integrado, calendário externo, assinatura eletrônica, IA, SSO/MFA, ERP (Bimer, se for objetivo).

---

## 5. Decisões que dependem de você [?]

1. **Objetivo do produto.** Uso interno da AST ou produto vendido a terceiros? Isso define quanto cuidado jurídico é necessário (seção 7) e se o multi-tenant precisa suportar muitos clientes.
2. **Compatibilidade com a Ploomes.** Quer que a nova API seja compatível com a OData pública da Ploomes (facilita migrar clientes e usar a documentação deles como contrato)? O AST já usa OData e já tem um importador de modelo.
3. **ODataBimer.** O `metadata.xml` do ERP Bimer está na pasta sem uso. Integrar com o Bimer é um objetivo real? Se sim, entra na Fase 3 ou 6.
4. **Fórmulas.** Avaliar JavaScript no navegador (como a Ploomes) ou no servidor? No servidor exige um motor sandbox (ex.: Jint) e cuidado com execução de código arbitrário.
5. **Estrutura da solução.** Hoje é um projeto único. Dividir em projetos (Domain/Application/Infrastructure/Api) agora ou depois da Fase 1?
6. **Prioridade entre fases 2 e 3.** Proposta antes de automação, ou o contrário?

---

## 6. Verificações que ainda não foram feitas

- **Rodar o sistema.** Os relatórios foram de leitura. O bug de tenant nos serviços de fundo, o problema da API key e o importador precisam ser confirmados em execução, com banco real.
- **`npm run build` do front** não foi executado (sem `node_modules`).
- **Respostas reais da API Ploomes** (pipelines com flags, tipos de campo, regras de estágio, erros de validação) não foram capturadas por nenhuma das fontes. Para o funil e os campos, o ideal é recapturar em uma conta de teste sua, só com leituras, guardando o corpo das respostas.
- **Bundle React da Ploomes** (10 MB) não analisado: afeta Operação, Relatórios novos, CPQ BETA, IA.
- Opções de gatilho e tipos de ação das automações vêm do servidor e continuam desconhecidas.

---

## 7. Riscos e cuidados

1. **Chave da Ploomes exposta no `rea-plom`.** O arquivo `fields-capture.json` contém um header `User-Key` com valor real, num repositório com remoto no GitHub. A chave deve ser revogada na Ploomes e o arquivo removido do histórico. Quem decide é o dono do repositório (seu amigo). [O]
2. **Alterações em conta de produção de terceiros.** O material do `rea-plom` registra um `PATCH` de campo e um `PATCH` de mover card numa conta real de outro cliente, sem sinal de reversão. Não repetir isso. Qualquer teste de escrita deve ser em conta própria de teste. [O]
3. **Dados pessoais de clientes** em `mapeamento/telas`, em `rea-plom/.rea-demos` e em `ast.comercial/HTML ploomes` (conteúdo de proposta de um cliente real). Não versionar nem redistribuir. [O]
4. **Propriedade intelectual.** Templates, textos, ícones, imagens e identidade visual da Ploomes são deles. Reproduzir funcionalidade observada é uma coisa. Copiar código, textos, arquivos de tradução ou o visual idêntico é outra. Os termos de uso da Ploomes também podem restringir engenharia reversa. Isto não é parecer jurídico: se o objetivo for comercial, vale uma revisão de um advogado antes de lançar. [I]
5. **Escopo.** Equivaler a uma Ploomes inteira é um projeto de anos. A priorização acima (N → A → P) existe para entregar valor por fases e poder parar em qualquer ponto com algo funcional.
