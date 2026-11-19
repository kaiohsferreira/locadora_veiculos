using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;

namespace AutoVia.Tests.Fakes;

public sealed class FakeCobrancasExtrasRepository : ICobrancasExtrasRepository
{
    private readonly IContextoEmpresa _contexto;
    private readonly SemaphoreSlim _trava = new SemaphoreSlim(1, 1);
    public List<ConfiguracaoCobrancaExtra> Configuracoes { get; } = new List<ConfiguracaoCobrancaExtra>();
    public List<Locacao> Locacoes { get; } = new List<Locacao>();
    public List<ApuracaoCobrancaExtra> Apuracoes { get; } = new List<ApuracaoCobrancaExtra>();
    public bool ConflitoManual { get; set; }
    public bool FalharSnapshot { get; set; }
    public int Insercoes { get; private set; }
    public FakeCobrancasExtrasRepository(IContextoEmpresa contexto) { _contexto = contexto; }
    private IEnumerable<ConfiguracaoCobrancaExtra> Ativas => Configuracoes.Where(x => x.DisabledAt == null && x.IdEmpresa == _contexto.IdEmpresa && _contexto.IdEmpresa > 0);
    public Task<List<ConfiguracaoCobrancaExtra>> ListarConfiguracoesAsync(int? idCategoria, int deslocamento, int tamanho) =>
        Task.FromResult(Ativas.Where(x => !idCategoria.HasValue || x.IdCategoria == idCategoria).Skip(deslocamento).Take(tamanho).ToList());
    public Task<int> ContarConfiguracoesAsync(int? idCategoria) => Task.FromResult(Ativas.Count(x => !idCategoria.HasValue || x.IdCategoria == idCategoria));
    public Task<ConfiguracaoCobrancaExtra?> BuscarConfiguracaoAsync(int id) => Task.FromResult(Ativas.FirstOrDefault(x => x.IdConfiguracao == id));
    public Task<ConfiguracaoCobrancaExtra?> BuscarConfiguracaoCategoriaAsync(int idCategoria, bool bloquear) => Task.FromResult(Ativas.SingleOrDefault(x => x.IdCategoria == idCategoria));
    public Task<bool> ExisteConfiguracaoAsync(int idCategoria, int? ignorarId) => Task.FromResult(Ativas.Any(x => x.IdCategoria == idCategoria && x.IdConfiguracao != ignorarId));
    public Task<ConfiguracaoCobrancaExtra> InserirConfiguracaoAsync(ConfiguracaoCobrancaExtra configuracao)
    { configuracao.IdConfiguracao = Configuracoes.Count + 1; Configuracoes.Add(configuracao); return Task.FromResult(configuracao); }
    public Task SalvarConfiguracaoAsync(ConfiguracaoCobrancaExtra configuracao) => Task.CompletedTask;
    public async Task<T> ComEmpresaBloqueadaAsync<T>(Func<Task<T>> operacao)
    {
        await _trava.WaitAsync();
        try { return await operacao(); }
        finally { _trava.Release(); }
    }
    public async Task<T> ComLocacaoBloqueadaAsync<T>(int idLocacao, Func<Task<T>> operacao)
    {
        await _trava.WaitAsync();
        int anteriores = Apuracoes.Count;
        try
        {
            if (await BuscarLocacaoAsync(idLocacao) == null) throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
            return await operacao();
        }
        catch
        {
            // Restaura a coleção do fake: representa o contrato transacional, não testa PostgreSQL.
            List<ApuracaoCobrancaExtra> preservadas = Apuracoes.Take(anteriores).ToList();
            Apuracoes.Clear(); Apuracoes.AddRange(preservadas);
            throw;
        }
        finally { _trava.Release(); }
    }
    public Task<Locacao?> BuscarLocacaoAsync(int idLocacao) => Task.FromResult(Locacoes.FirstOrDefault(x =>
        x.IdLocacao == idLocacao && x.DisabledAt == null && x.Veiculo.DisabledAt == null && x.Veiculo.IdEmpresa == _contexto.IdEmpresa && _contexto.IdEmpresa > 0));
    public async Task<ApuracaoCobrancaExtra?> BuscarApuracaoAsync(int idLocacao) =>
        await BuscarLocacaoAsync(idLocacao) == null ? null : Apuracoes.FirstOrDefault(x => x.IdLocacao == idLocacao && x.DisabledAt == null);
    public Task<bool> ExistemCobrancasConflitantesAsync(int idLocacao) => Task.FromResult(ConflitoManual);
    public Task<ApuracaoCobrancaExtra> InserirApuracaoAsync(ApuracaoCobrancaExtra apuracao)
    {
        Insercoes++; apuracao.IdApuracao = Apuracoes.Count + 1;
        int id = 1;
        foreach (CobrancaExtra cobranca in apuracao.Cobrancas) { cobranca.IdCobranca = id++; cobranca.IdApuracao = apuracao.IdApuracao; }
        Apuracoes.Add(apuracao); return Task.FromResult(apuracao);
    }
    public Task SalvarSnapshotAsync(ApuracaoCobrancaExtra apuracao)
    {
        if (FalharSnapshot) throw new InvalidOperationException(ConstantsMessage.ErroApuracaoCobrancasExtras);
        return Task.CompletedTask;
    }
}
