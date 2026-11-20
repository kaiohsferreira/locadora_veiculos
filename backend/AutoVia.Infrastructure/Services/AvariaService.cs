using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Avarias;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Integrations.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using System.Globalization;
using SkiaSharp;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>Regras de avaria, validacao de imagem e coordenacao com storage.</summary>
    public sealed class AvariaService : IAvariaService
    {
        private readonly IAvariaRepository _avariaRepository;
        private readonly IArmazenamentoGateway _armazenamentoGateway;
        private readonly IVistoriaRepository _vistoriaRepository;
        private readonly IVistoriaSaidaRepository _vistoriaSaidaRepository;
        private readonly ILogService _logService;

        public AvariaService(IAvariaRepository avariaRepository, IArmazenamentoGateway armazenamentoGateway,
            IVistoriaRepository vistoriaRepository, IVistoriaSaidaRepository vistoriaSaidaRepository, ILogService logService)
        {
            _avariaRepository = avariaRepository;
            _armazenamentoGateway = armazenamentoGateway;
            _vistoriaRepository = vistoriaRepository;
            _vistoriaSaidaRepository = vistoriaSaidaRepository;
            _logService = logService;
        }

        public async Task<ReturnAvariaVO> CriarAsync(int idVistoria, string descricao, string gravidade, decimal? valorEstimado, byte[]? foto, string? contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ordem: id da vistoria, campos, vistoria no banco e so depois a foto (a decodificacao fica por ultimo).
                if (idVistoria <= 0) throw new NotFoundException(ConstantsMessage.ErroAvariaVistoriaNaoEncontrada);
                ValidarCampos(descricao, gravidade, valorEstimado);
                Vistoria vistoria = await ObterVistoriaAtivaAsync(idVistoria, cancellationToken);
                await ValidarRegraDaVistoriaAsync(vistoria);
                foto = ValidarFoto(foto, contentType);

                Avaria criada = await _vistoriaRepository.ExecutarComLocacaoBloqueadaAsync(vistoria.IdLocacao,
                    () => CriarSobTravaAsync(idVistoria, descricao, gravidade, valorEstimado, cancellationToken));
                return await EnviarFotoAsync(criada, foto!, contentType!, cancellationToken);
            }
            catch (ValidationException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarAvaria, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarAvaria, GetType().ToString());
                throw;
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarAvaria, GetType().ToString());
                throw;
            }
            catch (IntegrationException ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroCriarAvaria, GetType().ToString());
                throw;
            }
            catch (AvariaUploadException ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenarFotoAvaria, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroCriarAvaria, GetType().ToString());
                throw;
            }
        }

        public async Task<ReturnAvariaVO> GetByIdAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default)
        {
            try
            {
                Avaria avaria = await ObterAvariaAsync(idVistoria, idAvaria, cancellationToken);
                ArquivoArmazenado? foto = await _armazenamentoGateway.ObterUrlAsync(CriarChave(idVistoria, idAvaria), cancellationToken);
                return Mapear(avaria, foto);
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
            catch (IntegrationException ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
        }

        public async Task<ReturnAvariaVO> RetryUploadAsync(int idVistoria, int idAvaria, byte[] foto, string contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                Avaria avaria = await ObterAvariaAsync(idVistoria, idAvaria, cancellationToken);
                foto = ValidarFoto(foto, contentType);
                return await EnviarFotoAsync(avaria, foto, contentType, cancellationToken);
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroArmazenarFotoAvaria, GetType().ToString());
                throw;
            }
            catch (IntegrationException ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenarFotoAvaria, GetType().ToString());
                throw;
            }
            catch (AvariaUploadException ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenarFotoAvaria, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenarFotoAvaria, GetType().ToString());
                throw;
            }
        }

        public async Task<ReturnAvariaVO> GetFotoUrlAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default)
        {
            try
            {
                Avaria avaria = await ObterAvariaAsync(idVistoria, idAvaria, cancellationToken);
                ArquivoArmazenado? foto = await _armazenamentoGateway.ObterUrlAsync(CriarChave(idVistoria, idAvaria), cancellationToken);
                if (foto == null) throw new NotFoundException(ConstantsMessage.ErroAvariaFotoNaoEncontrada);
                return Mapear(avaria, foto);
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
            catch (IntegrationException ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
        }

        public async Task RemoverFotoAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default)
        {
            try
            {
                await ObterAvariaAsync(idVistoria, idAvaria, cancellationToken);
                await _armazenamentoGateway.RemoverAsync(CriarChave(idVistoria, idAvaria), cancellationToken);
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroRemoverFotoAvaria, GetType().ToString());
                throw;
            }
            catch (IntegrationException ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroRemoverFotoAvaria, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroRemoverFotoAvaria, GetType().ToString());
                throw;
            }
        }

        /// <summary>Vistoria ativa da locadora com a locacao carregada; 404 quando nao existe.</summary>
        private async Task<Vistoria> ObterVistoriaAtivaAsync(int idVistoria, CancellationToken cancellationToken)
        {
            try
            {
                Vistoria? vistoria = await _avariaRepository.FindAtivaComLocacaoAsync(idVistoria, cancellationToken);
                if (vistoria == null) throw new NotFoundException(ConstantsMessage.ErroAvariaVistoriaNaoEncontrada);
                return vistoria;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroCriarAvaria, GetType().ToString());
                throw;
            }
        }

        /// <summary>Regra da vistoria (RegrasVistoria.MotivoRecusaAvaria): 409 quando a vistoria ou a locacao nao aceitam avaria nova.</summary>
        private async Task ValidarRegraDaVistoriaAsync(Vistoria vistoria)
        {
            try
            {
                Vistoria? saida = await _vistoriaSaidaRepository.FindSaidaByLocacaoAsync(vistoria.IdLocacao);
                string? motivo = RegrasVistoria.MotivoRecusaAvaria(vistoria.Tipo, vistoria.Locacao.Status, saida != null);
                if (motivo != null) throw new BusinessException(motivo);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroCriarAvaria, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Dentro da trava da locacao: relê a vistoria, repete a regra com o estado relido e grava a avaria. A foto e enviada depois, fora da trava.
        /// </summary>
        private async Task<Avaria> CriarSobTravaAsync(int idVistoria, string descricao, string gravidade, decimal? valorEstimado, CancellationToken cancellationToken)
        {
            try
            {
                Vistoria vistoria = await ObterVistoriaAtivaAsync(idVistoria, cancellationToken);
                await ValidarRegraDaVistoriaAsync(vistoria);

                Avaria avaria = new Avaria
                {
                    IdVistoria = idVistoria,
                    Descricao = descricao.Trim(),
                    Gravidade = ParseGravidade(gravidade),
                    ValorEstimado = valorEstimado
                };
                return await _avariaRepository.CriarAsync(avaria, cancellationToken);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroCriarAvaria, GetType().ToString());
                throw;
            }
        }

        private async Task<ReturnAvariaVO> EnviarFotoAsync(Avaria avaria, byte[] foto, string contentType, CancellationToken cancellationToken)
        {
            try
            {
                ArquivoArmazenado url = await _armazenamentoGateway.EnviarAsync(CriarChave(avaria.IdVistoria, avaria.IdAvaria), foto, contentType, cancellationToken);
                return Mapear(avaria, url);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenarFotoAvaria, GetType().ToString());
                throw;
            }
            catch (Exception ex) when (ex is not AvariaUploadException)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenarFotoAvaria, GetType().ToString());
                throw new AvariaUploadException(avaria.IdVistoria, avaria.IdAvaria, ConstantsMessage.ErroUploadAvariaPendente, ex);
            }
        }

        private async Task<Avaria> ObterAvariaAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken)
        {
            Avaria? avaria = await _avariaRepository.FindAtivaAsync(idVistoria, idAvaria, cancellationToken);
            if (avaria == null) throw new NotFoundException(ConstantsMessage.ErroAvariaNaoEncontrada);
            return avaria;
        }

        private static ReturnAvariaVO Mapear(Avaria avaria, ArquivoArmazenado? foto)
        {
            return new ReturnAvariaVO
            {
                IdAvaria = avaria.IdAvaria,
                IdVistoria = avaria.IdVistoria,
                Descricao = avaria.Descricao,
                Gravidade = avaria.Gravidade,
                ValorEstimado = avaria.ValorEstimado,
                Foto = foto == null ? null : new FotoAvariaVO { Url = foto.Url, ExpiraEm = foto.ExpiraEm.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) }
            };
        }

        private static void ValidarCampos(string descricao, string gravidade, decimal? valorEstimado)
        {
            if (string.IsNullOrWhiteSpace(descricao)) throw CriarValidacao(ConstantsMessage.ErroAvariaDescricaoObrigatoria, nameof(descricao));
            if (descricao.Length > 200) throw CriarValidacao(ConstantsMessage.ErroAvariaDescricaoTamanho, nameof(descricao));
            ParseGravidade(gravidade);
            if (valorEstimado.HasValue && (valorEstimado.Value < 0m || valorEstimado.Value > 99999999.99m || decimal.Round(valorEstimado.Value, 2) != valorEstimado.Value))
                throw CriarValidacao(ConstantsMessage.ErroAvariaValorEstimadoInvalido, nameof(valorEstimado));
        }

        private static GravidadeAvaria ParseGravidade(string gravidade)
        {
            if (string.Equals(gravidade, GravidadeAvaria.LEVE.ToString(), StringComparison.Ordinal)) return GravidadeAvaria.LEVE;
            if (string.Equals(gravidade, GravidadeAvaria.MEDIA.ToString(), StringComparison.Ordinal)) return GravidadeAvaria.MEDIA;
            if (string.Equals(gravidade, GravidadeAvaria.GRAVE.ToString(), StringComparison.Ordinal)) return GravidadeAvaria.GRAVE;
            throw CriarValidacao(ConstantsMessage.ErroAvariaGravidadeInvalida, nameof(gravidade));
        }

        private static byte[] ValidarFoto(byte[]? foto, string? contentType)
        {
            if (foto == null || foto.Length == 0 || foto.Length > ConstantsArmazenamento.TamanhoMaximoFotoBytes)
                throw CriarValidacao(ConstantsMessage.ErroAvariaFotoObrigatoria, nameof(foto));
            bool mimeJpeg = string.Equals(contentType, ConstantsArmazenamento.MimeJpeg, StringComparison.OrdinalIgnoreCase);
            bool mimePng = string.Equals(contentType, ConstantsArmazenamento.MimePng, StringComparison.OrdinalIgnoreCase);
            bool assinaturaJpeg = foto.Length >= 3 && foto[0] == 0xFF && foto[1] == 0xD8 && foto[2] == 0xFF;
            bool assinaturaPng = foto.Length >= 8 && foto[0] == 0x89 && foto[1] == 0x50 && foto[2] == 0x4E && foto[3] == 0x47 && foto[4] == 0x0D && foto[5] == 0x0A && foto[6] == 0x1A && foto[7] == 0x0A;
            if ((!mimeJpeg || !assinaturaJpeg) && (!mimePng || !assinaturaPng))
                throw CriarValidacao(ConstantsMessage.ErroAvariaFotoFormatoInvalido, nameof(foto));

            try
            {
                using SKData dadosOriginais = SKData.CreateCopy(foto);
                using SKCodec? codec = SKCodec.Create(dadosOriginais);
                if (codec == null)
                    throw CriarValidacao(ConstantsMessage.ErroAvariaFotoFormatoInvalido, nameof(foto));

                SKImageInfo informacao = codec.Info;
                if (informacao.Width <= 0 || informacao.Height <= 0
                    || informacao.Width > ConstantsArmazenamento.DimensaoMaximaFotoPixels
                    || informacao.Height > ConstantsArmazenamento.DimensaoMaximaFotoPixels
                    || (long)informacao.Width * informacao.Height > ConstantsArmazenamento.TotalMaximoPixelsFoto)
                    throw CriarValidacao(ConstantsMessage.ErroAvariaFotoFormatoInvalido, nameof(foto));

                // Decodifica e confere o resultado: JPEG cortado devolve bitmap parcial sem erro, entao o resultado do codec decide.
                using SKBitmap bitmap = new SKBitmap(codec.Info);
                SKCodecResult resultadoDecodificacao = codec.GetPixels(bitmap.Info, bitmap.GetPixels());
                if (resultadoDecodificacao != SKCodecResult.Success)
                    throw CriarValidacao(ConstantsMessage.ErroAvariaFotoFormatoInvalido, nameof(foto));

                using SKImage imagem = SKImage.FromBitmap(bitmap);
                SKEncodedImageFormat formato = mimeJpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png;
                using SKData? imagemNormalizada = imagem.Encode(formato, 90);
                if (imagemNormalizada == null)
                    throw CriarValidacao(ConstantsMessage.ErroAvariaFotoFormatoInvalido, nameof(foto));
                return imagemNormalizada.ToArray();
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception)
            {
                throw CriarValidacao(ConstantsMessage.ErroAvariaFotoFormatoInvalido, nameof(foto));
            }
        }

        /// <summary>Monta a excecao de validacao com a mensagem tambem no errors (o middleware responde 400 com a mensagem especifica).</summary>
        private static ValidationException CriarValidacao(string mensagem, string campo)
        {
            List<ValidationFailure> falhas = new List<ValidationFailure> { new ValidationFailure(campo, mensagem) };

            return new ValidationException(mensagem, falhas);
        }

        private static Exception CriarErroSanitizado(Exception exception)
        {
            return new Exception(exception.GetType().Name);
        }

        private static string CriarChave(int idVistoria, int idAvaria)
        {
            return string.Format(CultureInfo.InvariantCulture, "vistorias/{0}/avarias/{1}/foto", idVistoria, idAvaria);
        }
    }
}
