using AutoVia.Integrations.Communication.Multas;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>Gateway interno de consulta de infracoes; nao persiste cobrancas.</summary>
    public interface IMultasGateway
    {
        Task<MultasConsultaResultado> ConsultarInfracoesAsync(
            MultasConsultaSolicitacao solicitacao,
            CancellationToken cancellationToken = default);
    }
}