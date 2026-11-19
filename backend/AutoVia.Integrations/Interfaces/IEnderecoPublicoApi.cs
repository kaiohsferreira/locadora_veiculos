namespace AutoVia.Integrations.Interfaces
{
    /// <summary>Endereco publico da API usado nos links de arquivo (origem, sem barra final).</summary>
    public interface IEnderecoPublicoApi
    {
        /// <summary>Devolve a origem da API para os links. Configuracao invalida, ou requisicao fora de loopback sem configuracao, lanca ArmazenamentoDiscoException.</summary>
        string Obter();
    }
}
