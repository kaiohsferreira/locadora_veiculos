using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Communication.ViewObjects.Vendas;
using System.Reflection;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Exposicao publica dos VOs das leituras de veiculo (D-14): por reflexao, os dois VOs publicos nunca ganham placa,
    /// chassi, renavam, idEmpresa, CNPJ nem endereco completo da locadora; a locadora so entra por nome fantasia, cidade e
    /// UF. Falha se alguem acrescentar um desses campos.
    /// </summary>
    public class VeiculoPublicoVOTests
    {
        private static readonly string[] NomesProibidosEmQualquerVOPublico = new string[]
        {
            "Placa", "Chassi", "Renavam", "IdEmpresa", "Cnpj", "Logradouro", "Numero", "Complemento", "Bairro", "Cep", "Email", "Telefone"
        };

        private static readonly string[] CamposDaLocadora = new string[] { "LocadoraNomeFantasia", "LocadoraCidade", "LocadoraUf" };

        private static List<string> NomesPublicos(Type tipo)
        {
            return tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(propriedade => propriedade.Name)
                .ToList();
        }

        private static List<string> NomesQueContem(List<string> nomes, string[] trechos)
        {
            return nomes.Where(nome => trechos.Any(trecho => nome.Contains(trecho, StringComparison.Ordinal))).ToList();
        }

        [Fact]
        public void ReturnVeiculoDisponivelVO_NaoExpoeCamposSensiveisDaLocadoraNemDoVeiculo()
        {
            List<string> nomes = NomesPublicos(typeof(ReturnVeiculoDisponivelVO));

            Assert.Empty(NomesQueContem(nomes, NomesProibidosEmQualquerVOPublico));
            Assert.DoesNotContain("KmAtual", nomes);
            Assert.DoesNotContain("ValorVenda", nomes);
            Assert.DoesNotContain("Status", nomes);
        }

        [Fact]
        public void ReturnVeiculoCatalogoVO_NaoExpoeCamposSensiveisNemDiaria()
        {
            List<string> nomes = NomesPublicos(typeof(ReturnVeiculoCatalogoVO));

            Assert.Empty(NomesQueContem(nomes, NomesProibidosEmQualquerVOPublico));
            Assert.DoesNotContain("ValorDiaria", nomes);
            Assert.DoesNotContain("Status", nomes);
        }

        [Fact]
        public void OsDoisVOsPublicosTraemNomeCidadeEUfDaLocadora()
        {
            List<string> nomesDisponiveis = NomesPublicos(typeof(ReturnVeiculoDisponivelVO));
            List<string> nomesCatalogo = NomesPublicos(typeof(ReturnVeiculoCatalogoVO));

            Assert.All(CamposDaLocadora, campo => Assert.Contains(campo, nomesDisponiveis));
            Assert.All(CamposDaLocadora, campo => Assert.Contains(campo, nomesCatalogo));
        }

        [Fact]
        public void ReturnVeiculoCatalogoVO_MantemAQuilometragem()
        {
            List<string> nomes = NomesPublicos(typeof(ReturnVeiculoCatalogoVO));

            Assert.Contains("KmAtual", nomes);
        }

        [Fact]
        public void VOsPublicosEDeReserva_NaoExpoemFornecedorNemNotaFiscalDeCompra()
        {
            Type[] tipos = new Type[]
            {
                typeof(ReturnVeiculoDisponivelVO),
                typeof(ReturnVeiculoCatalogoVO),
                typeof(ReturnReservaVO),
                typeof(ReturnReservaCriadaVO)
            };

            foreach (Type tipo in tipos)
            {
                List<string> nomes = NomesPublicos(tipo);

                Assert.DoesNotContain(nameof(ReturnVeiculoVO.Fornecedor), nomes);
                Assert.DoesNotContain(nameof(ReturnVeiculoVO.NumeroNotaFiscalCompra), nomes);
            }
        }
    }
}
