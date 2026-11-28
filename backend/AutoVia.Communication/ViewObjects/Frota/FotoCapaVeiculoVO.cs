namespace AutoVia.Communication.ViewObjects.Frota
{
    /// <summary>
    /// Foto de capa do veiculo nas listas (campo fotoCapa). Nao tem id de foto: nenhuma rota publica conhece idFoto.
    /// Nao traz chave do objeto, id da locadora nem id de funcionario. Usado pelas listas (fatia seguinte).
    /// </summary>
    public class FotoCapaVeiculoVO
    {
        /// <summary>URL https temporaria da capa.</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>URL temporária da miniatura, quando disponível.</summary>
        public string? UrlMiniatura { get; set; }

        /// <summary>Instante em que a URL expira, em UTC, no formato de ida e volta do .NET com fuso.</summary>
        public string ExpiraEm { get; set; } = string.Empty;
    }
}
