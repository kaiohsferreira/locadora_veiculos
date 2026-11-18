namespace AutoVia.Core.Options
{
    /// <summary>
    /// Configuracao da maquina do armazenamento em pasta (secao Armazenamento do appsettings ou variavel de ambiente).
    /// Nao fica no banco: o caminho de um computador nao existe no outro. Vazias = padrao (ver RegrasArmazenamentoDisco e o provedor).
    /// </summary>
    public class ArmazenamentoDiscoOptions
    {
        /// <summary>Pasta raiz do armazenamento. Vazia no Windows fora de container = pasta de dados do usuario.</summary>
        public string Diretorio { get; set; } = string.Empty;

        /// <summary>Endereco publico da API usado nos links de arquivo. Vazio = derivado da requisicao, so em loopback.</summary>
        public string UrlPublicaApi { get; set; } = string.Empty;
    }
}
