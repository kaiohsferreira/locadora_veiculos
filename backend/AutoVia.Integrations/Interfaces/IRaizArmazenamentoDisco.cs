namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Pasta raiz do armazenamento em disco (contrato armazenamento-disco v1, regra 2). Os metodos Obter* criam as pastas
    /// (so a gravacao usa); TentarObterSemCriar confere sem criar nem limpar nada.
    /// </summary>
    public interface IRaizArmazenamentoDisco
    {
        /// <summary>Resolve e confere a raiz, cria as subpastas e devolve a pasta dos arquivos.</summary>
        string ObterPastaDosArquivos();

        /// <summary>Resolve e confere a raiz, cria as subpastas e devolve a pasta dos temporarios.</summary>
        string ObterPastaTemporaria();

        /// <summary>Resolve e confere a raiz, cria as subpastas e devolve a pasta do segredo.</summary>
        string ObterPastaDoSegredo();

        /// <summary>
        /// Resolve e confere a raiz sem criar nada. Devolve falso quando a pasta dos arquivos ainda nao existe.
        /// Configuracao invalida lanca ArmazenamentoDiscoException.
        /// </summary>
        bool TentarObterSemCriar(out string pastaDosArquivos, out string pastaDoSegredo);
    }
}
