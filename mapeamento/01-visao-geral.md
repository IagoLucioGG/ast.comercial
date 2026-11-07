# Mapeamento Ploomes: visão geral e como replicar

## Alvos analisados

| Alvo | O que é | Resultado |
|---|---|---|
| `https://app10.ploomes.com` | Aplicação web (front-end do CRM) | Protegida por desafio Cloudflare Turnstile. Navegador headless recebeu 307, depois 403 e redirecionou para `/login?redirectUrl=/favicon.ico`. Nenhum bundle da aplicação foi servido, então o front-end NÃO foi mapeado. |
| `https://developers.ploomes.com` | Documentação pública da API (SPA) | Carrega `/json/openapi.json` (público). Mapeado por completo. |
| `https://public-api2.ploomes.com` | Servidor da API (campo `servers` do OpenAPI) | Contrato conhecido pelo OpenAPI. Não foi chamado (exige User-Key). |

## Fatos observados

- API baseada em OData v4, OpenAPI 3.0.1, versão 1.0.0 (campo `info`).
- Autenticação: header `User-Key`, obtida em um usuário de integração criado na conta (doc de suporte da Ploomes).
- 79 paths, 129 operações, 22 tags (ver `02-endpoints.md`).
- Parâmetros OData comuns: `$top`, `$skip`, `$select`, `$expand`, `$filter`, `$orderby`.
- Convenção de path com `@` para junções de entidades (ex.: `/Cities@Countries@States`).
- Os `schemas` em `components` têm só 1 item, então modelos de corpo (request/response) não estão detalhados no arquivo.

## Como replicar

1. Baixar o contrato: `Invoke-WebRequest https://developers.ploomes.com/json/openapi.json -OutFile fontes/openapi.json`.
2. Gerar o inventário a partir de `paths` agrupando por `tags` (script usado: PowerShell com `ConvertFrom-Json`).
3. Para testar chamadas reais: criar usuário de integração na Ploomes, copiar a User-Key e enviar em `User-Key`. A Ploomes também publica uma coleção Postman (link no artigo de suporte "Como obter acesso à API do Ploomes").
4. Campos customizados: usar a `FieldKey` (artigo de suporte "Como identificar todos os campos...").

## Lacunas (não verificado)

- Estrutura do front-end `app10` (rotas, chunks, chamadas internas): bloqueada pelo Turnstile. Requer sessão autenticada em navegador operado pelo usuário.
- Modelos de dados das entidades e exemplos de resposta: dependem de chamadas reais com User-Key.
- Webhooks: endpoints listados (`Webhooks`), semântica de eventos não analisada.

## Atualização: front-end do app10 (observado após login)

Fonte: aba logada observada via REA (CDP) + bundles públicos baixados do CloudFront.

### Arquitetura observada
- Aplicação híbrida: **AngularJS** (`angular-app.min.js`, 7,4 MB) + **React** (`react-app.min.js`, 10,1 MB), mais 6 chunks lazy (`chunks/chunk.0..5.js`). Origem: `https://d3hvjm3wv4q03y.cloudfront.net/PROD/stable/`.
- Bibliotecas observadas nos bundles/HTML: CKEditor, FullCalendar, Microsoft SignalR, Google Maps/Charts, Intercom, Birdie.
- API principal usada pelo front: `api2-s08-app-canary.ploomes.com` (mesmo contrato OData da API pública). Outros serviços de backend por domínio: `reports-commands-api`, `operation-screen-br`, `ai-builder`, `flows-api-s08`, `identity-provider`, `logs-api`, `exporter-api`, `importation2`, `mailmapie`, `service-channel-api`, `sse-subscriptions`, `socket-hub` (SignalR), `pricing`, `central-api`, `gateway-api2`.
- Source maps: **não públicos** (HTTP 403). Código fica minificado.

### Telas, modais e componentes
- `fontes/templates/`: 464 templates HTML do AngularJS extraídos de `ng-templates.js` (40 módulos), com as chaves de tradução originais.
- `fontes/templates-ptbr/`: os mesmos templates com as chaves substituídas pelo texto pt-BR (2.524 trocas; 3 chaves sem tradução).
- `fontes/i18n/`: dicionários extraídos do bundle (`pt-BR`, `pt-PT`, `en`, `es-1`, `es-2`), 94 módulos e 8.155 textos cada.
- Índice por módulo: `04-templates-indice.md`.

### Limitações conhecidas
- Telas de proposta/documento renderizam o corpo dentro de `<iframe id="preview-ifr">`. O DOM e o screenshot do frame principal capturam só a moldura (barra de ações). O layout do corpo vem do editor de modelos (`/docgenerator`) e dos templates.
- Algumas capturas antigas em `telas/` foram feitas com espera fixa e podem estar incompletas. Devem ser refeitas com espera por seletor e validação do DOM.
- Opções de gatilho de automação (ex.: "Periodicamente") **não aparecem em nenhum bundle**. Inferência: vêm do servidor. Confirmar com uma chamada à API.
- Os templates cobrem o AngularJS. As telas em React (bundle de 10 MB) ainda não foram analisadas.
- `telas/` contém screenshots com dados reais de clientes da conta. Não versionar nem compartilhar.
