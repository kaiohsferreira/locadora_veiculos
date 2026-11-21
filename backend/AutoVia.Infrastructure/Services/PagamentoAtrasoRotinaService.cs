using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Rotina do sistema (contrato pagamento-atraso v1, P10): marca como ATRASADO os pagamentos vencidos de TODAS as locadoras,
    /// pelo bypass nomeado do repositorio. Data de hoje pelo relogio (IRelogio). Registra no log so o numero marcado. Falha e
    /// registrada aqui e relancada; quem chama (servico hospedado) registra e segue para a proxima rodada.
    /// </summary>
    public class PagamentoAtrasoRotinaService : IPagamentoAtrasoRotinaService
    {
        private readonly IPagamentoRepository _pagamentoRepository;
        private readonly IRelogio _relogio;
        private readonly ILogService _logService;

        public PagamentoAtrasoRotinaService(IPagamentoRepository pagamentoRepository, IRelogio relogio, ILogService logService)
        {
            _pagamentoRepository = pagamentoRepository;
            _relogio = relogio;
            _logService = logService;
        }

        /// <summary>Marca como ATRASADO os pagamentos vencidos de todas as locadoras e devolve so o numero de linhas.</summary>
        public async Task<int> MarcarAtrasosTodasLocadorasAsync()
        {
            try
            {
                DateTime agora = _relogio.Agora;
                DateOnly hoje = DateOnly.FromDateTime(agora);

                int quantidade = await _pagamentoRepository.AtualizarAtrasosTodasLocadorasAsync(hoje, agora);

                string mensagem = string.Format(ConstantsMessage.InfoAtrasosAtualizados, quantidade);
                _logService.WriteInfo(mensagem, GetType().ToString());

                return quantidade;
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
                _logService.Write(ex, ConstantsMessage.ErroLogAtualizarAtrasosTodasLocadoras, GetType().ToString());
                throw;
            }
        }
    }
}
