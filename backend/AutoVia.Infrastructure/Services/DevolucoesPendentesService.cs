using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>Lista das devolucoes pendentes: le as locacoes em andamento da locadora do contexto e marca as atrasadas pelo relogio.</summary>
    public class DevolucoesPendentesService : IDevolucoesPendentesService
    {
        private readonly ILocacaoRepository _locacaoRepository;
        private readonly IRelogio _relogio;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public DevolucoesPendentesService(ILocacaoRepository locacaoRepository, IRelogio relogio, IMapper mapper, ILogService logService)
        {
            _locacaoRepository = locacaoRepository;
            _relogio = relogio;
            _mapper = mapper;
            _logService = logService;
        }

        /// <summary>Pagina das devolucoes pendentes. Status ausente ou EM_ANDAMENTO traz todas as em andamento; ATRASADA, so as atrasadas.</summary>
        public async Task<PaginaVO<ReturnDevolucaoPendenteVO>> ListarAsync(FiltroDevolucaoPendenteVO filtro)
        {
            try
            {
                DateTime agora = _relogio.Agora;
                bool somenteAtrasadas = filtro.Status == nameof(StatusLocacao.ATRASADA);

                int total = await _locacaoRepository.ContarDevolucoesPendentesAsync(somenteAtrasadas, agora);
                List<Locacao> locacoes = await _locacaoRepository.ListarDevolucoesPendentesAsync(somenteAtrasadas, agora, filtro.Deslocamento, filtro.TamanhoPagina);

                List<ReturnDevolucaoPendenteVO> itens = new List<ReturnDevolucaoPendenteVO>();

                foreach (Locacao locacao in locacoes)
                {
                    ReturnDevolucaoPendenteVO item = _mapper.Map<ReturnDevolucaoPendenteVO>(locacao);
                    item.Atrasada = RegrasDevolucoesPendentes.EhAtrasada(locacao.Status, locacao.DataRetirada, locacao.DataDevolucaoPrevista, agora);
                    itens.Add(item);
                }

                PaginaVO<ReturnDevolucaoPendenteVO> pagina = new PaginaVO<ReturnDevolucaoPendenteVO>(itens, total, filtro.Pagina, filtro.TamanhoPagina);

                return pagina;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarDevolucoesPendentes, GetType().ToString());
                throw;
            }
        }
    }
}
