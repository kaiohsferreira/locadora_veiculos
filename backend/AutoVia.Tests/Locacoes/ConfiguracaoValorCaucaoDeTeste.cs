using AutoVia.Helpers;
using System.Runtime.CompilerServices;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// Fixa Pagamentos:ValorCaucao em 0 (sem caucao) em todos os testes deste processo, pela variavel de ambiente da configuracao
    /// (a chave com dois tracos baixos no lugar dos dois pontos; molde ConfiguracaoRotinaAtrasoDeTeste). Assim os servidores em
    /// memoria nao dependem do appsettings.Local.json da maquina (que pode ter 1200.00 para a apresentacao). O teste que precisa
    /// de outro valor o define na configuracao em memoria da fabrica, que vence a variavel de ambiente.
    /// </summary>
    internal static class ConfiguracaoValorCaucaoDeTeste
    {
        private const string SeparadorNivelConfiguracao = ":";
        private const string SeparadorVariavelAmbiente = "__";
        private const string ValorSemCaucao = "0";

        [ModuleInitializer]
        internal static void FixarValorCaucaoNosTestes()
        {
            string nomeVariavel = ConstantsValues.ChaveValorCaucao.Replace(SeparadorNivelConfiguracao, SeparadorVariavelAmbiente);

            Environment.SetEnvironmentVariable(nomeVariavel, ValorSemCaucao);
        }
    }
}
