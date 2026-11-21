using AutoVia.Helpers;
using System.Runtime.CompilerServices;

namespace AutoVia.Tests.Cobranca
{
    /// <summary>
    /// Desliga a rotina de atraso (contrato pagamento-atraso v1) em todos os testes deste processo, pela variavel de ambiente
    /// da configuracao (a chave com dois tracos baixos no lugar dos dois pontos). Assim os servidores em memoria nao falam com
    /// o banco pela rotina ao subir. Os testes da rotina constroem o servico direto, com a configuracao de cada teste.
    /// </summary>
    internal static class ConfiguracaoRotinaAtrasoDeTeste
    {
        private const string SeparadorNivelConfiguracao = ":";
        private const string SeparadorVariavelAmbiente = "__";

        [ModuleInitializer]
        internal static void DesligarRotinaAtrasoNosTestes()
        {
            string nomeVariavel = ConstantsValues.ChaveRotinaAtrasoPagamentoAtiva.Replace(SeparadorNivelConfiguracao, SeparadorVariavelAmbiente);

            Environment.SetEnvironmentVariable(nomeVariavel, bool.FalseString);
        }
    }
}
