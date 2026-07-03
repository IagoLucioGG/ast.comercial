# AST.Comercial

CRM comercial desenvolvido em .NET, inspirado na Ploomes. O objetivo é oferecer uma plataforma completa para gestão de contatos, negócios, funis de vendas, propostas e automações comerciais.

## Status

⚙️ No momento apenas o backend está sendo implementado. No futuro o projeto contará também com o frontend.

## Tecnologias

- .NET 8
- ASP.NET Core OData
- Entity Framework Core
- PostgreSQL
- Clean Architecture simplificada

## Estrutura do Projeto

```
Domain/          → Entidades de domínio (sem dependências externas)
Application/     → Serviços, DTOs e interfaces
Infrastructure/  → DbContext, configurações EF Core, automações
Controllers/     → Controllers OData
```

## Funcionalidades

- Gestão de contatos e empresas
- Funis de vendas com etapas configuráveis
- Negócios com acompanhamento de status (aberto, ganho, perdido)
- Propostas comerciais com itens, seções e parcelas
- Atividades (tarefas, reuniões, ligações)
- Campos personalizáveis com fórmulas JavaScript
- Automações com filtros, ações e fila de processamento
- Produtos, famílias e grupos de produto
- Autenticação JWT com refresh token


## API

A API segue o padrão OData, com suporte a `$filter`, `$select`, `$expand`, `$orderby`, `$top`, `$skip` e `$count`.

Base URL: `/odata/`

### Autenticação

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/Autenticacao/login` | Login com email e senha |
| POST | `/api/Autenticacao/refresh` | Renovar token via refresh token |
| POST | `/api/Autenticacao/logout` | Revogar token do usuário logado |

### Contatos

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/odata/Contatos` | Listar contatos |
| GET | `/odata/Contatos({id})` | Obter contato por ID |
| POST | `/odata/Contatos` | Criar contato |
| PATCH | `/odata/Contatos({id})` | Atualizar contato (parcial) |
| DELETE | `/odata/Contatos({id})` | Remover contato (soft delete) |
| GET | `/odata/Contatos@Origens` | Listar origens de contato |
| GET | `/odata/Contatos@Status` | Listar status de contato |
| GET | `/odata/Contatos@RamosAtividade` | Listar ramos de atividade |
| GET | `/odata/Contatos@Portes` | Listar portes de empresa |
| GET | `/odata/Contatos@Tipos` | Listar tipos de contato |

### Negócios

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/odata/Negocios` | Listar negócios |
| GET | `/odata/Negocios({id})` | Obter negócio por ID |
| POST | `/odata/Negocios` | Criar negócio |
| PATCH | `/odata/Negocios({id})` | Atualizar negócio (parcial) |
| DELETE | `/odata/Negocios({id})` | Remover negócio (soft delete) |
| POST | `/odata/Negocios({id})/Ganhar` | Marcar negócio como ganho |
| POST | `/odata/Negocios({id})/Perder` | Marcar negócio como perdido |
| POST | `/odata/Negocios({id})/Reabrir` | Reabrir negócio |
| GET | `/odata/Negocios@Status` | Listar status de negócio |
| GET | `/odata/Negocios@MotivosPerda` | Listar motivos de perda |
| GET | `/odata/Negocios@Funis` | Listar funis |
| GET | `/odata/Negocios@Etapas` | Listar etapas |

### Funis

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/odata/Funis` | Listar funis |
| GET | `/odata/Funis({id})` | Obter funil por ID |
| POST | `/odata/Funis` | Criar funil |
| PATCH | `/odata/Funis({id})` | Atualizar funil (parcial) |
| DELETE | `/odata/Funis({id})` | Remover funil (soft delete) |
| GET | `/odata/Funis({id})/Etapas` | Listar etapas do funil |
| POST | `/odata/Funis({id})/Etapas` | Criar etapa no funil |

### Atividades

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/odata/Atividades` | Listar atividades |
| GET | `/odata/Atividades({id})` | Obter atividade por ID |
| POST | `/odata/Atividades` | Criar atividade |
| PATCH | `/odata/Atividades({id})` | Atualizar atividade (parcial) |
| DELETE | `/odata/Atividades({id})` | Remover atividade (soft delete) |
| POST | `/odata/Atividades({id})/Concluir` | Marcar atividade como concluída |
| GET | `/odata/Atividades@Tipos` | Listar tipos de atividade |

### Propostas

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/odata/Propostas` | Listar propostas |
| GET | `/odata/Propostas({id})` | Obter proposta por ID |
| POST | `/odata/Propostas({id})/Aprovar` | Aprovar proposta |
| POST | `/odata/Propostas({id})/Rejeitar` | Rejeitar proposta |
| GET | `/odata/Propostas@Produtos` | Listar itens de proposta |
| GET | `/odata/Propostas@Secoes` | Listar seções de proposta |
| GET | `/odata/Propostas@Parcelas` | Listar parcelas |
| GET | `/odata/Propostas@StatusAprovacao` | Listar status de aprovação |

### Produtos

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/odata/Produtos` | Listar produtos |
| GET | `/odata/Produtos({id})` | Obter produto por ID |
| GET | `/odata/Produtos@Familias` | Listar famílias de produto |
| GET | `/odata/Produtos@Grupos` | Listar grupos de produto |
| GET | `/odata/Produtos@Categorias` | Listar categorias |

### Automações

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/odata/Automacoes` | Listar automações |
| GET | `/odata/Automacoes({id})` | Obter automação por ID |
| GET | `/odata/Automacoes@Fila` | Listar fila de execução |
| GET | `/odata/Automacoes@Logs` | Listar logs de execução |
| GET | `/odata/Automacoes@Gatilhos` | Listar tipos de gatilho |
| GET | `/odata/Automacoes@Acoes` | Listar tipos de ação |

### Campos Personalizáveis

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/odata/Campos` | Listar campos |
| GET | `/odata/Campos({id})` | Obter campo por ID |
| GET | `/odata/Campos@Opcoes` | Listar opções de campo |
| GET | `/odata/Campos@Regras` | Listar regras de campo |
| GET | `/odata/Campos@Configuracoes` | Listar configurações de campo |
| GET | `/odata/Campos@Valores` | Listar valores preenchidos |
| GET | `/odata/Campos@Entidades` | Listar entidades alvo |
| GET | `/odata/Campos@Tipos` | Listar tipos de campo |
| GET | `/odata/Campos@Esquema({entidade})` | Obter esquema de fórmulas por entidade |

## Exemplos de Uso (OData)

```http
# Listar contatos do tipo Empresa, ordenados por nome
GET /odata/Contatos?$filter=Tipo eq 'Empresa'&$orderby=Nome&$top=20

# Obter negócio com dados do contato expandidos
GET /odata/Negocios(1)?$expand=Contato

# Contar atividades pendentes
GET /odata/Atividades?$filter=Concluida eq false&$count=true

# Listar propostas com itens e parcelas
GET /odata/Propostas?$expand=Itens,Parcelas&$top=10
```

## Como Rodar

1. Configure a connection string do PostgreSQL em `appsettings.json`
2. Aplique as migrations:
   ```bash
   dotnet ef database update
   ```
3. Execute a aplicação:
   ```bash
   dotnet run
   ```

A API estará disponível em `https://localhost:5001/odata/`.
