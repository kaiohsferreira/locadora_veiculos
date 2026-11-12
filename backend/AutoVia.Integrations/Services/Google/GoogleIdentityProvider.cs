using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Google;
using AutoVia.Integrations.Helpers.Google;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Requests.Google;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace AutoVia.Integrations.Services.Google
{
    /// <summary>
    /// Provedor GOOGLE: valida a prova (id_token RS256) com JsonWebTokenHandler e as chaves publicas do JWKS.
    ///
    /// Validacao (fail-closed: qualquer falha da prova vira UnauthorizedException):
    /// assinatura RS256 com chave do JWKS, RequireSignedTokens, emissor em EmissoresValidos, audiencia = client_id,
    /// validade (exp e nbf) com ClockSkew de 60 s. Depois: sub (1..255), email (ate 254) e email_verified verdadeiro
    /// (booleano ou texto "true").
    ///
    /// Chaves publicas: GET {base_url}/oauth2/v3/certs pelo IComunicacaoService, com cache em IMemoryCache
    /// (3600 s). A base_url precisa ser https, o host do Google (www.googleapis.com), porta padrao, sem userinfo e
    /// caminho raiz; senao nem ha chamada.
    /// Chave desconhecida (kid) dispara no maximo uma atualizacao a cada 300 s. Depois de uma busca que falhou,
    /// nova busca so passados 60 s (cache negativo), inclusive sem cache: a rede nao e chamada a cada login.
    /// Falha da atualizacao com um JWKS anterior usa esse JWKS (reserva), so ate JwksReservaMaximaHoras (24 h) desde a
    /// ultima busca bem-sucedida; depois disso a reserva nao e mais usada. Sem reserva valida, IntegrationException (502).
    /// A busca e serializada por SemaphoreSlim.
    ///
    /// Log: so o nome do provedor, o status ou o tipo da excecao e o motivo generico da recusa. Nunca credencial,
    /// sub, e-mail, nome ou o objeto de excecao (a biblioteca de token pode trazer trecho da prova na mensagem).
    /// </summary>
    public class GoogleIdentityProvider : IGoogleIdentityProvider
    {
        // Criacao do estado do cache: a checagem e a gravacao precisam ser atomicas entre instancias scoped.
        private static readonly object TravaCriacaoEstado = new object();

        private readonly IComunicacaoService _comunicacao;
        private readonly IMemoryCache _cache;
        private readonly ILogService _logService;
        private readonly Func<DateTime> _relogio;
        private readonly JsonWebTokenHandler _tokenHandler;

        /// <summary>Construtor usado pela injecao de dependencia: relogio do sistema (DateTime.Now).</summary>
        public GoogleIdentityProvider(IComunicacaoService comunicacao, IMemoryCache cache, ILogService logService)
            : this(comunicacao, cache, logService, () => DateTime.Now)
        {
        }

        /// <summary>Construtor para testes: permite controlar o relogio do intervalo de atualizacao das chaves.</summary>
        internal GoogleIdentityProvider(IComunicacaoService comunicacao, IMemoryCache cache, ILogService logService, Func<DateTime> relogio)
        {
            _comunicacao = comunicacao;
            _cache = cache;
            _logService = logService;
            _relogio = relogio;
            _tokenHandler = new JsonWebTokenHandler();
        }

        public string Nome => ConstantsGoogle.ProvedorGoogle;

        /// <summary>Valida a prova do Google e devolve a identidade (sub, e-mail verificado em minusculas, nome).</summary>
        public async Task<GoogleIdentidade> ValidarAsync(ConfiguracaoIntegracao configuracao, string clientId, string credencial, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuracao);

            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    throw new IntegrationException(configuracao.Codigo, ConstantsGoogle.ChaveClientId, ConstantsMessage.ErroGoogleIndisponivel);
                }

                if (string.IsNullOrWhiteSpace(credencial))
                {
                    throw RecusarCredencial(ConstantsGoogle.MotivoCredencialAusente);
                }

                if (credencial.Length > ConstantsGoogle.TamanhoMaximoCredencial)
                {
                    throw RecusarCredencial(ConstantsGoogle.MotivoTamanho);
                }

                if (!_tokenHandler.CanReadToken(credencial))
                {
                    throw RecusarCredencial(ConstantsGoogle.MotivoCredencialMalformada);
                }

                JsonWebToken cabecalho = LerCabecalho(credencial);
                JsonWebKeySet chaves = await ObterChavesAsync(configuracao, cabecalho.Kid, cancellationToken);

                TokenValidationResult resultado = await _tokenHandler.ValidateTokenAsync(credencial, MontarParametros(clientId, chaves));

                if (!resultado.IsValid)
                {
                    throw RecusarCredencial(resultado.Exception?.GetType().Name ?? ConstantsGoogle.MotivoValidacao);
                }

                JsonWebToken? token = resultado.SecurityToken as JsonWebToken;
                if (token == null)
                {
                    throw RecusarCredencial(ConstantsGoogle.MotivoValidacao);
                }

                return LerIdentidade(token);
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelamento pedido pelo chamador: nao e falha do Google.
                throw;
            }
            catch (Exception ex)
            {
                // So o tipo da excecao: a mensagem da biblioteca pode conter trecho da prova. Sem a excecao como causa.
                _logService.Write(string.Format(ConstantsMessage.LogGoogleFalhaValidacao, ex.GetType().Name), ConstantsMessage.ErroGoogleIndisponivel, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroGoogleIndisponivel);
            }
        }

        // ------------------------------------------------------------------
        // Parametros da prova
        // ------------------------------------------------------------------

        /// <summary>Le o cabecalho (kid) sem validar. Token com estrutura ou conteudo invalido e recusa (401), nunca 502.</summary>
        private JsonWebToken LerCabecalho(string credencial)
        {
            try
            {
                return _tokenHandler.ReadJsonWebToken(credencial);
            }
            catch (SecurityTokenException)
            {
                throw RecusarCredencial(ConstantsGoogle.MotivoCredencialMalformada);
            }
            catch (ArgumentException)
            {
                throw RecusarCredencial(ConstantsGoogle.MotivoCredencialMalformada);
            }
        }

        /// <summary>Regras da prova: algoritmo, emissor, audiencia, validade, assinatura e tolerancia de relogio.</summary>
        private static TokenValidationParameters MontarParametros(string clientId, JsonWebKeySet chaves)
        {
            return new TokenValidationParameters
            {
                ValidAlgorithms = new List<string> { ConstantsGoogle.AlgoritmoAssinatura },
                RequireSignedTokens = true,
                ValidateIssuer = true,
                ValidIssuers = ConstantsGoogle.EmissoresValidos,
                ValidateAudience = true,
                ValidAudience = clientId,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = chaves.Keys,
                ClockSkew = TimeSpan.FromSeconds(ConstantsGoogle.ToleranciaRelogioSegundos)
            };
        }

        // ------------------------------------------------------------------
        // Claims da prova (depois da validacao criptografica)
        // ------------------------------------------------------------------

        /// <summary>Le sub, e-mail verificado e nome. Qualquer requisito faltando e recusa (401).</summary>
        private GoogleIdentidade LerIdentidade(JsonWebToken token)
        {
            string? sub = ObterClaim(token, ConstantsGoogle.ClaimSub);
            if (string.IsNullOrWhiteSpace(sub) || sub.Length > ConstantsGoogle.TamanhoMaximoSub)
            {
                throw RecusarCredencial(ConstantsGoogle.MotivoClaims);
            }

            string? email = ObterClaim(token, ConstantsGoogle.ClaimEmail);
            if (string.IsNullOrWhiteSpace(email) || email.Length > ConstantsGoogle.TamanhoMaximoEmail)
            {
                throw RecusarCredencial(ConstantsGoogle.MotivoClaims);
            }

            // O Google envia email_verified como booleano; o valor vira "True" ou "true" conforme a leitura.
            string? verificado = ObterClaim(token, ConstantsGoogle.ClaimEmailVerificado);
            if (!string.Equals(verificado, ConstantsGoogle.ValorVerdadeiro, StringComparison.OrdinalIgnoreCase))
            {
                throw RecusarCredencial(ConstantsGoogle.MotivoNaoVerificado);
            }

            return new GoogleIdentidade
            {
                Sub = sub,
                Email = email.Trim().ToLowerInvariant(),
                EmailVerificado = true,
                Nome = NormalizarNome(ObterClaim(token, ConstantsGoogle.ClaimNome))
            };
        }

        private static string? ObterClaim(JsonWebToken token, string tipo)
        {
            Claim? claim = token.Claims.FirstOrDefault(x => string.Equals(x.Type, tipo, StringComparison.Ordinal));
            return claim?.Value;
        }

        /// <summary>Nome opcional: vazio vira null e o excesso e cortado no limite de 120 caracteres.</summary>
        private static string? NormalizarNome(string? nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return null;
            }

            string limpo = nome.Trim();
            return limpo.Length > ConstantsGoogle.TamanhoMaximoNome
                ? limpo.Substring(0, ConstantsGoogle.TamanhoMaximoNome)
                : limpo;
        }

        /// <summary>Registra a recusa com motivo generico e devolve a excecao para ser lancada por quem chama.</summary>
        private UnauthorizedException RecusarCredencial(string motivo)
        {
            _logService.WriteInfo(string.Format(ConstantsMessage.LogGoogleCredencialRecusada, Nome, motivo), GetType().ToString());
            return new UnauthorizedException(ConstantsMessage.ErroGoogleCredencialInvalida);
        }

        // ------------------------------------------------------------------
        // Chaves publicas: origem, cache, cache negativo, reserva e atualizacao controlada
        // ------------------------------------------------------------------

        /// <summary>
        /// Devolve o JWKS. Usa o cache quando ele tem o kid (ou quando a prova nao traz kid). Sem cache ou com kid
        /// desconhecido, busca no Google, respeitando PodeBuscarChaves. Se a busca falha e ha um JWKS anterior, usa-o.
        /// A busca e serializada por SemaphoreSlim.
        /// </summary>
        private async Task<JsonWebKeySet> ObterChavesAsync(ConfiguracaoIntegracao configuracao, string? kid, CancellationToken cancellationToken)
        {
            JsonWebKeySet? emCache = ObterChavesEmCache();
            if (emCache != null && (kid == null || ContemKid(emCache, kid)))
            {
                return emCache;
            }

            // A base_url vem do banco: so segue para o host do Google, em https, antes de qualquer chamada.
            ValidarOrigemChaves(configuracao);

            EstadoChaves estado = ObterEstado(_cache);
            await estado.Trava.WaitAsync(cancellationToken);

            try
            {
                // Outra requisicao pode ter atualizado enquanto esperava a trava.
                JsonWebKeySet? atual = ObterChavesEmCache();
                if (atual != null && (kid == null || ContemKid(atual, kid)))
                {
                    return atual;
                }

                // Ultimo conjunto conhecido: o cache atual ou a reserva (o ultimo JWKS bom, valido por ate 24 h).
                JsonWebKeySet? conhecido = atual ?? ReservaValida(estado);

                if (!PodeBuscarChaves(estado, atual))
                {
                    if (conhecido != null)
                    {
                        return conhecido;
                    }

                    throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroGoogleIndisponivel);
                }

                try
                {
                    return await BuscarEGuardarAsync(configuracao, estado, cancellationToken);
                }
                catch (IntegrationException)
                {
                    if (conhecido == null)
                    {
                        throw;
                    }

                    // Falha da atualizacao nao derruba o login: o conjunto anterior continua valendo.
                    _logService.WriteInfo(ConstantsMessage.LogGoogleChavesReservaUsada, GetType().ToString());

                    return conhecido;
                }
            }
            finally
            {
                estado.Trava.Release();
            }
        }

        /// <summary>
        /// Exige https, o host do Google, a porta padrao, SEM userinfo, caminho raiz (ou vazio) e sem query nem fragmento
        /// na base_url. Fora disso, IntegrationException sem nenhuma chamada de rede.
        /// </summary>
        private void ValidarOrigemChaves(ConfiguracaoIntegracao configuracao)
        {
            bool origemValida = Uri.TryCreate(configuracao.BaseUrl, UriKind.Absolute, out Uri? url)
                && url != null
                && string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && string.Equals(url.Host, ConstantsGoogle.HostCertificados, StringComparison.OrdinalIgnoreCase)
                && url.IsDefaultPort
                && string.IsNullOrEmpty(url.UserInfo)
                && string.Equals(url.AbsolutePath, ConstantsGoogle.CaminhoRaizBaseUrl, StringComparison.Ordinal)
                && string.IsNullOrEmpty(url.Query)
                && string.IsNullOrEmpty(url.Fragment);

            if (!origemValida)
            {
                _logService.Write(ConstantsMessage.LogGoogleBaseUrlInvalida, ConstantsMessage.ErroGoogleIndisponivel, GetType().ToString());
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroGoogleIndisponivel);
            }
        }

        private JsonWebKeySet? ObterChavesEmCache()
        {
            if (_cache.TryGetValue(ConstantsGoogle.ChaveCacheJwks, out JsonWebKeySet? chaves))
            {
                return chaves;
            }

            return null;
        }

        /// <summary>
        /// Decide se pode buscar o JWKS agora. Depois de uma falha, so passados 60 s (cache negativo, sem rede), com ou
        /// sem cache. Com cache atual que nao tem o kid, so passados 300 s desde a ultima tentativa.
        /// </summary>
        private bool PodeBuscarChaves(EstadoChaves estado, JsonWebKeySet? atual)
        {
            if (estado.UltimaFalha != null && _relogio() - estado.UltimaFalha.Value < TimeSpan.FromSeconds(ConstantsGoogle.JwksIntervaloFalhaSegundos))
            {
                return false;
            }

            if (atual == null)
            {
                return true;
            }

            return PodeAtualizarPorKidDesconhecido(estado);
        }

        /// <summary>
        /// Reserva: o ultimo JWKS bom, so enquanto a idade desde a ultima busca bem-sucedida for menor que
        /// JwksReservaMaximaHoras. Depois disso devolve nulo e a falha vira IntegrationException (502).
        /// </summary>
        private JsonWebKeySet? ReservaValida(EstadoChaves estado)
        {
            if (estado.UltimoJwksValido == null || estado.UltimoJwksValidoEm == null)
            {
                return null;
            }

            TimeSpan idade = _relogio() - estado.UltimoJwksValidoEm.Value;

            if (idade >= TimeSpan.FromHours(ConstantsGoogle.JwksReservaMaximaHoras))
            {
                return null;
            }

            return estado.UltimoJwksValido;
        }

        private bool PodeAtualizarPorKidDesconhecido(EstadoChaves estado)
        {
            if (estado.UltimaTentativa == null)
            {
                return true;
            }

            TimeSpan decorrido = _relogio() - estado.UltimaTentativa.Value;
            return decorrido >= TimeSpan.FromSeconds(ConstantsGoogle.JwksIntervaloRefreshSegundos);
        }

        /// <summary>Busca o JWKS pelo IComunicacaoService, valida o conteudo e guarda no cache e na reserva. Falha = IntegrationException e marca o cache negativo.</summary>
        private async Task<JsonWebKeySet> BuscarEGuardarAsync(ConfiguracaoIntegracao configuracao, EstadoChaves estado, CancellationToken cancellationToken)
        {
            // A tentativa conta mesmo quando falha: limita a carga no Google durante uma indisponibilidade.
            estado.UltimaTentativa = _relogio();

            RequestResponse<string> resposta;

            try
            {
                resposta = await _comunicacao.SendAsync<string>(GoogleCertificadosRequest.Montar(configuracao), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelamento pedido pelo chamador: nao e falha do Google e nao abre o cache negativo.
                throw;
            }
            catch (Exception ex)
            {
                // So o tipo da excecao: a mensagem pode trazer conteudo da requisicao.
                _logService.Write(string.Format(ConstantsMessage.LogGoogleFalhaBuscaChaves, ex.GetType().Name), ConstantsMessage.ErroGoogleIndisponivel, GetType().ToString());
                estado.UltimaFalha = _relogio();
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroGoogleIndisponivel);
            }

            if (!resposta.IsSuccess || string.IsNullOrWhiteSpace(resposta.RawContent))
            {
                _logService.WriteInfo(string.Format(ConstantsMessage.LogGoogleChavesIndisponiveis, resposta.StatusCode), GetType().ToString());
                estado.UltimaFalha = _relogio();
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroGoogleIndisponivel);
            }

            JsonWebKeySet? chaves = Interpretar(resposta.RawContent);
            if (chaves == null || chaves.Keys.Count == 0)
            {
                estado.UltimaFalha = _relogio();
                throw new IntegrationException(configuracao.Codigo, ConstantsMessage.ErroGoogleCarregarChaves);
            }

            estado.UltimaFalha = null;
            estado.UltimoJwksValido = chaves;
            estado.UltimoJwksValidoEm = _relogio();

            _cache.Set(ConstantsGoogle.ChaveCacheJwks, chaves, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ConstantsGoogle.JwksCacheSegundos)
            });

            return chaves;
        }

        /// <summary>Converte o corpo em JWKS. Corpo invalido vira null (com log); quem chama lanca a excecao.</summary>
        private JsonWebKeySet? Interpretar(string conteudo)
        {
            try
            {
                return new JsonWebKeySet(conteudo);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroGoogleCarregarChaves, GetType().ToString());
                return null;
            }
        }

        private static bool ContemKid(JsonWebKeySet chaves, string kid)
        {
            return chaves.Keys.Any(x => string.Equals(x.Kid, kid, StringComparison.Ordinal));
        }

        /// <summary>Estado do cache de chaves (trava, ultima tentativa, ultima falha e reserva). Fica no IMemoryCache, que e singleton.</summary>
        private static EstadoChaves ObterEstado(IMemoryCache cache)
        {
            lock (TravaCriacaoEstado)
            {
                if (cache.TryGetValue(ConstantsGoogle.ChaveCacheEstadoJwks, out EstadoChaves? existente) && existente != null)
                {
                    return existente;
                }

                EstadoChaves novo = new EstadoChaves();
                cache.Set(ConstantsGoogle.ChaveCacheEstadoJwks, novo, new MemoryCacheEntryOptions
                {
                    Priority = CacheItemPriority.NeverRemove
                });

                return novo;
            }
        }

        /// <summary>
        /// Estado de atualizacao das chaves: trava de uma atualizacao por vez, instante da ultima tentativa, instante
        /// da ultima falha (cache negativo), o ultimo JWKS valido (reserva) e o instante em que ele foi obtido
        /// (a reserva vale ate JwksReservaMaximaHoras depois disso).
        /// </summary>
        private sealed class EstadoChaves
        {
            public SemaphoreSlim Trava { get; } = new SemaphoreSlim(1, 1);

            public DateTime? UltimaTentativa { get; set; }

            public DateTime? UltimaFalha { get; set; }

            public JsonWebKeySet? UltimoJwksValido { get; set; }

            public DateTime? UltimoJwksValidoEm { get; set; }
        }
    }
}
