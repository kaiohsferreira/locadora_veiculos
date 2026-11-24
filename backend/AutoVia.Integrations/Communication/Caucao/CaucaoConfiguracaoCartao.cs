namespace AutoVia.Integrations.Communication.Caucao
{
    /// <summary>
    /// Dados que o front precisa para gerar o token do cartao e se o provedor configurado e a simulacao liberada. A chave
    /// publica nao e segredo.
    /// </summary>
    public class CaucaoConfiguracaoCartao
    {
        /// <summary>Nome do provedor (MERCADOPAGO ou FAKE).</summary>
        public string Provedor { get; set; } = string.Empty;

        /// <summary>public_key da integracao PAGAMENTO (ou a chave fixa do FAKE).</summary>
        public string ChavePublica { get; set; } = string.Empty;

        /// <summary>
        /// Verdadeiro so quando o provedor e o FAKE e a trava dele esta liberada (ICaucaoProvider.SimulacaoLiberada; v1.3). E o
        /// valor que a leitura da retirada devolve como caucaoModoSimulacao, para o GET e o POST concordarem.
        /// </summary>
        public bool ModoSimulacao { get; set; }
    }
}
