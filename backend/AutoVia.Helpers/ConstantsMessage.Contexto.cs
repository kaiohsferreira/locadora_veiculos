namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do contexto de empresa (multi-locadora, T-BE-03b). As recusas de login e de sessao
    /// usam ErroCredenciaisInvalidas e ErroSessaoInvalida (mensagens genericas, ja existentes).
    /// </summary>
    public static partial class ConstantsMessage
    {
        /// <summary>Erro ao definir o contexto de empresa da requisicao (id ou funcionario invalido).</summary>
        public const string ErroDefinirContextoEmpresa = "Não foi possível definir o contexto da empresa.";

        /// <summary>Erro ao checar a politica de autorizacao por empresa ou cargo.</summary>
        public const string ErroAutorizarPolitica = "Não foi possível verificar a autorização do usuário.";

        /// <summary>Administrador inicial nao criado: a empresa operadora ainda nao existe no banco.</summary>
        public const string ErroAdminInicialSemEmpresaOperadora = "Não há empresa operadora cadastrada: o administrador inicial não foi criado.";
    }
}
