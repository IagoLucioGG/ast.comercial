using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Infrastructure.Data;

/// <summary>
/// Popula campos nativos para cada entidade no banco.
/// Roda no startup — só insere se não existirem ainda para a empresa.
/// Campos nativos representam propriedades fixas das entidades (baseados na Ploomes).
/// </summary>
public static class SeedCamposNativos
{
    public static async Task SeedAsync(AppDbContext db, long empresaId)
    {
        var existentes = await db.Campos
            .IgnoreQueryFilters()
            .Where(c => c.EmpresaId == empresaId && c.Nativo)
            .Select(c => new { c.EntidadeAlvo, c.Chave })
            .ToListAsync();

        var camposParaInserir = new List<Campo>();

        foreach (var (entidade, campos) in ObterCamposNativos())
        {
            foreach (var (nome, chave, tipo, obrigatorio) in campos)
            {
                if (existentes.Any(e => e.EntidadeAlvo == entidade && e.Chave == $"{entidade}_{chave}"))
                    continue;

                camposParaInserir.Add(new Campo
                {
                    Nome = nome,
                    Chave = $"{entidade}_{chave}",
                    Tipo = tipo,
                    EntidadeAlvo = entidade,
                    EmpresaId = empresaId,
                    Nativo = true,
                    Visivel = true,
                    Ativo = true,
                    Obrigatorio = obrigatorio
                });
            }
        }

        if (camposParaInserir.Count > 0)
        {
            db.Campos.AddRange(camposParaInserir);
            await db.SaveChangesAsync();
        }
    }

    private static IEnumerable<(EntidadeAlvo Entidade, (string Nome, string Chave, TipoCampo Tipo, bool Obrigatorio)[] Campos)> ObterCamposNativos()
    {
        // Cliente - dados de contato/empresa principal
        yield return (EntidadeAlvo.Cliente, new (string, string, TipoCampo, bool)[]
        {
            ("Nome", "Nome", TipoCampo.Texto, true),
            ("Razão social", "RazaoSocial", TipoCampo.Texto, false),
            ("E-mail", "Email", TipoCampo.Email, false),
            ("Telefone", "Telefone", TipoCampo.Telefone, false),
            ("CPF", "Cpf", TipoCampo.Cpf, false),
            ("CNPJ", "Cnpj", TipoCampo.Cnpj, false),
            ("Bairro", "Bairro", TipoCampo.Texto, false),
            ("Cidade", "Cidade", TipoCampo.Texto, false),
            ("Estado", "Estado", TipoCampo.Texto, false),
            ("País", "Pais", TipoCampo.Texto, false),
            ("Endereço", "Endereco", TipoCampo.Endereco, false),
            ("Complemento", "Complemento", TipoCampo.Texto, false),
            ("CEP", "Cep", TipoCampo.Cep, false),
            ("Observações", "Observacoes", TipoCampo.TextoLongo, false),
            ("Site", "Site", TipoCampo.Texto, false),
            ("Facebook", "Facebook", TipoCampo.Texto, false),
            ("Latitude", "Latitude", TipoCampo.Decimal, false),
            ("Longitude", "Longitude", TipoCampo.Decimal, false),
            ("Data de nascimento", "DataNascimento", TipoCampo.Data, false),
            ("Código CNAE", "CodigoCnae", TipoCampo.Texto, false),
            ("Inscrição estadual", "InscricaoEstadual", TipoCampo.Texto, false),
            ("Recebe e-mail marketing", "RecebeEmailMarketing", TipoCampo.Booleano, false),
            ("Avatar", "Avatar", TipoCampo.Imagem, false),
            ("Chave externa", "ChaveExterna", TipoCampo.Texto, false),
        });

        // Negócio - oportunidades de venda
        yield return (EntidadeAlvo.Negocio, new (string, string, TipoCampo, bool)[]
        {
            ("Título", "Titulo", TipoCampo.Texto, true),
            ("Valor", "Valor", TipoCampo.Moeda, false),
            ("Status", "Status", TipoCampo.Texto, false),
            ("Data de fechamento", "DataFechamento", TipoCampo.Data, false),
            ("Data prevista de fechamento", "DataPrevistaFechamento", TipoCampo.Data, false),
            ("Observações", "Observacoes", TipoCampo.TextoLongo, false),
            ("Chave externa", "ChaveExterna", TipoCampo.Texto, false),
        });

        // Proposta - propostas comerciais
        yield return (EntidadeAlvo.Proposta, new (string, string, TipoCampo, bool)[]
        {
            ("Título", "Titulo", TipoCampo.Texto, true),
            ("Valor", "Valor", TipoCampo.Moeda, false),
            ("Status", "Status", TipoCampo.Texto, false),
            ("Data de validade", "DataValidade", TipoCampo.Data, false),
            ("Observações", "Observacoes", TipoCampo.TextoLongo, false),
            ("Desconto", "Desconto", TipoCampo.Percentagem, false),
        });

        // Produto - catálogo de produtos/serviços
        yield return (EntidadeAlvo.Produto, new (string, string, TipoCampo, bool)[]
        {
            ("Nome", "Nome", TipoCampo.Texto, true),
            ("Código", "Codigo", TipoCampo.Texto, false),
            ("Descrição", "Descricao", TipoCampo.TextoLongo, false),
            ("Preço", "Preco", TipoCampo.Moeda, false),
            ("Custo", "Custo", TipoCampo.Moeda, false),
            ("Unidade", "Unidade", TipoCampo.Texto, false),
            ("Ativo", "Ativo", TipoCampo.Booleano, false),
        });

        // Atividade - tarefas e compromissos
        yield return (EntidadeAlvo.Atividade, new (string, string, TipoCampo, bool)[]
        {
            ("Título", "Titulo", TipoCampo.Texto, true),
            ("Descrição", "Descricao", TipoCampo.TextoLongo, false),
            ("Data de início", "DataInicio", TipoCampo.DataHora, false),
            ("Data de fim", "DataFim", TipoCampo.DataHora, false),
            ("Status", "Status", TipoCampo.Texto, false),
            ("Tipo", "Tipo", TipoCampo.Texto, false),
        });

        // PessoaContato - contatos vinculados a clientes
        yield return (EntidadeAlvo.PessoaContato, new (string, string, TipoCampo, bool)[]
        {
            ("Nome", "Nome", TipoCampo.Texto, true),
            ("E-mail", "Email", TipoCampo.Email, false),
            ("Telefone", "Telefone", TipoCampo.Telefone, false),
            ("Cargo", "Cargo", TipoCampo.Texto, false),
            ("Departamento", "Departamento", TipoCampo.Texto, false),
            ("Observações", "Observacoes", TipoCampo.TextoLongo, false),
        });

        // Usuário - usuários do sistema
        yield return (EntidadeAlvo.Usuario, new (string, string, TipoCampo, bool)[]
        {
            ("Nome", "Nome", TipoCampo.Texto, true),
            ("E-mail", "Email", TipoCampo.Email, false),
            ("Perfil", "Perfil", TipoCampo.Texto, false),
            ("Tipo", "Tipo", TipoCampo.Texto, false),
        });

        // Equipe - grupos de usuários
        yield return (EntidadeAlvo.Equipe, new (string, string, TipoCampo, bool)[]
        {
            ("Nome", "Nome", TipoCampo.Texto, true),
            ("Descrição", "Descricao", TipoCampo.TextoLongo, false),
        });

        // Empresa - dados da organização
        yield return (EntidadeAlvo.Empresa, new (string, string, TipoCampo, bool)[]
        {
            ("Nome", "Nome", TipoCampo.Texto, true),
            ("CNPJ", "Cnpj", TipoCampo.Cnpj, false),
            ("E-mail", "Email", TipoCampo.Email, false),
            ("Telefone", "Telefone", TipoCampo.Telefone, false),
            ("Endereço", "Endereco", TipoCampo.Endereco, false),
            ("Site", "Site", TipoCampo.Texto, false),
        });

        // Departamento - divisões organizacionais
        yield return (EntidadeAlvo.Departamento, new (string, string, TipoCampo, bool)[]
        {
            ("Nome", "Nome", TipoCampo.Texto, true),
            ("Descrição", "Descricao", TipoCampo.TextoLongo, false),
        });

        // Cargo - posições dentro da empresa
        yield return (EntidadeAlvo.Cargo, new (string, string, TipoCampo, bool)[]
        {
            ("Nome", "Nome", TipoCampo.Texto, true),
            ("Descrição", "Descricao", TipoCampo.TextoLongo, false),
        });
    }
}
