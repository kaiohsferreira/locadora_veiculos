namespace AutoVia.Helpers
{
    public static partial class ConstantsMessage
    {
        public const string ErroVistoriaKmObrigatorio = "Informe a quilometragem registrada.";
        public const string ErroVistoriaKmInvalido = "A quilometragem registrada deve ser maior ou igual a zero.";
        public const string ErroVistoriaCombustivelObrigatorio = "Informe o nível de combustível.";
        public const string ErroVistoriaCombustivelInvalido = "O nível de combustível deve estar entre zero e um, com até duas casas decimais.";
        public const string ErroVistoriaObservacoesInvalidas = "As observações não podem conter caractere nulo.";
        public const string ErroVistoriaEntradaDuplicada = "Já existe uma vistoria de entrada ativa para esta locação.";
        public const string ErroVistoriaNaoEncontrada = "A vistoria de entrada não foi encontrada.";
        public const string ErroVistoriaFuncionarioInativo = "O funcionário autenticado não está ativo.";
        public const string ErroVistoriaEntradaStatus = "A locação deve estar em andamento ou atrasada para registrar a vistoria de entrada.";
        public const string ErroVistoriaEntradaKm = "A quilometragem da vistoria de entrada não pode ser inferior à quilometragem de saída registrada na locação.";
        public const string ErroSalvarVistoria = "Não foi possível registrar a vistoria de entrada.";
        public const string ErroBuscarVistoria = "Não foi possível consultar a vistoria de entrada.";
    }
}
