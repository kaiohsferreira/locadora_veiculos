namespace AutoVia.Helpers
{
    /// <summary>Mensagens de validacao e falha da integracao MULTAS.</summary>
    public static partial class ConstantsMessage
    {
        public const string ErroMultasSolicitacaoInvalida = "Informe uma placa válida e um intervalo de locação válido para consultar infrações.";
        public const string ErroMultasProvedorNaoSuportado = "O provedor de consulta de multas configurado não é suportado.";
        public const string ErroMultasFakeForaDoSandbox = "O provedor de teste de multas (FAKE) só pode ser usado no ambiente SANDBOX.";
        public const string ErroMultasResultadoInvalido = "O provedor de multas retornou um resultado inválido.";
        public const string ErroMultasConsulta = "Não foi possível consultar as infrações de trânsito.";
    }
}