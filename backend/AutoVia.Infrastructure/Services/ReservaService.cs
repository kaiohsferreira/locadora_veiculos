using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Regras da reserva do cliente. Ordem do POST: periodo e retirada (400), pre-checagem do veiculo sem trava (404 e 409),
    /// valor estimado (409), teto de reservas ativas (409), cadastro aprovado na locadora FORA da transacao (404, 409, 429),
    /// e so entao a transacao do repositorio (trava, locadora ativa, status, conflito).
    /// </summary>
    public class ReservaService : IReservaService
    {
        private static readonly StatusReserva[] StatusAtivas = new StatusReserva[] { StatusReserva.ATIVA, StatusReserva.CONFIRMADA };
        private static readonly StatusReserva[] StatusHistorico = new StatusReserva[] { StatusReserva.CANCELADA, StatusReserva.CONCLUIDA };

        private readonly IReservaRepository _reservaRepository;
        private readonly IClienteLocadoraService _clienteLocadoraService;
        private readonly IRelogio _relogio;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IFotoCapaVeiculoService _fotoCapaVeiculoService;

        public ReservaService(IReservaRepository reservaRepository, IClienteLocadoraService clienteLocadoraService, IRelogio relogio, IMapper mapper, ILogService logService, IFotoCapaVeiculoService fotoCapaVeiculoService)
        {
            _reservaRepository = reservaRepository;
            _clienteLocadoraService = clienteLocadoraService;
            _relogio = relogio;
            _mapper = mapper;
            _logService = logService;
            _fotoCapaVeiculoService = fotoCapaVeiculoService;
        }

        public async Task<ReturnReservaCriadaVO> CriarAsync(int idCliente, CriarReservaVO entrada)
        {
            try
            {
                int diarias = RegrasReserva.CalcularDiarias(entrada.DataRetirada, entrada.DataDevolucao);
                ValidarPeriodo(entrada, diarias);

                Veiculo? veiculo = await _reservaRepository.FindVeiculoParaReservaAsync(entrada.IdVeiculo);
                if (veiculo == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroReservaVeiculoNaoEncontrado);
                }

                if (!RegrasReserva.VeiculoAceitaReserva(veiculo.Status))
                {
                    throw new BusinessException(ConstantsMessage.ErroReservaVeiculoIndisponivel);
                }

                decimal valorEstimado = RegrasReserva.CalcularValorEstimado(diarias, veiculo.ValorDiaria);
                if (valorEstimado > ConstantsValues.ValorMaximoEstimadoReserva)
                {
                    throw new BusinessException(ConstantsMessage.ErroReservaValorEstimadoExcedido);
                }

                int reservasAtivas = await _reservaRepository.CountAsync(idCliente, StatusAtivas, null, null);
                if (reservasAtivas >= ConstantsValues.LimiteReservasAtivasPorCliente)
                {
                    throw new BusinessException(string.Format(ConstantsMessage.ErroReservaLimiteAtivas, ConstantsValues.LimiteReservasAtivasPorCliente));
                }

                try
                {
                    await _clienteLocadoraService.ExigirCadastroAprovadoAsync(idCliente, veiculo.IdEmpresa);
                }
                catch (NotFoundException)
                {
                    throw new NotFoundException(ConstantsMessage.ErroReservaVeiculoNaoEncontrado);
                }

                Reserva reserva = new Reserva
                {
                    IdCliente = idCliente,
                    IdVeiculo = entrada.IdVeiculo,
                    DataReserva = _relogio.Agora,
                    DataRetiradaPrevista = entrada.DataRetirada.ToDateTime(TimeOnly.MinValue),
                    DataDevolucaoPrevista = entrada.DataDevolucao.ToDateTime(TimeOnly.MinValue),
                    ValorEstimado = valorEstimado,
                    Status = StatusReserva.ATIVA
                };

                Reserva criada = await _reservaRepository.InsertAtomicallyAsync(reserva);
                ReturnReservaCriadaVO retorno = _mapper.Map<ReturnReservaCriadaVO>(criada);
                return retorno;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (LimiteExcedidoException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarReserva, GetType().ToString());
                throw;
            }
        }

        public async Task<ReturnReservaVO> GetByIdAsync(int idReserva, int idCliente)
        {
            try
            {
                Reserva? reserva = await _reservaRepository.FindByIdAsync(idReserva, idCliente);
                if (reserva == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
                }

                ReturnReservaVO retorno = MapearReserva(reserva);
                List<ReturnReservaVO> itens = new List<ReturnReservaVO> { retorno };
                await _fotoCapaVeiculoService.PreencherAsync(new List<Veiculo> { reserva.Veiculo }, itens);
                return retorno;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarReserva, GetType().ToString());
                throw;
            }
        }

        public async Task<PaginaVO<ReturnReservaVO>> GetAllAsync(int idCliente, FiltroReservaVO filtro)
        {
            try
            {
                IReadOnlyCollection<StatusReserva>? status = ResolverStatus(filtro);
                bool ordemCrescente = filtro.Situacao == ConstantsValues.SituacaoReservaAtivas;
                int? codigoBusca = RegrasReserva.LerCodigoDaBusca(filtro.Busca);
                string? textoBusca = RegrasReserva.LerTextoDaBusca(filtro.Busca);

                int total = await _reservaRepository.CountAsync(idCliente, status, codigoBusca, textoBusca);
                List<Reserva> reservas = await _reservaRepository.FindAllAsync(idCliente, status, codigoBusca, textoBusca, ordemCrescente, filtro.Deslocamento, filtro.TamanhoPagina);
                List<ReturnReservaVO> itens = reservas.Select(MapearReserva).ToList();
                await _fotoCapaVeiculoService.PreencherAsync(reservas.Select(reserva => reserva.Veiculo).ToList(), itens);
                PaginaVO<ReturnReservaVO> pagina = new PaginaVO<ReturnReservaVO>(itens, total, filtro.Pagina, filtro.TamanhoPagina);
                return pagina;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarReservas, GetType().ToString());
                throw;
            }
        }

        public async Task CancelarAsync(int idReserva, int idCliente)
        {
            try
            {
                await _reservaRepository.CancelAtomicallyAsync(idReserva, idCliente);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCancelarReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>Confere, sem banco, o periodo (ao menos uma diaria, no maximo 90) e a retirada de hoje em diante (pelo IRelogio).</summary>
        private void ValidarPeriodo(CriarReservaVO entrada, int diarias)
        {
            try
            {
                if (diarias < 1)
                {
                    throw new ValidationException(new List<ValidationFailure>
                    {
                        new ValidationFailure(nameof(CriarReservaVO.DataDevolucao), ConstantsMessage.ErroReservaPeriodoInvalido)
                    });
                }

                if (diarias > ConstantsValues.DuracaoMaximaReservaDiarias)
                {
                    throw new ValidationException(new List<ValidationFailure>
                    {
                        new ValidationFailure(nameof(CriarReservaVO.DataDevolucao), string.Format(ConstantsMessage.ErroReservaDuracaoMaxima, ConstantsValues.DuracaoMaximaReservaDiarias))
                    });
                }

                DateOnly hoje = DateOnly.FromDateTime(_relogio.Agora);
                if (entrada.DataRetirada < hoje)
                {
                    throw new ValidationException(new List<ValidationFailure>
                    {
                        new ValidationFailure(nameof(CriarReservaVO.DataRetirada), ConstantsMessage.ErroReservaRetiradaNoPassado)
                    });
                }
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>Traduz situacao (ATIVAS ou HISTORICO) ou status para a colecao do repositorio. Nulo significa todos os status.</summary>
        private IReadOnlyCollection<StatusReserva>? ResolverStatus(FiltroReservaVO filtro)
        {
            try
            {
                bool temSituacao = filtro.Situacao != null;
                bool temStatus = filtro.Status != null;

                if (temSituacao && temStatus)
                {
                    throw new ValidationException(new List<ValidationFailure>
                    {
                        new ValidationFailure(nameof(FiltroReservaVO.Situacao), ConstantsMessage.ErroReservaSituacaoComStatus)
                    });
                }

                if (filtro.Situacao == ConstantsValues.SituacaoReservaAtivas)
                {
                    return StatusAtivas;
                }

                if (filtro.Situacao == ConstantsValues.SituacaoReservaHistorico)
                {
                    return StatusHistorico;
                }

                if (temSituacao)
                {
                    throw new ValidationException(new List<ValidationFailure>
                    {
                        new ValidationFailure(nameof(FiltroReservaVO.Situacao), string.Format(ConstantsMessage.ErroReservaSituacaoInvalida, string.Join(", ", ConstantsValues.SituacaoReservaAtivas, ConstantsValues.SituacaoReservaHistorico)))
                    });
                }

                if (temStatus)
                {
                    StatusReserva statusFiltro = Enum.Parse<StatusReserva>(filtro.Status!, false);
                    return new StatusReserva[] { statusFiltro };
                }

                return null;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarReservas, GetType().ToString());
                throw;
            }
        }

        private ReturnReservaVO MapearReserva(Reserva reserva)
        {
            try
            {
                ReturnReservaVO retorno = _mapper.Map<ReturnReservaVO>(reserva);
                return retorno;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarReserva, GetType().ToString());
                throw;
            }
        }
    }
}
