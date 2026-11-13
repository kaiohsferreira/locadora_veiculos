using System.Threading.Channels;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Fila em memoria dos pedidos de recuperacao de senha (singleton, capacidade
    /// ConstantsValues.CapacidadeFilaPedidosRecuperacao). A requisicao so enfileira; a busca da conta, o limite,
    /// a gravacao do link e o envio do e-mail rodam em segundo plano. Pedidos pendentes se perdem no restart.
    /// </summary>
    public interface IRecuperacaoSenhaFila
    {
        /// <summary>Enfileira sem bloquear e sem lancar. Devolve false se a fila estiver cheia ou se o pedido for nulo.</summary>
        bool Enfileirar(PedidoRecuperacaoSenha pedido);

        /// <summary>Leitor da fila, consumido pelo RecuperacaoSenhaHostedService.</summary>
        ChannelReader<PedidoRecuperacaoSenha> Leitor { get; }
    }
}
