using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Rota manual do financeiro (contrato pagamento-atraso v1, P10): marca como ATRASADO os pagamentos PENDENTE vencidos da
    /// locadora do contexto. A data de hoje sai do relogio (IRelogio). Registra no log so o numero marcado, sem dado de
    /// cobranca. Falha do repositorio e registrada aqui e relancada (500 pelo middleware).
    /// </summary>
    public class PagamentoAtrasoService : IPagamentoAtrasoService
    {
        private readonly IPagamentoRepository _pagamentoRepository;
        private readonly IRelogio _relogio;
        private readonly ILogService _logService;

        public PagamentoAtrasoService(IPagamentoRepository pagamentoRepository, IRelogio relogio, ILogService logService)
        {
            _pagamentoRepository = pagamentoRepository;
            _relogio = relogio;
            _logService = logService;
        }

        /// <summary>Marca como ATRASADO os pagamentos vencidos da locadora do contexto e devolve so a quantidade.</summary>
        public async Task<ReturnResultadoAtrasosVO> MarcarAtrasosDaLocadoraAsync()
        {
            try
            {
                DateTime agora = _relogio.Agora;
                DateOnly hoje = DateOnly.FromDateTime(agora);

                int quantidade = await _pagamentoRepository.AtualizarAtrasosDaLocadoraAsync(hoje, agora);

                string mensagem = string.Format(ConstantsMessage.InfoAtrasosAtualizados, quantidade);
                _logService.WriteInfo(mensagem, GetType().ToString());

                ReturnResultadoAtrasosVO resultado = new ReturnResultadoAtrasosVO { QuantidadeAtualizada = quantidade };

                return resultado;
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
                _logService.Write(ex, ConstantsMessage.ErroLogAtualizarAtrasosLocadora, GetType().ToString());
                throw;
            }
        }
    }
}
