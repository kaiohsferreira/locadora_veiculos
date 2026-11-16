using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>Profile da locacao: todos os destinos mapeados e os campos Str formatados por StaticMethods.</summary>
    public class LocacaoProfileTests
    {
        private static readonly DateTime DataRetiradaDeTeste = new DateTime(2026, 11, 19, 9, 30, 0);
        private static readonly DateTime DataDevolucaoDeTeste = new DateTime(2026, 11, 20, 0, 0, 0);

        [Fact]
        public void LocacaoProfile_ConfiguracaoEValida()
        {
            MapperConfiguration configuracao = new MapperConfiguration(
                cfg => cfg.AddProfile<LocacaoProfile>(),
                NullLoggerFactory.Instance);

            configuracao.AssertConfigurationIsValid();
        }

        [Fact]
        public void Mapeamento_DevolveCamposBrutosECamposStrAoLado()
        {
            IMapper mapper = CriarMapper();
            Locacao locacao = new Locacao
            {
                IdLocacao = 1,
                IdReserva = 10,
                IdCliente = 30,
                IdVeiculo = 40,
                IdFuncionario = 20,
                DataRetirada = DataRetiradaDeTeste,
                DataDevolucaoPrevista = DataDevolucaoDeTeste,
                KmSaida = 1200,
                ValorDiaria = 175m,
                ValorCaucao = 0m,
                Status = StatusLocacao.EM_ANDAMENTO
            };

            ReturnLocacaoVO retorno = mapper.Map<ReturnLocacaoVO>(locacao);

            Assert.Equal(DataRetiradaDeTeste, retorno.DataRetirada);
            Assert.Equal("19/11/2026 09:30", retorno.DataRetiradaStr);
            Assert.Equal(DataDevolucaoDeTeste, retorno.DataDevolucaoPrevista);
            Assert.Equal("20/11/2026", retorno.DataDevolucaoPrevistaStr);
            Assert.Equal(175m, retorno.ValorDiaria);
            Assert.Equal(StaticMethods.FormateToMonetary(175m), retorno.ValorDiariaStr);
            Assert.Contains("175,00", retorno.ValorDiariaStr);
            Assert.Equal(0m, retorno.ValorCaucao);
            Assert.Equal(StaticMethods.FormateToMonetary(0m), retorno.ValorCaucaoStr);
        }

        [Fact]
        public void IdReservaNulo_ViraZero()
        {
            IMapper mapper = CriarMapper();
            Locacao locacao = new Locacao
            {
                IdLocacao = 2,
                IdReserva = null,
                DataRetirada = DataRetiradaDeTeste,
                DataDevolucaoPrevista = DataDevolucaoDeTeste,
                Status = StatusLocacao.EM_ANDAMENTO
            };

            ReturnLocacaoVO retorno = mapper.Map<ReturnLocacaoVO>(locacao);

            Assert.Equal(0, retorno.IdReserva);
        }

        private static IMapper CriarMapper()
        {
            MapperConfiguration configuracao = new MapperConfiguration(
                cfg => cfg.AddProfile<LocacaoProfile>(),
                NullLoggerFactory.Instance);

            return configuracao.CreateMapper();
        }
    }
}
