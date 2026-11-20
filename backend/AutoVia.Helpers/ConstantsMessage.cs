namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens gerais da API, em portugues.
    ///
    /// Regra do projeto: nenhuma mensagem de erro ou de sucesso e escrita como
    /// string literal no codigo. Toda mensagem nova entra aqui (ou no arquivo parcial
    /// do modulo, ex.: ConstantsMessage.Veiculo.cs) e e referenciada pela constante.
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

        public const string ErroPaginaDeslocamentoInvalido = "A página informada excede o limite permitido para esta quantidade de itens.";

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
        public const string ErroBuscarVistoriaSaida = "Não foi possível consultar a vistoria de saída.";
        public const string ErroSalvarVistoriaSaida = "Não foi possível registrar a vistoria de saída.";
        public const string ErroVistoriaSaidaStatus = "A locação deve estar em andamento ou atrasada para registrar a saída.";
        public const string ErroVistoriaSaidaNaoEncontrada = "A vistoria de saída não foi encontrada.";
        public const string ErroVistoriaSaidaDuplicada = "Já existe uma vistoria de saída ativa nesta locação.";
        public const string ErroVistoriaSaidaEntradaObrigatoria = "Registre a vistoria de entrada antes da saída.";
        public const string ErroVistoriaSaidaKm = "A quilometragem de saída não pode ser inferior à retirada ou à entrada.";
        public const string ErroVistoriaSaidaData = "A data de saída não pode ser anterior à retirada ou à entrada.";
        public const string ErroVistoriaSaidaAvariasObrigatorias = "Informe as listas de avarias de entrada e novas avarias, mesmo que vazias.";
        public const string ErroVistoriaSaidaIdsInvalidos = "Informe identificadores positivos e distintos para as avarias de entrada.";
        public const string ErroVistoriaSaidaDescricaoInvalida = "A descrição da avaria não pode conter caractere nulo.";
        public const string ErroVistoriaSaidaAvariaValorObrigatorio = "Informe o valor estimado de cada nova avaria (use 0,00 quando não houver custo).";
        public const string ErroVistoriaSaidaAvariasSomaExcedida = "A soma dos valores estimados das novas avarias não pode passar de 99999999,99.";
    }
}
