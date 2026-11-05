namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores fixos de comportamento da aplicacao (nao sao credenciais nem
    /// configuracao de integracao, que ficam no banco).
    /// </summary>
    public static class ConstantsValues
    {
        // --- Cultura e formatos (pt-BR) ---
        public const string CulturaPadrao = "pt-BR";
        public const string FormatoData = "dd/MM/yyyy";
        public const string FormatoDataHora = "dd/MM/yyyy HH:mm";
        public const string FormatoDataHoraSegundos = "dd/MM/yyyy HH:mm:ss";

        // --- Paginacao (padrao do Prompt 3: pagina base 1, tamanho 20, maximo 100) ---
        public const int PaginaPadrao = 1;
        public const int TamanhoPaginaPadrao = 20;
        public const int TamanhoPaginaMaximo = 100;

        // --- Marcador de valor nao configurado no seed das integracoes ---
        public const string MarcadorValorNaoDefinido = "DEFINIR_NO_BANCO";

        // --- CORS ---
        public const string PoliticaCors = "AutoViaFrontend";
        public const string OrigemFrontendLocal = "http://localhost:5173";

        // --- Categoria ---
        public const int TamanhoMaximoNomeCategoria = 50;
        public const int TamanhoMaximoDescricaoCategoria = 200;
    }
}
