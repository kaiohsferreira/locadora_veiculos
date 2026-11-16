namespace AutoVia.Helpers
{
    /// <summary>
    /// Limite de requisicoes das leituras publicas de veiculo (busca de disponiveis e catalogo de seminovos),
    /// veiculo.contract.json v3 (D-3). Arquivo parcial de ConstantsValues: politica propria, separada do login.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Politica de limite por IP das leituras publicas de veiculo (busca e catalogo), 1 minuto.</summary>
        public const string PoliticaLimiteLeituraPublica = "AutoViaLeituraPublica";

        /// <summary>Chave de configuracao do limite por minuto das leituras publicas de veiculo.</summary>
        public const string ChaveLimiteLeituraPublica = "Veiculos:LimiteLeituraPublicaPorMinuto";

        /// <summary>Limite padrao por minuto e por IP das leituras publicas de veiculo.</summary>
        public const int LimiteLeituraPublicaPorMinutoPadrao = 60;
    }
}
