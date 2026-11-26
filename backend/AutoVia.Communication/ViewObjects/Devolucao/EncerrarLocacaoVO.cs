namespace AutoVia.Communication.ViewObjects.Devolucao
{
    /// <summary>
    /// Corpo do encerramento da locacao (devolucao-encerramento v4). So a politica de cobranca e o total que a tela mostrou;
    /// valor da caucao, da diaria, km e data nunca vem do cliente: o servidor le tudo do banco.
    /// </summary>
    public sealed class EncerrarLocacaoVO
    {
        /// <summary>Politica de cobranca da categoria que a tela simulou (o mesmo campo da apuracao).</summary>
        public int IdConfiguracao { get; set; }

        /// <summary>Versao da politica que a tela simulou (o mesmo campo da apuracao).</summary>
        public int VersaoEsperada { get; set; }

        /// <summary>Total que a caixa de confirmacao mostrou (valorTotalPrevisto do GET devolucao).</summary>
        public decimal TotalConfirmado { get; set; }
    }
}
