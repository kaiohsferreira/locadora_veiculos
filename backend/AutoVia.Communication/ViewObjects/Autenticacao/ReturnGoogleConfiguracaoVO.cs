namespace AutoVia.Communication.ViewObjects.Autenticacao
{
    /// <summary>
    /// VO de saida da configuracao publica do login com Google (GET /api/auth/google/configuracao).
    /// ClientId so vem preenchido quando Habilitado. Nao traz segredo nem dado de cliente.
    /// </summary>
    public class ReturnGoogleConfiguracaoVO
    {
        /// <summary>true = o botao do Google aparece (integracao ativa, Client ID configurado e provedor utilizavel).</summary>
        public bool Habilitado { get; set; }

        /// <summary>Client ID publico do Google; nulo quando nao habilitado.</summary>
        public string? ClientId { get; set; }

        /// <summary>true = provedor de teste (FAKE) efetivo; so existe em Development.</summary>
        public bool ModoTeste { get; set; }
    }
}
