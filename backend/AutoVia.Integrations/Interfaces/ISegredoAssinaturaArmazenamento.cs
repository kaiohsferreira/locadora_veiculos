namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Segredo de assinatura dos links de arquivo (32 bytes, um por maquina). So o envio cria o segredo; as demais leituras
    /// usam ObterSeExistir, que nunca cria.
    /// </summary>
    public interface ISegredoAssinaturaArmazenamento
    {
        /// <summary>Le ou cria o segredo. Chamado SO pelo envio de arquivo.</summary>
        byte[] ObterOuCriar();

        /// <summary>Le o segredo sem criar nada. Devolve nulo quando a pasta ou o arquivo do segredo nao existem.</summary>
        byte[]? ObterSeExistir();
    }
}
