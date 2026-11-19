namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores da fila de verificacao de cadastros da locadora (P2-BE-05, RF-10): tamanho da busca e limite por IP da listagem
    /// de documentos pela equipe (configuravel, padrao 60 por minuto, como as outras politicas de listagem).
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Tamanho maximo da busca da fila (nome, e-mail ou CPF), em caracteres depois de Trim.</summary>
        public const int TamanhoMaximoBuscaClienteVerificacao = 60;

        /// <summary>Chave de configuracao do limite por minuto e por IP da listagem de documentos pela equipe.</summary>
        public const string ChaveLimiteListaDocumentoEquipe = "Clientes:LimiteListaDocumentoEquipePorMinuto";

        /// <summary>Limite padrao, por minuto e por IP, da listagem de documentos pela equipe.</summary>
        public const int LimiteListaDocumentoEquipePorMinutoPadrao = 60;

        /// <summary>Nome da politica de limite por IP da listagem de documentos pela equipe (janela de 1 minuto).</summary>
        public const string PoliticaLimiteListaDocumentoEquipe = "AutoViaListaDocumentoEquipe";
    }
}
