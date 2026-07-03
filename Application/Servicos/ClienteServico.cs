using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class ClienteServico(AppDbContext db, IFilaAutomacaoPublicador publicador) : IClienteServico
{
    public IQueryable<Cliente> ObterTodos()
        => db.Clientes.AsNoTracking();

    public async Task<Cliente?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<ClienteDto> CriarAsync(Delta<Cliente> delta, CancellationToken ct = default)
    {
        var cliente = new Cliente { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(cliente);

        db.Clientes.Add(cliente);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = cliente.EmpresaId,
            RegistroId = cliente.Id,
            EntidadeAlvo = "Cliente",
            Gatilho = "Criado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return new ClienteDto
        {
            Id = cliente.Id,
            Nome = cliente.Nome,
            RazaoSocial = cliente.RazaoSocial,
            Email = cliente.Email,
            Telefone = cliente.Telefone,
            Documento = cliente.Documento,
            Cidade = cliente.Cidade,
            Estado = cliente.Estado,
            Endereco = cliente.Endereco,
            Cep = cliente.Cep,
            Observacoes = cliente.Observacoes,
            Site = cliente.Site,
            StatusClienteId = cliente.StatusClienteId,
            CriadoEm = cliente.CriadoEm,
        };
    }

    public async Task<ClienteDto?> AtualizarAsync(long id, Delta<Cliente> delta, CancellationToken ct = default)
    {
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cliente is null) return null;

        delta.Patch(cliente);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = cliente.EmpresaId,
            RegistroId = cliente.Id,
            EntidadeAlvo = "Cliente",
            Gatilho = "Editado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return new ClienteDto
        {
            Id = cliente.Id,
            Nome = cliente.Nome,
            RazaoSocial = cliente.RazaoSocial,
            Email = cliente.Email,
            Telefone = cliente.Telefone,
            Documento = cliente.Documento,
            Cidade = cliente.Cidade,
            Estado = cliente.Estado,
            Endereco = cliente.Endereco,
            Cep = cliente.Cep,
            Observacoes = cliente.Observacoes,
            Site = cliente.Site,
            StatusClienteId = cliente.StatusClienteId,
            CriadoEm = cliente.CriadoEm,
        };
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cliente is null) return false;

        cliente.Ativo = false;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = cliente.EmpresaId,
            RegistroId = cliente.Id,
            EntidadeAlvo = "Cliente",
            Gatilho = "Removido",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return true;
    }

    public IQueryable<OrigemContato> ObterOrigens()
        => db.OrigensContato.AsNoTracking();

    public async Task<OrigemContato> CriarOrigemAsync(Delta<OrigemContato> delta, CancellationToken ct = default)
    {
        var entidade = new OrigemContato { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.OrigensContato.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<OrigemContato?> AtualizarOrigemAsync(long id, Delta<OrigemContato> delta, CancellationToken ct = default)
    {
        var entidade = await db.OrigensContato.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverOrigemAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.OrigensContato.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<StatusCliente> ObterStatusClientes()
        => db.StatusClientes.AsNoTracking();

    public async Task<StatusCliente> CriarStatusAsync(Delta<StatusCliente> delta, CancellationToken ct = default)
    {
        var entidade = new StatusCliente { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.StatusClientes.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<StatusCliente?> AtualizarStatusAsync(long id, Delta<StatusCliente> delta, CancellationToken ct = default)
    {
        var entidade = await db.StatusClientes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverStatusAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.StatusClientes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<RamoAtividade> ObterRamos()
        => db.RamosAtividade.AsNoTracking();

    public async Task<RamoAtividade> CriarRamoAsync(Delta<RamoAtividade> delta, CancellationToken ct = default)
    {
        var entidade = new RamoAtividade { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.RamosAtividade.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<RamoAtividade?> AtualizarRamoAsync(long id, Delta<RamoAtividade> delta, CancellationToken ct = default)
    {
        var entidade = await db.RamosAtividade.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverRamoAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.RamosAtividade.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<PorteEmpresa> ObterPortes()
        => db.PortesEmpresa.AsNoTracking();

    public async Task<PorteEmpresa> CriarPorteAsync(Delta<PorteEmpresa> delta, CancellationToken ct = default)
    {
        var entidade = new PorteEmpresa { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.PortesEmpresa.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<PorteEmpresa?> AtualizarPorteAsync(long id, Delta<PorteEmpresa> delta, CancellationToken ct = default)
    {
        var entidade = await db.PortesEmpresa.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverPorteAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.PortesEmpresa.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
