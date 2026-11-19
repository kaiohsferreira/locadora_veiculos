using Microsoft.AspNetCore.Http;

namespace AutoVia.Api.Requests.ClienteDocumento
{
    /// <summary>
    /// Formulario multipart do envio de documento do cliente: campo tipo (CNH ou COMPROVANTE_RESIDENCIA) e campo arquivo (um so).
    /// Sem id do cliente (vem da rota e da sessao) e sem id da locadora.
    /// </summary>
    public sealed class EnviarDocumentoClienteRequest
    {
        /// <summary>Nome exato do tipo do documento: CNH ou COMPROVANTE_RESIDENCIA.</summary>
        public string? Tipo { get; set; }

        /// <summary>Arquivo PDF, JPG ou PNG de ate 10 MB (um so).</summary>
        public IFormFile? Arquivo { get; set; }
    }
}
