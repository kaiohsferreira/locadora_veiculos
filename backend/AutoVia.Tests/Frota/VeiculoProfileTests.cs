using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Testes do VeiculoProfile (Veiculo -> ReturnVeiculoVO): categoria achatada, anos em int,
    /// valores e datas formatados, e campos opcionais nulos viram string vazia.
    /// </summary>
    public class VeiculoProfileTests
    {
        [Fact]
        public void MapeiaVeiculoCompleto_ComCategoriaAnosFormatosEStatus()
        {
            IMapper mapper = CriarMapper();
            Categoria categoria = new Categoria { IdCategoria = 3, Nome = "SUV", ValorDiariaBase = 200m };
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 10,
                IdCategoria = 3,
                Categoria = categoria,
                Placa = "ABC1D23",
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                Cor = "Branco",
                KmAtual = 15000,
                ValorDiaria = 150.5m,
                ValorVenda = 45000m,
                Status = StatusVeiculo.LOCADO,
                DataAquisicao = new DateOnly(2022, 3, 10)
            };

            ReturnVeiculoVO retorno = mapper.Map<ReturnVeiculoVO>(veiculo);

            Assert.Equal("SUV", retorno.CategoriaNome);
            Assert.Equal(2022, retorno.AnoFabricacao);
            Assert.Equal(2023, retorno.AnoModelo);
            Assert.Equal(StaticMethods.FormateToMonetary(150.5m), retorno.ValorDiariaStr);
            Assert.Equal(StaticMethods.FormateToMonetary(45000m), retorno.ValorVendaStr);
            Assert.Equal("10/03/2022", retorno.DataAquisicaoStr);
            Assert.Equal(StatusVeiculo.LOCADO, retorno.Status);
        }

        [Fact]
        public void FornecedorENotaFiscalDeCompra_VaoParaORetornoEFicamNulosQuandoVazios()
        {
            IMapper mapper = CriarMapper();
            Categoria categoria = new Categoria { IdCategoria = 3, Nome = "SUV", ValorDiariaBase = 200m };
            Veiculo comDados = new Veiculo
            {
                IdVeiculo = 12,
                IdCategoria = 3,
                Categoria = categoria,
                Modelo = "Toro",
                Fornecedor = "Locadora Atlas Motors",
                NumeroNotaFiscalCompra = "000.123-4"
            };
            Veiculo semDados = new Veiculo
            {
                IdVeiculo = 13,
                IdCategoria = 3,
                Categoria = categoria,
                Modelo = "Toro"
            };

            ReturnVeiculoVO retornoComDados = mapper.Map<ReturnVeiculoVO>(comDados);
            ReturnVeiculoVO retornoSemDados = mapper.Map<ReturnVeiculoVO>(semDados);

            Assert.Equal("Locadora Atlas Motors", retornoComDados.Fornecedor);
            Assert.Equal("000.123-4", retornoComDados.NumeroNotaFiscalCompra);
            Assert.Null(retornoSemDados.Fornecedor);
            Assert.Null(retornoSemDados.NumeroNotaFiscalCompra);
        }

        [Fact]
        public void ValorVendaEDataAquisicaoNulos_ViramStringVazia()
        {
            IMapper mapper = CriarMapper();
            Categoria categoria = new Categoria { IdCategoria = 3, Nome = "Economico", ValorDiariaBase = 80m };
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 11,
                IdCategoria = 3,
                Categoria = categoria,
                Placa = "XYZ9A87",
                ValorDiaria = 80m,
                ValorVenda = null,
                DataAquisicao = null
            };

            ReturnVeiculoVO retorno = mapper.Map<ReturnVeiculoVO>(veiculo);

            Assert.Equal(string.Empty, retorno.ValorVendaStr);
            Assert.Equal(string.Empty, retorno.DataAquisicaoStr);
            Assert.Null(retorno.ValorVenda);
            Assert.Null(retorno.DataAquisicao);
        }

        [Fact]
        public void LocadoraDaNavegacao_VaiParaONomeCidadeEUfDoRetorno()
        {
            IMapper mapper = CriarMapper();
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 12,
                Placa = "LOC1A23",
                Empresa = new Empresa { IdEmpresa = 2, NomeFantasia = "Rota Sul", Cidade = "Campinas", Uf = "SP" }
            };

            ReturnVeiculoVO retorno = mapper.Map<ReturnVeiculoVO>(veiculo);

            Assert.Equal("Rota Sul", retorno.LocadoraNomeFantasia);
            Assert.Equal("Campinas", retorno.LocadoraCidade);
            Assert.Equal("SP", retorno.LocadoraUf);
        }

        [Fact]
        public void SemNavegacaoDeEmpresa_CamposDaLocadoraVaoVazios()
        {
            IMapper mapper = CriarMapper();
            Veiculo veiculo = new Veiculo { IdVeiculo = 13, Placa = "SEM1A23" };

            ReturnVeiculoVO retorno = mapper.Map<ReturnVeiculoVO>(veiculo);

            Assert.Equal(string.Empty, retorno.LocadoraNomeFantasia);
            Assert.Equal(string.Empty, retorno.LocadoraCidade);
            Assert.Equal(string.Empty, retorno.LocadoraUf);
        }

        private static IMapper CriarMapper()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<VeiculoProfile>(), NullLoggerFactory.Instance);

            return configuracao.CreateMapper();
        }
    }
}
