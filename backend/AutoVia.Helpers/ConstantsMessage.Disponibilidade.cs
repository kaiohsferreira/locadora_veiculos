namespace AutoVia.Helpers
{
    public static partial class ConstantsMessage
    {
        public const string ErroDisponibilidadeCidadeInvalida = "Informe uma cidade de até {0} caracteres para filtrar pela localização.";
        public const string ErroDisponibilidadeUfInvalida = "Informe uma UF brasileira válida para filtrar pela localização.";
        public const string ErroDisponibilidadeDataObrigatoria = "Informe as datas de retirada e devolução.";
        public const string ErroDisponibilidadePeriodoInvalido = "A data de devolução deve ser posterior à data de retirada.";
        public const string ErroDisponibilidadeCategoriaInvalida = "A categoria deve ser maior que zero.";
        public const string ErroDisponibilidadeValorInvalido = "Os valores da diária não podem ser negativos.";
        public const string ErroDisponibilidadeFaixaInvalida = "O valor máximo deve ser maior ou igual ao valor mínimo.";
        public const string ErroConsultarDisponibilidade = "Não foi possível consultar os veículos disponíveis.";
    }
}
