using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>Regras de negocio para iniciar locacao a partir de reserva.</summary>
    public class LocacaoService : ILocacaoService
    {
        private readonly ILocacaoRepository _locacaoRepository;
        private readonly IRelogio _relogio;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public LocacaoService(ILocacaoRepository locacaoRepository, IRelogio relogio, IMapper mapper, ILogService logService)
        {
            _locacaoRepository = locacaoRepository;
            _relogio = relogio;
            _mapper = mapper;
            _logService = logService;
        }

        /// <summary>Inicia a locacao de uma reserva valida para um funcionario ativo.</summary>
        public async Task<ReturnLocacaoVO> StartFromReservaAsync(LocacaoVO locacaoVO, int idFuncionario)
        {
            try
            {
                Reserva? reserva = await _locacaoRepository.FindReservaByIdAsync(locacaoVO.IdReserva);

                if (reserva == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
                }

                if (!RegrasLocacao.ReservaPermiteAbrirLocacao(reserva.Status))
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoReservaEstadoInvalido);
                }

                if (reserva.Veiculo == null || reserva.Veiculo.DisabledAt != null || !RegrasLocacao.VeiculoPermiteAbrirLocacao(reserva.Veiculo.Status))
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoVeiculoIndisponivel);
                }

                bool existeLocacao = await _locacaoRepository.ExistsActiveByReservaAsync(locacaoVO.IdReserva);

                if (existeLocacao)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoReservaDuplicada);
                }

                Funcionario? funcionario = await _locacaoRepository.FindActiveFuncionarioByIdAsync(idFuncionario);

                if (funcionario == null)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoFuncionarioNaoEncontrado);
                }

                DateTime dataRetirada = _relogio.Agora;

                Locacao criada = await _locacaoRepository.StartFromReservaAsync(locacaoVO.IdReserva, idFuncionario, dataRetirada);
                ReturnLocacaoVO retorno = _mapper.Map<ReturnLocacaoVO>(criada);

                return retorno;
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Consulta uma locacao ativa; registros desativados contam como inexistentes.</summary>
        public async Task<ReturnLocacaoVO> GetByIdAsync(int idLocacao)
        {
            try
            {
                Locacao? locacao = await _locacaoRepository.FindByIdAsync(idLocacao);

                if (locacao == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
                }

                ReturnLocacaoVO retorno = _mapper.Map<ReturnLocacaoVO>(locacao);

                return retorno;
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }
    }
}
