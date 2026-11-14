using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
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
    /// token precisa estar ativa (cliente sem DisabledAt; funcionario sem DisabledAt, com Ativo e
    /// com a empresa ativa, v3) e a versao da sessao (sv) precisa ser a da senha_hash atual. O
    /// resultado, inclusive o negativo, fica em IMemoryCache por usuario durante
    /// Autenticacao:Sessao:CacheSegundos, junto com a empresa e o cargo do banco.
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
                SessaoCacheada situacao = await ObterCacheadaAsync(perfil, idUsuario, chave);

                bool valida = situacao.Ativa && string.Equals(situacao.VersaoSessao, versaoSessao, StringComparison.Ordinal);

                return valida;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroValidarSessao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Situacao da conta (empresa, cargo, empresa ativa e operadora) do mesmo cache de SessaoValidaAsync.</summary>
        public async Task<SessaoSituacaoVO> ObterSituacaoAsync(PerfilUsuario perfil, int idUsuario)
        {
            try
            {
                string chave = MontarChave(perfil, idUsuario);
                SessaoCacheada cacheada = await ObterCacheadaAsync(perfil, idUsuario, chave);

                SessaoSituacaoVO situacao = new SessaoSituacaoVO
                {
                    Ativo = cacheada.Ativa,
                    IdEmpresa = cacheada.IdEmpresa,
                    Cargo = cacheada.Cargo,
                    EmpresaAtiva = cacheada.EmpresaAtiva,
                    EmpresaOperadora = cacheada.EmpresaOperadora
                };

                return situacao;
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
        private async Task<SessaoCacheada> ObterCacheadaAsync(PerfilUsuario perfil, int idUsuario, string chave)
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

        /// <summary>
        /// Consulta a conta e calcula a versao atual da sessao. Conta inexistente, desativada ou sem senha
        /// vira situacao encerrada. Funcionario: ativo so com a empresa ativa (v3); a empresa vem do banco.
        /// </summary>
        private async Task<SessaoCacheada> CarregarSituacaoAsync(PerfilUsuario perfil, int idUsuario)
        {
            if (perfil == PerfilUsuario.CLIENTE)
            {
                Cliente? cliente = await _autenticacaoRepository.FindClienteByIdAsync(idUsuario);

                if (cliente == null || string.IsNullOrWhiteSpace(cliente.SenhaHash))
                {
                    return SessaoCacheada.Encerrada();
                }

                return new SessaoCacheada(true, _tokenService.CalcularVersaoSessao(cliente.SenhaHash), null, null, false, false);
            }

            if (perfil == PerfilUsuario.FUNCIONARIO)
            {
                Funcionario? funcionario = await _autenticacaoRepository.FindFuncionarioByIdAsync(idUsuario);

                if (funcionario == null || string.IsNullOrWhiteSpace(funcionario.SenhaHash))
                {
                    return SessaoCacheada.Encerrada();
                }

                Empresa? empresa = funcionario.Empresa;
                bool empresaAtiva = empresa != null && empresa.Ativo && empresa.DisabledAt == null;
                bool empresaOperadora = empresa != null && empresa.Operadora;
                bool ativa = funcionario.Ativo && empresaAtiva;
                string? versaoSessao = _tokenService.CalcularVersaoSessao(funcionario.SenhaHash);

                return new SessaoCacheada(ativa, versaoSessao, funcionario.IdEmpresa, funcionario.Cargo, empresaAtiva, empresaOperadora);
            }

            return SessaoCacheada.Encerrada();
        }

        /// <summary>Chave do cache no formato autovia:sessao:{perfil}:{id}.</summary>
        private static string MontarChave(PerfilUsuario perfil, int idUsuario)
        {
            return $"{ConstantsValues.PrefixoCacheSessao}{perfil}:{idUsuario}";
        }

        /// <summary>Resultado guardado no cache: se a conta esta ativa, a versao atual da sessao (null quando encerrada), a empresa e o cargo do banco.</summary>
        private sealed class SessaoCacheada
        {
            public SessaoCacheada(bool ativa, string? versaoSessao, int? idEmpresa, CargoFuncionario? cargo, bool empresaAtiva, bool empresaOperadora)
            {
                Ativa = ativa;
                VersaoSessao = versaoSessao;
                IdEmpresa = idEmpresa;
                Cargo = cargo;
                EmpresaAtiva = empresaAtiva;
                EmpresaOperadora = empresaOperadora;
            }

            public bool Ativa { get; }

            public string? VersaoSessao { get; }

            public int? IdEmpresa { get; }

            public CargoFuncionario? Cargo { get; }

            public bool EmpresaAtiva { get; }

            public bool EmpresaOperadora { get; }

            public static SessaoCacheada Encerrada()
            {
                return new SessaoCacheada(false, null, null, null, false, false);
            }
        }
    }
}
