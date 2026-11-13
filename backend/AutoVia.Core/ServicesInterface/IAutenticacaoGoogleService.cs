using AutoVia.Communication.ViewObjects.Autenticacao;
using System.Threading;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Login de cliente com conta Google (N3, RF-51). A conta Google sozinha nunca cria cliente.
    /// O vinculo com o cadastro so nasce depois de provar a senha da conta AutoVia.
    /// </summary>
    public interface IAutenticacaoGoogleService
    {
        /// <summary>Configuracao publica do login com Google (botao, Client ID so quando habilitado, modo de teste).</summary>
        /// <summary>O cancellationToken (RequestAborted da requisicao) segue ate o gateway; cancelado, lanca OperationCanceledException.</summary>
        Task<ReturnGoogleConfiguracaoVO> ObterConfiguracaoAsync(CancellationToken cancellationToken = default);

        /// <summary>Valida a prova do Google e devolve a situacao: AUTENTICADO (com sessao), CONFIRMACAO_NECESSARIA, CADASTRO_NECESSARIO ou CONTA_AMBIGUA. Nao grava nada.</summary>
        Task<ReturnGoogleLoginVO> LoginAsync(GoogleLoginVO vo, CancellationToken cancellationToken = default);

        /// <summary>Vincula a conta Google ao cliente depois de conferir CPF e senha, e abre a sessao. Lanca UnauthorizedException (credenciais), BusinessException (409) ou IntegrationException (502).</summary>
        Task<ReturnLoginVO> VincularAsync(GoogleVinculoVO vo, CancellationToken cancellationToken = default);
    }
}
