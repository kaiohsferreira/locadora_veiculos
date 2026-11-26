using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Lista "Minhas locacoes" do cliente (RF-46). A consulta e a posse ficam no repositorio; aqui entram a situacao, o
    /// atraso (pelo instante do relogio), o total e a capa. Falha da capa nao derruba a lista (regra do servico de capa).
    /// </summary>
    public class MinhasLocacoesService : IMinhasLocacoesService
    {
        private readonly ILocacaoRepository _locacaoRepository;
        private readonly IRelogio _relogio;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IFotoCapaVeiculoService _fotoCapaVeiculoService;

        public MinhasLocacoesService(ILocacaoRepository locacaoRepository, IRelogio relogio, IMapper mapper, ILogService logService, IFotoCapaVeiculoService fotoCapaVeiculoService)
        {
            _locacaoRepository = locacaoRepository;
            _relogio = relogio;
            _mapper = mapper;
            _logService = logService;
            _fotoCapaVeiculoService = fotoCapaVeiculoService;
        }

        /// <summary>Pagina as locacoes do cliente na situacao pedida, com atraso, capa e total.</summary>
        public async Task<PaginaVO<ReturnMinhaLocacaoVO>> ListarAsync(int idCliente, FiltroMinhaLocacaoVO filtro)
        {
            try
            {
                IReadOnlyCollection<StatusLocacao> status = RegrasMinhasLocacoes.SituacaoParaStatus(filtro.Situacao);
                int total = await _locacaoRepository.ContarMinhasLocacoesAsync(idCliente, status);
                List<LocacaoClienteRegistro> registros = await _locacaoRepository.ListarMinhasLocacoesAsync(idCliente, status, filtro.Deslocamento, filtro.TamanhoPagina);

                DateTime agora = _relogio.Agora;
                List<ReturnMinhaLocacaoVO> itens = new List<ReturnMinhaLocacaoVO>();
                List<Veiculo?> veiculos = new List<Veiculo?>();

                foreach (LocacaoClienteRegistro registro in registros)
                {
                    ReturnMinhaLocacaoVO item = _mapper.Map<ReturnMinhaLocacaoVO>(registro);
                    item.DefinirIdVeiculo(registro.IdVeiculo);
                    item.Atrasada = RegrasMinhasLocacoes.EstaAtrasada(item.DevolucaoPrevistaEfetiva, agora, registro.Status);
                    itens.Add(item);
                    veiculos.Add(MontarVeiculoDaCapa(registro));
                }

                await _fotoCapaVeiculoService.PreencherAsync(veiculos, itens);

                PaginaVO<ReturnMinhaLocacaoVO> pagina = new PaginaVO<ReturnMinhaLocacaoVO>(itens, total, filtro.Pagina, filtro.TamanhoPagina);
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
            catch (LimiteExcedidoException)
            {
                throw;
            }
            catch (FluentValidation.ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarMinhasLocacoes, GetType().ToString());
                throw;
            }
        }

        /// <summary>Entrega ao servico de capa o veiculo da linha com as fotos de capa que a consulta ja trouxe (sem consulta extra).</summary>
        private Veiculo MontarVeiculoDaCapa(LocacaoClienteRegistro registro)
        {
            try
            {
                registro.Veiculo.Fotos = registro.FotosCapa;

                return registro.Veiculo;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarMinhasLocacoes, GetType().ToString());
                throw;
            }
        }
    }
}
