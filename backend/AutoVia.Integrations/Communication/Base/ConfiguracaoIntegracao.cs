using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;

namespace AutoVia.Integrations.Communication.Base
{
    /// <summary>Valor de um setting com a marcacao de protegido.</summary>
    public sealed record IntegrationSettingValue(string Valor, bool Protegido);

    /// <summary>
    /// Configuracao pronta de uma integracao, devolvida pelo IIntegrationResolver.
    /// Imutavel e segura para cache. ToString() nunca expoe valores.
    /// </summary>
    public sealed class ConfiguracaoIntegracao
    {
        private readonly IReadOnlyDictionary<string, IntegrationSettingValue> _settings;

        public ConfiguracaoIntegracao(
            string codigo,
            string nome,
            AmbienteIntegracao ambiente,
            string baseUrl,
            int timeoutMs,
            IReadOnlyDictionary<string, IntegrationSettingValue> settings)
        {
            Codigo = codigo;
            Nome = nome;
            Ambiente = ambiente;
            BaseUrl = baseUrl;
            TimeoutMs = timeoutMs;
            _settings = settings;
        }

        public string Codigo { get; }

        public string Nome { get; }

        public AmbienteIntegracao Ambiente { get; }

        public string BaseUrl { get; }

        public int TimeoutMs { get; }

        /// <summary>Nomes das chaves cadastradas (sem valores).</summary>
        public IReadOnlyCollection<string> Chaves => _settings.Keys.ToList();

        /// <summary>Valores das chaves protegidas, para mascarar logs (RequestInfo.SensitiveValues).</summary>
        public IReadOnlyList<string> ProtectedValues =>
            _settings.Values.Where(x => x.Protegido && !string.IsNullOrEmpty(x.Valor)).Select(x => x.Valor).ToList();

        /// <summary>
        /// Valor de uma chave obrigatoria. Lanca IntegrationException dizendo qual
        /// integracao e qual chave faltou, ou se ela ainda esta com DEFINIR_NO_BANCO.
        /// </summary>
        public string GetRequired(string chave)
        {
            IntegrationSettingValue? setting;
            if (!_settings.TryGetValue(chave, out setting) || string.IsNullOrWhiteSpace(setting.Valor))
            {
                throw new IntegrationException(
                    Codigo,
                    chave,
                    string.Format(ConstantsMessageIntegration.ErroChaveObrigatoriaAusente, Codigo, chave));
            }

            if (string.Equals(setting.Valor.Trim(), ConstantsValues.MarcadorValorNaoDefinido, StringComparison.OrdinalIgnoreCase))
            {
                throw new IntegrationException(
                    Codigo,
                    chave,
                    string.Format(ConstantsMessageIntegration.ErroChaveNaoConfigurada, Codigo, chave));
            }

            return setting.Valor;
        }

        /// <summary>Valor de uma chave opcional, ou null se nao existir.</summary>
        public string? GetOptional(string chave)
        {
            IntegrationSettingValue? setting;
            return _settings.TryGetValue(chave, out setting) ? setting.Valor : null;
        }

        /// <summary>Garante que todas as chaves obrigatorias existem e estao configuradas.</summary>
        public void EnsureRequiredKeys(params string[] chaves)
        {
            foreach (string chave in chaves)
            {
                GetRequired(chave);
            }
        }

        public bool IsProtected(string chave)
        {
            IntegrationSettingValue? setting;
            return _settings.TryGetValue(chave, out setting) && setting.Protegido;
        }

        /// <summary>Descricao para log: codigo, ambiente, URL e nomes das chaves. Nunca os valores.</summary>
        public override string ToString()
        {
            IEnumerable<string> keys = _settings.Select(x => x.Value.Protegido ? $"{x.Key} (protegida)" : x.Key);
            return $"{Codigo} [{Ambiente}] {BaseUrl} | chaves: {string.Join(", ", keys)}";
        }
    }
}
