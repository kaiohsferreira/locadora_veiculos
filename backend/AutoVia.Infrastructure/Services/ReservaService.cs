using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    public class ReservaService : IReservaService
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly IReservaRepository _reservaRepository;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public ReservaService(IClienteRepository clienteRepository, IReservaRepository reservaRepository, IMapper mapper, ILogService logService)
        {
            _clienteRepository = clienteRepository;
            _reservaRepository = reservaRepository;
            _mapper = mapper;
            _logService = logService;
        }

        public async Task<ReturnReservaCriadaVO> CriarAsync(int idCliente, CriarReservaVO entrada)
        {
            try
            {
                Cliente? cliente = await _clienteRepository.FindByIdAsync(idCliente);
                if (cliente == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroReservaClienteNaoEncontrado);
                }

                if (cliente.StatusCadastro != StatusCadastroCliente.APROVADO)
                {
                    throw new BusinessException(ConstantsMessage.ErroReservaClienteNaoAprovado);
                }

                Reserva reserva = new Reserva
                {
                    IdCliente = cliente.IdCliente,
                    IdVeiculo = entrada.IdVeiculo,
                    DataReserva = DateTime.Now,
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
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarReserva, GetType().ToString());
                throw;
            }
        }
    }
}
