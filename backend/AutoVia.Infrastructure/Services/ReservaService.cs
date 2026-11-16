using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    public class ReservaService : IReservaService
    {
        private readonly IReservaRepository _reservaRepository;
        private readonly IClienteLocadoraService _clienteLocadoraService;
        private readonly IRelogio _relogio;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public ReservaService(IReservaRepository reservaRepository, IClienteLocadoraService clienteLocadoraService, IRelogio relogio, IMapper mapper, ILogService logService)
        {
            _reservaRepository = reservaRepository;
            _clienteLocadoraService = clienteLocadoraService;
            _relogio = relogio;
            _mapper = mapper;
            _logService = logService;
        }

        public async Task<ReturnReservaCriadaVO> CriarAsync(int idCliente, CriarReservaVO entrada)
        {
            try
            {
                Veiculo? veiculo = await _reservaRepository.FindVeiculoParaReservaAsync(entrada.IdVeiculo);
                if (veiculo == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroReservaVeiculoNaoEncontrado);
                }

                // Antes da transacao da reserva: o repositorio do vinculo recusa gravar dentro de transacao.
                await _clienteLocadoraService.ExigirCadastroAprovadoAsync(idCliente, veiculo.IdEmpresa);

                Reserva reserva = new Reserva
                {
                    IdCliente = idCliente,
                    IdVeiculo = entrada.IdVeiculo,
                    DataReserva = _relogio.Agora,
                    DataRetiradaPrevista = entrada.DataRetirada.ToDateTime(TimeOnly.MinValue),
                    DataDevolucaoPrevista = entrada.DataDevolucao.ToDateTime(TimeOnly.MinValue),
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
                StatusReserva? status = filtro.Status == null ? null : Enum.Parse<StatusReserva>(filtro.Status, false);
                int total = await _reservaRepository.CountAsync(idCliente, status);
                List<Reserva> reservas = await _reservaRepository.FindAllAsync(idCliente, status, filtro.Deslocamento, filtro.TamanhoPagina);
                List<ReturnReservaVO> itens = reservas.Select(MapearReserva).ToList();
                PaginaVO<ReturnReservaVO> pagina = new PaginaVO<ReturnReservaVO>(itens, total, filtro.Pagina, filtro.TamanhoPagina);
                return pagina;
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

        private ReturnReservaVO MapearReserva(Reserva reserva)
        {
            try
            {
                ReturnReservaVO retorno = new ReturnReservaVO
                {
                    IdReserva = reserva.IdReserva,
                    IdVeiculo = reserva.IdVeiculo,
                    DataRetirada = DateOnly.FromDateTime(reserva.DataRetiradaPrevista),
                    DataDevolucao = DateOnly.FromDateTime(reserva.DataDevolucaoPrevista),
                    Status = reserva.Status.ToString(),
                    ValorEstimado = reserva.ValorEstimado
                };
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
