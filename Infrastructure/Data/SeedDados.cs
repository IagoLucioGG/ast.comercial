using AST.Comercial.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Infrastructure.Data;

/// <summary>
/// Seed de dados iniciais do sistema.
/// </summary>
public static class SeedDados
{
    public static async Task ExecutarAsync(AppDbContext db)
    {
        if (await db.Moedas.IgnoreQueryFilters().AnyAsync()) return;

        db.Moedas.AddRange(
            new Moeda { EmpresaId = 0, Nome = "Real Brasileiro", Codigo = "BRL", Simbolo = "R$", Padrao = true },
            new Moeda { EmpresaId = 0, Nome = "Dólar Americano", Codigo = "USD", Simbolo = "$" },
            new Moeda { EmpresaId = 0, Nome = "Euro", Codigo = "EUR", Simbolo = "€" }
        );

        var brasil = new Localidade { Nome = "Brasil", Tipo = TipoLocalidade.Pais, Codigo = "BR" };
        db.Localidades.Add(brasil);

        await db.SaveChangesAsync();

        // Criar empresa e usuário admin padrão
        await CriarEmpresaAdminAsync(db);
    }

    private static async Task CriarEmpresaAdminAsync(AppDbContext db)
    {
        if (await db.Empresas.IgnoreQueryFilters().AnyAsync()) return;

        var empresa = new Empresa
        {
            Nome = "AST Comercial",
            RazaoSocial = "AST Comercial Ltda",
            Cnpj = "00.000.000/0001-00",
            Email = "admin@astcomercial.com",
            Ativo = true
        };
        db.Empresas.Add(empresa);
        await db.SaveChangesAsync();

        var admin = new Usuario
        {
            EmpresaId = empresa.Id,
            Nome = "Administrador",
            Email = "admin@astcomercial.com",
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Tipo = TipoUsuario.Normal,
            Perfil = PerfilUsuario.Administrador,
            EmailConfirmado = true,
            Ativo = true
        };
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();

        db.EmpresaIdAtual = empresa.Id;
        await CriarDadosPadraoEmpresaAsync(db, empresa.Id);
    }

    public static async Task CriarDadosPadraoEmpresaAsync(AppDbContext db, long empresaId)
    {
        db.StatusNegocios.AddRange(
            new StatusNegocio { EmpresaId = empresaId, Nome = "Aberto", Cor = "#3498db", Ordem = 1, Padrao = true, TipoBase = TipoStatusNegocio.Aberto },
            new StatusNegocio { EmpresaId = empresaId, Nome = "Ganho", Cor = "#27ae60", Ordem = 2, TipoBase = TipoStatusNegocio.Ganho },
            new StatusNegocio { EmpresaId = empresaId, Nome = "Perdido", Cor = "#e74c3c", Ordem = 3, TipoBase = TipoStatusNegocio.Perdido }
        );

        db.StatusClientes.AddRange(
            new StatusCliente { EmpresaId = empresaId, Nome = "Prospect", Cor = "#f39c12", Ordem = 1, Padrao = true },
            new StatusCliente { EmpresaId = empresaId, Nome = "Ativo", Cor = "#27ae60", Ordem = 2 },
            new StatusCliente { EmpresaId = empresaId, Nome = "Inativo", Cor = "#95a5a6", Ordem = 3 }
        );

        var funil = new Funil { EmpresaId = empresaId, Nome = "Funil Padrão", Ordem = 1 };
        db.Funis.Add(funil);
        await db.SaveChangesAsync();

        db.EtapasFunil.AddRange(
            new EtapaFunil { EmpresaId = empresaId, FunilId = funil.Id, Nome = "Qualificação", Ordem = 1 },
            new EtapaFunil { EmpresaId = empresaId, FunilId = funil.Id, Nome = "Apresentação", Ordem = 2 },
            new EtapaFunil { EmpresaId = empresaId, FunilId = funil.Id, Nome = "Proposta", Ordem = 3 },
            new EtapaFunil { EmpresaId = empresaId, FunilId = funil.Id, Nome = "Negociação", Ordem = 4 },
            new EtapaFunil { EmpresaId = empresaId, FunilId = funil.Id, Nome = "Fechamento", Ordem = 5 }
        );

        await db.SaveChangesAsync();

        await CriarFormulariosPadraoAsync(db, empresaId);
    }

    private static async Task CriarFormulariosPadraoAsync(AppDbContext db, long empresaId)
    {
        // Delega para o seed de formulários com seções completas
        await SeedFormulariosPadrao.SeedAsync(db, empresaId);
    }
}
