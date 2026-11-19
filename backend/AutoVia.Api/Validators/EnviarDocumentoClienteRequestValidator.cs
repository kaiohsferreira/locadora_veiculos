using AutoVia.Api.Requests.ClienteDocumento;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Api.Validators
{
    /// <summary>
    /// Validacao do envio de documento do cliente, antes de qualquer chamada ao armazenamento (nunca vira 502): tipo ausente ou
    /// fora da lista; arquivo ausente, vazio ou acima de 10 MB; tipo declarado fora de PDF, JPG e PNG; primeiros bytes que nao
    /// sao de nenhum dos tres; e tipo declarado diferente do tipo do conteudo. Uma mensagem por campo (cascata com parada).
    /// </summary>
    public sealed class EnviarDocumentoClienteRequestValidator : AbstractValidator<EnviarDocumentoClienteRequest>
    {
        public EnviarDocumentoClienteRequestValidator()
        {
            RuleFor(x => x.Tipo)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage(ConstantsMessage.ErroClienteDocumentoTipoObrigatorio)
                .Must(tipo => RegrasClienteDocumento.TipoPermitido(tipo)).WithMessage(ConstantsMessage.ErroClienteDocumentoTipoInvalido);

            RuleFor(x => x.Arquivo)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage(ConstantsMessage.ErroClienteDocumentoArquivoObrigatorio)
                .Must(arquivo => arquivo != null && arquivo.Length > 0).WithMessage(ConstantsMessage.ErroClienteDocumentoArquivoVazio)
                .Must(arquivo => arquivo != null && arquivo.Length <= ConstantsValues.TamanhoMaximoArquivoArmazenamentoBytes)
                    .WithMessage(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.ErroClienteDocumentoTamanho, ConstantsValues.TamanhoMaximoArquivoDocumentoClienteMb))
                .MustAsync(ValidarConteudoAsync).WithMessage(ConstantsMessage.ErroClienteDocumentoFormato);
        }

        /// <summary>
        /// Tipo declarado (PDF, JPG ou PNG) e primeiros bytes de acordo com ele: o tipo decidido pelo conteudo tem de existir e ser
        /// igual ao declarado. Le so o cabecalho (no maximo o tamanho da maior assinatura), sem guardar o arquivo.
        /// </summary>
        internal static async Task<bool> ValidarConteudoAsync(IFormFile? arquivo, CancellationToken cancellationToken)
        {
            if (arquivo == null || arquivo.Length <= 0)
            {
                return false;
            }

            if (!TipoDeclaradoPermitido(arquivo.ContentType))
            {
                return false;
            }

            int tamanhoLeitura = (int)Math.Min(arquivo.Length, ConstantsValues.AssinaturaPng.Length);
            byte[] cabecalho = new byte[tamanhoLeitura];

            int bytesLidos = 0;
            using Stream fluxo = arquivo.OpenReadStream();
            while (bytesLidos < tamanhoLeitura)
            {
                int lidos = await fluxo.ReadAsync(cabecalho.AsMemory(bytesLidos, tamanhoLeitura - bytesLidos), cancellationToken);
                if (lidos == 0)
                {
                    break;
                }

                bytesLidos += lidos;
            }

            string? tipoDetectado = RegrasArmazenamentoDisco.TipoPeloConteudo(cabecalho.AsSpan(0, bytesLidos));

            return tipoDetectado != null && string.Equals(tipoDetectado, arquivo.ContentType, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Tipo declarado aceito pelo envio de documento: image/jpeg, image/png ou application/pdf.</summary>
        private static bool TipoDeclaradoPermitido(string? tipoDeclarado)
        {
            return string.Equals(tipoDeclarado, ConstantsArmazenamento.MimeJpeg, StringComparison.OrdinalIgnoreCase)
                || string.Equals(tipoDeclarado, ConstantsArmazenamento.MimePng, StringComparison.OrdinalIgnoreCase)
                || string.Equals(tipoDeclarado, ConstantsArmazenamento.MimePdf, StringComparison.OrdinalIgnoreCase);
        }
    }
}
