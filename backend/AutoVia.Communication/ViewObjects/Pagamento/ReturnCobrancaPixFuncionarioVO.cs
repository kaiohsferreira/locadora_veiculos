namespace AutoVia.Communication.ViewObjects.Pagamento
{
    /// <summary>
    /// Saida do PIX para o funcionario da locadora: tudo o que o ReturnCobrancaPixVO traz, mais o identificador devolvido pelo
    /// provedor (leitura adotada no contrato pagamento-pix). O cliente nunca recebe este tipo.
    /// </summary>
    public class ReturnCobrancaPixFuncionarioVO : ReturnCobrancaPixVO
    {
        /// <summary>Identificador da cobranca no provedor (id_transacao).</summary>
        public string IdentificadorProvedor { get; set; } = string.Empty;
    }
}
