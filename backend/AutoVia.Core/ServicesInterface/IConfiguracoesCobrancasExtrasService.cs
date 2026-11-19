using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Core.ServicesInterface;

public interface IConfiguracoesCobrancasExtrasService
{
    Task<PaginaVO<ReturnConfiguracaoCobrancaExtraVO>> ListarAsync(FiltroConfiguracaoCobrancaExtraVO filtro);
    Task<ReturnConfiguracaoCobrancaExtraVO> BuscarAsync(int id);
    Task<ReturnConfiguracaoCobrancaExtraVO> InserirAsync(ConfiguracaoCobrancaExtraVO model);
    Task AtualizarAsync(int id, ConfiguracaoCobrancaExtraVO model);
    Task DesativarAsync(int id);
}
