namespace AutoVia.Integrations.Helpers.Email
{
    /// <summary>
    /// Constantes da porta de e-mail (BE-3): chaves da integracao EMAIL, nome do provedor FAKE,
    /// limites de validacao, capacidade da fila e formato da caixa de teste.
    /// Mensagens ficam em ConstantsMessage (AutoVia.Helpers, arquivo ConstantsMessage.Email.cs).
    /// </summary>
    public static class ConstantsEmail
    {
        // --- Chaves de integration_setting (EMAIL) ---
        public const string ChaveProvedor = "provedor";
        public const string ChaveRemetenteEmail = "remetente_email";
        public const string ChaveRemetenteNome = "remetente_nome";

        // --- Nome do provedor de teste (valor da chave 'provedor') ---
        public const string ProvedorFake = "FAKE";

        // --- Fila em memoria e registro do FAKE ---
        public const int CapacidadeFila = 100;
        public const int LimiteRegistrosFake = 500;

        // --- Limites de validacao da mensagem ---
        public const int DestinatarioEmailTamanhoMaximo = 254;
        public const int DestinatarioNomeTamanhoMaximo = 120;
        public const int AssuntoTamanhoMaximo = 200;

        /// <summary>Padrao de e-mail com \z para nao aceitar quebra de linha no fim.</summary>
        public const string PadraoEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+\z";

        /// <summary>Caracteres de quebra de linha: proibidos em assunto e nome (evita injecao de cabecalho).</summary>
        public static readonly char[] CaracteresQuebraDeLinha = { '\r', '\n' };

        // --- Caixa de teste do provedor FAKE (App_Data/email-fake, so em Development + SANDBOX) ---
        public const string PastaAppData = "App_Data";
        public const string PastaEmailFake = "email-fake";
        public const string ExtensaoArquivoFake = ".txt";
        public const string FormatoIdentificadorData = "yyyyMMddHHmmssfff";
        public const string SeparadorIdentificador = "-";
        public const string RotuloDe = "De: ";
        public const string RotuloPara = "Para: ";
        public const string RotuloAssunto = "Assunto: ";
        public const string RotuloTexto = "--- Texto ---";
        public const string RotuloHtml = "--- HTML ---";
        public const string AberturaEndereco = " <";
        public const string FechamentoEndereco = ">";

        // --- Log ---
        public const string MarcadorAusente = "-";
    }
}
