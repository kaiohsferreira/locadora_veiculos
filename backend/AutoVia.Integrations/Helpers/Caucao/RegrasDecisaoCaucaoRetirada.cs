using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Caucao;

namespace AutoVia.Integrations.Helpers.Caucao
{
    /// <summary>
    /// Decisao da abertura da locacao sobre o estado da caucao no provedor (contrato caucao-retirada v1.1, correcao c). So
    /// RESERVADA abre. EM_ANALISE nao abre e o id novo e cancelado antes de recusar. RECUSADA, CAPTURADA, CANCELADA, EXPIRADA e
    /// ESTORNADA nao abrem e nao sao cancelados. Vale para o estado devolvido pela pre-autorizacao e para o confirmado pela
    /// consulta. Fica nas Integrations porque o Core nao enxerga CaucaoStatus (molde RegrasProvedorFakePix). Sem I/O.
    /// </summary>
    public static class RegrasDecisaoCaucaoRetirada
    {
        /// <summary>Verdadeiro so para RESERVADA, o unico estado que abre a locacao. EM_ANALISE ficaria igual a RESERVADA no banco (PENDENTE) e nao abre.</summary>
        public static bool PermiteAbrir(CaucaoStatus status)
        {
            bool permite = status == CaucaoStatus.RESERVADA;

            return permite;
        }

        /// <summary>Verdadeiro so para EM_ANALISE: o id e conhecido e a reserva ainda pode valer, entao o servidor cancela o id novo antes de recusar. Os estados encerrados nao sao cancelados.</summary>
        public static bool CancelarAntesDeRecusar(CaucaoStatus status)
        {
            bool cancelar = status == CaucaoStatus.EM_ANALISE;

            return cancelar;
        }

        /// <summary>
        /// Mensagem do 409 de um estado que nao abre: ErroLocacaoCaucaoEmAnalise para EM_ANALISE e ErroLocacaoCaucaoNovaTentativa
        /// para os demais (a tentativa nao vale mais; o front faz outra, com token novo). Chamada so quando PermiteAbrir e falso.
        /// </summary>
        public static string MensagemDaRecusa(CaucaoStatus status)
        {
            if (status == CaucaoStatus.EM_ANALISE)
            {
                return ConstantsMessage.ErroLocacaoCaucaoEmAnalise;
            }

            return ConstantsMessage.ErroLocacaoCaucaoNovaTentativa;
        }
    }
}
