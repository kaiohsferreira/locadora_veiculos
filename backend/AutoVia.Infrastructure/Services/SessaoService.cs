using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Conferencia da sessao (RF-50, RN2.5 e RN2.6): a cada requisicao autenticada, a conta do
    /// token precisa estar ativa (cliente sem DisabledAt; funcionario sem DisabledAt e com Ativo)
    /// e a versao da sessao (sv) precisa ser a da senha_hash atual. O resultado, inclusive o
    /// negativo, fica em IMemoryCache por usuario durante Autenticacao:Sessao:CacheSegundos.
    /// Nunca registra sv, senha, hash ou token no log.
    /// </summary>
    public class SessaoService : ISessaoService
    {
        private readonly IAutenticacaoRepository _autenticacaoRepository;
        private readonly ITokenService _tokenService;
        private readonly IMemoryCache _cache;
        private readonly SessaoOptions _options;
        private readonly ILogService _logService;

        public SessaoService(
            IAutenticacaoRepository autenticacaoRepository,
            ITokenService tokenService,
            IMemoryCache cache,
            SessaoOptions options,
            ILogService logService)
        {
            _autenticacaoRepository = autenticacaoRepository;
            _tokenService = tokenService;
            _cache = cache;
            _options = options;
            _logService = logService;
        }

        /// <summary>Indica se a sessao do token ainda vale: conta ativa e sv igual a atual. Sem sv, devolve false sem consultar o banco.</summary>
        public async Task<bool> SessaoValidaAsync(PerfilUsuario perfil, int idUsuario, string? versaoSessao)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(versaoSessao))
                {
                    return false;
                }

                string chave = MontarChave(perfil, idUsuario);
                SessaoCacheada situacao = await ObterSituacaoAsync(perfil, idUsuario, chave);

                bool valida = situacao.Ativa && string.Equals(situacao.VersaoSessao, versaoSessao, StringComparison.Ordinal);

                return valida;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroValidarSessao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Descarta o resultado em cache do usuario; a proxima verificacao consulta o banco.</summary>
        public void Invalidar(PerfilUsuario perfil, int idUsuario)
        {
            try
            {
                _cache.Remove(MontarChave(perfil, idUsuario));
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroValidarSessao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Le a situacao do usuario do cache; sem entrada, consulta o banco e guarda o resultado pelo TTL configurado.</summary>
        private async Task<SessaoCacheada> ObterSituacaoAsync(PerfilUsuario perfil, int idUsuario, string chave)
        {
            SessaoCacheada? emCache = _cache.Get<SessaoCacheada>(chave);

            if (emCache != null)
            {
                return emCache;
            }

            SessaoCacheada situacao = await CarregarSituacaoAsync(perfil, idUsuario);
            _cache.Set<SessaoCacheada>(chave, situacao, TimeSpan.FromSeconds(_options.CacheSegundos));

            return situacao;
        }

        /// <summary>Consulta a conta ativa e calcula a versao atual da sessao. Conta inexistente, desativada ou sem senha vira situacao encerrada.</summary>
        private async Task<SessaoCacheada> CarregarSituacaoAsync(PerfilUsuario perfil, int idUsuario)
        {
            if (perfil == PerfilUsuario.CLIENTE)
            {
                Cliente? cliente = await _autenticacaoRepository.FindClienteByIdAsync(idUsuario);

                if (cliente == null || string.IsNullOrWhiteSpace(cliente.SenhaHash))
                {
                    return SessaoCacheada.Encerrada();
                }

                return new SessaoCacheada(true, _tokenService.CalcularVersaoSessao(cliente.SenhaHash));
            }

            if (perfil == PerfilUsuario.FUNCIONARIO)
            {
                Funcionario? funcionario = await _autenticacaoRepository.FindFuncionarioByIdAsync(idUsuario);

                if (funcionario == null || !funcionario.Ativo || string.IsNullOrWhiteSpace(funcionario.SenhaHash))
                {
                    return SessaoCacheada.Encerrada();
                }

                return new SessaoCacheada(true, _tokenService.CalcularVersaoSessao(funcionario.SenhaHash));
            }

            return SessaoCacheada.Encerrada();
        }

        /// <summary>Chave do cache no formato autovia:sessao:{perfil}:{id}.</summary>
        private static string MontarChave(PerfilUsuario perfil, int idUsuario)
        {
            return $"{ConstantsValues.PrefixoCacheSessao}{perfil}:{idUsuario}";
        }

        /// <summary>Resultado guardado no cache: se a conta esta ativa e a versao atual da sessao (null quando encerrada).</summary>
        private sealed class SessaoCacheada
        {
            public SessaoCacheada(bool ativa, string? versaoSessao)
            {
                Ativa = ativa;
                VersaoSessao = versaoSessao;
            }

            public bool Ativa { get; }

            public string? VersaoSessao { get; }

            public static SessaoCacheada Encerrada()
            {
                return new SessaoCacheada(false, null);
            }
        }
    }
}
