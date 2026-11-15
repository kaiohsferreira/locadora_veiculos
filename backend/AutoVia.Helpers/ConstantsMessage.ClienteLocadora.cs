namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do vinculo cliente x locadora (T-BE-06): status do cadastro na locadora quando a reserva e barrada,
    /// decisao da locadora (PATCH de verificacao) e erros internos. Nas mensagens com {0}, o valor e o nome fantasia
    /// da locadora. Nenhuma mensagem ecoa CPF, CNH, e-mail nem justificativa.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Reserva: status do cadastro na locadora (409) ---

        /// <summary>409: cadastro pendente nesta locadora. {0} = nome fantasia da locadora.</summary>
        public const string ErroReservaCadastroEmAnalise = "Seu cadastro está em análise pela locadora {0}. Seus dados de cadastro foram compartilhados com ela para a verificação. A reserva fica disponível após a aprovação.";

        /// <summary>409: cadastro recusado nesta locadora. {0} = nome fantasia da locadora.</summary>
        public const string ErroReservaCadastroRecusado = "Seu cadastro foi recusado pela locadora {0}. Entre em contato com ela para regularizar.";

        /// <summary>409: o cliente nao tem vinculo aprovado com a locadora e quem chama nao e o proprio cliente (nada e criado).</summary>
        public const string ErroReservaCadastroNaoAprovado = "Este cliente ainda não tem cadastro aprovado nesta locadora.";

        /// <summary>404: a locadora do veiculo nao existe, esta inativa ou foi desativada.</summary>
        public const string ErroClienteLocadoraEmpresaNaoEncontrada = "A locadora informada não foi encontrada.";

        /// <summary>429: o cliente atingiu o teto de locadoras novas com pedido de aprovacao na janela (nada e gravado).</summary>
        public const string ErroLimitePedidosAprovacao = "Você atingiu o limite de pedidos de aprovação em 24 horas. Tente novamente mais tarde.";

        // --- Verificacao do cadastro pela locadora (PATCH /api/clientes/{id}/verificacao) ---

        /// <summary>409: o cadastro ja foi aprovado pela locadora (APROVADO e final nesta entrega).</summary>
        public const string ErroVerificacaoJaAprovado = "Este cadastro já foi aprovado.";

        /// <summary>400: a decisao e obrigatoria e so pode ser APROVADO ou RECUSADO.</summary>
        public const string ErroVerificacaoDecisaoObrigatoria = "Informe a decisão: APROVADO ou RECUSADO.";

        /// <summary>400: a recusa exige a justificativa.</summary>
        public const string ErroVerificacaoJustificativaObrigatoria = "Informe a justificativa da recusa.";

        /// <summary>400: a justificativa contem caractere NUL (o PostgreSQL recusa o texto e viraria erro 500).</summary>
        public const string ErroVerificacaoJustificativaInvalida = "A justificativa contém caracteres inválidos.";

        /// <summary>400: limite da justificativa. {0} = maximo de caracteres.</summary>
        public const string ErroVerificacaoJustificativaTamanho = "A justificativa deve ter no máximo {0} caracteres.";

        // --- Falhas internas (500, detalhe so no log) ---

        public const string ErroExigirCadastroAprovado = "Não foi possível verificar o cadastro do cliente na locadora.";

        public const string ErroDecidirCadastroCliente = "Não foi possível registrar a decisão sobre o cadastro do cliente.";

        public const string ErroListarMinhasLocadoras = "Não foi possível listar as locadoras do cliente.";

        public const string ErroBuscarVinculoClienteLocadora = "Não foi possível consultar o vínculo do cliente com a locadora.";

        public const string ErroSalvarVinculoClienteLocadora = "Não foi possível salvar o vínculo do cliente com a locadora.";

        /// <summary>500 (falha de integracao): o vinculo foi chamado dentro de uma transacao aberta, o que desfaria o PENDENTE.</summary>
        public const string ErroVinculoDentroDeTransacao = "O vínculo do cliente com a locadora não pode ser gravado dentro de uma transação aberta. Chame a verificação antes da transação.";

        /// <summary>500 (falha de uso): contexto com locadora e cliente ao mesmo tempo (o filtro "Empresa" usaria os dois ids).</summary>
        public const string ErroContextoMisto = "O contexto não pode ter locadora e cliente ao mesmo tempo.";
    }
}
