namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores do contexto de empresa (multi-locadora, T-BE-03b): claim do token, nomes das
    /// politicas de autorizacao e nome do filtro global do EF.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Claim do token de funcionario com o id da empresa (inteiro em texto). Conferida contra o banco.</summary>
        public const string ClaimEmpresa = "emp";

        /// <summary>Politica do ADMINISTRADOR da empresa (role FUNCIONARIO + cargo ADMINISTRADOR).</summary>
        public const string PoliticaAdministrador = "Administrador";

        /// <summary>Politica da empresa operadora da plataforma (role FUNCIONARIO + empresa operadora + cargo ADMINISTRADOR).</summary>
        public const string PoliticaOperadora = "Operadora";

        /// <summary>Nome do filtro global de empresa no AutoViaDbContext (unico ponto de bypass nomeado: SemEscopoEmpresa).</summary>
        public const string NomeFiltroEmpresa = "Empresa";
    }
}
