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
    }
}
