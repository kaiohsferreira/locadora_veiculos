namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da integracao SEGURADORA (P9-INT-01). Arquivo parcial da classe
    /// ConstantsMessage. {0}, {1} = valores formatados pelo chamador.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Seguradora: validacao da solicitacao (409) ---
        public const string ErroSeguradoraReferenciaInvalida = "A referência do sinistro para a seguradora é obrigatória, com até {0} caracteres (letras, números, hífen e sublinhado).";
        public const string ErroSeguradoraApoliceObrigatoria = "Informe o número da apólice, com até {0} caracteres.";
        public const string ErroSeguradoraPlacaObrigatoria = "Informe a placa do veículo, com até {0} caracteres.";
        public const string ErroSeguradoraDataReferenciaObrigatoria = "Informe a data de referência da consulta de vigência.";
        public const string ErroSeguradoraDataOcorrenciaInvalida = "A data da ocorrência é obrigatória e não pode ser posterior a hoje.";
        public const string ErroSeguradoraTipoInvalido = "O tipo de sinistro informado não é válido.";
        public const string ErroSeguradoraDescricaoTamanho = "A descrição do sinistro deve ter no máximo {0} caracteres.";
        public const string ErroSeguradoraValorPrejuizo = "O valor do prejuízo, quando informado, deve ser maior que zero.";
        public const string ErroSeguradoraProtocoloObrigatorio = "Informe o protocolo do sinistro na seguradora, com até {0} caracteres.";

        // --- Seguradora: regras do provedor ---
        public const string ErroSeguradoraSinistroNaoEncontrado = "O sinistro não foi encontrado na seguradora.";
        public const string ErroSeguradoraTransicaoInvalida = "A seguradora não permite mudar o sinistro de {0} para {1}.";

        // --- Seguradora: configuracao (502) ---
        public const string ErroSeguradoraProvedorNaoSuportado = "O provedor configurado para a seguradora não é suportado.";
        public const string ErroSeguradoraFakeForaDoSandbox = "O provedor de teste da seguradora só pode ser usado no ambiente SANDBOX.";

        // --- Seguradora: falhas inesperadas (502) ---
        public const string ErroSeguradoraConsultarVigencia = "Não foi possível consultar a vigência da apólice na seguradora.";
        public const string ErroSeguradoraAbrirSinistro = "Não foi possível abrir o sinistro na seguradora.";
        public const string ErroSeguradoraConsultarSinistro = "Não foi possível consultar o sinistro na seguradora.";
    }
}
