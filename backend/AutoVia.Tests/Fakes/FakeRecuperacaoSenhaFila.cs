using AutoVia.Core.ServicesInterface;
using System.Threading.Channels;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// IRecuperacaoSenhaFila em memoria, sem processamento: guarda os pedidos aceitos (Pendentes) para o teste
    /// processar quando quiser. Aceitar = false simula a fila cheia; ExcecaoAoEnfileirar simula falha.
    /// TotalChamadas conta cada Enfileirar, aceito ou nao. O Leitor entrega os mesmos pedidos ao servico hospedado.
    /// </summary>
    public class FakeRecuperacaoSenhaFila : IRecuperacaoSenhaFila
    {
        private readonly Channel<PedidoRecuperacaoSenha> _canal = Channel.CreateUnbounded<PedidoRecuperacaoSenha>();

        /// <summary>Quando false, a fila esta cheia: Enfileirar devolve false e nada e guardado.</summary>
        public bool Aceitar { get; set; } = true;

        /// <summary>Quando nao for null, Enfileirar lanca esta excecao (simula falha inesperada na fila).</summary>
        public Exception? ExcecaoAoEnfileirar { get; set; }

        /// <summary>Quantas vezes Enfileirar foi chamado (aceito ou nao).</summary>
        public int TotalChamadas { get; private set; }

        /// <summary>Pedidos aceitos e ainda nao processados pelo teste, na ordem de chegada.</summary>
        public List<PedidoRecuperacaoSenha> Pendentes { get; } = new List<PedidoRecuperacaoSenha>();

        /// <summary>Leitor do canal, para o servico hospedado de teste.</summary>
        public ChannelReader<PedidoRecuperacaoSenha> Leitor => _canal.Reader;

        /// <summary>Guarda o pedido se a fila aceitar; senao devolve false. Lanca so se ExcecaoAoEnfileirar estiver configurada.</summary>
        public bool Enfileirar(PedidoRecuperacaoSenha pedido)
        {
            TotalChamadas++;

            if (ExcecaoAoEnfileirar != null)
            {
                throw ExcecaoAoEnfileirar;
            }

            if (!Aceitar)
            {
                return false;
            }

            Pendentes.Add(pedido);
            _canal.Writer.TryWrite(pedido);

            return true;
        }
    }
}
