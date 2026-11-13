namespace AutoVia.Integrations.Communication.Caucao
{
    /// <summary>
    /// Dados que o front precisa para gerar o token do cartao. A chave publica nao e segredo.
    /// </summary>
    public class CaucaoConfiguracaoCartao
    {
        /// <summary>Nome do provedor (MERCADOPAGO ou FAKE).</summary>
        public string Provedor { get; set; } = string.Empty;

        /// <summary>public_key da integracao PAGAMENTO (ou a chave fixa do FAKE).</summary>
        public string ChavePublica { get; set; } = string.Empty;
    }
}
