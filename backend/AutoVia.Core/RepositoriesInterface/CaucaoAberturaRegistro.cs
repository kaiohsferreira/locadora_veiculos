namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Caucao pre-autorizada que a abertura da locacao grava junto (contrato caucao-retirada v1.1): valor da configuracao do
    /// servidor, identificador do provedor (pagamento.id_transacao) e referencia enviada (CAUCAO-RES-{idReserva}). Imutavel.
    /// Nunca vem do pedido do funcionario nem do cliente.
    /// </summary>
    public class CaucaoAberturaRegistro
    {
        public CaucaoAberturaRegistro(decimal valor, string idTransacao, string referenciaExterna)
        {
            Valor = valor;
            IdTransacao = idTransacao;
            ReferenciaExterna = referenciaExterna;
        }

        /// <summary>Valor da caucao (Pagamentos:ValorCaucao): o mesmo de locacao.valor_caucao e de pagamento.valor.</summary>
        public decimal Valor { get; }

        /// <summary>Identificador da caucao no provedor, gravado em pagamento.id_transacao (ate 40 caracteres).</summary>
        public string IdTransacao { get; }

        /// <summary>Referencia enviada ao provedor (CAUCAO-RES-{idReserva}); pode ir para o log, nunca para a resposta.</summary>
        public string ReferenciaExterna { get; }
    }
}
