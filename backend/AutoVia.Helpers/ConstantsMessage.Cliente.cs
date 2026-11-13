namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do modulo de clientes (P2-BE-02, RF-07). Arquivo parcial: as
    /// mensagens gerais ficam em ConstantsMessage.cs e as dos outros modulos nos
    /// seus proprios arquivos parciais.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Cliente: consulta e cadastro ---
        public const string ErroClienteNaoEncontrada = "O cliente informado não foi encontrado.";
        public const string ErroClienteCpfDuplicado = "Já existe um cliente ativo com este CPF.";
        public const string ErroClienteCnhDuplicada = "Já existe um cliente ativo com este número de CNH.";
        public const string ErroClienteEmailDuplicado = "Já existe um cliente ativo com este e-mail.";

        // --- Cliente: verificacao de cadastro (POST /api/clientes/verificar-cadastro) ---
        public const string ErroClienteVerificacaoCampoInvalido = "Informe qual campo verificar: CPF, EMAIL ou CNH.";
        public const string ErroClienteVerificacaoValorInvalido = "O valor informado para verificação é inválido.";

        // --- Cliente: validacao do VO de entrada ---
        public const string ErroClienteNomeObrigatorio = "Informe o nome do cliente.";
        public const string ErroClienteCampoTamanho = "O campo {0} deve ter no máximo {1} caracteres.";
        public const string ErroClienteCpfObrigatorio = "Informe o CPF.";
        public const string ErroClienteCpfInvalido = "O CPF informado é inválido.";
        public const string ErroClienteDataNascimentoObrigatoria = "Informe a data de nascimento.";
        public const string ErroClienteDataNascimentoFutura = "A data de nascimento não pode ser futura.";
        public const string ErroClienteMenorDeIdade = "O cliente deve ter pelo menos {0} anos.";
        public const string ErroClienteEmailObrigatorio = "Informe o e-mail.";
        public const string ErroClienteEmailInvalido = "O e-mail informado é inválido.";
        public const string ErroClienteTelefoneObrigatorio = "Informe o telefone.";
        public const string ErroClienteTelefoneInvalido = "O telefone deve ter 10 ou 11 dígitos (DDD e número).";
        public const string ErroClienteCnhNumeroObrigatorio = "Informe o número da CNH.";
        public const string ErroClienteCnhNumeroInvalido = "O número da CNH deve ter {0} dígitos.";
        public const string ErroClienteCnhCategoriaInvalida = "Informe uma categoria de CNH válida: A, B, AB, C, D ou E.";
        public const string ErroClienteCnhValidadeObrigatoria = "Informe a validade da CNH.";
        public const string ErroClienteCnhVencida = "A CNH informada está vencida.";
        public const string ErroClienteUfInvalida = "A UF deve ter 2 letras.";
        public const string ErroClienteCepInvalido = "O CEP deve ter 8 dígitos.";
        public const string ErroClienteSenhaObrigatoria = "Informe a senha.";
        public const string ErroClienteSenhaTamanho = "A senha deve ter entre {0} e {1} caracteres.";
        public const string ErroClienteSenhaFraca = "A senha deve ter ao menos uma letra e um número.";
        public const string ErroClienteStatusInvalido = "Informe um status de cadastro válido: PENDENTE, APROVADO ou RECUSADO.";

        // --- Cliente: rotulos de campo usados nas mensagens de tamanho (ErroClienteCampoTamanho) ---
        public const string RotuloNomeCliente = "nome";
        public const string RotuloEmailCliente = "e-mail";
        public const string RotuloLogradouroCliente = "logradouro";
        public const string RotuloNumeroEnderecoCliente = "número";
        public const string RotuloComplementoCliente = "complemento";
        public const string RotuloBairroCliente = "bairro";
        public const string RotuloCidadeCliente = "cidade";

        // --- Cliente: erros inesperados (log e ProblemDetails) ---
        public const string ErroListarClientes = "Não foi possível listar os clientes.";
        public const string ErroBuscarCliente = "Não foi possível consultar o cliente.";
        public const string ErroSalvarCliente = "Não foi possível salvar o cliente.";
        public const string ErroVerificarCadastroCliente = "Não foi possível verificar o cadastro.";
    }
}
