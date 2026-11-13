namespace AutoVia.Communication.ViewObjects.Autenticacao
{
    /// <summary>
    /// VO de saida do pedido de recuperacao de senha (202). Traz sempre a mesma mensagem neutra,
    /// qualquer que seja o resultado interno. Nao traz CPF, e-mail nem dado da conta.
    /// </summary>
    public class ReturnRecuperacaoSolicitadaVO
    {
        /// <summary>Mensagem neutra: se houver uma conta com esses dados, enviamos instrucoes ao e-mail cadastrado.</summary>
        public string Mensagem { get; set; } = string.Empty;
    }
}
