using AutoVia.Helpers;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AutoVia.Api.ActionResults
{
    /// <summary>
    /// Referrer-Policy da rota publica de leitura de arquivo. Registrado com OnStarting: o middleware e o escritor de
    /// ProblemDetails limpam os cabecalhos antes de escrever o erro, e so o OnStarting sobrevive a essa limpeza. Assim o
    /// cabecalho vai em toda resposta da rota (200, 304, 400, 404, 429 e 500).
    /// </summary>
    public static class CabecalhoReferrerLeituraArquivo
    {
        /// <summary>Registra o Referrer-Policy da resposta da rota de leitura, antes de qualquer resultado.</summary>
        public static void Registrar(HttpResponse resposta)
        {
            resposta.OnStarting(AplicarNaPartida, resposta);
        }

        private static Task AplicarNaPartida(object estado)
        {
            HttpResponse resposta = (HttpResponse)estado;
            resposta.Headers[ConstantsValues.NomeCabecalhoReferrerPolicy] = ConstantsValues.ValorReferrerPolicySemReferencia;
            return Task.CompletedTask;
        }
    }
}
