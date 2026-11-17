namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores do modulo de fotos do veiculo (RF-94 a RF-101 e RF-105): teto de fotos, limites da imagem gravada, chave do objeto
    /// no armazenamento, politica de limite por IP do envio e nome do campo do formulario.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Teto de fotos ativas por veiculo (KF1). Unica fonte do numero no back-end.</summary>
        public const int MaximoFotosPorVeiculo = 15;

        /// <summary>Folga, em bytes, do corpo multipart alem da foto (bordas e cabecalhos das partes).</summary>
        public const int FolgaCorpoMultipartFotoVeiculo = 65536;

        /// <summary>
        /// Teto do corpo do POST de foto, em bytes: 10.485.760 da foto mais a folga multipart.
        /// AutoVia.Helpers nao enxerga AutoVia.Integrations, por isso os 10.485.760 ficam escritos aqui; um teste prende a igualdade com ConstantsArmazenamento.TamanhoMaximoFotoBytes.
        /// </summary>
        public const int TamanhoMaximoCorpoFotoVeiculo = 10485760 + FolgaCorpoMultipartFotoVeiculo;

        /// <summary>Lado maior, em pixels, da imagem gravada no armazenamento.</summary>
        public const int LadoMaximoFotoVeiculoPixels = 1600;

        /// <summary>Qualidade (0 a 100) do JPEG gravado.</summary>
        public const int QualidadeJpegFotoVeiculo = 85;

        /// <summary>Quantas imagens podem ser decodificadas ao mesmo tempo no processo.</summary>
        public const int MaximoProcessamentosSimultaneosFoto = 2;

        /// <summary>Quantas chamadas podem ficar AGUARDANDO vaga do limite de processamento; a seguinte e recusada na hora.</summary>
        public const int MaximoFotosNaFilaDeProcessamento = 8;

        /// <summary>Tempo maximo, em segundos, de espera pela vaga do limite de processamento; estourado, a chamada e recusada.</summary>
        public const int TempoMaximoEsperaProcessamentoFotoSegundos = 30;

        /// <summary>Chave do objeto no armazenamento; {0} = idVeiculo e {1} = GUID, formatados com CultureInfo.InvariantCulture.</summary>
        public const string FormatoChaveFotoVeiculo = "veiculos/{0}/fotos/{1}.jpg";

        /// <summary>Formato do GUID dentro da chave: 32 digitos hexadecimais, sem hifen.</summary>
        public const string FormatoGuidChaveFotoVeiculo = "N";

        /// <summary>Formato de ida e volta do .NET usado no campo expiraEm das fotos.</summary>
        public const string FormatoExpiracaoUrlFoto = "O";

        /// <summary>Nome da politica de limite por IP do envio de foto (janela de 1 minuto).</summary>
        public const string PoliticaLimiteEnvioFotoVeiculo = "AutoViaEnvioFotoVeiculo";

        /// <summary>Chave de configuracao do limite por minuto do envio de foto.</summary>
        public const string ChaveLimiteEnvioFotoVeiculo = "Veiculos:LimiteEnvioFotoPorMinuto";

        /// <summary>Limite padrao, por minuto e por IP, do envio de foto.</summary>
        public const int LimiteEnvioFotoVeiculoPorMinutoPadrao = 60;

        /// <summary>Nome do campo do formulario (multipart) que recebe a foto; usado como chave nas falhas de validacao.</summary>
        public const string CampoFotoVeiculo = "foto";

        /// <summary>Tipo de midia do formulario de envio de foto (atributo Consumes da rota).</summary>
        public const string TipoMultipartFormData = "multipart/form-data";
    }
}
