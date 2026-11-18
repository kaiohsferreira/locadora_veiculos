namespace AutoVia.Api.Requests.Armazenamento
{
    /// <summary>
    /// Parametros do link de arquivo da rota publica (chave, exp e sig) e quantas vezes cada um veio na query. A contagem
    /// existe para recusar parametro repetido (400), porque o model binding sozinho pegaria so o primeiro valor.
    /// </summary>
    public sealed class LerArquivoArmazenadoRequest
    {
        /// <summary>Chave do arquivo no armazenamento (texto cru da query).</summary>
        public string? Chave { get; set; }

        /// <summary>Instante de validade do link em segundos Unix (texto cru da query).</summary>
        public string? Exp { get; set; }

        /// <summary>Assinatura do link em base64url (texto cru da query).</summary>
        public string? Sig { get; set; }

        /// <summary>Quantas vezes o parametro chave veio na query.</summary>
        public int QuantidadeChave { get; set; }

        /// <summary>Quantas vezes o parametro exp veio na query.</summary>
        public int QuantidadeExp { get; set; }

        /// <summary>Quantas vezes o parametro sig veio na query.</summary>
        public int QuantidadeSig { get; set; }
    }
}
