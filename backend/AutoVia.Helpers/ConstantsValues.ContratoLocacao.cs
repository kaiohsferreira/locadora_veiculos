using System.Text.Json;

namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores do contrato digital da locacao (contrato-locacao v5): situacoes, tetos da assinatura, tamanho do nome do assinante,
    /// cabecalho do PNG e a unica configuracao de JSON do snapshot (o hash depende do texto exato, por isso nao pode haver outra).
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Situacao do contrato sem assinatura (derivada: nao existe linha ativa em contrato_locacao).</summary>
        public const string ContratoSituacaoPendente = "PENDENTE_ASSINATURA";

        /// <summary>Situacao do contrato assinado (existe linha ativa em contrato_locacao).</summary>
        public const string ContratoSituacaoAssinado = "ASSINADO";

        /// <summary>Versao do formato do documento guardado no snapshot.</summary>
        public const int ContratoVersaoDocumento = 1;

        /// <summary>Tamanho maximo do nome de quem assina (coluna nome_assinante, VARCHAR(100)).</summary>
        public const int ContratoNomeAssinanteTamanhoMaximo = 100;

        /// <summary>Teto da imagem da assinatura depois de decodificada: 65.536 bytes.</summary>
        public const int ContratoAssinaturaTamanhoMaximoBytes = 65536;

        /// <summary>Teto do texto base64 da assinatura: 65.536 bytes codificados em base64 somam 87.384 caracteres.</summary>
        public const int ContratoAssinaturaBase64TamanhoMaximo = 87384;

        /// <summary>Teto do corpo HTTP do POST de assinatura ([RequestSizeLimit]): o base64 mais o JSON, com folga.</summary>
        public const int ContratoAssinaturaLimiteCorpoBytes = 131072;

        /// <summary>Cabecalho de qualquer PNG valido (89 50 4E 47 0D 0A 1A 0A).</summary>
        public static readonly byte[] ContratoAssinaturaCabecalhoPng = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>
        /// Opcoes unicas do System.Text.Json do snapshot (documento_json), na gravacao e na leitura. O hash SHA-256 e calculado
        /// sobre o texto serializado com estas opcoes.
        /// </summary>
        public static readonly JsonSerializerOptions ContratoOpcoesJson = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
