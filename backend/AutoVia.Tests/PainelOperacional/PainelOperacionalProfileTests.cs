using AutoMapper;
using AutoVia.Core.Profiles;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.PainelOperacional
{
    /// <summary>Profile do painel operacional: a configuracao do AutoMapper precisa ser valida, com codigoReserva mapeado nos dois maps.</summary>
    public class PainelOperacionalProfileTests
    {
        [Fact]
        public void PainelOperacionalProfile_ConfiguracaoEValida()
        {
            MapperConfiguration configuracao = new MapperConfiguration(
                opcoes => opcoes.AddProfile<PainelOperacionalProfile>(),
                NullLoggerFactory.Instance);

            configuracao.AssertConfigurationIsValid();
        }
    }
}
