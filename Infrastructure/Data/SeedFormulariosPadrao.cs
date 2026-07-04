using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Infrastructure.Data;

/// <summary>
/// Seed de formulários padrão para cada empresa.
/// Cria formulários com seções e campos nativos pré-configurados,
/// similar à estrutura da Ploomes (Empresas, Empresas (mini), Pessoas, etc.)
/// </summary>
public static class SeedFormulariosPadrao
{
    public static async Task SeedAsync(AppDbContext db, long empresaId)
    {
        var temFormularios = await db.Formularios
            .IgnoreQueryFilters()
            .AnyAsync(f => f.EmpresaId == empresaId);

        if (!temFormularios)
        {
            // Primeira vez: criar tudo do zero
            var formularios = ObterFormulariosPadrao(empresaId);
            db.Formularios.AddRange(formularios);
            await db.SaveChangesAsync();
            return;
        }

        // Já tem formulários — verificar se já tem seção "Localização" (migração já feita)
        var temLocalizacao = await db.SecoesFormulario
            .IgnoreQueryFilters()
            .AnyAsync(s => s.Formulario.EmpresaId == empresaId && s.Nome == "Localização");

        if (temLocalizacao) return;

        // Migrar: criar seção Localização e mover campos de endereço
        await CriarSecoesParaFormulariosExistentesAsync(db, empresaId);
    }

    private static async Task CriarSecoesParaFormulariosExistentesAsync(AppDbContext db, long empresaId)
    {
        var formularios = await db.Formularios
            .IgnoreQueryFilters()
            .Where(f => f.EmpresaId == empresaId)
            .ToListAsync();

        var camposEndereco = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Endereco", "Bairro", "Cidade", "Estado", "Cep", "Pais", "Latitude", "Longitude", "Complemento" };

        foreach (var formulario in formularios)
        {
            // Apenas criar seção Localização para formulários de Cliente completos
            if (formulario.EntidadeAlvo != EntidadeAlvo.Cliente || formulario.Nome.Contains("mini", StringComparison.OrdinalIgnoreCase) || formulario.Nome.Contains("Filtro", StringComparison.OrdinalIgnoreCase))
                continue;

            // Buscar campos de endereço (podem estar em qualquer seção ou sem seção)
            var camposDoForm = await db.CamposFormulario
                .IgnoreQueryFilters()
                .Where(c => c.FormularioId == formulario.Id)
                .ToListAsync();

            var camposDeEndereco = camposDoForm.Where(c => c.NomeCampoFixo is not null && camposEndereco.Contains(c.NomeCampoFixo)).ToList();
            if (camposDeEndereco.Count == 0) continue;

            // Remover seção genérica "Campos" se existir e mover seus campos para sem-seção
            var secaoGenerica = await db.SecoesFormulario
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.FormularioId == formulario.Id && s.Nome == "Campos");

            if (secaoGenerica is not null)
            {
                // Mover todos os campos dessa seção para sem-seção
                var camposNaSecao = camposDoForm.Where(c => c.SecaoFormularioId == secaoGenerica.Id).ToList();
                foreach (var c in camposNaSecao) c.SecaoFormularioId = null;
                db.SecoesFormulario.Remove(secaoGenerica);
                await db.SaveChangesAsync();
            }

            // Criar seção Localização
            var secaoLocalizacao = new SecaoFormulario
            {
                Nome = "Localização",
                Ordem = 1,
                Colunas = 2,
                Colapsavel = true,
                IniciaColapsada = true,
                Ativo = true,
                EmpresaId = empresaId,
                FormularioId = formulario.Id
            };
            db.SecoesFormulario.Add(secaoLocalizacao);
            await db.SaveChangesAsync();

            // Mover campos de endereço para a seção Localização
            foreach (var campo in camposDeEndereco)
            {
                campo.SecaoFormularioId = secaoLocalizacao.Id;
            }
            await db.SaveChangesAsync();
        }
    }

    private static List<Formulario> ObterFormulariosPadrao(long empresaId)
    {
        var lista = new List<Formulario>();
        var ordem = 0;

        // === CLIENTE ===
        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Cliente, "Empresas", ordem++, true,
            [
                CriarSecao("Localização", 1, 2, [
                    ("Endereço", "Endereco", false), ("Bairro", "Bairro", false),
                    ("Cidade", "Cidade", false), ("Estado", "Estado", false),
                    ("CEP", "Cep", false), ("País", "Pais", false),
                    ("Latitude", "Latitude", false), ("Longitude", "Longitude", false),
                ], colapsavel: true),
                CriarSecao("Informações Adicionais", 2, 2, [
                    ("Observações", "Observacoes", false), ("Código CNAE", "CodigoCnae", false),
                    ("Inscrição estadual", "InscricaoEstadual", false), ("Chave externa", "ChaveExterna", false),
                ], colapsavel: true),
            ],
            camposSoltos: [
                ("Nome", "Nome", true), ("CNPJ", "Cnpj", false),
                ("Razão social", "RazaoSocial", false), ("E-mail", "Email", false),
                ("Telefone", "Telefone", false), ("Site", "Site", false),
                ("Facebook", "Facebook", false), ("Data de nascimento", "DataNascimento", false),
            ]));

        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Cliente, "Empresas (mini)", ordem++, false,
            [],
            camposSoltos: [
                ("Nome", "Nome", true), ("CNPJ", "Cnpj", false),
                ("E-mail", "Email", false), ("Telefone", "Telefone", false),
            ]));

        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.PessoaContato, "Pessoas de Contato", ordem++, true,
            [
                CriarSecao("Informações Adicionais", 1, 2, [
                    ("Observações", "Observacoes", false),
                ], colapsavel: true),
            ],
            camposSoltos: [
                ("Nome", "Nome", true), ("E-mail", "Email", false),
                ("Telefone", "Telefone", false), ("Cargo", "Cargo", false),
                ("Departamento", "Departamento", false),
            ]));

        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.PessoaContato, "Pessoas (mini)", ordem++, false,
            [],
            camposSoltos: [
                ("Nome", "Nome", true), ("E-mail", "Email", false),
                ("Telefone", "Telefone", false),
            ]));

        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Produto, "Produto de cliente", ordem++, false,
            [],
            camposSoltos: [
                ("Nome", "Nome", true), ("Código", "Codigo", false),
                ("Preço", "Preco", false), ("Unidade", "Unidade", false),
            ]));

        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Produto, "Produto de cliente (mini)", ordem++, false,
            [],
            camposSoltos: [("Nome", "Nome", true), ("Preço", "Preco", false)]));

        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Cliente, "Filtro de produto de cliente", ordem++, false,
            [],
            camposSoltos: [
                ("Nome", "Nome", false), ("Cidade", "Cidade", false),
                ("Estado", "Estado", false),
            ]));

        // === PRODUTO ===
        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Produto, "Produtos", ordem++, true,
            [],
            camposSoltos: [
                ("Nome", "Nome", true), ("Código", "Codigo", false),
                ("Descrição", "Descricao", false), ("Preço", "Preco", false),
                ("Custo", "Custo", false), ("Unidade", "Unidade", false),
            ]));

        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Produto, "Produtos (mini)", ordem++, false,
            [],
            camposSoltos: [
                ("Nome", "Nome", true), ("Preço", "Preco", false),
                ("Código", "Codigo", false),
            ]));

        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Produto, "Grupos de produtos", ordem++, false,
            [],
            camposSoltos: [("Nome", "Nome", true), ("Descrição", "Descricao", false)]));

        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Produto, "Famílias de produtos", ordem++, false,
            [],
            camposSoltos: [("Nome", "Nome", true), ("Descrição", "Descricao", false)]));

        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Produto, "Vínculos", ordem++, false,
            [],
            camposSoltos: [
                ("Nome", "Nome", true), ("Preço", "Preco", false),
                ("Unidade", "Unidade", false),
            ]));

        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Produto, "Vínculos (mini)", ordem++, false,
            [],
            camposSoltos: [("Nome", "Nome", true), ("Preço", "Preco", false)]));

        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Produto, "Filtro de produtos da base", ordem++, false,
            [],
            camposSoltos: [("Nome", "Nome", false), ("Código", "Codigo", false)]));

        // === SUA EMPRESA ===
        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Empresa, "Sua empresa", ordem++, true,
            [],
            camposSoltos: [
                ("Nome", "Nome", true), ("CNPJ", "Cnpj", false),
                ("E-mail", "Email", false), ("Telefone", "Telefone", false),
                ("Endereço", "Endereco", false), ("Site", "Site", false),
            ]));

        // === USUÁRIO ===
        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Usuario, "Usuários", ordem++, true,
            [],
            camposSoltos: [
                ("Nome", "Nome", true), ("E-mail", "Email", false),
                ("Perfil", "Perfil", false), ("Tipo", "Tipo", false),
            ]));

        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Equipe, "Equipes", ordem++, true,
            [],
            camposSoltos: [("Nome", "Nome", true), ("Descrição", "Descricao", false)]));

        // === REGISTRO DE INTERAÇÃO (Atividade) ===
        lista.Add(CriarFormulario(empresaId, EntidadeAlvo.Atividade, "Registro de interação", ordem++, true,
            [],
            camposSoltos: [
                ("Título", "Titulo", true), ("Descrição", "Descricao", false),
                ("Tipo", "Tipo", false), ("Status", "Status", false),
                ("Data de início", "DataInicio", false), ("Data de fim", "DataFim", false),
            ]));

        return lista;
    }

    private static Formulario CriarFormulario(long empresaId, EntidadeAlvo entidade, string nome, int ordem, bool padrao, List<SecaoFormulario> secoes, List<(string Rotulo, string NomeCampoFixo, bool Obrigatorio)>? camposSoltos = null)
    {
        var formulario = new Formulario
        {
            Nome = nome,
            EntidadeAlvo = entidade,
            Ordem = ordem,
            Padrao = padrao,
            Ativo = true,
            EmpresaId = empresaId,
            Secoes = secoes,
            Campos = []
        };

        var ordemCampo = 0;

        // Campos soltos (sem seção) — ficam diretamente no formulário
        if (camposSoltos is not null)
        {
            foreach (var (rotulo, nomeCampoFixo, obrigatorio) in camposSoltos)
            {
                formulario.Campos.Add(new CampoFormulario
                {
                    NomeCampoFixo = nomeCampoFixo,
                    Rotulo = rotulo,
                    Ordem = ordemCampo++,
                    Visivel = true,
                    Obrigatorio = obrigatorio,
                    Ativo = true,
                    EmpresaId = empresaId
                });
            }
        }

        // Campos das seções
        foreach (var secao in secoes)
        {
            secao.EmpresaId = empresaId;
            foreach (var campo in secao.Campos)
            {
                campo.EmpresaId = empresaId;
                campo.Ordem = ordemCampo++;
                formulario.Campos.Add(campo);
            }
        }

        return formulario;
    }

    private static SecaoFormulario CriarSecao(string nome, int ordem, int colunas, List<(string Rotulo, string NomeCampoFixo, bool Obrigatorio)> campos, bool colapsavel = false)
    {
        var secao = new SecaoFormulario
        {
            Nome = nome,
            Ordem = ordem,
            Colunas = colunas,
            Colapsavel = colapsavel,
            IniciaColapsada = colapsavel,
            Ativo = true,
            Campos = []
        };

        var ordemCampo = 0;
        foreach (var (rotulo, nomeCampoFixo, obrigatorio) in campos)
        {
            secao.Campos.Add(new CampoFormulario
            {
                NomeCampoFixo = nomeCampoFixo,
                Rotulo = rotulo,
                Ordem = ordemCampo++,
                Visivel = true,
                Obrigatorio = obrigatorio,
                Ativo = true
            });
        }

        return secao;
    }
}
