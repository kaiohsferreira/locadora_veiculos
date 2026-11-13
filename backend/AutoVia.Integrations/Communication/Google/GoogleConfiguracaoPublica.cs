namespace AutoVia.Integrations.Communication.Google
{
    /// <summary>
    /// Configuracao publica do login com Google (vai ao front). ClientId so vem preenchido quando Habilitado;
    /// ModoTeste indica que o provedor efetivo e o FAKE (so possivel em Development + SANDBOX).
    /// </summary>
    public class GoogleConfiguracaoPublica
    {
        /// <summary>true = integracao ativa, client_id configurado e provedor utilizavel.</summary>
        public bool Habilitado { get; set; }

        /// <summary>Client ID publico do Google; null quando nao habilitado.</summary>
        public string? ClientId { get; set; }

        /// <summary>true = provedor FAKE efetivo (botao de teste).</summary>
        public bool ModoTeste { get; set; }
    }
}
