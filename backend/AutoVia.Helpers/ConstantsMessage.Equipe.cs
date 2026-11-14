namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da equipe da locadora (multi-locadora, P10-BE-03): validacao do funcionario, unicidade (CPF na
    /// plataforma inteira e matricula na locadora), travas do ADMINISTRADOR (proprio acesso e ultimo administrador
    /// ativo) e erros de leitura e gravacao. Nenhuma mensagem ecoa CPF, e-mail, matricula ou senha.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Consulta e 404 -------------------------------------------------------------------

        /// <summary>404: inexistente, desativado (DisabledAt) ou de outra locadora.</summary>
        public const string ErroEquipeFuncionarioNaoEncontrado = "O funcionário informado não foi encontrado.";

        // --- Unicidade e travas (409) ----------------------------------------------------------

        /// <summary>409: ja existe funcionario com DisabledAt nulo com o CPF, em qualquer locadora.</summary>
        public const string ErroEquipeCpfDuplicado = "Já existe um funcionário cadastrado com este CPF.";

        /// <summary>409: ja existe funcionario com DisabledAt nulo com a matricula, na mesma locadora.</summary>
        public const string ErroEquipeMatriculaDuplicada = "Já existe um funcionário com esta matrícula nesta locadora.";

        /// <summary>409: a troca de cargo ou a desativacao deixaria a locadora sem administrador ativo.</summary>
        public const string ErroEquipeUltimoAdministrador = "A locadora precisa manter pelo menos um administrador ativo.";

        /// <summary>409: o ADMINISTRADOR tentou desativar a si mesmo ou trocar o proprio cargo.</summary>
        public const string ErroEquipePropriaConta = "Você não pode alterar o próprio acesso. Peça a outro administrador.";

        // --- Validacao (400) -------------------------------------------------------------------

        /// <summary>Limite da busca da listagem: {0} = maximo de caracteres.</summary>
        public const string ErroEquipeBuscaTamanho = "A busca deve ter no máximo {0} caracteres.";

        public const string ErroEquipeSituacaoObrigatoria = "Informe a situação do funcionário (ativo ou inativo).";

        public const string ErroEquipeNomeObrigatorio = "Informe o nome do funcionário.";

        /// <summary>Tamanho maximo de texto: {0} = rotulo do campo, {1} = limite.</summary>
        public const string ErroEquipeCampoTamanho = "O campo {0} deve ter no máximo {1} caracteres.";

        public const string ErroEquipeCpfObrigatorio = "Informe o CPF do funcionário.";

        public const string ErroEquipeCpfInvalido = "O CPF informado é inválido.";

        public const string ErroEquipeMatriculaObrigatoria = "Informe a matrícula do funcionário.";

        public const string ErroEquipeEmailObrigatorio = "Informe o e-mail do funcionário.";

        public const string ErroEquipeEmailInvalido = "O e-mail informado é inválido.";

        public const string ErroEquipeDataAdmissaoObrigatoria = "Informe a data de admissão.";

        /// <summary>400: a data de admissao nao pode ser posterior a hoje (decisao de 14/11/2026).</summary>
        public const string ErroEquipeDataAdmissaoFutura = "A data de admissão não pode ser futura.";

        public const string ErroEquipeCargoObrigatorio = "Informe o cargo do funcionário.";

        public const string ErroEquipeSenhaInicialObrigatoria = "Informe a senha inicial do funcionário.";

        /// <summary>Faixa de tamanho da senha inicial: {0} = minimo, {1} = maximo.</summary>
        public const string ErroEquipeSenhaInicialTamanho = "A senha inicial deve ter entre {0} e {1} caracteres.";

        /// <summary>Regra da senha inicial: ao menos uma letra maiuscula e um numero.</summary>
        public const string ErroEquipeSenhaInicialFraca = "A senha inicial deve ter ao menos uma letra maiúscula e um número.";

        // --- Falhas internas (500, detalhe so no log) ------------------------------------------

        public const string ErroListarEquipe = "Não foi possível listar a equipe.";

        public const string ErroBuscarFuncionario = "Não foi possível consultar o funcionário.";

        public const string ErroSalvarFuncionario = "Não foi possível salvar o funcionário.";

        public const string ErroAlterarSituacaoFuncionario = "Não foi possível alterar a situação do funcionário.";
    }
}
