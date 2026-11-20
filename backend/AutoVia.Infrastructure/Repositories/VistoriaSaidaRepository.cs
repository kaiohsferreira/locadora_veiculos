using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Infrastructure.Repositories;

public sealed class VistoriaSaidaRepository : IVistoriaSaidaRepository
{
    private readonly AutoViaDbContext _db;
    private readonly ILogService _logService;

    public VistoriaSaidaRepository(AutoViaDbContext db, ILogService logService)
    {
        _db = db;
        _logService = logService;
    }

    internal IQueryable<Vistoria> ConsultaSaidaAtiva(int idLocacao)
    {
        try
        {
            return _db.Vistorias.AsNoTracking().Where(x => x.IdLocacao == idLocacao &&
                x.Tipo == TipoVistoria.SAIDA && x.DisabledAt == null && x.Locacao.DisabledAt == null);
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroBuscarVistoriaSaida, GetType().ToString());
            throw;
        }
    }

    internal IQueryable<Avaria> ConsultaAvariasAtivas(int idVistoria)
    {
        try
        {
            return _db.Avarias.AsNoTracking().Where(x => x.IdVistoria == idVistoria &&
                x.DisabledAt == null && x.Vistoria.DisabledAt == null && x.Vistoria.Locacao.DisabledAt == null)
                .OrderBy(x => x.IdAvaria);
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroBuscarVistoriaSaida, GetType().ToString());
            throw;
        }
    }

    public async Task<Vistoria?> FindSaidaByLocacaoAsync(int idLocacao)
    {
        try
        {
            Vistoria? saida = await ConsultaSaidaAtiva(idLocacao).FirstOrDefaultAsync();
            return saida;
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroBuscarVistoriaSaida, GetType().ToString());
            throw;
        }
    }

    public async Task<List<Avaria>> FindAvariasAtivasAsync(int idVistoria)
    {
        try
        {
            List<Avaria> avarias = await ConsultaAvariasAtivas(idVistoria).ToListAsync();
            return avarias;
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroBuscarVistoriaSaida, GetType().ToString());
            throw;
        }
    }

    public async Task<Vistoria> InsertAsync(Vistoria vistoria)
    {
        try
        {
            vistoria.IdVistoria = 0;
            _db.Vistorias.Add(vistoria);
            await _db.SaveChangesAsync();
            return vistoria;
        }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroSalvarVistoriaSaida, GetType().ToString());
            throw;
        }
    }
}
