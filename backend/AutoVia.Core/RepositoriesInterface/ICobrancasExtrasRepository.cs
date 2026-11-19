using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.RepositoriesInterface;

public interface ICobrancasExtrasRepository
{
    Task<List<ConfiguracaoCobrancaExtra>> ListarConfiguracoesAsync(int? idCategoria, int deslocamento, int tamanho);
    Task<int> ContarConfiguracoesAsync(int? idCategoria);
    Task<ConfiguracaoCobrancaExtra?> BuscarConfiguracaoAsync(int id);
    Task<ConfiguracaoCobrancaExtra?> BuscarConfiguracaoCategoriaAsync(int idCategoria, bool bloquear);
    Task<bool> ExisteConfiguracaoAsync(int idCategoria, int? ignorarId);
    Task<ConfiguracaoCobrancaExtra> InserirConfiguracaoAsync(ConfiguracaoCobrancaExtra configuracao);
    Task SalvarConfiguracaoAsync(ConfiguracaoCobrancaExtra configuracao);
    Task<T> ComEmpresaBloqueadaAsync<T>(Func<Task<T>> operacao);
    Task<T> ComLocacaoBloqueadaAsync<T>(int idLocacao, Func<Task<T>> operacao);
    Task<Locacao?> BuscarLocacaoAsync(int idLocacao);
    Task<ApuracaoCobrancaExtra?> BuscarApuracaoAsync(int idLocacao);
    Task<bool> ExistemCobrancasConflitantesAsync(int idLocacao);
    Task<ApuracaoCobrancaExtra> InserirApuracaoAsync(ApuracaoCobrancaExtra apuracao);
    Task SalvarSnapshotAsync(ApuracaoCobrancaExtra apuracao);
}
