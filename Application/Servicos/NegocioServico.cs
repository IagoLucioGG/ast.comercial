using AST.Comercial.Application.Dtos;
using AST.Comercial.Application.Interfaces;
using AST.Comercial.Domain.Entidades;
using AST.Comercial.Infrastructure.Data;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.EntityFrameworkCore;

namespace AST.Comercial.Application.Servicos;

public class NegocioServico(AppDbContext db, IFilaAutomacaoPublicador publicador, IIntegracaoEventoPublicador integracaoPublicador, IChecklistServico checklists) : INegocioServico
{
    public IQueryable<Negocio> ObterTodos()
        => db.Negocios.AsNoTracking();

    public async Task<Negocio?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await db.Negocios.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<Negocio> CriarAsync(Delta<Negocio> delta, CancellationToken ct = default)
    {
        var negocio = new Negocio();
        delta.Patch(negocio);
        negocio.EmpresaId = db.EmpresaIdAtual;
        negocio.ResponsavelId ??= db.UsuarioIdAtual;
        await ValidarResponsavelAsync(negocio.ResponsavelId, ct);

        // A etapa precisa existir e pertencer ao funil informado
        var etapaValida = await db.EtapasFunil.AnyAsync(e => e.Id == negocio.EtapaId && e.FunilId == negocio.FunilId, ct);
        if (!etapaValida)
            throw new RegraNegocioException("A etapa informada não pertence ao funil selecionado.");

        // Sem status informado, assume o status padrão (tipo Aberto)
        if (negocio.StatusId is null)
        {
            negocio.StatusId = await db.StatusNegocios
                .Where(s => s.TipoBase == TipoStatusNegocio.Aberto)
                .OrderByDescending(s => s.Padrao).ThenBy(s => s.Ordem)
                .Select(s => (long?)s.Id).FirstOrDefaultAsync(ct);
        }

        db.Negocios.Add(negocio);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = negocio.EmpresaId,
            RegistroId = negocio.Id,
            EntidadeAlvo = "Negocio",
            Gatilho = "Criado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        // Retorna a entidade (tipo do EDM) para o Created() do OData conseguir gerar o Location.
        return negocio;
    }

    public async Task<Negocio?> AtualizarAsync(long id, Delta<Negocio> delta, CancellationToken ct = default)
    {
        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (negocio is null) return null;

        var etapaAnteriorId = negocio.EtapaId;
        var funilAnteriorId = negocio.FunilId;
        var empresaId = negocio.EmpresaId;
        var responsavelAnteriorId = negocio.ResponsavelId;

        delta.Patch(negocio);
        negocio.EmpresaId = empresaId; // o tenant nunca muda via PATCH

        if (negocio.ResponsavelId != responsavelAnteriorId)
        {
            try { await ValidarResponsavelAsync(negocio.ResponsavelId, ct); }
            catch { await db.Entry(negocio).ReloadAsync(ct); throw; }
        }

        var etapaMudou = negocio.EtapaId != etapaAnteriorId;
        if (etapaMudou || negocio.FunilId != funilAnteriorId)
        {
            try
            {
                await ValidarMudancaDeEtapaAsync(negocio, etapaAnteriorId, funilAnteriorId, ct);
                await ValidarChecklistAoSairAsync(negocio, etapaAnteriorId, funilAnteriorId, ct);
            }
            catch
            {
                // Descarta a alteração em memória; nada foi persistido
                await db.Entry(negocio).ReloadAsync(ct);
                throw;
            }
        }

        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = negocio.EmpresaId,
            RegistroId = negocio.Id,
            EntidadeAlvo = "Negocio",
            Gatilho = "Editado",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        if (etapaMudou)
        {
            var nomes = await db.EtapasFunil.AsNoTracking()
                .Where(e => e.Id == etapaAnteriorId || e.Id == negocio.EtapaId)
                .ToDictionaryAsync(e => e.Id, e => e.Nome, ct);
            var dados = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["EtapaAnteriorId"] = etapaAnteriorId,
                ["EtapaAnterior"] = nomes.GetValueOrDefault(etapaAnteriorId),
                ["EtapaId"] = negocio.EtapaId,
                ["Etapa"] = nomes.GetValueOrDefault(negocio.EtapaId),
                ["FunilId"] = negocio.FunilId,
            });
            await publicador.PublicarEventoAsync(new EventoAutomacao
            {
                EmpresaId = negocio.EmpresaId,
                RegistroId = negocio.Id,
                EntidadeAlvo = "Negocio",
                Gatilho = "EtapaMudou",
                DadosEvento = dados,
                DisparadoPor = db.UsuarioAtual
            }, ct);
        }

        return negocio;
    }

    public async Task<bool> RemoverAsync(long id, CancellationToken ct = default)
    {
        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (negocio is null) return false;

        negocio.Ativo = false;
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = negocio.EmpresaId,
            RegistroId = negocio.Id,
            EntidadeAlvo = "Negocio",
            Gatilho = "Removido",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        return true;
    }

    public async Task GanharAsync(long id, CancellationToken ct = default)
    {
        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new KeyNotFoundException($"Negócio {id} não encontrado.");
        negocio.FechadoEm = DateTime.UtcNow;
        negocio.MotivoPerdaId = null;
        negocio.StatusId = await ObterStatusPorTipoAsync(TipoStatusNegocio.Ganho, negocio.StatusId, ct);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = negocio.EmpresaId,
            RegistroId = negocio.Id,
            EntidadeAlvo = "Negocio",
            Gatilho = "StatusMudou",
            DisparadoPor = db.UsuarioAtual
        }, ct);

        await integracaoPublicador.PublicarEventoSaidaAsync("NegocioGanho", Domain.Entidades.EntidadeAlvo.Negocio, negocio.Id, negocio.EmpresaId, ct);
    }

    public async Task PerderAsync(long id, long? motivoPerdaId = null, CancellationToken ct = default)
    {
        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new KeyNotFoundException($"Negócio {id} não encontrado.");
        if (motivoPerdaId is not null && !await db.MotivosPerda.AnyAsync(m => m.Id == motivoPerdaId, ct))
            throw new RegraNegocioException("O motivo de perda informado não existe.");
        negocio.FechadoEm = DateTime.UtcNow;
        negocio.MotivoPerdaId = motivoPerdaId;
        negocio.StatusId = await ObterStatusPorTipoAsync(TipoStatusNegocio.Perdido, negocio.StatusId, ct);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = negocio.EmpresaId,
            RegistroId = negocio.Id,
            EntidadeAlvo = "Negocio",
            Gatilho = "StatusMudou",
            DisparadoPor = db.UsuarioAtual
        }, ct);
    }

    public async Task ReabrirAsync(long id, CancellationToken ct = default)
    {
        var negocio = await db.Negocios.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new KeyNotFoundException($"Negócio {id} não encontrado.");
        negocio.FechadoEm = null;
        negocio.MotivoPerdaId = null;
        negocio.StatusId = await ObterStatusPorTipoAsync(TipoStatusNegocio.Aberto, negocio.StatusId, ct);
        await db.SaveChangesAsync(ct);

        await publicador.PublicarEventoAsync(new EventoAutomacao
        {
            EmpresaId = negocio.EmpresaId,
            RegistroId = negocio.Id,
            EntidadeAlvo = "Negocio",
            Gatilho = "StatusMudou",
            DisparadoPor = db.UsuarioAtual
        }, ct);
    }

    /// <summary>
    /// Bloqueia o avanço (ou a troca de funil) quando a etapa ATUAL tem itens obrigatórios de checklist não concluídos.
    /// Voltar para uma etapa anterior (mesmo funil) não é bloqueado. Lança RegraNegocioException (HTTP 422).
    /// </summary>
    private async Task ValidarChecklistAoSairAsync(Negocio negocio, long etapaAnteriorId, long funilAnteriorId, CancellationToken ct)
    {
        if (negocio.EtapaId == etapaAnteriorId && negocio.FunilId == funilAnteriorId) return;
        if (negocio.FunilId == funilAnteriorId)
        {
            var ordens = await db.EtapasFunil.AsNoTracking()
                .Where(e => e.Id == etapaAnteriorId || e.Id == negocio.EtapaId)
                .ToDictionaryAsync(e => e.Id, e => e.Ordem, ct);
            if (ordens.TryGetValue(etapaAnteriorId, out var atual) && ordens.TryGetValue(negocio.EtapaId, out var destino) && destino <= atual)
                return;
        }
        await checklists.ValidarObrigatoriosAsync(negocio.Id, etapaAnteriorId, ct);
    }

    /// <summary>O responsável precisa ser um usuário ativo da mesma empresa (filtro global do EF).</summary>
    private async Task ValidarResponsavelAsync(long? responsavelId, CancellationToken ct)
    {
        if (responsavelId is null) return;
        if (!await db.Usuarios.AnyAsync(u => u.Id == responsavelId, ct))
            throw new RegraNegocioException("O responsável informado não existe.");
    }

    /// <summary>Status do tipo pedido (preferindo o padrão); se não existir, mantém o atual.</summary>
    private async Task<long?> ObterStatusPorTipoAsync(TipoStatusNegocio tipo, long? atual, CancellationToken ct)
    {
        var id = await db.StatusNegocios.AsNoTracking()
            .Where(s => s.TipoBase == tipo)
            .OrderByDescending(s => s.Padrao).ThenBy(s => s.Ordem)
            .Select(s => (long?)s.Id).FirstOrDefaultAsync(ct);
        return id ?? atual;
    }

    /// <summary>
    /// Valida a troca de etapa conforme as regras do funil. Lança RegraNegocioException (HTTP 422).
    /// "Etapa já alcançada" = etapa atual do negócio antes da mudança (o modelo não guarda histórico de etapas).
    /// - ProibirRetornoEtapa: destino com Ordem menor que a da etapa atual é proibido.
    /// - ExigirTodasEtapas: só pode avançar para a etapa imediatamente seguinte (menor Ordem maior que a atual).
    /// Mudança de funil só exige que a etapa pertença ao novo funil (as regras valem dentro do mesmo funil).
    /// </summary>
    private async Task ValidarMudancaDeEtapaAsync(Negocio negocio, long etapaAnteriorId, long funilAnteriorId, CancellationToken ct)
    {
        var destino = await db.EtapasFunil.AsNoTracking().FirstOrDefaultAsync(e => e.Id == negocio.EtapaId, ct);
        if (destino is null || destino.FunilId != negocio.FunilId)
            throw new RegraNegocioException("A etapa informada não pertence ao funil selecionado.");

        if (negocio.FunilId != funilAnteriorId || negocio.EtapaId == etapaAnteriorId) return;

        var funil = await db.Funis.AsNoTracking().FirstOrDefaultAsync(f => f.Id == negocio.FunilId, ct);
        if (funil is null || (!funil.ProibirRetornoEtapa && !funil.ExigirTodasEtapas)) return;

        var atual = await db.EtapasFunil.AsNoTracking().FirstOrDefaultAsync(e => e.Id == etapaAnteriorId, ct);
        if (atual is null) return;

        if (funil.ProibirRetornoEtapa && destino.Ordem < atual.Ordem)
            throw new RegraNegocioException(
                $"Este funil não permite retornar o negócio para uma etapa anterior (de \"{atual.Nome}\" para \"{destino.Nome}\").");

        if (funil.ExigirTodasEtapas && destino.Ordem > atual.Ordem)
        {
            var proximaOrdem = await db.EtapasFunil.AsNoTracking()
                .Where(e => e.FunilId == funil.Id && e.Ordem > atual.Ordem)
                .MinAsync(e => (int?)e.Ordem, ct);
            if (destino.Ordem != proximaOrdem)
                throw new RegraNegocioException(
                    $"Este funil exige passar por todas as etapas: não é possível pular de \"{atual.Nome}\" para \"{destino.Nome}\".");
        }
    }

    public IQueryable<StatusNegocio> ObterStatus()
        => db.StatusNegocios.AsNoTracking();

    public async Task<StatusNegocio> CriarStatusAsync(Delta<StatusNegocio> delta, CancellationToken ct = default)
    {
        var entidade = new StatusNegocio { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.StatusNegocios.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<StatusNegocio?> AtualizarStatusAsync(long id, Delta<StatusNegocio> delta, CancellationToken ct = default)
    {
        var entidade = await db.StatusNegocios.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverStatusAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.StatusNegocios.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<MotivoPerda> ObterMotivosPerda()
        => db.MotivosPerda.AsNoTracking();

    public async Task<MotivoPerda> CriarMotivoPerdaAsync(Delta<MotivoPerda> delta, CancellationToken ct = default)
    {
        var entidade = new MotivoPerda { EmpresaId = db.EmpresaIdAtual };
        delta.Patch(entidade);
        db.MotivosPerda.Add(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<MotivoPerda?> AtualizarMotivoPerdaAsync(long id, Delta<MotivoPerda> delta, CancellationToken ct = default)
    {
        var entidade = await db.MotivosPerda.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return null;
        delta.Patch(entidade);
        await db.SaveChangesAsync(ct);
        return entidade;
    }

    public async Task<bool> RemoverMotivoPerdaAsync(long id, CancellationToken ct = default)
    {
        var entidade = await db.MotivosPerda.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entidade is null) return false;
        entidade.Ativo = false;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<Funil> ObterFunis()
        => db.Funis.AsNoTracking();

    public IQueryable<EtapaFunil> ObterEtapas()
        => db.EtapasFunil.AsNoTracking();
}

