using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Interfaces;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoVia.Integrations.Services.Caucao
{
    /// <summary>
    /// Gateway de caucao no cartao (P7-INT-01, RF-20). Valida a entrada (BusinessException, 409), resolve
    /// a integracao PAGAMENTO, le 'provedor' e 'caucao_validade_dias', escolhe o ICaucaoProvider pelo Nome,
    /// confere o formato do identificador e aplica as regras de estado. Nao persiste nada.
    ///
    /// Regras: capturar so em RESERVADA (valor menor = parcial, maior = erro); cancelar em RESERVADA e
    /// EM_ANALISE vai ao provedor; CAPTURADA/ESTORNADA nao cancelam; CANCELADA, EXPIRADA e RECUSADA devolvem
    /// o estado atual. Pre-autorizacao nunca captura (CAPTURADA = anomalia).
    /// Captura e cancelamento que falharem (BusinessException ou IntegrationException) consultam o provedor uma
    /// vez: se o estado pedido ja vale, devolvem o estado; senao relancam a excecao ORIGINAL.
    /// ReservaValidaAte existe somente em RESERVADA.
    /// </summary>
    public class CaucaoGateway : ICaucaoGateway
    {
        private readonly IIntegrationResolver _integrationResolver;
        private readonly IEnumerable<ICaucaoProvider> _providers;
        private readonly ILogService _logService;

        public CaucaoGateway(IIntegrationResolver integrationResolver, IEnumerable<ICaucaoProvider> providers, ILogService logService)
        {
            _integrationResolver = integrationResolver;
            _providers = providers;
            _logService = logService;
        }

        public async Task<CaucaoResultado> PreAutorizarAsync(CaucaoSolicitacao solicitacao, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(solicitacao);

            try
            {
                ValidarSolicitacao(solicitacao);

                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Pagamento, cancellationToken);
                int validadeDias = ObterValidadeDias(configuracao);
                ICaucaoProvider provider = SelecionarProvedor(configuracao);
                string chaveIdempotencia = GerarChaveIdempotencia(solicitacao);

                CaucaoResultado resultado = await provider.PreAutorizarAsync(configuracao, solicitacao, chaveIdempotencia, validadeDias, cancellationToken);

                if (resultado.Status == CaucaoStatus.CAPTURADA)
                {
                    // Anomalia: o provedor capturou em vez de reservar. Cliente recebe mensagem neutra;
                    // o log leva a referencia ENVIADA e o valor, para o estorno manual.
                    string registro = string.Format(
                        CultureInfo.InvariantCulture,
                        ConstantsMessage.LogCaucaoReservaCapturadaIndevida,
                        configuracao.Codigo,
                        solicitacao.ReferenciaExterna,
                        solicitacao.Valor.ToString(ConstantsCaucao.FormatoValorIdempotencia, CultureInfo.InvariantCulture),
                        resultado.IdentificadorProvedor);

                    _logService.Write(registro, ConstantsMessage.ErroCaucaoReservaNaoSuportada, GetType().ToString());
                    throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroCaucaoReservaNaoSuportada);
                }

                return AplicarValidade(resultado, validadeDias);
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
                // Falha de pre-autorizacao: registra a referencia (nao e dado pessoal) para localizar a reserva no provedor.
                RegistrarFalhaPreAutorizacao(solicitacao.ReferenciaExterna);
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCaucaoPreAutorizar, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoPreAutorizar, ex);
            }
        }

        public async Task<CaucaoResultado> ConsultarAsync(string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidarIdentificador(identificadorProvedor);

                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Pagamento, cancellationToken);
                int validadeDias = ObterValidadeDias(configuracao);
                ICaucaoProvider provider = SelecionarProvedor(configuracao);
                ValidarIdentificadorDoProvedor(provider, identificadorProvedor);

                CaucaoResultado resultado = await provider.ConsultarAsync(configuracao, identificadorProvedor, cancellationToken);

                return AplicarValidade(resultado, validadeDias);
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCaucaoConsultar, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoConsultar, ex);
            }
        }

        public async Task<CaucaoResultado> CapturarAsync(string identificadorProvedor, decimal? valor = null, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidarIdentificador(identificadorProvedor);
                ValidarValorCaptura(valor);

                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Pagamento, cancellationToken);
                int validadeDias = ObterValidadeDias(configuracao);
                ICaucaoProvider provider = SelecionarProvedor(configuracao);
                ValidarIdentificadorDoProvedor(provider, identificadorProvedor);

                CaucaoResultado atual = await provider.ConsultarAsync(configuracao, identificadorProvedor, cancellationToken);

                if (atual.Status == CaucaoStatus.CAPTURADA)
                {
                    // Ja capturada: mesmo valor devolve o estado (idempotente, sem PUT); outro valor e erro.
                    decimal valorDesejado = valor ?? atual.Valor;

                    if (atual.ValorCapturado == valorDesejado)
                    {
                        return AplicarValidade(atual, validadeDias);
                    }

                    throw new BusinessException(ConstantsMessage.ErroCaucaoJaCapturadaOutroValor);
                }

                if (atual.Status != CaucaoStatus.RESERVADA)
                {
                    throw new BusinessException(ConstantsMessage.ErroCaucaoNaoReservada);
                }

                if (valor.HasValue && valor.Value > atual.Valor)
                {
                    throw new BusinessException(ConstantsMessage.ErroCaucaoCapturaAcimaReservado);
                }

                decimal valorEsperado = valor ?? atual.Valor;
                CaucaoResultado capturada;

                try
                {
                    capturada = await provider.CapturarAsync(configuracao, identificadorProvedor, valor, cancellationToken);
                }
                catch (Exception excecaoOriginal) when (excecaoOriginal is BusinessException || excecaoOriginal is IntegrationException)
                {
                    // Reconciliacao: a captura pode ter ocorrido antes de a resposta se perder. Re-consulta uma vez.
                    CaucaoResultado? depois = await ConsultarParaReconciliacaoAsync(provider, configuracao, identificadorProvedor, cancellationToken);

                    if (depois != null && depois.Status == CaucaoStatus.CAPTURADA && depois.ValorCapturado == valorEsperado)
                    {
                        return AplicarValidade(depois, validadeDias);
                    }

                    throw;
                }

                ValidarEstadoDevolvido(provider, identificadorProvedor, capturada, CaucaoStatus.CAPTURADA);

                return AplicarValidade(capturada, validadeDias);
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCaucaoCapturar, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoCapturar, ex);
            }
        }

        public async Task<CaucaoResultado> CancelarAsync(string identificadorProvedor, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidarIdentificador(identificadorProvedor);

                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Pagamento, cancellationToken);
                int validadeDias = ObterValidadeDias(configuracao);
                ICaucaoProvider provider = SelecionarProvedor(configuracao);
                ValidarIdentificadorDoProvedor(provider, identificadorProvedor);

                CaucaoResultado atual = await provider.ConsultarAsync(configuracao, identificadorProvedor, cancellationToken);

                if (atual.Status == CaucaoStatus.CAPTURADA || atual.Status == CaucaoStatus.ESTORNADA)
                {
                    throw new BusinessException(ConstantsMessage.ErroCaucaoCancelarCapturada);
                }

                // Encerrada (cancelada, expirada ou recusada): devolve o estado atual, sem chamar o provedor.
                if (atual.Status == CaucaoStatus.CANCELADA || atual.Status == CaucaoStatus.EXPIRADA || atual.Status == CaucaoStatus.RECUSADA)
                {
                    return AplicarValidade(atual, validadeDias);
                }

                // RESERVADA e EM_ANALISE: o cancelamento vai ao provedor (EM_ANALISE tambem e cancelavel).
                CaucaoResultado cancelada;

                try
                {
                    cancelada = await provider.CancelarAsync(configuracao, identificadorProvedor, cancellationToken);
                }
                catch (Exception excecaoOriginal) when (excecaoOriginal is BusinessException || excecaoOriginal is IntegrationException)
                {
                    // Reconciliacao: o cancelamento pode ter ocorrido antes de a resposta se perder. Re-consulta uma vez.
                    CaucaoResultado? depois = await ConsultarParaReconciliacaoAsync(provider, configuracao, identificadorProvedor, cancellationToken);

                    if (depois != null && depois.Status == CaucaoStatus.CANCELADA)
                    {
                        return AplicarValidade(depois, validadeDias);
                    }

                    throw;
                }

                ValidarEstadoDevolvido(provider, identificadorProvedor, cancelada, CaucaoStatus.CANCELADA, CaucaoStatus.EXPIRADA);

                return AplicarValidade(cancelada, validadeDias);
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCaucaoCancelar, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoCancelar, ex);
            }
        }

        public async Task<CaucaoConfiguracaoCartao> ObterConfiguracaoCartaoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ConfiguracaoIntegracao configuracao = await _integrationResolver.ResolveAsync(ConstantsIntegration.Pagamento, cancellationToken);
                ICaucaoProvider provider = SelecionarProvedor(configuracao);

                string chavePublica;

                if (string.Equals(provider.Nome, ConstantsCaucao.ProvedorFake, StringComparison.OrdinalIgnoreCase))
                {
                    // FAKE usa a chave fixa de teste.
                    chavePublica = ConstantsCaucao.ChavePublicaFake;
                }
                else
                {
                    // Mercado Pago: public_key da integracao (nao e segredo), mas nunca igual a um valor protegido nem ao access_token.
                    chavePublica = configuracao.GetRequired(ConstantsCaucao.ChavePublica);

                    if (ChavePublicaIgualAValorProtegido(configuracao, chavePublica))
                    {
                        throw new IntegrationException(configuracao.Codigo, ConstantsCaucao.ChavePublica, ConstantsMessage.ErroCaucaoChavePublicaInvalida);
                    }
                }

                return new CaucaoConfiguracaoCartao
                {
                    Provedor = provider.Nome,
                    ChavePublica = chavePublica
                };
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCaucaoObterConfiguracaoCartao, GetType().ToString());
                throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoObterConfiguracaoCartao, ex);
            }
        }

        // ------------------------------------------------------------------
        // Reconciliacao e estado devolvido
        // ------------------------------------------------------------------

        /// <summary>Re-consulta usada na reconciliacao. Falha aqui e registrada e devolve null: quem chama relanca a excecao original.</summary>
        private async Task<CaucaoResultado?> ConsultarParaReconciliacaoAsync(ICaucaoProvider provider, ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken)
        {
            try
            {
                return await provider.ConsultarAsync(configuracao, identificadorProvedor, cancellationToken);
            }
            catch (Exception ex) when (ex is BusinessException || ex is IntegrationException || ex is NotFoundException)
            {
                _logService.Write(ex, ConstantsMessage.ErroCaucaoConsultar, GetType().ToString());
                return null;
            }
        }

        /// <summary>
        /// Estado devolvido por captura ou cancelamento. Fora dos estados aceitos = resposta inesperada, com log.
        /// Captura aceita so CAPTURADA; cancelamento aceita CANCELADA ou EXPIRADA.
        /// </summary>
        private void ValidarEstadoDevolvido(ICaucaoProvider provider, string identificadorProvedor, CaucaoResultado resultado, params CaucaoStatus[] aceitos)
        {
            if (aceitos.Contains(resultado.Status))
            {
                return;
            }

            string registro = string.Format(
                CultureInfo.InvariantCulture,
                ConstantsMessage.LogCaucaoEstadoInesperado,
                ConstantsIntegration.Pagamento,
                provider.Nome,
                resultado.Status.ToString(),
                identificadorProvedor);

            _logService.Write(registro, ConstantsMessage.ErroCaucaoRespostaInesperada, GetType().ToString());
            throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoRespostaInesperada);
        }

        private void RegistrarFalhaPreAutorizacao(string referenciaExterna)
        {
            string registro = string.Format(
                CultureInfo.InvariantCulture,
                ConstantsMessage.LogCaucaoFalhaPreAutorizacao,
                ConstantsIntegration.Pagamento,
                referenciaExterna);

            _logService.Write(registro, ConstantsMessage.ErroCaucaoPreAutorizar, GetType().ToString());
        }

        // ------------------------------------------------------------------
        // Validacoes (antes de qualquer chamada externa)
        // ------------------------------------------------------------------

        private static void ValidarSolicitacao(CaucaoSolicitacao solicitacao)
        {
            ValidarValorDuasCasas(solicitacao.Valor, ConstantsMessage.ErroCaucaoValorInvalido);

            string referencia = solicitacao.ReferenciaExterna;

            if (string.IsNullOrEmpty(referencia) || referencia.Length > ConstantsCaucao.ReferenciaTamanhoMaximo || !Regex.IsMatch(referencia, ConstantsCaucao.PadraoReferenciaExterna))
            {
                throw new BusinessException(ConstantsMessage.ErroCaucaoReferenciaInvalida);
            }

            if (string.IsNullOrWhiteSpace(solicitacao.Descricao) || solicitacao.Descricao.Length > ConstantsCaucao.DescricaoTamanhoMaximo)
            {
                throw new BusinessException(ConstantsMessage.ErroCaucaoDescricaoInvalida);
            }

            ValidarCartao(solicitacao);
            ValidarPagador(solicitacao.Pagador);
        }

        private static void ValidarCartao(CaucaoSolicitacao solicitacao)
        {
            string token = solicitacao.TokenCartao;

            if (string.IsNullOrEmpty(token) || token.Length < ConstantsCaucao.TokenTamanhoMinimo || token.Length > ConstantsCaucao.TokenTamanhoMaximo || !Regex.IsMatch(token, ConstantsCaucao.PadraoToken))
            {
                throw new BusinessException(ConstantsMessage.ErroCaucaoCartaoInvalido);
            }

            string metodo = solicitacao.MetodoPagamentoId;

            if (string.IsNullOrEmpty(metodo) || metodo.Length < ConstantsCaucao.MetodoPagamentoTamanhoMinimo || metodo.Length > ConstantsCaucao.MetodoPagamentoTamanhoMaximo || !Regex.IsMatch(metodo, ConstantsCaucao.PadraoMetodoPagamento))
            {
                throw new BusinessException(ConstantsMessage.ErroCaucaoCartaoInvalido);
            }

            if (solicitacao.EmissorId.HasValue && solicitacao.EmissorId.Value <= 0)
            {
                throw new BusinessException(ConstantsMessage.ErroCaucaoCartaoInvalido);
            }

            // Dispositivo em branco = ausente (nao e erro). Se informado, precisa ter o formato seguro (sem quebra de linha).
            string? dispositivo = solicitacao.IdentificadorDispositivo;

            if (!string.IsNullOrWhiteSpace(dispositivo) && !Regex.IsMatch(dispositivo, ConstantsCaucao.PadraoDispositivo))
            {
                throw new BusinessException(ConstantsMessage.ErroCaucaoCartaoInvalido);
            }
        }

        private static void ValidarPagador(CaucaoPagador? pagador)
        {
            if (pagador == null)
            {
                throw new BusinessException(ConstantsMessage.ErroCaucaoPagadorInvalido);
            }

            // Limite de tamanho antes do regex: entrada enorme nao chega ao padrao de e-mail.
            if (string.IsNullOrWhiteSpace(pagador.Email) || pagador.Email.Length > ConstantsCaucao.EmailTamanhoMaximo || !Regex.IsMatch(pagador.Email, ConstantsCaucao.PadraoEmail))
            {
                throw new BusinessException(ConstantsMessage.ErroCaucaoPagadorInvalido);
            }

            if (!string.IsNullOrWhiteSpace(pagador.Documento) && !Regex.IsMatch(pagador.Documento, ConstantsCaucao.PadraoCpf))
            {
                throw new BusinessException(ConstantsMessage.ErroCaucaoPagadorInvalido);
            }
        }

        private static void ValidarIdentificador(string identificadorProvedor)
        {
            if (string.IsNullOrWhiteSpace(identificadorProvedor))
            {
                throw new BusinessException(ConstantsMessage.ErroCaucaoIdentificadorObrigatorio);
            }
        }

        /// <summary>Formato do identificador conforme o provedor selecionado (ex.: FAKE nao aceita id numerico do Mercado Pago).</summary>
        private static void ValidarIdentificadorDoProvedor(ICaucaoProvider provider, string identificadorProvedor)
        {
            if (!provider.IdentificadorValido(identificadorProvedor))
            {
                throw new BusinessException(ConstantsMessage.ErroCaucaoIdentificadorInvalido);
            }
        }

        /// <summary>Captura: valor nulo = integral (sem validar); valor informado precisa ser maior que zero e ter ate 2 casas.</summary>
        private static void ValidarValorCaptura(decimal? valor)
        {
            if (valor.HasValue)
            {
                ValidarValorDuasCasas(valor.Value, ConstantsMessage.ErroCaucaoValorInvalido);
            }
        }

        private static void ValidarValorDuasCasas(decimal valor, string mensagem)
        {
            if (valor <= 0 || decimal.Round(valor, 2) != valor)
            {
                throw new BusinessException(mensagem);
            }
        }

        // ------------------------------------------------------------------
        // Chave de idempotencia e validade da reserva
        // ------------------------------------------------------------------

        /// <summary>
        /// Chave da tentativa: mesma referencia + token + valor = mesma chave = mesma reserva. Token novo = chave nova.
        /// O hash nao e logado e o token nunca sai da requisicao.
        /// </summary>
        private static string GerarChaveIdempotencia(CaucaoSolicitacao solicitacao)
        {
            string materia = string.Join(
                ConstantsCaucao.SeparadorHashIdempotencia,
                solicitacao.ReferenciaExterna,
                solicitacao.TokenCartao,
                solicitacao.Valor.ToString(ConstantsCaucao.FormatoValorIdempotencia, CultureInfo.InvariantCulture));

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(materia));
            string hex = Convert.ToHexString(hash).ToLowerInvariant();

            return ConstantsCaucao.PrefixoChaveIdempotencia + hex.Substring(0, ConstantsCaucao.TamanhoHashChaveIdempotencia);
        }

        /// <summary>caucao_validade_dias (opcional, padrao 5). Fora de 1..30 ou nao numerica e erro de configuracao.</summary>
        private static int ObterValidadeDias(ConfiguracaoIntegracao configuracao)
        {
            string? valorConfigurado = configuracao.GetOptional(ConstantsCaucao.ChaveValidadeDias);

            if (string.IsNullOrWhiteSpace(valorConfigurado))
            {
                return ConstantsCaucao.ValidadePadraoDias;
            }

            int dias;

            if (!int.TryParse(valorConfigurado.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out dias)
                || dias < ConstantsCaucao.ValidadeMinimaDias
                || dias > ConstantsCaucao.ValidadeMaximaDias)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsCaucao.ChaveValidadeDias, ConstantsMessage.ErroCaucaoValidadeConfiguracaoInvalida);
            }

            return dias;
        }

        /// <summary>ReservaValidaAte = CriadoEm + validade, somente em RESERVADA. Em qualquer outro estado fica nulo.</summary>
        private static CaucaoResultado AplicarValidade(CaucaoResultado resultado, int validadeDias)
        {
            if (resultado.Status == CaucaoStatus.RESERVADA && resultado.CriadoEm.HasValue)
            {
                resultado.ReservaValidaAte = resultado.CriadoEm.Value.AddDays(validadeDias);
            }
            else
            {
                resultado.ReservaValidaAte = null;
            }

            return resultado;
        }

        /// <summary>
        /// True quando a public_key e igual a algum valor protegido da integracao (webhook_secret, client_secret...)
        /// ou ao access_token. Compara os valores pelo nome da chave (ConfiguracaoIntegracao nao expoe a lista de valores
        /// sem a chave), ignorando a propria public_key.
        /// </summary>
        private static bool ChavePublicaIgualAValorProtegido(ConfiguracaoIntegracao configuracao, string chavePublica)
        {
            string candidata = chavePublica.Trim();

            foreach (string chave in configuracao.Chaves)
            {
                bool sensivel = configuracao.IsProtected(chave) || string.Equals(chave, ConstantsCaucao.ChaveAccessToken, StringComparison.OrdinalIgnoreCase);

                if (!sensivel || string.Equals(chave, ConstantsCaucao.ChavePublica, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string? valor = configuracao.GetOptional(chave);

                if (!string.IsNullOrWhiteSpace(valor) && string.Equals(valor.Trim(), candidata, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------------
        // Selecao do provedor
        // ------------------------------------------------------------------

        /// <summary>Escolhe o provedor pela chave 'provedor'. FAKE so funciona em SANDBOX.</summary>
        private ICaucaoProvider SelecionarProvedor(ConfiguracaoIntegracao configuracao)
        {
            string nomeProvedor = configuracao.GetRequired(ConstantsCaucao.ChaveProvedor).Trim();

            ICaucaoProvider? provider = _providers.FirstOrDefault(x => string.Equals(x.Nome, nomeProvedor, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsCaucao.ChaveProvedor, ConstantsMessage.ErroCaucaoProvedorNaoSuportado);
            }

            bool isFake = string.Equals(provider.Nome, ConstantsCaucao.ProvedorFake, StringComparison.OrdinalIgnoreCase);

            if (isFake && configuracao.Ambiente != AmbienteIntegracao.SANDBOX)
            {
                throw new IntegrationException(configuracao.Codigo, ConstantsCaucao.ChaveProvedor, ConstantsMessage.ErroCaucaoFakeForaDoSandbox);
            }

            return provider;
        }
    }
}
