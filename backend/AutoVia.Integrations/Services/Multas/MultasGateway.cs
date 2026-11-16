using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Multas;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Multas;
using AutoVia.Integrations.Interfaces;

namespace AutoVia.Integrations.Services.Multas
{
    /// <summary>Valida e encaminha consultas de multas; persistencia cabe ao fluxo consumidor.</summary>
    public class MultasGateway : IMultasGateway
    {
        private readonly IIntegrationResolver _integrationResolver;
        private readonly IEnumerable<IMultasProvider> _providers;
        private readonly ILogService _logService;

        public MultasGateway(IIntegrationResolver integrationResolver, IEnumerable<IMultasProvider> providers, ILogService logService)
        {
            _integrationResolver = integrationResolver;
            _providers = providers;
            _logService = logService;
        }

        public async Task<MultasConsultaResultado> ConsultarInfracoesAsync(
            MultasConsultaSolicitacao solicitacao,
            CancellationToken cancellationToken = default)
        {
            try
            {
                MultasConsultaSolicitacao normalizada = ValidarENormalizar(solicitacao);
                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Multas, cancellationToken);
                IMultasProvider provider = SelecionarProvedor(configuracao);
                MultasConsultaResultado resultado = await provider.ConsultarInfracoesAsync(configuracao, normalizada, cancellationToken);
                ValidarResultado(resultado, normalizada);
                return resultado;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroMultasConsulta, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Multas, ConstantsMessage.ErroMultasConsulta, ex);
            }
        }

        private static MultasConsultaSolicitacao ValidarENormalizar(MultasConsultaSolicitacao solicitacao)
        {
            if (solicitacao == null)
            {
                throw new BusinessException(ConstantsMessage.ErroMultasSolicitacaoInvalida);
            }

            string placaNormalizada = StaticMethods.NormalizePlaca(solicitacao.Placa);
            if (!StaticMethods.IsPlacaValida(placaNormalizada) || solicitacao.InicioLocal > solicitacao.FimLocal)
            {
                throw new BusinessException(ConstantsMessage.ErroMultasSolicitacaoInvalida);
            }

            return new MultasConsultaSolicitacao
            {
                Placa = placaNormalizada,
                InicioLocal = solicitacao.InicioLocal,
                FimLocal = solicitacao.FimLocal
            };
        }

        private IMultasProvider SelecionarProvedor(ConfiguracaoIntegracao configuracao)
        {
            string nomeProvedor = configuracao.GetRequired(ConstantsMultas.ChaveProvedor).Trim();
            IMultasProvider? provider = _providers.FirstOrDefault(
                item => string.Equals(item.Nome, nomeProvedor, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsMultas.ChaveProvedor, ConstantsMessage.ErroMultasProvedorNaoSuportado);
            }

            bool provedorFake = string.Equals(provider.Nome, ConstantsMultas.ProvedorFake, StringComparison.OrdinalIgnoreCase);
            if (provedorFake && configuracao.Ambiente != AmbienteIntegracao.SANDBOX)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsMultas.ChaveProvedor, ConstantsMessage.ErroMultasFakeForaDoSandbox);
            }

            return provider;
        }

        private void ValidarResultado(MultasConsultaResultado resultado, MultasConsultaSolicitacao solicitacao)
        {
            if (resultado == null || resultado.Infracoes == null)
            {
                throw new IntegrationException(ConstantsIntegration.Multas, ConstantsMessage.ErroMultasResultadoInvalido);
            }

            foreach (MultaResultado? infracao in resultado.Infracoes)
            {
                if (infracao == null
                    || string.IsNullOrWhiteSpace(infracao.IdentificadorProvedor)
                    || infracao.IdentificadorProvedor.Length > ConstantsMultas.IdentificadorProvedorTamanhoMaximo
                    || infracao.IdentificadorProvedor.Any(caractere => char.IsControl(caractere))
                    || (!string.IsNullOrEmpty(infracao.CodigoInfracao) && (infracao.CodigoInfracao.Length > ConstantsMultas.CodigoInfracaoTamanhoMaximo || infracao.CodigoInfracao.Any(caractere => char.IsControl(caractere))))
                    || string.IsNullOrWhiteSpace(infracao.Descricao)
                    || infracao.Descricao.Length > ConstantsMultas.DescricaoTamanhoMaximo
                    || infracao.Descricao.Any(caractere => char.IsControl(caractere))
                    || infracao.Valor < decimal.Zero
                    || infracao.Valor > ConstantsMultas.ValorMaximo
                    || decimal.Round(infracao.Valor, ConstantsMultas.CasasDecimaisValor) != infracao.Valor
                    || infracao.DataHoraLocal < solicitacao.InicioLocal
                    || infracao.DataHoraLocal > solicitacao.FimLocal)
                {
                    throw new IntegrationException(ConstantsIntegration.Multas, ConstantsMessage.ErroMultasResultadoInvalido);
                }
            }
        }
    }
}