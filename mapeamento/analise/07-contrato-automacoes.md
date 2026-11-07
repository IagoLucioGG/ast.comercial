# Contrato da API de automações (B2 → F2)

Fonte da verdade do que o motor **realmente executa**. Tudo aqui exige JWT (`Authorization: Bearer ...`) e é filtrado pela empresa do token.
Base: `/api/automacoes` (controller MVC, JSON camelCase, enums **como string com o nome do enum**). As rotas OData antigas (`odata/Automacoes`, `odata/Automacoes@Fila`, `odata/Automacoes@Logs`) continuam, só leitura.

## 1. Rotas

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/automacoes?entidadeAlvo=&gatilho=&ativo=&busca=&skip=0&top=50` | Lista (com filtros e ações). Resposta `{ total, itens: [Automacao] }`. `top` máx. 200. Ordenada por `ordem`, `id`. Todos os parâmetros são opcionais. |
| GET | `/api/automacoes/{id}` | Uma automação (com filtros e ações). 404 se não existir/excluída. |
| POST | `/api/automacoes` | Cria. Corpo = `AutomacaoEntrada`. 201 + `Automacao`. |
| PUT | `/api/automacoes/{id}` | Edita **substituindo** filtros e ações em conjunto (a ordem do array vira a ordem de execução). 200 + `Automacao`. |
| DELETE | `/api/automacoes/{id}` | Exclusão lógica. 204. Itens pendentes na fila ficam `Cancelado`. |
| POST | `/api/automacoes/{id}/ativar` | `ativo = true`. 200 + `Automacao`. (Revalida a configuração.) |
| POST | `/api/automacoes/{id}/desativar` | `ativo = false` (o motor deixa de disparar e cancela o que estava pendente). 200 + `Automacao`. |
| GET | `/api/automacoes/{id}/execucoes?status=&skip=0&top=50` | Histórico (`ExecucaoAutomacao`), mais recentes primeiro. `{ total, itens: [Execucao] }`. |
| GET | `/api/automacoes/catalogo` | Catálogo (seção 3). |

Erros (sempre `{ "erro": "mensagem em português", "status": N }`): 400 corpo inválido, 404 não encontrada, **422** validação de negócio (`erro` = primeira mensagem; `erros` = lista completa de strings).

## 2. Corpos

`AutomacaoEntrada` (POST/PUT):
```json
{
  "nome": "string 1..200 (obrigatório)",
  "descricao": "string ≤ 1000 | null",
  "entidadeAlvo": "Cliente|PessoaContato|Negocio|Atividade|Proposta|Funil",
  "gatilho": "Criado|Editado|Removido|Concluido|StatusMudou|EtapaMudou|PropostaCriada|PropostaAprovada|PropostaEnviada|Periodicamente",
  "ordem": 0,
  "impedirCascata": false,
  "ativo": true,
  "horarioExecucao": "HH:mm (UTC) — obrigatório só para Periodicamente, senão null",
  "filtros": [ { "grupo": 0, "campoReferencia": "Valor", "operador": "Maior", "valor": "1000" } ],
  "acoes":   [ { "tipo": "CriarTarefa", "configuracao": { "titulo": "Follow-up", "prazo": 3 } } ]
}
```
- `ativo` omitido no POST = `true`; no PUT omitido = mantém.
- `filtros` pode ser vazio (sempre passa). Filtros do mesmo `grupo` = **E**; grupos diferentes = **OU**. A `ordem` do filtro é a posição no array.
- `acoes` precisa ter **ao menos 1** item. Executadas em sequência na ordem do array. Se uma falha, a execução vira `Falha` e o item da fila é reprocessado (até 3 tentativas, backoff 10s·n²).
- `cronExpressao`, `formulaCondicao`, `gatilhoCampoReferencia` **não são suportados** pelo motor: enviar valor não nulo → 422.

`Automacao` (resposta):
```json
{
  "id": 1, "nome": "...", "descricao": null,
  "entidadeAlvo": "Negocio", "gatilho": "EtapaMudou",
  "ordem": 0, "impedirCascata": false, "ativo": true,
  "horarioExecucao": null, "proximaExecucao": null, "ultimaExecucao": null,
  "criadoEm": "...", "atualizadoEm": null,
  "criadoPorUsuarioId": 1, "criadoPor": "Nome do usuário",
  "filtros": [ { "id": 1, "grupo": 0, "ordem": 0, "campoReferencia": "Valor", "operador": "Maior", "valor": "1000" } ],
  "acoes":   [ { "id": 1, "ordem": 0, "tipo": "CriarTarefa", "configuracao": { "titulo": "...", "prazo": 3 } } ]
}
```

Campos novos (B3):
- `criadoPorUsuarioId` (long, nullable) e `criadoPor` (string, nullable): usuário que criou a automação (o do token no POST). `null` em automações criadas antes desta versão. Somente leitura: PUT/POST ignoram esses campos se enviados.
- Exclusão lógica agora usa a coluna própria `Excluida` (bool). O marcador antigo `ChaveExterna='__excluida__'` deixou de existir (as linhas foram migradas e a `ChaveExterna` delas ficou nula). Excluída continua com `ativo=false`, some de `GET /api/automacoes`, de `GET /api/automacoes/{id}` (404) e de `odata/Automacoes`. Não existe campo `excluida` no JSON da API (ela nunca aparece nas respostas); só a entidade OData `Automacoes` expõe `Excluida`, `CriadoPorUsuarioId` e a navegação `CriadoPor` (`$expand=CriadoPor($select=Id,Nome)`).

`Execucao`:
```json
{ "id": 1, "automacaoId": 1, "registroId": 10, "entidadeAlvo": "Negocio",
  "status": "EmExecucao|Sucesso|Falha|FalhaFiltro|Cancelada",
  "iniciadaEm": "...", "finalizadaEm": "...", "duracaoMs": 12,
  "detalhes": "texto/JSON com o resultado por ação", "erro": null, "disparadoPor": "Nome" }
```
`FalhaFiltro` = os filtros não bateram (não é erro).

## 3. Catálogo — `GET /api/automacoes/catalogo`

```json
{
  "entidades": [ { "valor": "Negocio", "nome": "Negócio", "gatilhos": ["Criado", "..."],
                   "campos": [ { "nome": "Valor", "tipo": "numero|texto|booleano|data|enum", "opcoes": ["..."], "editavel": true } ] } ],
  "gatilhos":  [ { "valor": "EtapaMudou", "nome": "Etapa mudou", "implementado": true,
                   "entidades": ["Negocio"], "observacao": "..." } ],
  "acoes":     [ { "valor": "CriarTarefa", "nome": "Criar tarefa", "implementado": true,
                   "entidades": null,
                   "configuracao": [ { "chave": "titulo", "tipo": "texto", "obrigatorio": true, "descricao": "..." } ] } ],
  "operadores":[ { "valor": "Maior", "nome": "Maior que", "implementado": true, "precisaValor": true } ]
}
```
- `entidades` (da ação): lista de entidades alvo onde a ação é válida; `null` = qualquer entidade.
- Itens com `implementado:false` **aparecem** (para a UI mostrar desabilitado "em breve"), mas o backend responde 422 se forem usados.
- `campos`: campos escalares reais da entidade, usáveis em `campoReferencia` (filtros) e em `EditarDados`. Sem navegação (`Cliente.Nome` não existe). Campos personalizados (`Campo`) **ainda não** são avaliados pelo motor.
- Comparação: texto sem diferenciar maiúsculas; `Maior/Menor/...` numérico (cultura invariante, `1000.5`) com fallback textual; booleano como `true/false`; datas ISO 8601; enums pelo nome.

### 3.1 Gatilhos implementados (quem emite o evento hoje)
| Gatilho | Entidades | Observação |
|---|---|---|
| Criado / Editado / Removido | Cliente, PessoaContato, Negocio, Atividade, Funil | emitidos pelos serviços |
| Concluido | Atividade | |
| StatusMudou | Negocio, Proposta | Proposta: ao rejeitar |
| EtapaMudou | Negocio | emitido pelo B1 ao mudar de etapa |
| PropostaCriada | Proposta | |
| PropostaAprovada | Proposta | aprovação interna ou aceite público |
| PropostaEnviada | Proposta | ao compartilhar pela 1ª vez |
| Periodicamente | qualquer entidade | 1x/dia no `horarioExecucao` (UTC); roda **sem registro** → só aceita as ações `CriarTarefa`, `CriarRegistroInteracao`, `ExecutarWebhook` |

**Não implementados** (sem emissor): `CampoAlterado`, `Vencido`, `PropostaExpirada`, `EtiquetaAdicionada`.

### 3.2 Ações implementadas e `configuracao`
| Ação | Entidades | Configuração |
|---|---|---|
| EditarDados | Cliente, PessoaContato, Negocio, Atividade, Proposta | `{ "alteracoes": [ { "campo": "Observacoes", "valor": "texto" } ] }` (campo escalar gravável da entidade; `Id`, `EmpresaId`, `Ativo`, datas de controle e `ChaveExterna` são proibidos) |
| CriarTarefa | qualquer | `{ "titulo": "texto ≤200", "tipo": "Tarefa|Ligacao|Email|Reuniao|Nota", "prazo": 0..3650 (dias, int) }` — vincula ao Cliente/Negócio quando a entidade for uma dessas |
| AlterarStatus | Negocio | `{ "statusId": 1 }` (precisa existir na empresa) |
| MoverEtapa | Negocio | `{ "etapaId": 1 }` (precisa existir; o funil do negócio passa a ser o da etapa) |
| ConcluirTarefa | Atividade | `{}` |
| AdicionarEtiqueta | qualquer | `{ "etiquetaId": 1 }` |
| RemoverEtiqueta | qualquer | `{ "etiquetaId": 1 }` |
| CriarRegistroInteracao | qualquer | `{ "descricao": "texto ≤1000" }` (grava no histórico de alterações) |
| ExecutarWebhook | qualquer | `{ "url": "https://...", "metodo": "POST|GET|PUT|PATCH|DELETE", "headers": { "X": "v" }, "corpo": "string" }` — só http/https; hosts locais/privados são bloqueados; resposta não-2xx = falha (reprocessa) |

**Não implementadas** (bloqueadas com 422): `EnviarEmail`, `NotificarUsuario`, `CriarContato`, `CriarProposta`, `AlterarResponsavel`, `CriarNegocio`, `GerarDocumento`.

### 3.3 Operadores
Implementados (12): `Igual, Diferente, Contem, NaoContem, Maior, Menor, MaiorOuIgual, MenorOuIgual, Vazio, NaoVazio, ComecaCom, TerminaCom`. `Vazio`/`NaoVazio` não usam `valor`; os demais exigem `valor`.
Não implementados (422): `MudouPara`, `MudouDe` (o evento não carrega o valor anterior).

## 4. Validações (422)
Nome obrigatório; `entidadeAlvo`/`gatilho`/`operador`/`tipo` precisam ser nomes válidos; gatilho precisa estar implementado **e** ser coerente com a entidade (tabela 3.1); pelo menos 1 ação; ação implementada e permitida para a entidade; `configuracao` conforme 3.2 (referências `statusId/etapaId/etiquetaId` conferidas na empresa); filtros com `campoReferencia` existente na entidade (`^[A-Za-z_][A-Za-z0-9_]*$`) e operador implementado; `Periodicamente` exige `horarioExecucao`.

## 5. Observações de comportamento
- `ativo=false` ≠ excluída: desativada continua na listagem; excluída (`Excluida=true`, que também deixa `Ativo=false`) some.
- Os filtros agora funcionam de verdade: o motor monta o snapshot do registro no momento da execução (os publicadores não enviavam `DadosEvento`).
- Cascata: ações do motor **não** disparam novas automações hoje (nenhum ciclo possível); `impedirCascata` é guardado para o futuro.
