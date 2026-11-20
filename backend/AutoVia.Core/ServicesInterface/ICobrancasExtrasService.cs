using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.ServicesInterface;

public interface ICobrancasExtrasService
{
    Task<ReturnSimulacaoCobrancasExtrasVO> SimularAsync(int idLocacao, SimularCobrancasExtrasVO model);
    ReturnSimulacaoCobrancasExtrasVO Calcular(Locacao locacao, Vistoria? entrada, Vistoria? saida,
        List<Avaria> snapshots, SimularCobrancasExtrasVO model);
}
