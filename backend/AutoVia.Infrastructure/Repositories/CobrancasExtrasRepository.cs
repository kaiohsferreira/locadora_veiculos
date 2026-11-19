using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

namespace AutoVia.Infrastructure.Repositories;

public sealed class CobrancasExtrasRepository : ICobrancasExtrasRepository
{
    private const string SqlEmpresa = "SELECT id_empresa FROM empresa WHERE id_empresa = @id AND disabled_at IS NULL AND ativo = TRUE FOR UPDATE";
    private const string SqlLocacao = "SELECT id_locacao FROM locacao WHERE id_locacao = @id AND disabled_at IS NULL FOR UPDATE";
    private const string SqlConfiguracao = "SELECT id_configuracao FROM configuracao_cobranca_extra WHERE id_configuracao = @id AND disabled_at IS NULL FOR SHARE";
    private const string ParametroId = "id";
    private readonly AutoViaDbContext _db;
    private readonly IContextoEmpresa _contexto;
    private readonly ILogService _logService;

    public CobrancasExtrasRepository(AutoViaDbContext db, IContextoEmpresa contexto, ILogService logService)
    {
        _db = db;
        _contexto = contexto;
        _logService = logService;
    }

    public IQueryable<ConfiguracaoCobrancaExtra> ConsultaConfiguracoesAtivas()
    {
        try
        {
            return _db.ConfiguracoesCobrancasExtras.AsNoTracking()
                .Where(x => x.DisabledAt == null && x.IdEmpresa == _contexto.IdEmpresa && _contexto.IdEmpresa > 0)
                .Include(x => x.Categoria);
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public IQueryable<Locacao> ConsultaLocacao(int idLocacao)
    {
        try
        {
            return _db.Locacoes.AsNoTracking().Where(x => x.IdLocacao == idLocacao && x.DisabledAt == null &&
                    x.Veiculo.DisabledAt == null && x.Veiculo.IdEmpresa == _contexto.IdEmpresa && _contexto.IdEmpresa > 0)
                .Include(x => x.Veiculo).Include(x => x.Vistorias.Where(v => v.DisabledAt == null))
                .ThenInclude(x => x.Avarias.Where(a => a.DisabledAt == null)).AsSingleQuery();
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<List<ConfiguracaoCobrancaExtra>> ListarConfiguracoesAsync(int? idCategoria, int deslocamento, int tamanho)
    {
        try
        {
            return await ConsultaConfiguracoesAtivas().Where(x => !idCategoria.HasValue || x.IdCategoria == idCategoria.Value)
                .OrderBy(x => x.Categoria.Nome).ThenBy(x => x.IdConfiguracao).Skip(deslocamento).Take(tamanho).ToListAsync();
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<int> ContarConfiguracoesAsync(int? idCategoria)
    {
        try { return await ConsultaConfiguracoesAtivas().CountAsync(x => !idCategoria.HasValue || x.IdCategoria == idCategoria.Value); }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<ConfiguracaoCobrancaExtra?> BuscarConfiguracaoAsync(int id)
    {
        try { return await ConsultaConfiguracoesAtivas().FirstOrDefaultAsync(x => x.IdConfiguracao == id); }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<ConfiguracaoCobrancaExtra?> BuscarConfiguracaoCategoriaAsync(int idCategoria, bool bloquear)
    {
        try
        {
            List<ConfiguracaoCobrancaExtra> configuracoes = await ConsultaConfiguracoesAtivas()
                .Where(x => x.IdCategoria == idCategoria && x.Categoria.DisabledAt == null).ToListAsync();
            if (configuracoes.Count > 1) throw new BusinessException(ConstantsMessage.ErroConfiguracaoCobrancasExtrasDuplicada);
            ConfiguracaoCobrancaExtra? configuracao = configuracoes.SingleOrDefault();
            if (bloquear && configuracao != null)
            {
                if (!await BloquearAsync(SqlConfiguracao, configuracao.IdConfiguracao)) return null;
                configuracao = await BuscarConfiguracaoAsync(configuracao.IdConfiguracao);
                if (configuracao?.IdCategoria != idCategoria) return null;
            }
            return configuracao;
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<bool> ExisteConfiguracaoAsync(int idCategoria, int? ignorarId)
    {
        try { return await ConsultaConfiguracoesAtivas().AnyAsync(x => x.IdCategoria == idCategoria && (!ignorarId.HasValue || x.IdConfiguracao != ignorarId.Value)); }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<ConfiguracaoCobrancaExtra> InserirConfiguracaoAsync(ConfiguracaoCobrancaExtra configuracao)
    {
        try
        {
            _db.ConfiguracoesCobrancasExtras.Add(configuracao);
            await _db.SaveChangesAsync();
            return configuracao;
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task SalvarConfiguracaoAsync(ConfiguracaoCobrancaExtra configuracao)
    {
        try
        {
            // A entidade veio de consulta sem rastreamento; não anexar a categoria carregada para exibição.
            _db.Entry(configuracao).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<T> ComEmpresaBloqueadaAsync<T>(Func<Task<T>> operacao)
    {
        try
        {
            if (_contexto.IdEmpresa <= 0 || !await _db.Empresas.AsNoTracking().AnyAsync(x =>
                    x.IdEmpresa == _contexto.IdEmpresa && x.DisabledAt == null && x.Ativo))
                throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            await using IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            if (!await BloquearAsync(SqlEmpresa, _contexto.IdEmpresa)) throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            T resultado = await operacao();
            await transacao.CommitAsync();
            return resultado;
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<T> ComLocacaoBloqueadaAsync<T>(int idLocacao, Func<Task<T>> operacao)
    {
        try
        {
            if (!await ConsultaLocacao(idLocacao).AnyAsync()) throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
            await using IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            if (!await BloquearAsync(SqlLocacao, idLocacao) || !await ConsultaLocacao(idLocacao).AnyAsync())
                throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
            T resultado = await operacao();
            await transacao.CommitAsync();
            return resultado;
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    private async Task<bool> BloquearAsync(string sql, int id)
    {
        try
        {
            DbConnection conexao = _db.Database.GetDbConnection();
            await using DbCommand comando = conexao.CreateCommand();
            comando.CommandText = sql;
            comando.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
            DbParameter parametro = comando.CreateParameter();
            parametro.ParameterName = ParametroId;
            parametro.Value = id;
            comando.Parameters.Add(parametro);
            object? resultado = await comando.ExecuteScalarAsync();
            return resultado != null && resultado != DBNull.Value;
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<Locacao?> BuscarLocacaoAsync(int idLocacao)
    {
        try { return await ConsultaLocacao(idLocacao).FirstOrDefaultAsync(); }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<ApuracaoCobrancaExtra?> BuscarApuracaoAsync(int idLocacao)
    {
        try
        {
            // Não carregar a configuração atual: uma política desativada não pode esconder o histórico.
            return await _db.ApuracoesCobrancasExtras.AsNoTracking().FirstOrDefaultAsync(x =>
                x.IdLocacao == idLocacao && x.DisabledAt == null && x.Locacao.DisabledAt == null);
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<bool> ExistemCobrancasConflitantesAsync(int idLocacao)
    {
        try
        {
            return await _db.CobrancasExtras.AsNoTracking().AnyAsync(x => x.IdLocacao == idLocacao && x.DisabledAt == null &&
                x.IdApuracao == null && (x.Tipo == TipoCobrancaExtra.KM_EXCEDENTE || x.Tipo == TipoCobrancaExtra.COMBUSTIVEL ||
                x.Tipo == TipoCobrancaExtra.ATRASO || x.Tipo == TipoCobrancaExtra.AVARIA));
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<ApuracaoCobrancaExtra> InserirApuracaoAsync(ApuracaoCobrancaExtra apuracao)
    {
        try
        {
            _db.ApuracoesCobrancasExtras.Add(apuracao);
            await _db.SaveChangesAsync();
            return apuracao;
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task SalvarSnapshotAsync(ApuracaoCobrancaExtra apuracao)
    {
        try { await _db.SaveChangesAsync(); }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }
}
