namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores do vinculo cliente x locadora (T-BE-06): nome da politica de verificacao do cadastro, limite da
    /// justificativa da recusa e mascara da CNH para quem nao verifica cadastro.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Politica de autorizacao da verificacao do cadastro do cliente (ATENDENTE ou ADMINISTRADOR da locadora).</summary>
        public const string PoliticaVerificadorCadastro = "VerificadorCadastro";

        /// <summary>Tamanho maximo da justificativa da recusa (coluna justificativa_recusa, 500).</summary>
        public const int TamanhoMaximoJustificativaRecusa = 500;

        /// <summary>Quantidade de digitos finais da CNH que continuam visiveis quando a CNH e mascarada.</summary>
        public const int DigitosVisiveisCnhMascarada = 3;

        /// <summary>Caractere que substitui os digitos ocultos da CNH mascarada.</summary>
        public const char CaractereMascaraCnh = '*';

        /// <summary>Secao do appsettings com os limites do cliente (pedidos de aprovacao por locadora).</summary>
        public const string SecaoClientes = "Clientes";

        /// <summary>Chave do teto de locadoras novas em que o cliente pode pedir aprovacao na janela (padrao 5).</summary>
        public const string ChaveLimitePedidosAprovacaoPorDia = "Clientes:LimitePedidosAprovacaoPorDia";

        /// <summary>Teto padrao de locadoras novas por cliente na janela, quando a chave nao esta configurada ou nao e positiva.</summary>
        public const int LimitePedidosAprovacaoPorDiaPadrao = 5;

        /// <summary>Janela do teto de pedidos de aprovacao, em horas corridas (contadas pelo IRelogio).</summary>
        public const int JanelaPedidosAprovacaoHoras = 24;
    }
}
