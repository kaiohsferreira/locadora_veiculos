using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Seguros;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Regras de negocio das apolices (P9-BE-01, RF-41): veiculo inexistente ou desativado (404),
    /// numero da apolice unico entre as ativas (409), normalizacao dos textos e destaque das
    /// apolices que vencem em ate 60 dias. O validador de entrada roda antes, no filtro de acao.
    /// </summary>
    public class SeguroService : ISeguroService
    {
        private readonly ISeguroRepository _seguroRepository;
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public SeguroService(ISeguroRepository seguroRepository, IVeiculoRepository veiculoRepository, IMapper mapper, ILogService logService)
        {
            _seguroRepository = seguroRepository;
            _veiculoRepository = veiculoRepository;
            _mapper = mapper;
            _logService = logService;
        }

        /// <summary>
        /// Lista as apolices ativas, paginadas e ordenadas pelo fim da vigencia. Com ApenasAVencer,
        /// traz so as que vencem de hoje ate hoje + 60 dias.
        /// </summary>
        public async Task<PaginaVO<ReturnSeguroVO>> GetAllAsync(FiltroSeguroVO filtro)
        {
            try
            {
                DateOnly hoje = DateOnly.FromDateTime(DateTime.Now);
                DateOnly? fimVigenciaDe = null;
                DateOnly? fimVigenciaAte = null;

                if (filtro.ApenasAVencer)
                {
                    fimVigenciaDe = hoje;
                    fimVigenciaAte = hoje.AddDays(ConstantsValues.DiasAlertaVencimentoSeguro);
                }

                int total = await _seguroRepository.CountAsync(filtro.IdVeiculo, fimVigenciaDe, fimVigenciaAte);
                List<Seguro> seguros = await _seguroRepository.FindAllAsync(filtro.IdVeiculo, fimVigenciaDe, fimVigenciaAte, filtro.Deslocamento, filtro.TamanhoPagina);
                List<ReturnSeguroVO> itens = _mapper.Map<List<ReturnSeguroVO>>(seguros);

                foreach (ReturnSeguroVO item in itens)
                {
                    PreencherDestaqueVencimento(item, hoje);
                }

                PaginaVO<ReturnSeguroVO> pagina = new PaginaVO<ReturnSeguroVO>(itens, total, filtro.Pagina, filtro.TamanhoPagina);

                return pagina;
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
                _logService.Write(ex, ConstantsMessage.ErroListarSeguros, GetType().ToString());
                throw;
            }
        }

        /// <summary>Consulta uma apolice ativa pelo id; lanca NotFoundException se nao existir.</summary>
        public async Task<ReturnSeguroVO> GetByIdAsync(int idSeguro)
        {
            try
            {
                Seguro? seguro = await _seguroRepository.FindByIdAsync(idSeguro);

                if (seguro == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroSeguroNaoEncontrado);
                }

                ReturnSeguroVO retorno = _mapper.Map<ReturnSeguroVO>(seguro);
                PreencherDestaqueVencimento(retorno, DateOnly.FromDateTime(DateTime.Now));

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
                _logService.Write(ex, ConstantsMessage.ErroBuscarSeguro, GetType().ToString());
                throw;
            }
        }

        /// <summary>Cadastra uma apolice; NotFoundException se o veiculo nao existir, BusinessException se o numero ja existir.</summary>
        public async Task<ReturnSeguroVO> InsertAsync(SeguroVO seguroVO)
        {
            try
            {
                Veiculo? veiculo = await _veiculoRepository.FindByIdAsync(seguroVO.IdVeiculo);

                if (veiculo == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                }

                string numeroApolice = seguroVO.NumeroApolice.Trim();

                bool numeroDuplicado = await _seguroRepository.ExistsByNumeroApoliceAsync(numeroApolice, null);

                if (numeroDuplicado)
                {
                    throw new BusinessException(ConstantsMessage.ErroSeguroNumeroApoliceDuplicado);
                }

                Seguro seguro = MontarEntidade(seguroVO, numeroApolice);

                Seguro gravado = await _seguroRepository.InsertAsync(seguro);
                ReturnSeguroVO retorno = _mapper.Map<ReturnSeguroVO>(gravado);
                retorno.PlacaVeiculo = veiculo.Placa;
                PreencherDestaqueVencimento(retorno, DateOnly.FromDateTime(DateTime.Now));

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
                _logService.Write(ex, ConstantsMessage.ErroSalvarSeguro, GetType().ToString());
                throw;
            }
        }

        /// <summary>Altera uma apolice ativa; lanca NotFoundException ou BusinessException conforme a regra.</summary>
        public async Task UpdateAsync(int idSeguro, SeguroVO seguroVO)
        {
            try
            {
                Seguro? existente = await _seguroRepository.FindByIdAsync(idSeguro);

                if (existente == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroSeguroNaoEncontrado);
                }

                Veiculo? veiculo = await _veiculoRepository.FindByIdAsync(seguroVO.IdVeiculo);

                if (veiculo == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroVeiculoNaoEncontrado);
                }

                string numeroApolice = seguroVO.NumeroApolice.Trim();

                bool numeroDuplicado = await _seguroRepository.ExistsByNumeroApoliceAsync(numeroApolice, idSeguro);

                if (numeroDuplicado)
                {
                    throw new BusinessException(ConstantsMessage.ErroSeguroNumeroApoliceDuplicado);
                }

                Seguro seguro = MontarEntidade(seguroVO, numeroApolice);
                seguro.IdSeguro = idSeguro;

                bool atualizou = await _seguroRepository.UpdateAsync(seguro);

                if (!atualizou)
                {
                    throw new NotFoundException(ConstantsMessage.ErroSeguroNaoEncontrado);
                }
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
                _logService.Write(ex, ConstantsMessage.ErroSalvarSeguro, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Converte o VO em entidade e normaliza os textos: seguradora e numero com Trim;
        /// cobertura com Trim e, se ficar vazia, null.
        /// </summary>
        private Seguro MontarEntidade(SeguroVO seguroVO, string numeroApolice)
        {
            try
            {
                Seguro seguro = _mapper.Map<Seguro>(seguroVO);
                seguro.Seguradora = seguroVO.Seguradora.Trim();
                seguro.NumeroApolice = numeroApolice;
                seguro.Cobertura = NormalizarTextoOpcional(seguroVO.Cobertura);

                return seguro;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarSeguro, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Destaque do RF-41: DiasParaVencer = fim da vigencia menos hoje (negativo = ja venceu);
        /// VenceEm60Dias quando faltam de 0 a 60 dias. Funcao pura, sem acesso a dados.
        /// </summary>
        private static void PreencherDestaqueVencimento(ReturnSeguroVO seguro, DateOnly hoje)
        {
            int diasParaVencer = seguro.DataFim.DayNumber - hoje.DayNumber;

            seguro.DiasParaVencer = diasParaVencer;
            seguro.VenceEm60Dias = diasParaVencer >= 0 && diasParaVencer <= ConstantsValues.DiasAlertaVencimentoSeguro;
        }

        /// <summary>Remove os espacos das pontas; null ou vazio vira null. Funcao pura, sem acesso a dados.</summary>
        private static string? NormalizarTextoOpcional(string? texto)
        {
            if (texto == null)
            {
                return null;
            }

            string textoSemEspacos = texto.Trim();

            if (textoSemEspacos.Length == 0)
            {
                return null;
            }

            return textoSemEspacos;
        }
    }
}
