using AutoVia.Api.Requests.Avaria;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace AutoVia.Api.Validators
{
    /// <summary>Validacao estrutural dos campos multipart para avaria.</summary>
    public sealed class CriarAvariaRequestValidator : AbstractValidator<CriarAvariaRequest>
    {
        private const char CaractereNulo = '\0';
        private const string NomeCampoDescricao = "Descricao";

        public CriarAvariaRequestValidator()
        {
            RuleFor(x => x.Descricao).NotEmpty().WithMessage(ConstantsMessage.ErroAvariaDescricaoObrigatoria)
                .MaximumLength(200).WithMessage(ConstantsMessage.ErroAvariaDescricaoTamanho);
            // Caractere NUL: o PostgreSQL recusa o texto e o erro sairia como 500. Recusa aqui, com 400.
            RuleFor(x => x.Descricao).Must(descricao => descricao == null || !descricao.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoDescricao));
            RuleFor(x => x.Gravidade).Must(GravidadePermitida)
                .WithMessage(ConstantsMessage.ErroAvariaGravidadeInvalida);
            RuleFor(x => x.ValorEstimado).Must(x => !x.HasValue || (x.Value >= 0m && x.Value <= 99999999.99m && decimal.Round(x.Value, 2) == x.Value))
                .WithMessage(ConstantsMessage.ErroAvariaValorEstimadoInvalido);
            RuleFor(x => x.Foto).NotNull().WithMessage(ConstantsMessage.ErroAvariaFotoObrigatoria)
                .MustAsync(ValidarFotoAsync).WithMessage(ConstantsMessage.ErroAvariaFotoFormatoInvalido);
        }

        private static bool GravidadePermitida(string valor)
        {
            GravidadeAvaria gravidade;
            return Enum.TryParse<GravidadeAvaria>(valor, false, out gravidade)
                && Enum.IsDefined(typeof(GravidadeAvaria), gravidade);
        }

        internal static async Task<bool> ValidarFotoAsync(IFormFile? foto, CancellationToken cancellationToken)
        {
            if (foto == null || foto.Length <= 0 || foto.Length > ConstantsArmazenamento.TamanhoMaximoFotoBytes)
                return false;

            bool mimeJpeg = string.Equals(foto.ContentType, ConstantsArmazenamento.MimeJpeg, StringComparison.OrdinalIgnoreCase);
            bool mimePng = string.Equals(foto.ContentType, ConstantsArmazenamento.MimePng, StringComparison.OrdinalIgnoreCase);
            if (!mimeJpeg && !mimePng)
                return false;

            byte[] assinatura = new byte[8];
            using Stream stream = foto.OpenReadStream();
            int bytesLidos = await stream.ReadAsync(assinatura.AsMemory(0, assinatura.Length), cancellationToken);
            bool assinaturaJpeg = bytesLidos >= 3 && assinatura[0] == 0xFF && assinatura[1] == 0xD8 && assinatura[2] == 0xFF;
            bool assinaturaPng = bytesLidos == 8 && assinatura[0] == 0x89 && assinatura[1] == 0x50 && assinatura[2] == 0x4E && assinatura[3] == 0x47 && assinatura[4] == 0x0D && assinatura[5] == 0x0A && assinatura[6] == 0x1A && assinatura[7] == 0x0A;
            return (mimeJpeg && assinaturaJpeg) || (mimePng && assinaturaPng);
        }
    }
}
