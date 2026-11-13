using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Caucao;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Provedor concreto de caucao (MERCADOPAGO, FAKE). Escolhido pelo CaucaoGateway pelo valor
    /// da chave 'provedor' da integracao PAGAMENTO. Os metodos nao aplicam regra de estado: isso
    /// e do gateway. Cartao recusado na pre-autorizacao lanca BusinessException aqui.
    /// </summary>
    public interface ICaucaoProvider
    {
        /// <summary>Nome do provedor (ConstantsCaucao.ProvedorMercadoPago ou ProvedorFake).</summary>
        string Nome { get; }

        /// <summary>Formato do identificador deste provedor (ex.: numerico no Mercado Pago, FAKE-CAU-... no FAKE). Chamado antes de qualquer chamada externa.</summary>
        bool IdentificadorValido(string identificadorProvedor);

        public Task<CaucaoResultado> PreAutorizarAsync(ConfiguracaoIntegracao configuracao, CaucaoSolicitacao solicitacao, string chaveIdempotencia, int validadeReservaDias, CancellationToken cancellationToken = default);

        public Task<CaucaoResultado> ConsultarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default);

        public Task<CaucaoResultado> CapturarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, decimal? valor, CancellationToken cancellationToken = default);

        public Task<CaucaoResultado> CancelarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default);
    }
}
