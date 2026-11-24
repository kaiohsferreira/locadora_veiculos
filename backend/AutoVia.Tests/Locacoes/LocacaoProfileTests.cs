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

        [Fact]
        public void Retirada_DatasBrutasSemHoraEStrSoComData()
        {
            IMapper mapper = CriarMapper();
            Reserva reserva = CriarReservaDeRetirada(new DateTime(2026, 11, 18, 9, 30, 0), new DateTime(2026, 11, 19, 9, 30, 0));

            ReturnRetiradaVO retorno = mapper.Map<ReturnRetiradaVO>(reserva);

            Assert.Equal(new DateOnly(2026, 11, 18), retorno.DataRetiradaPrevista);
            Assert.Equal("18/11/2026", retorno.DataRetiradaPrevistaStr);
            Assert.Equal(new DateOnly(2026, 11, 19), retorno.DataDevolucaoPrevista);
            Assert.Equal("19/11/2026", retorno.DataDevolucaoPrevistaStr);
        }

        [Theory]
        [InlineData(18, 19, 1)]
        [InlineData(18, 22, 4)]
        public void Retirada_DiariasPrevistasSaemEntreAsDuasDatas(int diaRetirada, int diaDevolucao, int esperado)
        {
            IMapper mapper = CriarMapper();
            Reserva reserva = CriarReservaDeRetirada(new DateTime(2026, 11, diaRetirada, 0, 0, 0), new DateTime(2026, 11, diaDevolucao, 0, 0, 0));

            ReturnRetiradaVO retorno = mapper.Map<ReturnRetiradaVO>(reserva);

            Assert.Equal(esperado, retorno.DiariasPrevistas);
        }

        [Fact]
        public void Retirada_ValoresEValidadeComBrutoAoLadoDoStr()
        {
            IMapper mapper = CriarMapper();
            Reserva reserva = CriarReservaDeRetirada(new DateTime(2026, 11, 18, 9, 30, 0), new DateTime(2026, 11, 19, 9, 30, 0));

            ReturnRetiradaVO retorno = mapper.Map<ReturnRetiradaVO>(reserva);

            Assert.Equal(200m, retorno.ValorDiaria);
            Assert.Equal(StaticMethods.FormateToMonetary(200m), retorno.ValorDiariaStr);
            Assert.Equal(200m, retorno.ValorEstimado);
            Assert.Equal(StaticMethods.FormateToMonetary(200m), retorno.ValorEstimadoStr);
            Assert.Equal(new DateOnly(2030, 1, 1), retorno.CnhValidade);
            Assert.Equal("01/01/2030", retorno.CnhValidadeStr);
        }

        [Fact]
        public void Retirada_ValorEstimadoNulo_StrVazio()
        {
            IMapper mapper = CriarMapper();
            Reserva reserva = CriarReservaDeRetirada(new DateTime(2026, 11, 18, 9, 30, 0), new DateTime(2026, 11, 19, 9, 30, 0));
            reserva.ValorEstimado = null;

            ReturnRetiradaVO retorno = mapper.Map<ReturnRetiradaVO>(reserva);

            Assert.Null(retorno.ValorEstimado);
            Assert.Equal(string.Empty, retorno.ValorEstimadoStr);
        }

        [Fact]
        public void Retirada_CategoriaAnoELocalDaLocadora()
        {
            IMapper mapper = CriarMapper();
            Reserva reserva = CriarReservaDeRetirada(new DateTime(2026, 11, 18, 9, 30, 0), new DateTime(2026, 11, 19, 9, 30, 0));

            ReturnRetiradaVO retorno = mapper.Map<ReturnRetiradaVO>(reserva);

            Assert.Equal("SUV", retorno.CategoriaNome);
            Assert.Equal(2024, retorno.AnoModelo);
            Assert.Equal("Locadora Teste", retorno.LocalRetiradaNome);
            Assert.Equal("Rua das Flores, 100 - Centro, Campinas - SP", retorno.LocalRetiradaEndereco);
        }

        [Fact]
        public void Retirada_CamposDoServicoNaoSaemDoProfile()
        {
            IMapper mapper = CriarMapper();
            Reserva reserva = CriarReservaDeRetirada(new DateTime(2026, 11, 18, 9, 30, 0), new DateTime(2026, 11, 19, 9, 30, 0));

            ReturnRetiradaVO retorno = mapper.Map<ReturnRetiradaVO>(reserva);

            Assert.Null(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
            Assert.False(retorno.CnhVencida);
            Assert.Null(retorno.CnhEnviadaEm);
            Assert.Equal(string.Empty, retorno.CnhEnviadaEmStr);
            Assert.Null(retorno.ComprovanteResidenciaEnviadoEm);
            Assert.Equal(string.Empty, retorno.ComprovanteResidenciaEnviadoEmStr);
        }

        /// <summary>
        /// Os cinco campos da caucao sao do servico (ICaucaoRetiradaService.PreencherRetiradaAsync): o profile os ignora (a
        /// configuracao continua valida, LocacaoProfile_ConfiguracaoEValida) e eles saem do mapeamento com o valor inicial.
        /// </summary>
        [Fact]
        public void Retirada_CamposDaCaucaoNaoSaemDoProfile()
        {
            IMapper mapper = CriarMapper();
            Reserva reserva = CriarReservaDeRetirada(new DateTime(2026, 11, 18, 9, 30, 0), new DateTime(2026, 11, 19, 9, 30, 0));

            ReturnRetiradaVO retorno = mapper.Map<ReturnRetiradaVO>(reserva);

            Assert.False(retorno.CaucaoExigida);
            Assert.Equal(0m, retorno.ValorCaucao);
            Assert.Equal(string.Empty, retorno.ValorCaucaoStr);
            Assert.False(retorno.CaucaoDisponivel);
            Assert.False(retorno.CaucaoModoSimulacao);
        }

        private static Reserva CriarReservaDeRetirada(DateTime retirada, DateTime devolucao)
        {
            Cliente cliente = new Cliente
            {
                IdCliente = 30, Nome = "Maria da Silva", Cpf = "12345678909", CnhNumero = "12345678901",
                CnhCategoria = CategoriaCnh.B, CnhValidade = new DateOnly(2030, 1, 1)
            };
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 40, IdEmpresa = 1, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Toro", AnoModelo = 2024,
                KmAtual = 1500, ValorDiaria = 200m, Status = StatusVeiculo.RESERVADO, IdCategoria = 3,
                Categoria = new Categoria { IdCategoria = 3, Nome = "SUV" },
                Empresa = new Empresa { IdEmpresa = 1, NomeFantasia = "Locadora Teste", Logradouro = "Rua das Flores", Numero = "100", Bairro = "Centro", Cidade = "Campinas", Uf = "SP" }
            };

            return new Reserva
            {
                IdReserva = 10, IdCliente = 30, Cliente = cliente, IdVeiculo = 40, Veiculo = veiculo,
                DataRetiradaPrevista = retirada, DataDevolucaoPrevista = devolucao, ValorEstimado = 200m, Status = StatusReserva.CONFIRMADA
            };
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
