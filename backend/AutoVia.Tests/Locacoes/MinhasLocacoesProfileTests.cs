using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Profiles;
using AutoVia.Core.RepositoriesInterface;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>Mapa da locacao do cliente: diarias pela data (virada de dia), codigo da reserva, combustivel com duas casas e devolucao combinada.</summary>
    public class MinhasLocacoesProfileTests
    {
        private static IMapper CriarMapper()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<MinhasLocacoesProfile>(), NullLoggerFactory.Instance);

            return configuracao.CreateMapper();
        }

        private static LocacaoClienteRegistro CriarRegistro(DateTime retirada, DateTime devolucaoPrevista)
        {
            return new LocacaoClienteRegistro
            {
                IdLocacao = 1,
                IdReserva = 42,
                Status = StatusLocacao.EM_ANDAMENTO,
                DataRetirada = retirada,
                DataDevolucaoPrevista = devolucaoPrevista,
                ValorDiaria = 150m,
                KmSaida = 1000,
                CombustivelRetirada = 0.5m,
                Placa = "ABC1D23",
                CategoriaNome = "Economico",
                LocadoraNomeFantasia = "Locadora Um"
            };
        }

        [Fact]
        public void Diarias_ViradaDoDia_ContamUmaDiariaMesmoComPoucasHoras()
        {
            LocacaoClienteRegistro registro = CriarRegistro(new DateTime(2026, 11, 25, 23, 30, 0), new DateTime(2026, 11, 26, 0, 10, 0));

            ReturnMinhaLocacaoVO vo = CriarMapper().Map<ReturnMinhaLocacaoVO>(registro);

            Assert.Equal(1, vo.Diarias);
        }

        [Fact]
        public void CodigoReserva_ComReserva_SaiComPrefixoAV()
        {
            LocacaoClienteRegistro registro = CriarRegistro(new DateTime(2026, 11, 25, 9, 0, 0), new DateTime(2026, 11, 27, 9, 0, 0));

            ReturnMinhaLocacaoVO vo = CriarMapper().Map<ReturnMinhaLocacaoVO>(registro);

            Assert.Equal("AV-00042", vo.CodigoReserva);
        }

        [Fact]
        public void CodigoReserva_SemReserva_FicaNulo()
        {
            LocacaoClienteRegistro registro = CriarRegistro(new DateTime(2026, 11, 25, 9, 0, 0), new DateTime(2026, 11, 27, 9, 0, 0));
            registro.IdReserva = null;

            ReturnMinhaLocacaoVO vo = CriarMapper().Map<ReturnMinhaLocacaoVO>(registro);

            Assert.Null(vo.CodigoReserva);
        }

        [Fact]
        public void CombustivelRetirada_SaiComDuasCasas()
        {
            LocacaoClienteRegistro registro = CriarRegistro(new DateTime(2026, 11, 25, 9, 0, 0), new DateTime(2026, 11, 27, 9, 0, 0));

            ReturnMinhaLocacaoVO vo = CriarMapper().Map<ReturnMinhaLocacaoVO>(registro);

            Assert.Equal("0.50", vo.CombustivelRetiradaStr);
        }

        [Fact]
        public void CombustivelRetirada_SemVistoria_FicaNulo()
        {
            LocacaoClienteRegistro registro = CriarRegistro(new DateTime(2026, 11, 25, 9, 0, 0), new DateTime(2026, 11, 27, 9, 0, 0));
            registro.CombustivelRetirada = null;

            ReturnMinhaLocacaoVO vo = CriarMapper().Map<ReturnMinhaLocacaoVO>(registro);

            Assert.Null(vo.CombustivelRetiradaStr);
        }

        [Fact]
        public void DevolucaoPrevistaEfetiva_ViaDiaCombinadoNaHoraDaRetirada()
        {
            LocacaoClienteRegistro registro = CriarRegistro(new DateTime(2026, 11, 25, 9, 30, 0), new DateTime(2026, 11, 27, 18, 0, 0));

            ReturnMinhaLocacaoVO vo = CriarMapper().Map<ReturnMinhaLocacaoVO>(registro);

            Assert.Equal(new DateTime(2026, 11, 27, 9, 30, 0), vo.DevolucaoPrevistaEfetiva);
        }

        [Fact]
        public void ValorTotal_EmAndamento_FicaNuloNoTexto()
        {
            LocacaoClienteRegistro registro = CriarRegistro(new DateTime(2026, 11, 25, 9, 0, 0), new DateTime(2026, 11, 27, 9, 0, 0));
            registro.ValorTotal = null;

            ReturnMinhaLocacaoVO vo = CriarMapper().Map<ReturnMinhaLocacaoVO>(registro);

            Assert.Null(vo.ValorTotal);
            Assert.Null(vo.ValorTotalStr);
        }

        [Fact]
        public void Configuracao_TodosOsDestinosDoVOEstaoMapeadosOuIgnorados()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<MinhasLocacoesProfile>(), NullLoggerFactory.Instance);

            configuracao.AssertConfigurationIsValid();
        }
    }
}
