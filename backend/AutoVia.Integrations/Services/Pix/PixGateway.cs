using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Interfaces;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoVia.Integrations.Services.Pix
{
    /// <summary>
    /// Gateway de cobranca PIX. Valida a solicitacao (BusinessException, 409), resolve a
    /// integracao PAGAMENTO, le 'provedor' e 'pix_expiracao_segundos', escolhe o IPixProvider
    /// pelo Nome e repassa a chamada. Nao persiste nada.
    ///
    /// Cancelamento: antes de cancelar, consulta o provedor; cobranca paga nao cancela.
    /// </summary>
    public class PixGateway : IPixGateway
    {
        private readonly IIntegrationResolver _integrationResolver;
        private readonly IEnumerable<IPixProvider> _providers;
        private readonly ILogService _logService;

        public PixGateway(IIntegrationResolver integrationResolver, IEnumerable<IPixProvider> providers, ILogService logService)
        {
            _integrationResolver = integrationResolver;
            _providers = providers;
            _logService = logService;
        }

        public async Task<PixCobrancaResultado> CriarCobrancaAsync(PixCobrancaSolicitacao solicitacao, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(solicitacao);

            try
            {
                ValidarSolicitacao(solicitacao);

                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Pagamento, cancellationToken);
                int expiracaoSegundos = ObterExpiracao(configuracao, solicitacao.ExpiracaoSegundos);
                IPixProvider provider = SelecionarProvedor(configuracao);

                return await provider.CriarAsync(configuracao, solicitacao, expiracaoSegundos, cancellationToken);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroPixCriarCobranca, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroPixCriarCobranca, ex);
            }
        }

        public async Task<PixCobrancaResultado> ConsultarCobrancaAsync(string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidarIdentificador(identificadorProvedor);

                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Pagamento, cancellationToken);
                IPixProvider provider = SelecionarProvedor(configuracao);

                return await provider.ConsultarAsync(configuracao, identificadorProvedor, cancellationToken);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroPixConsultarCobranca, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroPixConsultarCobranca, ex);
            }
        }

        public async Task<PixCobrancaResultado> CancelarCobrancaAsync(string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidarIdentificador(identificadorProvedor);

                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Pagamento, cancellationToken);
                IPixProvider provider = SelecionarProvedor(configuracao);

                PixCobrancaResultado atual = await provider.ConsultarAsync(configuracao, identificadorProvedor, cancellationToken);

                if (atual.Status == PixStatusCobranca.PAGA)
                {
                    throw new BusinessException(ConstantsMessage.ErroPixCancelarCobrancaPaga);
                }

                return await provider.CancelarAsync(configuracao, identificadorProvedor, cancellationToken);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroPixCancelarCobranca, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroPixCancelarCobranca, ex);
            }
        }

        // ------------------------------------------------------------------
        // Validacoes (antes de qualquer chamada externa)
        // ------------------------------------------------------------------

        private static void ValidarSolicitacao(PixCobrancaSolicitacao solicitacao)
        {
            if (solicitacao.Valor <= 0 || decimal.Round(solicitacao.Valor, 2) != solicitacao.Valor)
            {
                throw new BusinessException(ConstantsMessage.ErroPixValorInvalido);
            }

            string referencia = solicitacao.ReferenciaExterna;

            if (string.IsNullOrEmpty(referencia) || referencia.Length > ConstantsPix.ReferenciaTamanhoMaximo || !Regex.IsMatch(referencia, ConstantsPix.PadraoReferenciaExterna))
            {
                throw new BusinessException(ConstantsMessage.ErroPixReferenciaInvalida);
            }

            if (string.IsNullOrWhiteSpace(solicitacao.Descricao) || solicitacao.Descricao.Length > ConstantsPix.DescricaoTamanhoMaximo)
            {
                throw new BusinessException(ConstantsMessage.ErroPixDescricaoInvalida);
            }

            ValidarPagador(solicitacao.Pagador);

            if (solicitacao.ExpiracaoSegundos.HasValue && !ExpiracaoDentroDaFaixa(solicitacao.ExpiracaoSegundos.Value))
            {
                throw new BusinessException(ConstantsMessage.ErroPixExpiracaoInvalida);
            }
        }

        private static void ValidarPagador(PixPagador pagador)
        {
            if (string.IsNullOrWhiteSpace(pagador.Nome) || pagador.Nome.Length > ConstantsPix.PagadorNomeTamanhoMaximo)
            {
                throw new BusinessException(ConstantsMessage.ErroPixPagadorInvalido);
            }

            if (string.IsNullOrWhiteSpace(pagador.Email) || !Regex.IsMatch(pagador.Email, ConstantsPix.PadraoEmail))
            {
                throw new BusinessException(ConstantsMessage.ErroPixPagadorInvalido);
            }

            if (!string.IsNullOrWhiteSpace(pagador.Documento) && !Regex.IsMatch(pagador.Documento, ConstantsPix.PadraoCpf))
            {
                throw new BusinessException(ConstantsMessage.ErroPixPagadorInvalido);
            }
        }

        private static void ValidarIdentificador(string identificadorProvedor)
        {
            if (string.IsNullOrWhiteSpace(identificadorProvedor))
            {
                throw new BusinessException(ConstantsMessage.ErroPixIdentificadorObrigatorio);
            }
        }

        private static bool ExpiracaoDentroDaFaixa(int expiracaoSegundos)
        {
            return expiracaoSegundos > 0 && expiracaoSegundos <= ConstantsPix.ExpiracaoMaximaSegundos;
        }

        // ------------------------------------------------------------------
        // Configuracao da integracao PAGAMENTO
        // ------------------------------------------------------------------

        /// <summary>Expiracao da solicitacao, ou pix_expiracao_segundos da integracao, ou o padrao (3600).</summary>
        private static int ObterExpiracao(ConfiguracaoIntegracao configuracao, int? expiracaoSolicitada)
        {
            if (expiracaoSolicitada.HasValue)
            {
                return expiracaoSolicitada.Value;
            }

            string? valorConfigurado = configuracao.GetOptional(ConstantsPix.ChavePixExpiracaoSegundos);

            if (string.IsNullOrWhiteSpace(valorConfigurado))
            {
                return ConstantsPix.ExpiracaoPadraoSegundos;
            }

            int expiracao;

            if (!int.TryParse(valorConfigurado.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out expiracao) || !ExpiracaoDentroDaFaixa(expiracao))
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsPix.ChavePixExpiracaoSegundos, ConstantsMessage.ErroPixExpiracaoConfiguracaoInvalida);
            }

            return expiracao;
        }

        /// <summary>Escolhe o provedor pela chave 'provedor'. FAKE so funciona em SANDBOX.</summary>
        private IPixProvider SelecionarProvedor(ConfiguracaoIntegracao configuracao)
        {
            string nomeProvedor = configuracao.GetRequired(ConstantsPix.ChaveProvedor).Trim();

            IPixProvider? provider = _providers.FirstOrDefault(x => string.Equals(x.Nome, nomeProvedor, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsPix.ChaveProvedor, ConstantsMessage.ErroPixProvedorNaoSuportado);
            }

            bool isFake = string.Equals(provider.Nome, ConstantsPix.ProvedorFake, StringComparison.OrdinalIgnoreCase);

            if (isFake && configuracao.Ambiente != AmbienteIntegracao.SANDBOX)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsPix.ChaveProvedor, ConstantsMessage.ErroPixFakeForaDoSandbox);
            }

            return provider;
        }
    }
}
