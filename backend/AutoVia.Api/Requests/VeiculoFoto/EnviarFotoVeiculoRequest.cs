using Microsoft.AspNetCore.Http;

namespace AutoVia.Api.Requests.VeiculoFoto
{
    /// <summary>
    /// Formulario multipart do envio de foto do veiculo. Tem EXATAMENTE a propriedade Foto (campo "foto"): sem id da locadora,
    /// sem id do veiculo (vem da rota) e sem id de funcionario (vem da sessao).
    /// </summary>
    public sealed class EnviarFotoVeiculoRequest
    {
        /// <summary>Arquivo JPEG ou PNG (um so).</summary>
        public IFormFile? Foto { get; set; }
    }
}
