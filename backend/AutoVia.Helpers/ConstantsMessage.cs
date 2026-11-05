namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens gerais da API, em portugues.
    ///
    /// Regra do projeto: nenhuma mensagem de erro ou de sucesso e escrita como
    /// string literal no codigo. Toda mensagem nova entra aqui (ou na classe de
    /// mensagens do modulo, ex.: ConstantsMessageVeiculo) e e referenciada pela
    /// constante.
    ///
    /// Mensagens com {0}, {1} sao formatadas com string.Format.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Erros genericos (usados pelo middleware de erro) ---
        public const string ErroInterno = "Ocorreu um erro inesperado ao processar a requisição. Tente novamente em instantes.";
        public const string ErroRequisicaoInvalida = "A requisição contém dados inválidos.";
        public const string ErroRecursoNaoEncontrado = "O recurso solicitado não foi encontrado.";
        public const string ErroConflitoRegraNegocio = "A operação não pode ser concluída porque viola uma regra de negócio.";
        public const string ErroIntegracaoExterna = "Não foi possível concluir a comunicação com um serviço externo.";
        public const string ErroBancoDados = "Não foi possível acessar o banco de dados.";
        public const string ErroRestricaoBanco = "Os dados informados violam uma restrição do cadastro (registro duplicado, referência inexistente ou valor fora do permitido).";
        public const string ErroCampoInvalido = "O valor informado para o campo {0} é inválido.";

        // --- Titulos do ProblemDetails (RFC 7807) ---
        public const string TituloErroValidacao = "Erro de validação";
        public const string TituloRecursoNaoEncontrado = "Recurso não encontrado";
        public const string TituloConflito = "Conflito de regra de negócio";
        public const string TituloErroIntegracao = "Falha em serviço externo";
        public const string TituloErroInterno = "Erro interno";

        // --- Paginacao ---
        public const string ErroPaginaInvalida = "O número da página deve ser maior ou igual a 1.";
        public const string ErroTamanhoPaginaInvalido = "O tamanho da página deve estar entre 1 e {0}.";

        // --- Saude da aplicacao ---
        public const string SucessoAplicacaoNoAr = "A API da AutoVia está no ar.";
        public const string StatusSaudeOk = "ok";

        // --- Categoria ---
        public const string ErroCategoriaNaoEncontrada = "A categoria informada não foi encontrada.";
        public const string ErroCategoriaNomeDuplicado = "Já existe uma categoria ativa com este nome.";
        public const string ErroCategoriaNomeObrigatorio = "Informe o nome da categoria.";
        public const string ErroCategoriaNomeTamanho = "O nome da categoria deve ter no máximo {0} caracteres.";
        public const string ErroCategoriaDescricaoTamanho = "A descrição da categoria deve ter no máximo {0} caracteres.";
        public const string ErroCategoriaValorDiaria = "O valor da diária base deve ser maior que zero.";
        public const string ErroCategoriaValorDiariaPrecisao = "O valor da diária base deve ter no máximo 8 dígitos inteiros e 2 casas decimais.";
        public const string ErroListarCategorias = "Não foi possível listar as categorias.";
        public const string ErroBuscarCategoria = "Não foi possível consultar a categoria.";
        public const string ErroSalvarCategoria = "Não foi possível salvar a categoria.";
    }
}
