using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.ServicesInterface;
using System.Linq;
using System.Reflection;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Os VOs de foto do veiculo tem exatamente os campos do contrato e nenhum deles leva chave do objeto, id da locadora
    /// ou id de funcionario (prova por reflexao).
    /// </summary>
    public sealed class VeiculoFotoVOTests
    {
        private static readonly string[] CamposProibidos = { "Chave", "Empresa", "Funcionario" };

        private static string[] NomesDasPropriedades(Type tipo)
        {
            return tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(propriedade => propriedade.Name)
                .OrderBy(nome => nome, StringComparer.Ordinal)
                .ToArray();
        }

        [Fact]
        public void ReturnVeiculoFotoVO_TemExatamenteOsCamposDoContrato()
        {
            string[] esperados = { "ExpiraEm", "IdVeiculoFoto", "Ordem", "Url", "UrlMiniatura" };

            Assert.Equal(esperados, NomesDasPropriedades(typeof(ReturnVeiculoFotoVO)));
        }

        [Fact]
        public void FotoCapaVeiculoVO_TemExatamenteOsCamposDoContrato()
        {
            string[] esperados = { "ExpiraEm", "Url", "UrlMiniatura" };

            Assert.Equal(esperados, NomesDasPropriedades(typeof(FotoCapaVeiculoVO)));
        }

        [Fact]
        public void VOsDeFoto_NaoTemChaveDoObjetoIdDaLocadoraNemIdDeFuncionario()
        {
            Type[] tipos = { typeof(ReturnVeiculoFotoVO), typeof(FotoCapaVeiculoVO) };

            foreach (Type tipo in tipos)
            {
                foreach (string nome in NomesDasPropriedades(tipo))
                {
                    Assert.DoesNotContain(CamposProibidos, proibido => nome.Contains(proibido, StringComparison.Ordinal));
                    Assert.False(nome.Equals("IdEmpresa", StringComparison.Ordinal));
                }
            }
        }

        [Fact]
        public void UrlMiniatura_EhAnulavelEReservadaParaAFatiaCompleta()
        {
            PropertyInfo? miniatura = typeof(ReturnVeiculoFotoVO).GetProperty(nameof(ReturnVeiculoFotoVO.UrlMiniatura));

            Assert.NotNull(miniatura);
            Assert.Equal(typeof(string), miniatura.PropertyType);
            Assert.Null(new ReturnVeiculoFotoVO().UrlMiniatura);
        }

        [Fact]
        public void EnviarFotoVeiculoRequest_TemSoAPropriedadeFoto()
        {
            string[] nomes = NomesDasPropriedades(typeof(AutoVia.Api.Requests.VeiculoFoto.EnviarFotoVeiculoRequest));

            Assert.Equal(new string[] { "Foto" }, nomes);
        }

        [Fact]
        public void IVeiculoFotoService_NaoRecebeLocadoraNemFuncionarioComoParametro()
        {
            foreach (MethodInfo metodo in typeof(IVeiculoFotoService).GetMethods())
            {
                foreach (ParameterInfo parametro in metodo.GetParameters())
                {
                    string nome = parametro.Name ?? string.Empty;
                    Assert.DoesNotContain(CamposProibidos, proibido => nome.Contains(proibido, StringComparison.OrdinalIgnoreCase));
                }
            }
        }
    }
}
