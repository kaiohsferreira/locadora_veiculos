using Microsoft.AspNetCore.Http;

namespace AutoVia.Api.Requests.Avaria
{
    /// <summary>Arquivo multipart usado para repetir o upload de uma avaria existente.</summary>
    public sealed class RetryAvariaFotoRequest
    {
        public IFormFile? Foto { get; set; }
    }
}
