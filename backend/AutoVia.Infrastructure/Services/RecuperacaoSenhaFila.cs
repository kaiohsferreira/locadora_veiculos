using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System.Threading.Channels;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Fila em memoria (singleton) dos pedidos de recuperacao, com capacidade ConstantsValues.CapacidadeFilaPedidosRecuperacao.
    /// Enfileirar usa TryWrite: nunca bloqueia a requisicao e nunca lanca. Fila cheia nao e registrada aqui
    /// (quem chama, o RecuperacaoSenhaService, registra sem dado pessoal). Pedido nulo ou falha vao para o log.
    /// </summary>
    public class RecuperacaoSenhaFila : IRecuperacaoSenhaFila
    {
        private readonly Channel<PedidoRecuperacaoSenha> _canal;
        private readonly ILogService _logService;

        public RecuperacaoSenhaFila(ILogService logService)
        {
            _logService = logService;
            _canal = Channel.CreateBounded<PedidoRecuperacaoSenha>(new BoundedChannelOptions(ConstantsValues.CapacidadeFilaPedidosRecuperacao)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
        }

        /// <summary>Leitor do canal, consumido pelo servico hospedado.</summary>
        public ChannelReader<PedidoRecuperacaoSenha> Leitor => _canal.Reader;

        /// <summary>Enfileira sem bloquear. false = fila cheia ou pedido nulo; nunca lanca.</summary>
        public bool Enfileirar(PedidoRecuperacaoSenha pedido)
        {
            try
            {
                if (pedido == null)
                {
                    _logService.Write(ConstantsMessage.ErroPedidoRecuperacaoInvalido, ConstantsMessage.ErroPedidoRecuperacaoInvalido, GetType().ToString());
                    return false;
                }

                bool enfileirado = _canal.Writer.TryWrite(pedido);

                return enfileirado;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroProcessarFilaRecuperacaoSenha, GetType().ToString());
                return false;
            }
        }
    }
}
