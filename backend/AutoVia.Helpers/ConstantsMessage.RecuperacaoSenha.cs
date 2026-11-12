namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da recuperacao de senha por link (N1, RF-49). Arquivo parcial de ConstantsMessage.
    /// Textos EXATOS do contrato auth-recuperacao-senha v1. Nenhuma mensagem revela se a conta existe,
    /// e nenhum valor de CPF, e-mail, token, link ou senha entra em mensagem de erro ou de log.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Resposta do pedido (202): mesma para qualquer CPF ---
        /// <summary>Mensagem neutra da solicitacao: nunca diz se a conta existe.</summary>
        public const string InfoRecuperacaoSolicitada = "Se houver uma conta com esses dados, enviamos instruções para o e-mail cadastrado.";

        // --- Link invalido (409): mesma mensagem para expirado, usado, adulterado, inventado ou titular inativo ---
        /// <summary>Link expirado, usado, adulterado, inventado, substituido ou de titular inativo (BusinessException, 409).</summary>
        public const string ErroRedefinicaoLinkInvalido = "O link de redefinição é inválido ou expirou. Solicite um novo.";

        // --- Validacao do RedefinirSenhaVO (400) ---
        public const string ErroRedefinicaoTokenObrigatorio = "O link de redefinição está incompleto. Solicite um novo.";
        public const string ErroRedefinicaoSenhaObrigatoria = "Informe a nova senha.";
        public const string ErroRedefinicaoSenhaTamanho = "A nova senha deve ter entre {0} e {1} caracteres.";
        public const string ErroRedefinicaoSenhaFraca = "A nova senha deve ter ao menos uma letra e um número.";
        public const string ErroRedefinicaoConfirmacaoObrigatoria = "Confirme a nova senha.";
        public const string ErroRedefinicaoConfirmacaoDiferente = "A confirmação não é igual à nova senha.";

        // --- Falhas internas (log e ProblemDetails 500; sem dado pessoal) ---
        public const string ErroSolicitarRecuperacaoSenha = "Não foi possível processar a solicitação de recuperação de senha.";
        public const string ErroRedefinirSenha = "Não foi possível redefinir a senha.";
        public const string ErroBuscarRedefinicaoSenha = "Não foi possível consultar a redefinição de senha.";
        public const string ErroSalvarRedefinicaoSenha = "Não foi possível salvar a redefinição de senha.";
        public const string ErroEnviarEmailRecuperacao = "Não foi possível enviar o e-mail de recuperação de senha.";
        public const string ErroConfiguracaoRecuperacao = "A configuração da recuperação de senha (Autenticacao:Recuperacao) está ausente ou inválida.";

        // --- Fila de pedidos em segundo plano (so log; sem CPF, e-mail, token, hash ou link) ---
        public const string ErroFilaRecuperacaoCheia = "A fila de pedidos de recuperação de senha está cheia.";
        public const string ErroProcessarFilaRecuperacaoSenha = "Não foi possível processar a fila de pedidos de recuperação de senha.";
        public const string ErroProcessarPedidoRecuperacaoSenha = "Não foi possível processar um pedido de recuperação de senha.";
        public const string ErroPedidoRecuperacaoInvalido = "O pedido de recuperação de senha é inválido.";

        // --- Sucesso (so log, sem identificadores) ---
        public const string InfoSenhaRedefinida = "Senha redefinida.";

        // --- E-mail de recuperacao (texto e HTML montados no servico; {0} nome, {1} minutos de validade, {2} link) ---
        public const string EmailRecuperacaoAssunto = "Redefinição de senha da AutoVia";
        public const string EmailRecuperacaoCorpoTexto = "Olá, {0}.\n\nRecebemos um pedido para redefinir a senha da sua conta na AutoVia. Para criar uma nova senha, abra o link abaixo em até {1} minutos:\n\n{2}\n\nSe não foi você, ignore esta mensagem: sua senha continua a mesma.\n\nAutoVia";
        public const string EmailRecuperacaoCorpoHtml = "<p>Olá, {0}.</p><p>Recebemos um pedido para redefinir a senha da sua conta na AutoVia. Para criar uma nova senha, use o link abaixo em até {1} minutos:</p><p><a href=\"{2}\">Redefinir minha senha</a></p><p>Se não foi você, ignore esta mensagem: sua senha continua a mesma.</p><p>AutoVia</p>";
    }
}
