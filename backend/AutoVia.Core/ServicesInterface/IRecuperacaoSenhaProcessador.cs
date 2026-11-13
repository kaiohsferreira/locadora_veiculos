namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Processamento de um pedido de recuperacao de senha em segundo plano (N1, RF-49): busca a conta elegivel
    /// pelo CPF e perfil, aplica o limite por hora, grava o link (so o hash do token) e enfileira o e-mail.
    /// O pedido ja foi respondido ao cliente; por isso o resultado nao volta a ninguem. Erro sai para quem chama
    /// (o servico hospedado registra no log).
    /// </summary>
    public interface IRecuperacaoSenhaProcessador
    {
        /// <summary>Processa o pedido. Conta nao elegivel nao grava nada e nao envia nada.</summary>
        Task ProcessarAsync(PedidoRecuperacaoSenha pedido, CancellationToken cancellationToken);
    }
}
