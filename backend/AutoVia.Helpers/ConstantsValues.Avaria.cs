namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores das rotas de avaria (P4-BE-03): teto do corpo multipart das fotos e chaves do 502 de upload.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Folga, em bytes, do corpo multipart alem da foto (bordas, cabecalhos das partes e campos de texto).</summary>
        public const int FolgaCorpoMultipartAvaria = 65536;

        /// <summary>
        /// Teto do corpo das rotas multipart de foto (POST da avaria e PUT da foto): 10 MiB da foto mais a folga.
        /// AutoVia.Helpers nao enxerga AutoVia.Integrations, por isso os 10.485.760 ficam escritos aqui; um teste prende a igualdade com ConstantsArmazenamento.TamanhoMaximoFotoBytes.
        /// </summary>
        public const int TamanhoMaximoCorpoFotoAvaria = 10485760 + FolgaCorpoMultipartAvaria;

        /// <summary>Chave do campo idAvaria no corpo do 502 de upload da foto.</summary>
        public const string ExtensaoProblemaIdAvaria = "idAvaria";

        /// <summary>Chave do campo retryUploadUrl no corpo do 502 de upload da foto.</summary>
        public const string ExtensaoProblemaRetryUploadUrl = "retryUploadUrl";

        /// <summary>Caminho relativo do PUT da foto; {0} = idVistoria e {1} = idAvaria, formatados com CultureInfo.InvariantCulture.</summary>
        public const string FormatoRotaRetryFotoAvaria = "/api/vistorias/{0}/avarias/{1}/foto";

        /// <summary>Maior valor estimado aceito numa avaria (R$, NUMERIC(10,2)); o menor e zero.</summary>
        public const decimal LimiteValorEstimadoAvaria = 99999999.99m;

        /// <summary>
        /// Formato do valor estimado no formulario da avaria: so digitos, com no maximo um ponto decimal e ate duas casas.
        /// Sem sinal, espaco, separador de milhar, virgula ou expoente. \A e \z (e nao $) para nao aceitar quebra de linha no fim.
        /// </summary>
        public const string PadraoValorEstimadoFormulario = @"\A[0-9]+(\.[0-9]{1,2})?\z";

        /// <summary>Teto (R$) da soma dos valores estimados das avarias novas de uma saida; o mesmo limite da apuracao (ErroCobrancasExtrasLimite).</summary>
        public const decimal LimiteSomaValorEstimadoNovasAvariasSaida = 99999999.99m;
    }
}
