using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// Servico da lista de devolucoes pendentes com o repositorio em memoria: filtro de status, paginacao, ordem, flag de atraso,
    /// codigo da reserva nulo, formatacao da devolucao prevista efetiva e log da falha. A prova do filtro da locadora e do SQL fica
    /// em LocacaoConsultasSqlTests.
    /// </summary>
    public class DevolucoesPendentesServiceTests
    {
        private static readonly DateTime AgoraDeTeste = new DateTime(2026, 11, 26, 10, 0, 0);
        private const string NomeClienteDeTeste = "Maria da Silva";
        private const string CpfDoClienteDeTeste = "12345678909";
        private const string PlacaDeTeste = "ABC1D23";

        private static IMapper CriarMapper()
        {
            MapperConfiguration configuracao = new MapperConfiguration(
                opcoes => opcoes.AddProfile<DevolucaoPendenteProfile>(),
                NullLoggerFactory.Instance);

            return configuracao.CreateMapper();
        }

        private static Locacao CriarLocacao(int idLocacao, StatusLocacao status, DateTime retirada, DateTime previsao, int? idReserva, string placa)
        {
            Locacao locacao = new Locacao
            {
                IdLocacao = idLocacao,
                IdReserva = idReserva,
                IdCliente = 1,
                IdVeiculo = 1,
                IdFuncionario = 20,
                DataRetirada = retirada,
                DataDevolucaoPrevista = previsao,
                KmSaida = 100,
                Status = status,
                Cliente = new Cliente { IdCliente = 1, Nome = NomeClienteDeTeste, Cpf = CpfDoClienteDeTeste },
                Veiculo = new Veiculo { IdVeiculo = 1, Placa = placa, Marca = "Fiat", Modelo = "Toro" }
            };

            return locacao;
        }

        /// <summary>
        /// Conjunto do teste (agora = 26/11 as 10:00): 1 em andamento atrasada (efetiva 22/11 09:00), 2 em andamento no prazo
        /// (efetiva 28/11 08:00), 3 com status ATRASADA (efetiva 12/11), 4 FINALIZADA (fora), 5 em andamento sem reserva (efetiva 26/11 12:00,
        /// no prazo) e 6 desativada (fora). A ordem pela devolucao prevista efetiva e: 3, 1, 5, 2.
        /// </summary>
        private static FakeLocacaoRepository CriarRepositorioDeTeste()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();

            repositorio.Adicionar(CriarLocacao(1, StatusLocacao.EM_ANDAMENTO, new DateTime(2026, 11, 20, 9, 0, 0), new DateTime(2026, 11, 22, 14, 0, 0), 7, "ABC1D23"));
            repositorio.Adicionar(CriarLocacao(2, StatusLocacao.EM_ANDAMENTO, new DateTime(2026, 11, 25, 8, 0, 0), new DateTime(2026, 11, 28, 18, 0, 0), 8, "DEF2G34"));
            repositorio.Adicionar(CriarLocacao(3, StatusLocacao.ATRASADA, new DateTime(2026, 11, 10, 0, 0, 0), new DateTime(2026, 11, 12, 0, 0, 0), 9, "GHI3J45"));
            repositorio.Adicionar(CriarLocacao(4, StatusLocacao.FINALIZADA, new DateTime(2026, 11, 1, 9, 0, 0), new DateTime(2026, 11, 3, 9, 0, 0), 10, "JKL4M56"));
            repositorio.Adicionar(CriarLocacao(5, StatusLocacao.EM_ANDAMENTO, new DateTime(2026, 11, 24, 12, 0, 0), new DateTime(2026, 11, 26, 9, 0, 0), null, "MNO5P67"));

            Locacao desativada = CriarLocacao(6, StatusLocacao.EM_ANDAMENTO, new DateTime(2026, 11, 20, 9, 0, 0), new DateTime(2026, 11, 22, 9, 0, 0), 11, "PQR6S78");
            desativada.DisabledAt = new DateTime(2026, 11, 21, 8, 0, 0);
            repositorio.Adicionar(desativada);

            return repositorio;
        }

        private static DevolucoesPendentesService CriarServico(ILocacaoRepository repositorio, FakeLogService log)
        {
            return new DevolucoesPendentesService(repositorio, new FakeRelogio(AgoraDeTeste), CriarMapper(), log);
        }

        [Fact]
        public async Task SemStatus_TrazAsEmAndamentoEAsAtrasadasSemFinalizadasNemDesativadas()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioDeTeste();
            DevolucoesPendentesService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnDevolucaoPendenteVO> pagina = await servico.ListarAsync(new FiltroDevolucaoPendenteVO());

            Assert.Equal(4, pagina.Total);
            Assert.Equal(new List<int> { 3, 1, 5, 2 }, pagina.Itens.Select(item => item.IdLocacao).ToList());
        }

        [Fact]
        public async Task StatusAtrasada_TrazSoAsAtrasadas()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioDeTeste();
            DevolucoesPendentesService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnDevolucaoPendenteVO> pagina = await servico.ListarAsync(new FiltroDevolucaoPendenteVO { Status = nameof(StatusLocacao.ATRASADA) });

            Assert.Equal(2, pagina.Total);
            Assert.Equal(new List<int> { 3, 1 }, pagina.Itens.Select(item => item.IdLocacao).ToList());
            Assert.All(pagina.Itens, item => Assert.True(item.Atrasada));
        }

        [Fact]
        public async Task Atrasada_MarcaSoQuemPassouDaDevolucaoPrevistaEfetiva()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioDeTeste();
            DevolucoesPendentesService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnDevolucaoPendenteVO> pagina = await servico.ListarAsync(new FiltroDevolucaoPendenteVO());

            Dictionary<int, bool> atrasadas = pagina.Itens.ToDictionary(item => item.IdLocacao, item => item.Atrasada);
            Assert.True(atrasadas[3]);
            Assert.True(atrasadas[1]);
            Assert.False(atrasadas[5]);
            Assert.False(atrasadas[2]);
        }

        [Fact]
        public async Task Paginacao_DevolveAPaginaPedidaComOTotalDeTodas()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioDeTeste();
            DevolucoesPendentesService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnDevolucaoPendenteVO> pagina = await servico.ListarAsync(new FiltroDevolucaoPendenteVO { Pagina = 2, TamanhoPagina = 2 });

            Assert.Equal(4, pagina.Total);
            Assert.Equal(2, pagina.Pagina);
            Assert.Equal(2, pagina.TamanhoPagina);
            Assert.Equal(new List<int> { 5, 2 }, pagina.Itens.Select(item => item.IdLocacao).ToList());
        }

        [Fact]
        public async Task ItemComReserva_TrazOCodigoDaReserva_SemReservaFicaNulo()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioDeTeste();
            DevolucoesPendentesService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnDevolucaoPendenteVO> pagina = await servico.ListarAsync(new FiltroDevolucaoPendenteVO());

            Dictionary<int, ReturnDevolucaoPendenteVO> itens = pagina.Itens.ToDictionary(item => item.IdLocacao);
            Assert.Equal(RegrasReserva.FormateCodigoReserva(7), itens[1].CodigoReserva);
            Assert.Null(itens[5].CodigoReserva);
        }

        [Fact]
        public async Task Item_TrazDevolucaoPrevistaEfetivaBrutaEFormatada_ENomesDoVeiculoEDoCliente()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioDeTeste();
            DevolucoesPendentesService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnDevolucaoPendenteVO> pagina = await servico.ListarAsync(new FiltroDevolucaoPendenteVO { Status = nameof(StatusLocacao.EM_ANDAMENTO) });

            ReturnDevolucaoPendenteVO primeiro = pagina.Itens.First(item => item.IdLocacao == 1);
            DateTime esperada = new DateTime(2026, 11, 22, 9, 0, 0);
            Assert.Equal(esperada, primeiro.DevolucaoPrevistaEfetiva);
            Assert.Equal(StaticMethods.FormateDateTimeToPTBR(esperada), primeiro.DevolucaoPrevistaEfetivaStr);
            Assert.Equal("ABC1D23", primeiro.Placa);
            Assert.Equal("Fiat", primeiro.Marca);
            Assert.Equal("Toro", primeiro.Modelo);
            Assert.Equal("Maria da Silva", primeiro.NomeCliente);
            Assert.Equal(nameof(StatusLocacao.EM_ANDAMENTO), primeiro.Status);
        }

        [Fact]
        public void DevolucaoPendenteProfile_ConfiguracaoEValida()
        {
            MapperConfiguration configuracao = new MapperConfiguration(
                opcoes => opcoes.AddProfile<DevolucaoPendenteProfile>(),
                NullLoggerFactory.Instance);

            configuracao.AssertConfigurationIsValid();
        }

        [Fact]
        public async Task FalhaDaLeitura_RegistraNoLogERelanca()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioDeTeste();
            repositorio.FalhaNaListagemDevolucoes = new InvalidOperationException("falha de teste");
            FakeLogService log = new FakeLogService();
            DevolucoesPendentesService servico = CriarServico(repositorio, log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.ListarAsync(new FiltroDevolucaoPendenteVO()));

            List<string> entradas = log.Snapshot();
            Assert.Contains(entradas, entrada => entrada.StartsWith(ConstantsMessage.ErroListarDevolucoesPendentes, StringComparison.Ordinal));
            foreach (string entrada in entradas)
            {
                Assert.DoesNotContain(PlacaDeTeste, entrada);
                Assert.DoesNotContain(CpfDoClienteDeTeste, entrada);
                Assert.DoesNotContain(NomeClienteDeTeste, entrada);
            }
        }
    }
}
