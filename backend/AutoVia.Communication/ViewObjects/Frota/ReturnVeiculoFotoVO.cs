namespace AutoVia.Communication.ViewObjects.Frota
{
    /// <summary>
    /// VO de retorno de uma foto do veiculo (201 do envio e itens da listagem). Nao traz chave do objeto, id da locadora
    /// nem id de funcionario. A URL e temporaria (ate 900 s) e vem de uma assinatura do armazenamento. Quando a foto nao tem
    /// arquivo no armazenamento desta maquina, Url, UrlMiniatura e ExpiraEm vem nulos e ErroFoto traz o texto para o usuario.
    /// </summary>
    public class ReturnVeiculoFotoVO
    {
        /// <summary>Id da foto (usado no DELETE).</summary>
        public int IdVeiculoFoto { get; set; }

        /// <summary>Ordem da foto no veiculo; a menor ordem ativa e a capa.</summary>
        public int Ordem { get; set; }

        /// <summary>URL https temporaria da foto; nula quando a foto nao tem URL assinada (arquivo ausente).</summary>
        public string? Url { get; set; }

        /// <summary>Reservado para a miniatura da fatia completa; nesta fatia sai sempre nulo.</summary>
        public string? UrlMiniatura { get; set; }

        /// <summary>Instante em que a URL expira, em UTC, no formato de ida e volta do .NET com fuso; nulo quando a foto nao tem URL.</summary>
        public string? ExpiraEm { get; set; }

        /// <summary>Texto para o usuario quando a foto nao tem URL assinada (arquivo ausente); nulo quando ha URL. Nunca traz chave, caminho ou motivo tecnico.</summary>
        public string? ErroFoto { get; set; }
    }
}
