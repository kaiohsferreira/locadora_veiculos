namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do cadastro publico de locadora (multi-locadora, P10-BE-01): validacao do cadastro,
    /// unicidade (CNPJ, e-mail da empresa e CPF do responsavel), verificacao publica e erros de gravacao.
    /// Nenhuma mensagem ecoa CNPJ, CPF, e-mail ou senha.
    /// </summary>
    public static partial class ConstantsMessage
    {
        /// <summary>404 quando a chave Locadoras:CadastroPublicoHabilitado esta desligada.</summary>
        public const string ErroLocadoraCadastroIndisponivel = "O cadastro de locadoras não está disponível no momento.";

        /// <summary>409: ja existe empresa ativa (DisabledAt nulo) com o CNPJ informado.</summary>
        public const string ErroLocadoraCnpjDuplicado = "Já existe uma locadora cadastrada com este CNPJ.";

        /// <summary>409: ja existe empresa ativa (DisabledAt nulo) com o e-mail informado.</summary>
        public const string ErroLocadoraEmailDuplicado = "Já existe uma locadora cadastrada com este e-mail.";

        /// <summary>409: ja existe funcionario ativo (DisabledAt nulo) com o CPF do responsavel, de qualquer locadora.</summary>
        public const string ErroLocadoraResponsavelCpfDuplicado = "Já existe um funcionário cadastrado com este CPF.";

        public const string ErroLocadoraRazaoSocialObrigatoria = "Informe a razão social da locadora.";

        public const string ErroLocadoraNomeFantasiaObrigatorio = "Informe o nome fantasia da locadora.";

        /// <summary>Tamanho maximo de texto: {0} = rotulo do campo, {1} = limite.</summary>
        public const string ErroLocadoraCampoTamanho = "O campo {0} deve ter no máximo {1} caracteres.";

        public const string ErroLocadoraCnpjObrigatorio = "Informe o CNPJ da locadora.";

        /// <summary>CNPJ sem 14 digitos, com digito verificador errado, sequencia repetida ou o CNPJ da empresa operadora.</summary>
        public const string ErroLocadoraCnpjInvalido = "O CNPJ informado é inválido.";

        public const string ErroLocadoraTelefoneObrigatorio = "Informe o telefone da locadora.";

        public const string ErroLocadoraTelefoneInvalido = "O telefone deve ter 10 ou 11 dígitos (DDD e número).";

        public const string ErroLocadoraEmailObrigatorio = "Informe o e-mail da locadora.";

        public const string ErroLocadoraEmailInvalido = "O e-mail informado é inválido.";

        public const string ErroLocadoraCepInvalido = "O CEP deve ter 8 dígitos.";

        public const string ErroLocadoraLogradouroObrigatorio = "Informe o logradouro da sede.";

        public const string ErroLocadoraNumeroObrigatorio = "Informe o número do endereço da sede.";

        public const string ErroLocadoraBairroObrigatorio = "Informe o bairro da sede.";

        public const string ErroLocadoraCidadeObrigatoria = "Informe a cidade da sede.";

        public const string ErroLocadoraUfInvalida = "A UF deve ter 2 letras.";

        public const string ErroLocadoraResponsavelNomeObrigatorio = "Informe o nome do responsável.";

        public const string ErroLocadoraResponsavelCpfObrigatorio = "Informe o CPF do responsável.";

        public const string ErroLocadoraResponsavelCpfInvalido = "O CPF do responsável é inválido.";

        public const string ErroLocadoraResponsavelEmailObrigatorio = "Informe o e-mail do responsável.";

        public const string ErroLocadoraResponsavelEmailInvalido = "O e-mail do responsável é inválido.";

        public const string ErroLocadoraSenhaObrigatoria = "Informe a senha.";

        /// <summary>Faixa de tamanho da senha: {0} = minimo, {1} = maximo.</summary>
        public const string ErroLocadoraSenhaTamanho = "A senha deve ter entre {0} e {1} caracteres.";

        /// <summary>Regra da tela do responsavel: ao menos uma letra maiuscula e um numero.</summary>
        public const string ErroLocadoraSenhaFraca = "A senha deve ter ao menos uma letra maiúscula e um número.";

        public const string ErroLocadoraAceiteTermosObrigatorio = "É preciso aceitar os termos de uso e a política de privacidade.";

        public const string ErroLocadoraVerificacaoCampoInvalido = "Informe qual campo verificar: CNPJ ou EMAIL.";

        public const string ErroLocadoraVerificacaoValorInvalido = "O valor informado para verificação é inválido.";

        /// <summary>500 da verificacao publica: o detalhe tecnico vai so para o log.</summary>
        public const string ErroVerificarCadastroLocadora = "Não foi possível verificar o cadastro.";

        /// <summary>500 do cadastro: o detalhe tecnico vai so para o log.</summary>
        public const string ErroSalvarLocadora = "Não foi possível cadastrar a locadora.";

        /// <summary>Falha ao consultar empresas ou funcionarios para a unicidade (so vai para o log).</summary>
        public const string ErroConsultarLocadora = "Não foi possível consultar a locadora.";

        // --- Rotulos de campo usados nas mensagens de tamanho (ErroLocadoraCampoTamanho) ---
        public const string RotuloRazaoSocialLocadora = "razão social";

        public const string RotuloNomeFantasiaLocadora = "nome fantasia";

        public const string RotuloEmailLocadora = "e-mail";

        public const string RotuloLogradouroLocadora = "logradouro";

        public const string RotuloNumeroLocadora = "número";

        public const string RotuloComplementoLocadora = "complemento";

        public const string RotuloBairroLocadora = "bairro";

        public const string RotuloCidadeLocadora = "cidade";

        public const string RotuloResponsavelNomeLocadora = "nome do responsável";

        public const string RotuloResponsavelEmailLocadora = "e-mail do responsável";
    }
}
