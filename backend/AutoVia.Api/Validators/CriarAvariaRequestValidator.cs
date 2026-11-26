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
            RuleFor(x => x.ValorEstimado).Must(ValorEstimadoValido)
                .WithMessage(ConstantsMessage.ErroAvariaValorEstimadoInvalido);
            RuleFor(x => x.Angulo).Must(AnguloValido)
                .WithMessage(ConstantsMessage.ErroAvariaAnguloInvalido);
            // Tudo ou nada (N8): angulo, posX e posY vem juntos ou todos vazios; a parcial aponta em angulo.
            RuleFor(x => x.Angulo).Must((request, angulo) => PosicaoCompletaOuAusente(request))
                .WithMessage(ConstantsMessage.ErroAvariaPosicaoIncompleta);
            RuleFor(x => x.PosX).Must(PosicaoValida)
                .WithMessage(ConstantsMessage.ErroAvariaPosicaoInvalida);
            RuleFor(x => x.PosY).Must(PosicaoValida)
                .WithMessage(ConstantsMessage.ErroAvariaPosicaoInvalida);
            RuleFor(x => x.Foto).NotNull().WithMessage(ConstantsMessage.ErroAvariaFotoObrigatoria)
                .MustAsync(ValidarFotoAsync).WithMessage(ConstantsMessage.ErroAvariaFotoFormatoInvalido);
        }

        /// <summary>Texto do valor estimado aceito pela conversao unica e dentro da faixa de 0 a 99999999.99; vazio e sem valor.</summary>
        private static bool ValorEstimadoValido(string? texto)
        {
            decimal? valor;
            if (!ConversorValorEstimadoFormulario.TentarConverter(texto, out valor))
                return false;
            return !valor.HasValue || (valor.Value >= 0m && valor.Value <= ConstantsValues.LimiteValorEstimadoAvaria);
        }

        /// <summary>Angulo vazio ou um dos quatro nomes exatos do enum (maiusculas).</summary>
        private static bool AnguloValido(string? texto)
        {
            AnguloAvaria? angulo;
            return ConversorAnguloAvariaFormulario.TentarConverter(texto, out angulo);
        }

        /// <summary>Posicao vazia, ou texto no formato do valor estimado e dentro da faixa de 0 a 100.</summary>
        private static bool PosicaoValida(string? texto)
        {
            decimal? valor;
            if (!ConversorValorEstimadoFormulario.TentarConverter(texto, out valor))
                return false;
            return !valor.HasValue || (valor.Value >= ConstantsValues.LimitePosicaoAvariaMinima && valor.Value <= ConstantsValues.LimitePosicaoAvariaMaxima);
        }

        /// <summary>Tudo ou nada: sem nenhum dos tres campos, ou com os tres. Angulo preenchido e fora da lista fica so com ErroAvariaAnguloInvalido.</summary>
        private static bool PosicaoCompletaOuAusente(CriarAvariaRequest request)
        {
            if (!string.IsNullOrEmpty(request.Angulo) && !AnguloValido(request.Angulo))
                return true;
            return !TemAlgumaPosicao(request) || TodasAsPosicoesPreenchidas(request);
        }

        private static bool TemAlgumaPosicao(CriarAvariaRequest request)
        {
            return !string.IsNullOrEmpty(request.Angulo) || !string.IsNullOrEmpty(request.PosX) || !string.IsNullOrEmpty(request.PosY);
        }

        private static bool TodasAsPosicoesPreenchidas(CriarAvariaRequest request)
        {
            return !string.IsNullOrEmpty(request.Angulo) && !string.IsNullOrEmpty(request.PosX) && !string.IsNullOrEmpty(request.PosY);
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
