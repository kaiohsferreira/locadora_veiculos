using AutoVia.Communication.ViewObjects.PainelOperacional;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    public class PainelOperacionalService : IPainelOperacionalService
    {
        private readonly IPainelOperacionalRepository _painelRepository;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public PainelOperacionalService(IPainelOperacionalRepository painelRepository, IMapper mapper, ILogService logService)
        {
            _painelRepository = painelRepository;
            _mapper = mapper;
            _logService = logService;
        }

        public async Task<ReturnPainelDiarioVO> GetPainelDiarioAsync()
        {
            try
            {
                DateTime inicioDoDia = DateTime.Now.Date;
                DateTime inicioDoProximoDia = inicioDoDia.AddDays(1);
                List<Reserva> reservas = await _painelRepository.FindReservasDoDiaAsync(inicioDoDia, inicioDoProximoDia);
                List<Locacao> locacoes = await _painelRepository.FindLocacoesDoDiaAsync(inicioDoDia, inicioDoProximoDia);
                List<(DateTime DataHora, int IdRegistro, ItemPainelOperacionalVO Item)> retiradas = new List<(DateTime, int, ItemPainelOperacionalVO)>();
                List<(DateTime DataHora, int IdRegistro, ItemPainelOperacionalVO Item)> devolucoes = new List<(DateTime, int, ItemPainelOperacionalVO)>();

                foreach (Reserva reserva in reservas)
                {
                    if (reserva.DataRetiradaPrevista >= inicioDoDia && reserva.DataRetiradaPrevista < inicioDoProximoDia)
                    {
                        ItemPainelOperacionalVO itemRetirada = _mapper.Map<Reserva, ItemPainelOperacionalVO>(reserva,
                            opcoes => opcoes.Items[nameof(ItemPainelOperacionalVO.DataHora)] = reserva.DataRetiradaPrevista);
                        retiradas.Add((reserva.DataRetiradaPrevista, reserva.IdReserva, itemRetirada));
                    }

                    if (reserva.DataDevolucaoPrevista >= inicioDoDia && reserva.DataDevolucaoPrevista < inicioDoProximoDia)
                    {
                        ItemPainelOperacionalVO itemDevolucao = _mapper.Map<Reserva, ItemPainelOperacionalVO>(reserva,
                            opcoes => opcoes.Items[nameof(ItemPainelOperacionalVO.DataHora)] = reserva.DataDevolucaoPrevista);
                        devolucoes.Add((reserva.DataDevolucaoPrevista, reserva.IdReserva, itemDevolucao));
                    }
                }

                foreach (Locacao locacao in locacoes)
                {
                    if (locacao.DataRetirada >= inicioDoDia && locacao.DataRetirada < inicioDoProximoDia)
                    {
                        ItemPainelOperacionalVO itemRetirada = _mapper.Map<Locacao, ItemPainelOperacionalVO>(locacao,
                            opcoes => opcoes.Items[nameof(ItemPainelOperacionalVO.DataHora)] = locacao.DataRetirada);
                        retiradas.Add((locacao.DataRetirada, locacao.IdLocacao, itemRetirada));
                    }

                    if (locacao.DataDevolucaoPrevista >= inicioDoDia && locacao.DataDevolucaoPrevista < inicioDoProximoDia)
                    {
                        ItemPainelOperacionalVO itemDevolucao = _mapper.Map<Locacao, ItemPainelOperacionalVO>(locacao,
                            opcoes => opcoes.Items[nameof(ItemPainelOperacionalVO.DataHora)] = locacao.DataDevolucaoPrevista);
                        devolucoes.Add((locacao.DataDevolucaoPrevista, locacao.IdLocacao, itemDevolucao));
                    }
                }

                List<ItemPainelOperacionalVO> itensRetirada = retiradas
                    .OrderBy(item => item.DataHora)
                    .ThenBy(item => item.IdRegistro)
                    .Select(item => item.Item)
                    .ToList();
                List<ItemPainelOperacionalVO> itensDevolucao = devolucoes
                    .OrderBy(item => item.DataHora)
                    .ThenBy(item => item.IdRegistro)
                    .Select(item => item.Item)
                    .ToList();

                ReturnPainelDiarioVO retorno = new ReturnPainelDiarioVO
                {
                    Data = DateOnly.FromDateTime(inicioDoDia),
                    Retiradas = itensRetirada,
                    Devolucoes = itensDevolucao
                };
                return retorno;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPainelOperacional, GetType().ToString());
                throw;
            }
        }
    }
}
