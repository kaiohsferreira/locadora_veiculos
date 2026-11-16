using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Communication.Validators.Reservas;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.ClienteLocadora;
using AutoVia.Tests.Fakes;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;

namespace AutoVia.Tests.Reservas
{
    /// <summary>
    /// Regras de negocio da reserva no servico (com fakes), validadores de entrada e mapeamento dos campos novos.
    /// Cobre: retirada no passado, duracao maxima, valor estimado, teto de ativas, ordem do POST (inclusive o cadastro
    /// por locadora: 409 de primeira tentativa, PENDENTE, RECUSADO e 429, sem linha nova quando nao deve), situacao,
    /// busca, e os campos Str e os dados do veiculo e da locadora para o proprio cliente.
    /// </summary>
    public class ReservaRegrasTests
    {
        private const int IdCliente = 31;
        private const int IdOutroCliente = 32;
        private const int IdEmpresa = 1;
        private const int IdVeiculo = 8;
        private const string NomeLocadora = "Locadora Um";
        private const string Modelo = "Renegade";
        private const string ModeloNumerico = "208";
        private const string ModeloOnix = "Onix";
        private const string JustificativaRecusa = "Documento ilegível";
        private static readonly DateTime AgoraFixo = new DateTime(2026, 11, 16, 10, 0, 0);
        private static readonly DateOnly RetiradaPadrao = new DateOnly(2026, 12, 10);
        private static readonly DateOnly DevolucaoPadrao = new DateOnly(2026, 12, 14);

        [Fact]
        public async System.Threading.Tasks.Task RetiradaDeOntemRespondeValidationExceptionComMensagemDeRetiradaNoPassado()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            FakeLogService log = new FakeLogService();
            ReservaService servico = CriarServico(reservas, log, CriarArmazenamento());
            CriarReservaVO entrada = new CriarReservaVO { IdVeiculo = IdVeiculo, DataRetirada = new DateOnly(2026, 11, 15), DataDevolucao = DevolucaoPadrao };

            ValidationException erro = await Assert.ThrowsAsync<ValidationException>(() => servico.CriarAsync(IdCliente, entrada));

            Assert.Equal(ConstantsMessage.ErroReservaRetiradaNoPassado, erro.Errors.First().ErrorMessage);
            Assert.Equal(0, reservas.ChamadasDeInsercao);
            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async System.Threading.Tasks.Task RetiradaDeHojeEAceita()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            ReservaService servico = CriarServico(reservas, new FakeLogService(), CriarArmazenamento());
            CriarReservaVO entrada = new CriarReservaVO { IdVeiculo = IdVeiculo, DataRetirada = new DateOnly(2026, 11, 16), DataDevolucao = new DateOnly(2026, 11, 18) };

            ReturnReservaCriadaVO retorno = await servico.CriarAsync(IdCliente, entrada);

            Assert.Equal(1, reservas.ChamadasDeInsercao);
            Assert.Equal(2, retorno.Diarias);
        }

        [Fact]
        public async System.Threading.Tasks.Task NoventaDiariasAceitasNoventaEUmaRecusadaComValidationExceptionSemLog()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            FakeLogService log = new FakeLogService();
            ReservaService servico = CriarServico(reservas, log, CriarArmazenamento());
            DateOnly retirada = new DateOnly(2026, 12, 10);

            ReturnReservaCriadaVO retorno = await servico.CriarAsync(IdCliente, new CriarReservaVO { IdVeiculo = IdVeiculo, DataRetirada = retirada, DataDevolucao = retirada.AddDays(ConstantsValues.DuracaoMaximaReservaDiarias) });
            Assert.Equal(ConstantsValues.DuracaoMaximaReservaDiarias, retorno.Diarias);

            ValidationException erro = await Assert.ThrowsAsync<ValidationException>(() => servico.CriarAsync(IdCliente, new CriarReservaVO { IdVeiculo = IdVeiculo, DataRetirada = retirada, DataDevolucao = retirada.AddDays(ConstantsValues.DuracaoMaximaReservaDiarias + 1) }));
            Assert.Equal(string.Format(ConstantsMessage.ErroReservaDuracaoMaxima, ConstantsValues.DuracaoMaximaReservaDiarias), erro.Errors.First().ErrorMessage);
            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async System.Threading.Tasks.Task PeriodoSemDiariaChamadoDiretoNoServicoRespondeValidationExceptionSemLog()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            FakeLogService log = new FakeLogService();
            ReservaService servico = CriarServico(reservas, log, CriarArmazenamento());

            ValidationException erro = await Assert.ThrowsAsync<ValidationException>(() => servico.CriarAsync(IdCliente, new CriarReservaVO { IdVeiculo = IdVeiculo, DataRetirada = RetiradaPadrao, DataDevolucao = RetiradaPadrao }));

            Assert.Equal(ConstantsMessage.ErroReservaPeriodoInvalido, erro.Errors.First().ErrorMessage);
            Assert.Equal(0, reservas.ChamadasDeInsercao);
            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async System.Threading.Tasks.Task ValorEstimadoEhDiariasVezesDiariaGravadoEDevolvidoComOStr()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            ReservaService servico = CriarServico(reservas, new FakeLogService(), CriarArmazenamento());

            ReturnReservaCriadaVO retorno = await servico.CriarAsync(IdCliente, new CriarReservaVO { IdVeiculo = IdVeiculo, DataRetirada = new DateOnly(2026, 12, 14), DataDevolucao = new DateOnly(2026, 12, 18) });

            Assert.Equal(4, retorno.Diarias);
            Assert.Equal(602.00m, retorno.ValorEstimado);
            Assert.Equal(StaticMethods.FormateToMonetary(602.00m), retorno.ValorEstimadoStr);
            Assert.Equal(602.00m, reservas.Armazenada?.ValorEstimado);
            Assert.Equal("AV-00001", retorno.Codigo);
            Assert.Equal("14/12/2026", retorno.DataRetiradaStr);
            Assert.Equal("18/12/2026", retorno.DataDevolucaoStr);
        }

        [Fact]
        public async System.Threading.Tasks.Task ClienteAprovadoCriaReservaComDataReservaPeloRelogio()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            ReservaService servico = CriarServico(reservas, new FakeLogService(), CriarArmazenamento());

            await servico.CriarAsync(IdCliente, CriarEntrada());

            Assert.Equal(AgoraFixo, reservas.Armazenada?.DataReserva);
            Assert.Equal(RetiradaPadrao.ToDateTime(TimeOnly.MinValue), reservas.Armazenada?.DataRetiradaPrevista);
        }

        [Fact]
        public async System.Threading.Tasks.Task ValorAcimaDoMaximoRespondeBusinessExceptionSemGravarNemPendente()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo(valorDiaria: 99999999.99m));
            ArmazenamentoClienteLocadora armazenamento = CriarArmazenamentoSemVinculo();
            ReservaService servico = CriarServico(reservas, new FakeLogService(), armazenamento);

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => servico.CriarAsync(IdCliente, CriarEntrada()));

            Assert.Equal(ConstantsMessage.ErroReservaValorEstimadoExcedido, erro.Message);
            Assert.Equal(0, reservas.ChamadasDeInsercao);
            Assert.Empty(armazenamento.Vinculos);
        }

        [Fact]
        public async System.Threading.Tasks.Task SextaReservaAtivaRespondeConflitoAntesDoCadastroEMensagemDoTeto()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            for (int indice = 1; indice <= ConstantsValues.LimiteReservasAtivasPorCliente; indice++)
            {
                reservas.Adicionar(CriarReservaDoCliente(100 + indice, IdCliente, StatusReserva.CONFIRMADA));
            }
            ArmazenamentoClienteLocadora armazenamento = CriarArmazenamentoSemVinculo();
            ReservaService servico = CriarServico(reservas, new FakeLogService(), armazenamento);

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => servico.CriarAsync(IdCliente, CriarEntrada()));

            Assert.Equal(string.Format(ConstantsMessage.ErroReservaLimiteAtivas, ConstantsValues.LimiteReservasAtivasPorCliente), erro.Message);
            Assert.Equal(0, reservas.ChamadasDeInsercao);
            Assert.Empty(armazenamento.Vinculos);
        }

        [Fact]
        public async System.Threading.Tasks.Task CancelarUmaReservaLiberaUmaVagaNoTeto()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            for (int indice = 1; indice <= ConstantsValues.LimiteReservasAtivasPorCliente; indice++)
            {
                reservas.Adicionar(CriarReservaDoCliente(100 + indice, IdCliente, StatusReserva.ATIVA));
            }
            ReservaService servico = CriarServico(reservas, new FakeLogService(), CriarArmazenamento());

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => servico.CriarAsync(IdCliente, CriarEntrada()));
            Assert.Equal(string.Format(ConstantsMessage.ErroReservaLimiteAtivas, ConstantsValues.LimiteReservasAtivasPorCliente), erro.Message);

            await servico.CancelarAsync(101, IdCliente);
            ReturnReservaCriadaVO retorno = await servico.CriarAsync(IdCliente, CriarEntrada());

            Assert.Equal(1, reservas.ChamadasDeInsercao);
            Assert.Equal(StatusReserva.ATIVA, retorno.Status);
        }

        [Fact]
        public async System.Threading.Tasks.Task CanceladasConcluidasDesativadasEDeOutroClienteNaoContamNoTeto()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            for (int indice = 1; indice <= ConstantsValues.LimiteReservasAtivasPorCliente - 1; indice++)
            {
                reservas.Adicionar(CriarReservaDoCliente(200 + indice, IdCliente, StatusReserva.ATIVA));
            }
            reservas.Adicionar(CriarReservaDoCliente(301, IdCliente, StatusReserva.CANCELADA));
            reservas.Adicionar(CriarReservaDoCliente(302, IdCliente, StatusReserva.CONCLUIDA));
            Reserva desativada = CriarReservaDoCliente(303, IdCliente, StatusReserva.ATIVA);
            desativada.DisabledAt = AgoraFixo;
            reservas.Adicionar(desativada);
            reservas.Adicionar(CriarReservaDoCliente(304, IdOutroCliente, StatusReserva.ATIVA));
            ReservaService servico = CriarServico(reservas, new FakeLogService(), CriarArmazenamento());

            ReturnReservaCriadaVO retorno = await servico.CriarAsync(IdCliente, CriarEntrada());

            Assert.Equal(1, reservas.ChamadasDeInsercao);
            Assert.Equal(StatusReserva.ATIVA, retorno.Status);
        }

        [Fact]
        public async System.Threading.Tasks.Task LocadoraDesativadaEntrePrecheckEExigenciaRespondeVeiculoNaoEncontrado()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            ArmazenamentoClienteLocadora armazenamento = new ArmazenamentoClienteLocadora();
            armazenamento.AdicionarEmpresa(new Empresa { IdEmpresa = IdEmpresa, NomeFantasia = NomeLocadora, Ativo = false });
            ReservaService servico = CriarServico(reservas, new FakeLogService(), armazenamento);

            NotFoundException erro = await Assert.ThrowsAsync<NotFoundException>(() => servico.CriarAsync(IdCliente, CriarEntrada()));

            Assert.Equal(ConstantsMessage.ErroReservaVeiculoNaoEncontrado, erro.Message);
            Assert.Equal(0, reservas.ChamadasDeInsercao);
            Assert.Empty(armazenamento.Vinculos);
        }

        [Fact]
        public async System.Threading.Tasks.Task VeiculoLocadoRespondeIndisponivelAntesDoCadastroSemPendente()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo(status: StatusVeiculo.LOCADO));
            ArmazenamentoClienteLocadora armazenamento = CriarArmazenamentoSemVinculo();
            ReservaService servico = CriarServico(reservas, new FakeLogService(), armazenamento);

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => servico.CriarAsync(IdCliente, CriarEntrada()));

            Assert.Equal(ConstantsMessage.ErroReservaVeiculoIndisponivel, erro.Message);
            Assert.Empty(armazenamento.Vinculos);
        }

        [Fact]
        public async System.Threading.Tasks.Task VeiculoInexistenteRespondeNaoEncontradoAntesDoCadastroSemPendente()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            ArmazenamentoClienteLocadora armazenamento = CriarArmazenamentoSemVinculo();
            ReservaService servico = CriarServico(reservas, new FakeLogService(), armazenamento);

            NotFoundException erro = await Assert.ThrowsAsync<NotFoundException>(() => servico.CriarAsync(IdCliente, CriarEntrada()));

            Assert.Equal(ConstantsMessage.ErroReservaVeiculoNaoEncontrado, erro.Message);
            Assert.Empty(armazenamento.Vinculos);
            Assert.Equal(0, reservas.ChamadasDeInsercao);
        }

        [Fact]
        public async System.Threading.Tasks.Task VeiculoDesativadoRespondeNaoEncontradoAntesDoCadastroSemPendente()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            Veiculo desativado = CriarVeiculo();
            desativado.DisabledAt = AgoraFixo;
            reservas.VincularVeiculo(desativado);
            ArmazenamentoClienteLocadora armazenamento = CriarArmazenamentoSemVinculo();
            ReservaService servico = CriarServico(reservas, new FakeLogService(), armazenamento);

            NotFoundException erro = await Assert.ThrowsAsync<NotFoundException>(() => servico.CriarAsync(IdCliente, CriarEntrada()));

            Assert.Equal(ConstantsMessage.ErroReservaVeiculoNaoEncontrado, erro.Message);
            Assert.Empty(armazenamento.Vinculos);
            Assert.Equal(0, reservas.ChamadasDeInsercao);
        }

        [Fact]
        public async System.Threading.Tasks.Task ClienteSemVinculoNaPrimeiraTentativaGravaPendenteE409ComNomeDaLocadoraSemReserva()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            ArmazenamentoClienteLocadora armazenamento = CriarArmazenamentoSemVinculo();
            FakeLogService log = new FakeLogService();
            ReservaService servico = CriarServico(reservas, log, armazenamento);

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => servico.CriarAsync(IdCliente, CriarEntrada()));

            Assert.Equal(string.Format(ConstantsMessage.ErroReservaCadastroEmAnalise, NomeLocadora), erro.Message);
            Assert.Equal(0, reservas.ChamadasDeInsercao);
            Assert.Single(armazenamento.Vinculos);
            Assert.Equal(StatusCadastroCliente.PENDENTE, armazenamento.Vinculos.Single().Status);
            Assert.Equal(IdCliente, armazenamento.Vinculos.Single().IdCliente);
            Assert.Equal(IdEmpresa, armazenamento.Vinculos.Single().IdEmpresa);
            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async System.Threading.Tasks.Task ClienteComPendenteRespondeEmAnaliseSemLinhaNova()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            ArmazenamentoClienteLocadora armazenamento = CriarArmazenamentoSemVinculo();
            armazenamento.Semear(IdCliente, IdEmpresa, StatusCadastroCliente.PENDENTE);
            ReservaService servico = CriarServico(reservas, new FakeLogService(), armazenamento);

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => servico.CriarAsync(IdCliente, CriarEntrada()));

            Assert.Equal(string.Format(ConstantsMessage.ErroReservaCadastroEmAnalise, NomeLocadora), erro.Message);
            Assert.Single(armazenamento.Vinculos);
            Assert.Equal(0, reservas.ChamadasDeInsercao);
        }

        [Fact]
        public async System.Threading.Tasks.Task ClienteRecusadoRespondeRecusadoSemReservaNemLinhaNova()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            ArmazenamentoClienteLocadora armazenamento = CriarArmazenamentoSemVinculo();
            armazenamento.Semear(IdCliente, IdEmpresa, StatusCadastroCliente.RECUSADO, null, JustificativaRecusa);
            ReservaService servico = CriarServico(reservas, new FakeLogService(), armazenamento);

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => servico.CriarAsync(IdCliente, CriarEntrada()));

            Assert.Equal(string.Format(ConstantsMessage.ErroReservaCadastroRecusado, NomeLocadora), erro.Message);
            Assert.Single(armazenamento.Vinculos);
            Assert.Equal(0, reservas.ChamadasDeInsercao);
        }

        [Fact]
        public async System.Threading.Tasks.Task TetoDePedidosDeAprovacaoRespondeLimiteSemGravarNadaENemLog()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.VincularVeiculo(CriarVeiculo());
            ArmazenamentoClienteLocadora armazenamento = CriarArmazenamentoSemVinculo();
            for (int idEmpresaOutra = 2; idEmpresaOutra <= 6; idEmpresaOutra++)
            {
                armazenamento.AdicionarEmpresa(new Empresa { IdEmpresa = idEmpresaOutra, NomeFantasia = "Locadora " + idEmpresaOutra, Ativo = true });
                armazenamento.Semear(IdCliente, idEmpresaOutra, StatusCadastroCliente.PENDENTE, null, null, AgoraFixo.AddHours(-1));
            }
            FakeLogService log = new FakeLogService();
            ReservaService servico = CriarServico(reservas, log, armazenamento);

            LimiteExcedidoException erro = await Assert.ThrowsAsync<LimiteExcedidoException>(() => servico.CriarAsync(IdCliente, CriarEntrada()));

            Assert.Equal(ConstantsMessage.ErroLimitePedidosAprovacao, erro.Message);
            Assert.Equal(5, armazenamento.Vinculos.Count);
            Assert.Equal(0, reservas.ChamadasDeInsercao);
            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async System.Threading.Tasks.Task ListaSituacaoAtivasEmOrdemCrescenteDeRetirada()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.Adicionar(CriarReservaComRetirada(1, IdCliente, StatusReserva.ATIVA, new DateTime(2026, 12, 10)));
            reservas.Adicionar(CriarReservaComRetirada(2, IdCliente, StatusReserva.CONFIRMADA, new DateTime(2026, 12, 1)));
            reservas.Adicionar(CriarReservaComRetirada(3, IdCliente, StatusReserva.CANCELADA, new DateTime(2026, 12, 5)));
            ReservaService servico = CriarServico(reservas, new FakeLogService(), CriarArmazenamento());

            PaginaVO<ReturnReservaVO> pagina = await servico.GetAllAsync(IdCliente, new FiltroReservaVO { Situacao = ConstantsValues.SituacaoReservaAtivas, Pagina = 1, TamanhoPagina = 20 });

            Assert.Equal(new[] { 2, 1 }, pagina.Itens.Select(item => item.IdReserva).ToArray());
        }

        [Fact]
        public async System.Threading.Tasks.Task ListaSituacaoHistoricoEmOrdemDecrescenteDeRetirada()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.Adicionar(CriarReservaComRetirada(1, IdCliente, StatusReserva.CANCELADA, new DateTime(2026, 12, 1)));
            reservas.Adicionar(CriarReservaComRetirada(2, IdCliente, StatusReserva.CONCLUIDA, new DateTime(2026, 12, 10)));
            reservas.Adicionar(CriarReservaComRetirada(3, IdCliente, StatusReserva.ATIVA, new DateTime(2026, 12, 5)));
            ReservaService servico = CriarServico(reservas, new FakeLogService(), CriarArmazenamento());

            PaginaVO<ReturnReservaVO> pagina = await servico.GetAllAsync(IdCliente, new FiltroReservaVO { Situacao = ConstantsValues.SituacaoReservaHistorico, Pagina = 1, TamanhoPagina = 20 });

            Assert.Equal(new[] { 2, 1 }, pagina.Itens.Select(item => item.IdReserva).ToArray());
        }

        [Fact]
        public async System.Threading.Tasks.Task SituacaoComStatusOuSituacaoInvalidaRespondeValidationExceptionSemLog()
        {
            FakeLogService log = new FakeLogService();
            ReservaService servico = CriarServico(new FakeReservaRepository(), log, CriarArmazenamento());

            ValidationException comStatus = await Assert.ThrowsAsync<ValidationException>(() => servico.GetAllAsync(IdCliente, new FiltroReservaVO { Situacao = ConstantsValues.SituacaoReservaAtivas, Status = StatusReserva.ATIVA.ToString(), Pagina = 1, TamanhoPagina = 20 }));
            ValidationException invalida = await Assert.ThrowsAsync<ValidationException>(() => servico.GetAllAsync(IdCliente, new FiltroReservaVO { Situacao = "OUTRA", Pagina = 1, TamanhoPagina = 20 }));

            Assert.Equal(ConstantsMessage.ErroReservaSituacaoComStatus, comStatus.Errors.First().ErrorMessage);
            Assert.Equal(string.Format(ConstantsMessage.ErroReservaSituacaoInvalida, "ATIVAS, HISTORICO"), invalida.Errors.First().ErrorMessage);
            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async System.Threading.Tasks.Task BuscaPorCodigoAchaAReservaPeloIdComOuSemPrefixo()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.Adicionar(CriarReservaComVeiculo(42, IdCliente, StatusReserva.ATIVA, Modelo));
            reservas.Adicionar(CriarReservaComVeiculo(43, IdCliente, StatusReserva.ATIVA, Modelo));
            ReservaService servico = CriarServico(reservas, new FakeLogService(), CriarArmazenamento());

            foreach (string busca in new[] { "AV-00042", "av-42", "#AV-00042", "42" })
            {
                PaginaVO<ReturnReservaVO> pagina = await servico.GetAllAsync(IdCliente, new FiltroReservaVO { Busca = busca, Pagina = 1, TamanhoPagina = 20 });
                Assert.Equal(42, Assert.Single(pagina.Itens).IdReserva);
            }
        }

        [Fact]
        public async System.Threading.Tasks.Task BuscaPorModeloCasaSemDiferenciarMaiusculasENaoCasaPorMarca()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            reservas.Adicionar(CriarReservaComVeiculo(44, IdCliente, StatusReserva.ATIVA, Modelo));
            reservas.Adicionar(CriarReservaComVeiculo(45, IdCliente, StatusReserva.ATIVA, ModeloOnix));
            reservas.Adicionar(CriarReservaComVeiculo(208, IdCliente, StatusReserva.ATIVA, ModeloNumerico));
            ReservaService servico = CriarServico(reservas, new FakeLogService(), CriarArmazenamento());

            PaginaVO<ReturnReservaVO> porParteDoModelo = await servico.GetAllAsync(IdCliente, new FiltroReservaVO { Busca = "RENE", Pagina = 1, TamanhoPagina = 20 });
            PaginaVO<ReturnReservaVO> porMarca = await servico.GetAllAsync(IdCliente, new FiltroReservaVO { Busca = "jeep", Pagina = 1, TamanhoPagina = 20 });
            PaginaVO<ReturnReservaVO> porNumeroDoModelo = await servico.GetAllAsync(IdCliente, new FiltroReservaVO { Busca = "208", Pagina = 1, TamanhoPagina = 20 });
            PaginaVO<ReturnReservaVO> porLiteralPercentual = await servico.GetAllAsync(IdCliente, new FiltroReservaVO { Busca = "%", Pagina = 1, TamanhoPagina = 20 });

            Assert.Equal(44, Assert.Single(porParteDoModelo.Itens).IdReserva);
            Assert.Empty(porMarca.Itens);
            Assert.Equal(208, porNumeroDoModelo.Itens.Single(item => item.IdReserva == 208).IdReserva);
            Assert.Empty(porLiteralPercentual.Itens);
        }

        [Fact]
        public async System.Threading.Tasks.Task ReturnReservaTrazCodigoDatasStrDiariasEVeiculoELocadoraDoProprioCliente()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            Reserva reserva = CriarReservaComVeiculo(42, IdCliente, StatusReserva.CONFIRMADA, Modelo);
            reserva.DataReserva = new DateTime(2026, 11, 16, 14, 22, 10);
            reserva.ValorEstimado = 602.00m;
            reservas.Adicionar(reserva);
            ReservaService servico = CriarServico(reservas, new FakeLogService(), CriarArmazenamento());

            ReturnReservaVO retorno = await servico.GetByIdAsync(42, IdCliente);

            Assert.Equal("AV-00042", retorno.Codigo);
            Assert.Equal(new DateTime(2026, 11, 16, 14, 22, 10), retorno.DataReserva);
            Assert.Equal("16/11/2026 14:22", retorno.DataReservaStr);
            Assert.Equal("10/12/2026", retorno.DataRetiradaStr);
            Assert.Equal(2, retorno.Diarias);
            Assert.Equal(StaticMethods.FormateToMonetary(602.00m), retorno.ValorEstimadoStr);
            Assert.Equal("SUV", retorno.CategoriaNome);
            Assert.Equal("ABC1D23", retorno.Placa);
            Assert.Equal(NomeLocadora, retorno.LocadoraNomeFantasia);
            Assert.Equal("SP", retorno.LocadoraUf);
            Assert.DoesNotContain(retorno.GetType().GetProperties(), propriedade => propriedade.Name == "IdEmpresa" || propriedade.Name == "IdCliente" || propriedade.Name == "Chassi" || propriedade.Name == "Renavam");
        }

        [Fact]
        public void ValidadorDeCriacaoRecusaNoventaEUmaDiariasESaceitaNoventa()
        {
            CriarReservaVOValidator validador = new CriarReservaVOValidator();
            DateOnly retirada = new DateOnly(2026, 12, 10);

            Assert.True(validador.Validate(new CriarReservaVO { IdVeiculo = IdVeiculo, DataRetirada = retirada, DataDevolucao = retirada.AddDays(90) }).IsValid);

            FluentValidation.Results.ValidationResult resultado = validador.Validate(new CriarReservaVO { IdVeiculo = IdVeiculo, DataRetirada = retirada, DataDevolucao = retirada.AddDays(91) });
            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == string.Format(ConstantsMessage.ErroReservaDuracaoMaxima, 90));
        }

        [Theory]
        [InlineData("XPTO")]
        [InlineData("atativas")]
        public void ValidadorRecusaSituacaoForaDeAtivasEHistorico(string situacao)
        {
            FiltroReservaVOValidator validador = new FiltroReservaVOValidator();

            FluentValidation.Results.ValidationResult resultado = validador.Validate(new FiltroReservaVO { Situacao = situacao, Pagina = 1, TamanhoPagina = 20 });

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == string.Format(ConstantsMessage.ErroReservaSituacaoInvalida, "ATIVAS, HISTORICO"));
        }

        [Fact]
        public void ValidadorRecusaBuscaComMaisDeSessentaCaracteresDepoisDeAparar()
        {
            FiltroReservaVOValidator validador = new FiltroReservaVOValidator();
            string sessenta = new string('a', ConstantsValues.TamanhoMaximoBuscaReserva);

            Assert.True(validador.Validate(new FiltroReservaVO { Busca = "   " + sessenta + "   ", Pagina = 1, TamanhoPagina = 20 }).IsValid);

            FluentValidation.Results.ValidationResult resultado = validador.Validate(new FiltroReservaVO { Busca = sessenta + "a", Pagina = 1, TamanhoPagina = 20 });
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == string.Format(ConstantsMessage.ErroReservaBuscaTamanho, ConstantsValues.TamanhoMaximoBuscaReserva));
        }

        [Fact]
        public void ValidadorRecusaBuscaComCaractereNul()
        {
            FiltroReservaVOValidator validador = new FiltroReservaVOValidator();

            FluentValidation.Results.ValidationResult resultado = validador.Validate(new FiltroReservaVO { Busca = "a\0b", Pagina = 1, TamanhoPagina = 20 });

            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroReservaBuscaInvalida);
        }

        [Fact]
        public void ValidadorRecusaSituacaoJuntoComStatus()
        {
            FiltroReservaVOValidator validador = new FiltroReservaVOValidator();

            FluentValidation.Results.ValidationResult resultado = validador.Validate(new FiltroReservaVO { Situacao = ConstantsValues.SituacaoReservaAtivas, Status = StatusReserva.ATIVA.ToString(), Pagina = 1, TamanhoPagina = 20 });

            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroReservaSituacaoComStatus);
        }

        private static CriarReservaVO CriarEntrada()
        {
            return new CriarReservaVO { IdVeiculo = IdVeiculo, DataRetirada = RetiradaPadrao, DataDevolucao = DevolucaoPadrao };
        }

        private static Veiculo CriarVeiculo(StatusVeiculo status = StatusVeiculo.DISPONIVEL, decimal valorDiaria = 150.50m)
        {
            return new Veiculo
            {
                IdVeiculo = IdVeiculo,
                IdEmpresa = IdEmpresa,
                Status = status,
                ValorDiaria = valorDiaria,
                Placa = "ABC1D23",
                Marca = "Jeep",
                Modelo = Modelo,
                AnoModelo = 2024,
                Categoria = new Categoria { Nome = "SUV" },
                Empresa = new Empresa { IdEmpresa = IdEmpresa, NomeFantasia = NomeLocadora, Uf = "SP", Ativo = true }
            };
        }

        private static Reserva CriarReservaDoCliente(int idReserva, int idCliente, StatusReserva status)
        {
            return CriarReservaComRetirada(idReserva, idCliente, status, new DateTime(2026, 12, 10));
        }

        private static Reserva CriarReservaComRetirada(int idReserva, int idCliente, StatusReserva status, DateTime retirada)
        {
            return new Reserva
            {
                IdReserva = idReserva,
                IdCliente = idCliente,
                IdVeiculo = IdVeiculo,
                DataRetiradaPrevista = retirada,
                DataDevolucaoPrevista = retirada.AddDays(2),
                Status = status
            };
        }

        private static Reserva CriarReservaComVeiculo(int idReserva, int idCliente, StatusReserva status, string modelo)
        {
            Reserva reserva = CriarReservaComRetirada(idReserva, idCliente, status, new DateTime(2026, 12, 10));
            reserva.Veiculo = new Veiculo
            {
                IdVeiculo = IdVeiculo,
                Placa = "ABC1D23",
                Marca = "Jeep",
                Modelo = modelo,
                Categoria = new Categoria { Nome = "SUV" },
                Empresa = new Empresa { IdEmpresa = IdEmpresa, NomeFantasia = NomeLocadora, Uf = "SP" }
            };
            return reserva;
        }

        /// <summary>Armazenamento com a locadora do veiculo ativa e SEM vinculo do cliente: o cadastro ainda nao foi pedido.</summary>
        private static ArmazenamentoClienteLocadora CriarArmazenamentoSemVinculo()
        {
            ArmazenamentoClienteLocadora armazenamento = new ArmazenamentoClienteLocadora();
            armazenamento.AdicionarEmpresa(new Empresa { IdEmpresa = IdEmpresa, NomeFantasia = NomeLocadora, Ativo = true });
            return armazenamento;
        }

        /// <summary>Armazenamento com o cliente APROVADO na locadora do veiculo.</summary>
        private static ArmazenamentoClienteLocadora CriarArmazenamento()
        {
            ArmazenamentoClienteLocadora armazenamento = CriarArmazenamentoSemVinculo();
            armazenamento.Semear(IdCliente, IdEmpresa, StatusCadastroCliente.APROVADO);
            return armazenamento;
        }

        private static ReservaService CriarServico(FakeReservaRepository reservas, FakeLogService log, ArmazenamentoClienteLocadora armazenamento)
        {
            IMapper mapper = new MapperConfiguration(opcoes => opcoes.AddProfile<ReservaProfile>(), NullLoggerFactory.Instance).CreateMapper();
            ContextoEmpresa contexto = new ContextoEmpresa(log);
            contexto.DefinirCliente(IdCliente);
            FakeClienteLocadoraRepository repositorioLocadora = new FakeClienteLocadoraRepository(contexto, armazenamento);
            FakeRelogio relogio = new FakeRelogio(AgoraFixo);
            ClienteLocadoraService clienteLocadora = new ClienteLocadoraService(repositorioLocadora, contexto, relogio, mapper, log, new ClientesOptions());
            return new ReservaService(reservas, clienteLocadora, relogio, mapper, log);
        }
    }
}
