using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Vistorias;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    public class VistoriaService : IVistoriaService
    {
        private readonly IVistoriaRepository _vistoriaRepository;
        private readonly ILocacaoRepository _locacaoRepository;
        private readonly IRelogio _relogio;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public VistoriaService(IVistoriaRepository vistoriaRepository, ILocacaoRepository locacaoRepository,
            IRelogio relogio, IMapper mapper, ILogService logService)
        {
            _vistoriaRepository = vistoriaRepository;
            _locacaoRepository = locacaoRepository;
            _relogio = relogio;
            _mapper = mapper;
            _logService = logService;
        }

        public async Task<ReturnVistoriaVO> RegistrarEntradaAsync(int idLocacao, VistoriaEntradaVO entrada, int idFuncionario)
        {
            try
            {
                Vistoria criada = await _vistoriaRepository.ExecutarComLocacaoBloqueadaAsync(idLocacao,
                    () => RegistrarEntradaSobTravaAsync(idLocacao, entrada, idFuncionario));
                ReturnVistoriaVO retorno = _mapper.Map<ReturnVistoriaVO>(criada);
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
                _logService.Write(ex, ConstantsMessage.ErroSalvarVistoria, GetType().ToString());
                throw;
            }
        }

        private async Task<Vistoria> RegistrarEntradaSobTravaAsync(int idLocacao, VistoriaEntradaVO entrada, int idFuncionario)
        {
            try
            {
                Locacao? locacao = await _locacaoRepository.FindByIdAsync(idLocacao);
                if (locacao == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
                }

                Funcionario? funcionario = await _locacaoRepository.FindActiveFuncionarioByIdAsync(idFuncionario);
                if (funcionario == null)
                {
                    throw new BusinessException(ConstantsMessage.ErroVistoriaFuncionarioInativo);
                }

                if (!RegrasVistoria.LocacaoAceitaVistoria(locacao.Status))
                {
                    throw new BusinessException(ConstantsMessage.ErroVistoriaEntradaStatus);
                }

                if (!RegrasVistoria.KmEntradaValido(entrada.KmRegistrado!.Value, locacao.KmSaida))
                {
                    throw new BusinessException(ConstantsMessage.ErroVistoriaEntradaKm);
                }

                Vistoria? existente = await _vistoriaRepository.FindEntradaByLocacaoAsync(idLocacao);
                if (existente != null)
                {
                    throw new BusinessException(ConstantsMessage.ErroVistoriaEntradaDuplicada);
                }

                DateTime agora = _relogio.Agora;
                Vistoria vistoria = new Vistoria
                {
                    IdLocacao = idLocacao,
                    IdFuncionario = idFuncionario,
                    Tipo = TipoVistoria.ENTRADA,
                    DataHora = agora,
                    KmRegistrado = entrada.KmRegistrado!.Value,
                    NivelCombustivel = entrada.NivelCombustivel!.Value,
                    Observacoes = string.IsNullOrWhiteSpace(entrada.Observacoes) ? null : entrada.Observacoes.Trim(),
                    CreatedAt = agora,
                    UpdatedAt = agora,
                    DisabledAt = null
                };
                Vistoria criada = await _vistoriaRepository.InsertAsync(vistoria);
                return criada;
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
                _logService.Write(ex, ConstantsMessage.ErroSalvarVistoria, GetType().ToString());
                throw;
            }
        }

        public async Task<ReturnVistoriaVO> GetEntradaAsync(int idLocacao)
        {
            try
            {
                Locacao? locacao = await _locacaoRepository.FindByIdAsync(idLocacao);
                if (locacao == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
                }

                Vistoria? vistoria = await _vistoriaRepository.FindEntradaByLocacaoAsync(idLocacao);
                if (vistoria == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVistoriaNaoEncontrada);
                }

                ReturnVistoriaVO retorno = _mapper.Map<ReturnVistoriaVO>(vistoria);
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
                _logService.Write(ex, ConstantsMessage.ErroBuscarVistoria, GetType().ToString());
                throw;
            }
        }
    }
}
