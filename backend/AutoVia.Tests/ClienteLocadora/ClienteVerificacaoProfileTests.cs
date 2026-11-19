using AutoMapper;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Perfil da fila e do detalhe de verificacao (P2-BE-05): a configuracao do AutoMapper e valida (todo destino mapeado) e
    /// as duas mensagens do 409 da reserva terminam com o codigo da regra RP-02 (decisao 6 do Kaio, 19/11/2026).
    /// </summary>
    public class ClienteVerificacaoProfileTests
    {
        private const string SufixoRegra = " (RP-02)";

        [Fact]
        public void ConfiguracaoDoPerfilEValida()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<ClienteVerificacaoProfile>(), NullLoggerFactory.Instance);

            configuracao.AssertConfigurationIsValid();
        }

        [Fact]
        public void MensagemDeCadastroEmAnaliseTerminaComRP02()
        {
            Assert.EndsWith(SufixoRegra, ConstantsMessage.ErroReservaCadastroEmAnalise, StringComparison.Ordinal);
        }

        [Fact]
        public void MensagemDeCadastroRecusadoTerminaComRP02()
        {
            Assert.EndsWith(SufixoRegra, ConstantsMessage.ErroReservaCadastroRecusado, StringComparison.Ordinal);
        }

        [Fact]
        public void ReturnDoLoginNaoEsperaMaisStatusCadastro()
        {
            Assert.Null(typeof(ReturnLoginVO).GetProperty("StatusCadastro"));
        }

        [Fact]
        public void VoDoDetalheHerdaOsCamposDaFilaEAcrescentaOsDeCadastro()
        {
            Assert.NotNull(typeof(ReturnClienteVerificacaoVO).GetProperty(nameof(ReturnClienteFilaVO.Cpf)));
            Assert.NotNull(typeof(ReturnClienteVerificacaoVO).GetProperty(nameof(ReturnClienteVerificacaoVO.CnhNumero)));
            Assert.Equal(typeof(ReturnClienteFilaVO), typeof(ReturnClienteVerificacaoVO).BaseType);
        }
    }
}
