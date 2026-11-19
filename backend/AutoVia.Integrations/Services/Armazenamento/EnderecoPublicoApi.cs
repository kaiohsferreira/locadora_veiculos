using AutoVia.Core.Options;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoVia.Integrations.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AutoVia.Integrations.Services.Armazenamento
{
    /// <summary>
    /// Endereco publico da API nos links de arquivo (contrato armazenamento-disco v1, regra 12). Com a configuracao preenchida,
    /// usa a configuracao. Sem ela, deriva da requisicao SO quando o host e loopback (host sem porta na checagem; origem com porta).
    /// </summary>
    public sealed class EnderecoPublicoApi : IEnderecoPublicoApi
    {
        private readonly IOptions<ArmazenamentoDiscoOptions> _opcoes;
        private readonly IHttpContextAccessor _acessorHttp;

        public EnderecoPublicoApi(IOptions<ArmazenamentoDiscoOptions> opcoes, IHttpContextAccessor acessorHttp)
        {
            _opcoes = opcoes;
            _acessorHttp = acessorHttp;
        }

        public string Obter()
        {
            string configurado = _opcoes.Value.UrlPublicaApi.Trim();
            if (configurado.Length > 0)
            {
                return ValidarOrigem(configurado);
            }

            HttpContext? contexto = _acessorHttp.HttpContext;
            if (contexto == null || !RegrasArmazenamentoDisco.HostEhLoopback(contexto.Request.Host.Host))
            {
                throw new ArmazenamentoDiscoException(ConstantsMessage.LogArmazenamentoDiscoUrlPublicaInvalida);
            }

            string origemDerivada = contexto.Request.Scheme + ConstantsValues.SeparadorEsquemaHost + contexto.Request.Host.Value;
            return ValidarOrigem(origemDerivada);
        }

        private static string ValidarOrigem(string endereco)
        {
            if (!RegrasArmazenamentoDisco.EnderecoPublicoAceito(endereco))
            {
                throw new ArmazenamentoDiscoException(ConstantsMessage.LogArmazenamentoDiscoUrlPublicaInvalida);
            }

            if (!Uri.TryCreate(endereco, UriKind.Absolute, out Uri? uri))
            {
                throw new ArmazenamentoDiscoException(ConstantsMessage.LogArmazenamentoDiscoUrlPublicaInvalida);
            }

            if (uri.Port < ConstantsValues.PortaMinimaEnderecoPublico || uri.Port > ConstantsValues.PortaMaximaEnderecoPublico)
            {
                throw new ArmazenamentoDiscoException(ConstantsMessage.LogArmazenamentoDiscoUrlPublicaInvalida);
            }

            return uri.GetLeftPart(UriPartial.Authority);
        }
    }
}
