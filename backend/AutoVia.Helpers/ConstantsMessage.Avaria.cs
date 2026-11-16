namespace AutoVia.Helpers
{
    public static partial class ConstantsMessage
    {
        public const string ErroAvariaVistoriaNaoEncontrada = "A vistoria informada não foi encontrada.";
        public const string ErroAvariaNaoEncontrada = "A avaria informada não foi encontrada.";
        public const string ErroAvariaFotoNaoEncontrada = "A foto da avaria não foi encontrada no armazenamento.";
        public const string ErroAvariaDescricaoObrigatoria = "Informe a descrição da avaria.";
        public const string ErroAvariaDescricaoTamanho = "A descrição da avaria deve ter no máximo 200 caracteres.";
        public const string ErroAvariaGravidadeInvalida = "A gravidade deve ser LEVE, MEDIA ou GRAVE.";
        public const string ErroAvariaValorEstimadoInvalido = "O valor estimado deve respeitar NUMERIC(10,2).";
        public const string ErroAvariaFotoObrigatoria = "Envie uma foto JPEG ou PNG de até 10 MiB.";
        public const string ErroAvariaFotoFormatoInvalido = "A foto deve ser JPEG ou PNG válida e ter até 10 MiB.";
        public const string ErroCriarAvaria = "Não foi possível registrar a avaria.";
        public const string ErroConsultarAvaria = "Não foi possível consultar a avaria.";
        public const string ErroArmazenarFotoAvaria = "Não foi possível armazenar a foto da avaria.";
        public const string ErroRemoverFotoAvaria = "Não foi possível remover a foto da avaria.";
        public const string ErroUploadAvariaPendente = "A avaria foi criada, mas o upload da foto falhou. Repita o envio pela rota informada.";
    }
}
