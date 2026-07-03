using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class PessoaContatoServico(AppDbContext db, IFilaAutomacaoPublicador publicador) : IPessoaContatoServico
{
    public IQueryable<PessoaContato> ObterTodos()
        => db.PessoasContato.AsNoTracking();

    public async Task<PessoaContato?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.PessoasContato.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PessoaContatoDto> CriarAsync(Delta<PessoaContato> delta, CancellationToken ct = default)
    {
        var pessoa = new PessoaContato { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(pessoa);

        db.PessoasContato.Add(pessoa);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = pessoa.EmpresaId,
            RegistroId = pessoa.Id,
            EntidadeAlvo = "PessoaContato",
            Gatilho = "Criado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return new PessoaContatoDto
        {
            Id = pessoa.Id,
            Nome = pessoa.Nome,
            Email = pessoa.Email,
            Telefone = pessoa.Telefone,
            Cargo = pessoa.Cargo,
            Documento = pessoa.Documento,
            Observacoes = pessoa.Observacoes,
            Decisor = pessoa.Decisor,
            ClienteId = pessoa.ClienteId,
            CriadoEm = pessoa.CriadoEm,
        };
    }

    public async Task<PessoaContatoDto?> AtualizarAsync(long id, Delta<PessoaContato> delta, CancellationToken ct = default)
    {
        var pessoa = await db.PessoasContato.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pessoa is null) return null;

        delta.Patch(pessoa);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = pessoa.EmpresaId,
            RegistroId = pessoa.Id,
            EntidadeAlvo = "PessoaContato",
            Gatilho = "Editado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return new PessoaContatoDto
        {
            Id = pessoa.Id,
            Nome = pessoa.Nome,
            Email = pessoa.Email,
            Telefone = pessoa.Telefone,
            Cargo = pessoa.Cargo,
            Documento = pessoa.Documento,
            Observacoes = pessoa.Observacoes,
            Decisor = pessoa.Decisor,
            ClienteId = pessoa.ClienteId,
            CriadoEm = pessoa.CriadoEm,
        };
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var pessoa = await db.PessoasContato.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pessoa is null) return false;

        pessoa.Ativo = false;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = pessoa.EmpresaId,
            RegistroId = pessoa.Id,
            EntidadeAlvo = "PessoaContato",
            Gatilho = "Removido",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return true;
    }
}
