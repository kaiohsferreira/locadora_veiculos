using AutoVia.Communication.CustomExceptions;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Gateway PIX de teste: registra cada solicitacao e devolve o que o teste prepara em Responder (sem rede e sem provedor).
    /// O padrao devolve uma cobranca AGUARDANDO_PAGAMENTO com copia e cola, QR e validade de 900 s, no mesmo valor e referencia pedidos.
    /// </summary>
    public class FakePixGateway : IPixGateway
    {
        /// <summary>Solicitacoes recebidas, na ordem (inclusive as repeticoes).</summary>
        public List<PixCobrancaSolicitacao> Solicitacoes { get; } = new List<PixCobrancaSolicitacao>();

        /// <summary>Relogio do provedor de teste: a validade devolvida parte dele (o teste informa o mesmo "agora" do relogio do servico).</summary>
        public DateTime Agora { get; set; } = DateTime.Now;

        /// <summary>Quem decide o retorno de cada chamada.</summary>
        public Func<PixCobrancaSolicitacao, PixCobrancaResultado> Responder { get; set; } = null!;

        /// <summary>Identificadores pedidos ao cancelamento no provedor, na ordem.</summary>
        public List<string> Cancelamentos { get; } = new List<string>();

        /// <summary>Quando verdadeiro, o cancelamento no provedor falha (excecao do gateway).</summary>
        public bool FalharCancelamento { get; set; }

        /// <summary>Retorno do cancelamento (ex.: EXPIRADA ou AGUARDANDO_PAGAMENTO). Sem ele, devolve CANCELADA.</summary>
        public Func<string, PixCobrancaResultado>? RespostaCancelamento { get; set; }

        /// <summary>Excecao que o cancelamento lanca (ex.: BusinessException de cobranca paga ou NotFoundException).</summary>
        public Exception? ExcecaoCancelamento { get; set; }

        /// <summary>Gancho chamado dentro do cancelamento, antes do retorno (simula outra requisicao no meio da chamada ao provedor).</summary>
        public Action<string>? AoCancelar { get; set; }

        /// <summary>Retorno padrao: aguardando pagamento, com a referencia e o valor pedidos e validade de 900 s a partir de Agora.</summary>
        public PixCobrancaResultado RetornoAguardando(PixCobrancaSolicitacao solicitacao)
        {
            return new PixCobrancaResultado
            {
                IdentificadorProvedor = "ID-" + solicitacao.ReferenciaExterna,
                ReferenciaExterna = solicitacao.ReferenciaExterna,
                Valor = solicitacao.Valor,
                Status = PixStatusCobranca.AGUARDANDO_PAGAMENTO,
                CopiaECola = "COPIA-" + solicitacao.ReferenciaExterna,
                QrCodeBase64 = "QRBASE64-" + solicitacao.ReferenciaExterna,
                ExpiraEm = Agora.AddSeconds(900),
                Provedor = "FAKE"
            };
        }

        public FakePixGateway()
        {
            Responder = RetornoAguardando;
        }

        public Task<PixCobrancaResultado> CriarCobrancaAsync(PixCobrancaSolicitacao solicitacao, CancellationToken cancellationToken = default)
        {
            Solicitacoes.Add(solicitacao);

            return Task.FromResult(Responder(solicitacao));
        }

        public Task<PixCobrancaResultado> ConsultarCobrancaAsync(string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<PixCobrancaResultado> CancelarCobrancaAsync(string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            Cancelamentos.Add(identificadorProvedor);
            AoCancelar?.Invoke(identificadorProvedor);

            if (ExcecaoCancelamento != null)
            {
                throw ExcecaoCancelamento;
            }

            if (FalharCancelamento)
            {
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroPixProvedorIndisponivel);
            }

            if (RespostaCancelamento != null)
            {
                return Task.FromResult(RespostaCancelamento(identificadorProvedor));
            }

            return Task.FromResult(new PixCobrancaResultado
            {
                IdentificadorProvedor = identificadorProvedor,
                Status = PixStatusCobranca.CANCELADA,
                Provedor = ConstantsPix.ProvedorFake
            });
        }
    }
}
