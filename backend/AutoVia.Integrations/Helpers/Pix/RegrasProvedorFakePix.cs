using AutoVia.Communication.Enums;
using Microsoft.Extensions.Configuration;

namespace AutoVia.Integrations.Helpers.Pix
{
    /// <summary>
    /// Regra unica que libera o provedor de teste (FAKE) do PIX (decisao 1 de 21/11/2026, 'Bloqueado, com chave'). So responde
    /// quando as tres condicoes valem juntas: integracao PAGAMENTO em SANDBOX, ambiente da API Development e a chave
    /// Pagamentos:PermitirProvedorFake verdadeira. Usada somente por FakePixProvider.ConferirTravaDupla.
    /// </summary>
    public static class RegrasProvedorFakePix
    {
        /// <summary>Verdadeiro so com as tres condicoes: Development, SANDBOX e chave verdadeira.</summary>
        public static bool PodeResponder(bool ehDesenvolvimento, AmbienteIntegracao ambiente, bool chavePermitida)
        {
            return chavePermitida && ehDesenvolvimento && ambiente == AmbienteIntegracao.SANDBOX;
        }

        /// <summary>Le a chave Pagamentos:PermitirProvedorFake. Ausente, vazia ou que nao e booleana vale falso.</summary>
        public static bool LerChave(IConfiguration configuracao)
        {
            string? valor = configuracao[ConstantsPix.ChavePermitirProvedorFake];
            bool permitido;

            if (bool.TryParse(valor, out permitido))
            {
                return permitido;
            }

            return ConstantsPix.PermitirProvedorFakePadrao;
        }
    }
}
