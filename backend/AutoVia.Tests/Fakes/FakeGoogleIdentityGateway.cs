using AutoVia.Communication.CustomExceptions;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Google;
using AutoVia.Integrations.Interfaces;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// IGoogleIdentityGateway em memoria: a identidade de cada credencial e a configuracao publica sao escolhidas
    /// pelo teste; credencial nao cadastrada vira UnauthorizedException (prova invalida). Sem rede.
    /// ExcecaoAoValidar simula Google indisponivel ou outra falha da integracao.
    /// </summary>
    public class FakeGoogleIdentityGateway : IGoogleIdentityGateway
    {
        private readonly Dictionary<string, GoogleIdentidade> _identidades = new Dictionary<string, GoogleIdentidade>();

        /// <summary>Configuracao publica devolvida pelo fake (padrao: habilitado, com Client ID de teste).</summary>
        public GoogleConfiguracaoPublica Configuracao { get; set; } = new GoogleConfiguracaoPublica
        {
            Habilitado = true,
            ClientId = "cliente-google-teste",
            ModoTeste = false
        };

        /// <summary>Quando nao for null, a validacao lanca esta excecao (ex.: IntegrationException para Google indisponivel).</summary>
        public Exception? ExcecaoAoValidar { get; set; }

        /// <summary>Quantas validacoes de credencial foram pedidas.</summary>
        public int Validacoes { get; private set; }

        /// <summary>Token de cancelamento recebido na ultima validacao (o teste confere que o servico o repassou).</summary>
        public CancellationToken UltimoTokenRecebido { get; private set; }

        /// <summary>Faz a credencial informada valer como a identidade dada.</summary>
        public void Aceitar(string credencial, GoogleIdentidade identidade)
        {
            _identidades[credencial] = identidade;
        }

        /// <summary>Devolve a configuracao publica configurada pelo teste.</summary>
        public Task<GoogleConfiguracaoPublica> ObterConfiguracaoPublicaAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Configuracao);
        }

        /// <summary>Devolve a identidade da credencial aceita; qualquer outra credencial e prova invalida.</summary>
        public Task<GoogleIdentidade> ValidarCredencialAsync(string credencial, CancellationToken cancellationToken = default)
        {
            Validacoes++;
            UltimoTokenRecebido = cancellationToken;

            // Como o gateway real: requisicao cancelada nao segue para a rede.
            cancellationToken.ThrowIfCancellationRequested();

            if (ExcecaoAoValidar != null)
            {
                throw ExcecaoAoValidar;
            }

            if (!_identidades.TryGetValue(credencial, out GoogleIdentidade? identidade))
            {
                throw new UnauthorizedException(ConstantsMessage.ErroGoogleCredencialInvalida);
            }

            return Task.FromResult(identidade);
        }
    }
}
