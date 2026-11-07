# O que ainda falta no AST em relação à Ploomes (e plano da onda 3)

Base: `00-inventario-rotas.md` e `02-mapeamento-ploomes.md` (o que a Ploomes tem) contra o estado do AST em 07/11/2026 (branch `feature/frontend-recursos-ploomes`, revisado em tela).
Legenda: **Ok** existe e foi visto funcionando · **Parcial** · **Falta**.

## 1. Comparativo por área

| Área | Ploomes | AST hoje | Falta no AST |
|---|---|---|---|
| Resumo | Painel com indicadores e atalhos | **Ok** | Card de metas |
| Clientes | Tabela com abas de filtro, detalhe com abas, mapa | **Ok** (grade + detalhe) | Etiquetas no registro, edição em massa, mesclar duplicados, mapa |
| Contatos | Lista + vínculo com empresa | **Ok** | — |
| Negócios / funil | Kanban, ganhar/perder, regras de etapa | **Ok** | **Produtos no negócio**, **checklists por etapa** (obrigatórios para avançar), negócios derivados |
| Funis (admin) | Editor de funil: etapas, regras, checklists | **Parcial** (só lista) | **Editor completo** (criar/renomear/ordenar etapas, regras, checklists) |
| Atividades | Lista, calendário, repetição, lembretes | **Ok** (sem repetição/lembrete) | Repetição e lembretes |
| Produtos | Tabela, **detalhe do produto**, famílias, grupos, **listas de preço** | **Parcial** (só grade) | **Detalhe do produto**, **listas de preço**, produtos vinculados |
| Propostas | Editor, revisões, CPQ, aprovação multinível, link público | **Parcial** | **Fluxos de aprovação ativos**, revisões, geração de PDF, editor de modelo com abas (Campos/Blocos/Estilos) |
| Pedidos / Vendas | Entidade própria ligada à proposta | **Falta** | Decisão pendente (ver seção 3) |
| Relatórios | Painéis com gráficos, filtros, modo apresentação | **Falta** (rota aponta para o Resumo) | **Painéis e gráficos configuráveis** |
| Metas | Metas por usuário/equipe e acompanhamento | **Falta** | **Metas com progresso** |
| Aprovações | Caixa de aprovações pendentes | **Falta** | **Tela "Minhas aprovações"** |
| Importação / exportação | Planilha para várias entidades | **Falta** | **Importar CSV** e **exportar CSV** das grades |
| Perfis e permissões | Permissão por perfil, entidade e visibilidade | **Falta** ("Em breve") | **Matriz de permissões aplicada no servidor** (crítico: hoje qualquer logado altera tudo) |
| Formulários externos / Leads | Captura de leads com cadência | **Falta** | Próxima onda |
| E-mail, WhatsApp, IA, CPQ completo, Sandbox, SSO | Periféricos | **Falta** | Sob demanda |

### Pendências pequenas já conhecidas
- `TabelaEntidade`: coluna de moeda com "R$" fixo e status cru ("EmRevisao").
- `GET` de registro único ignora `$expand=Responsavel` (Negócios e Atividades).
- Avatar "[V" na tabela de usuários.
- Etapas órfãs `[teste-b1]` no banco.

## 2. Sobre "estar diferente da Ploomes"

O plano é **paridade de função e de fluxo**: mesmas telas, abas, campos, ações e regras, organizadas de forma parecida. O **visual** continua com a identidade do AST (tema escuro, ciano), sem copiar ícones, textos, imagens ou o desenho exato da Ploomes, que são deles. Se quiser aproximar mais algum fluxo específico, aponte a tela da Ploomes e eu descrevo e implemento o comportamento.

## 3. Decisão pendente

**Pedidos/Vendas**: entidade própria (como a Ploomes, com itens copiados da proposta aceita) ou estado da proposta ("Venda fechada")? Fica fora da onda 3 até você decidir.

## 4. Onda 3

Fase A (um agente, sozinho): **esquema** de todas as tabelas novas numa única migration, para os agentes da fase B não colidirem em migrations.

Fase B (em paralelo, cada um com arquivos próprios):

| Agente | Entrega |
|---|---|
| PERM | Matriz de permissões por perfil × entidade × ação e escopo de visibilidade, aplicada no servidor; tela de perfis; menus e botões escondidos conforme permissão |
| NEG | Produtos no negócio, checklists por etapa (com bloqueio ao avançar) e editor completo de funis |
| PROD | Detalhe do produto e listas de preço |
| REL | Painéis de relatório com gráficos configuráveis (agregação segura no servidor) |
| METAS | Metas por usuário/equipe com progresso calculado |
| IMP | Importação CSV com mapeamento de colunas, validação e relatório de erros |
| APROV | Fluxos de aprovação ativos nas propostas e tela "Minhas aprovações" |
| POLIR | Pendências pequenas e exportação CSV nas grades |

Ao final: revisão visual em navegador, correções, commit e push na branch.
