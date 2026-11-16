using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Multas;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>Abstracao do provedor usado pela integracao MULTAS.</summary>
    public interface IMultasProvider
    {
        string Nome { get; }
        Task<MultasConsultaResultado> ConsultarInfracoesAsync(
            ConfiguracaoIntegracao configuracao,
            MultasConsultaSolicitacao solicitacao,
            CancellationToken cancellationToken = default);
    }
}