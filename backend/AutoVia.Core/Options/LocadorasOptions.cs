namespace AutoVia.Core.Options
{
    /// <summary>
    /// Secao Locadoras do appsettings. Liga e desliga o cadastro publico de locadora: com a chave
    /// desligada (padrao), POST /api/empresas e POST /api/empresas/verificar-cadastro devolvem 404
    /// antes de validar o corpo. O limite por IP e lido pela politica de limite de requisicoes.
    /// </summary>
    public class LocadorasOptions
    {
        /// <summary>Padrao false: o cadastro publico fica desligado ate o Kaio liberar (Locadoras:CadastroPublicoHabilitado).</summary>
        public bool CadastroPublicoHabilitado { get; set; }
    }
}
