using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Caucao;

namespace AutoVia.Integrations.Helpers.Caucao
{
    /// <summary>Acao que a tabela manda fazer no provedor antes da decisao final.</summary>
    public enum AcaoProvedorCaucao
    {
        NENHUMA,
        CAPTURAR,
        CANCELAR
    }

    /// <summary>
    /// Regra pura da caucao no encerramento (caucao-devolucao v4, tabela C1 a C14). Sem banco, sem provedor, sem log e sem relogio.
    /// R = valor reservado (locacao.valor_caucao) e T = total das extras. Quem chama executa a acao no provedor e volta a decidir
    /// com o estado resultante. Estado RESERVADA aqui e defensivo: nao e um resultado final.
    /// </summary>
    public static class RegrasDecisaoCaucaoDevolucao
    {
        /// <summary>
        /// Acao no provedor para um estado RESERVADA: captura T quando 0 &lt; T &lt;= R (C2 e C3), senao cancela (C1 e C4). Outros
        /// estados nao pedem acao.
        /// </summary>
        public static AcaoProvedorCaucao AcaoNoProvedor(CaucaoStatus? status, decimal valorReservado, decimal totalExtras)
        {
            AcaoProvedorCaucao acao = AcaoProvedorCaucao.NENHUMA;

            if (status == CaucaoStatus.RESERVADA)
            {
                acao = totalExtras > 0m && totalExtras <= valorReservado ? AcaoProvedorCaucao.CAPTURAR : AcaoProvedorCaucao.CANCELAR;
            }

            return acao;
        }

        /// <summary>Valor da captura parcial ou integral: T (com T igual a R, a captura integral e o proprio R).</summary>
        public static decimal ValorDaCaptura(decimal totalExtras)
        {
            decimal valor = decimal.Round(totalExtras, 2);

            return valor;
        }

        /// <summary>
        /// Decisao pelo estado do provedor (tabela C1 a C14). Nulo = NotFound (C12). RESERVADA nao e resultado final: devolve nao
        /// confirmada (C14). Nunca chama o provedor.
        /// </summary>
        public static DecisaoCaucaoEncerramento Decidir(CaucaoStatus? status, decimal? valorCapturado, decimal valorReservado, decimal totalExtras)
        {
            DecisaoCaucaoEncerramento decisao = new DecisaoCaucaoEncerramento();

            if (status == null)
            {
                MarcarNaoConfirmada(decisao, ConstantsMessage.AvisoDevolucaoCaucaoNaoEncontrada);
                return decisao;
            }

            switch (status.Value)
            {
                case CaucaoStatus.CAPTURADA:
                    DecidirCapturada(decisao, valorCapturado, valorReservado, totalExtras);
                    break;
                case CaucaoStatus.CANCELADA:
                    MarcarBaixada(decisao, valorReservado, null);
                    break;
                case CaucaoStatus.EXPIRADA:
                case CaucaoStatus.RECUSADA:
                    MarcarBaixada(decisao, valorReservado, ConstantsMessage.AvisoDevolucaoCaucaoSemGarantia);
                    break;
                case CaucaoStatus.ESTORNADA:
                    MarcarBaixada(decisao, valorReservado, ConstantsMessage.AvisoDevolucaoCaucaoSemGarantia);
                    decisao.DataHojeNaCaucao = false;
                    break;
                case CaucaoStatus.EM_ANALISE:
                    MarcarNaoConfirmada(decisao, ConstantsMessage.AvisoDevolucaoCaucaoEmAnalise);
                    break;
                default:
                    MarcarNaoConfirmada(decisao, ConstantsMessage.AvisoDevolucaoCaucaoDivergente);
                    break;
            }

            return decisao;
        }

        /// <summary>
        /// Aviso extra da acao no provedor: cancelar com T maior que R (C4) avisa que as extras ficaram para PIX. Nulo nos demais.
        /// </summary>
        public static string? AvisoDaAcao(AcaoProvedorCaucao acao, decimal valorReservado, decimal totalExtras)
        {
            string? aviso = null;

            if (acao == AcaoProvedorCaucao.CANCELAR && totalExtras > 0m && totalExtras > valorReservado)
            {
                aviso = ConstantsMessage.AvisoDevolucaoExtrasAcimaDaCaucao;
            }

            return aviso;
        }

        /// <summary>Estado CAPTURADA: C5 quando o valor capturado e T (retencao); C6 quando difere (nao confirmada, encerra).</summary>
        private static void DecidirCapturada(DecisaoCaucaoEncerramento decisao, decimal? valorCapturado, decimal valorReservado, decimal totalExtras)
        {
            if (valorCapturado.HasValue && totalExtras > 0m && valorCapturado.Value == totalExtras)
            {
                decimal retido = decimal.Round(valorCapturado.Value, 2);
                decisao.Acao = retido >= valorReservado ? AcaoCaucaoDevolucao.RETIDA : AcaoCaucaoDevolucao.RETIDA_PARCIAL;
                decisao.NovoStatusCaucao = StatusPagamento.PAGO;
                decisao.ValorCaucaoGravado = retido;
                decisao.DataHojeNaCaucao = true;
                decisao.QuitarAlvo = true;
                decisao.ValorRetido = retido;
                decisao.ValorLiberado = decimal.Round(valorReservado - retido, 2);
                return;
            }

            MarcarNaoConfirmada(decisao, ConstantsMessage.AvisoDevolucaoCaucaoDivergente);
        }

        /// <summary>Baixa: a caucao vira CANCELADO com o valor reservado; nada e retido e tudo e liberado.</summary>
        private static void MarcarBaixada(DecisaoCaucaoEncerramento decisao, decimal valorReservado, string? aviso)
        {
            decisao.Acao = AcaoCaucaoDevolucao.BAIXADA;
            decisao.NovoStatusCaucao = StatusPagamento.CANCELADO;
            decisao.ValorCaucaoGravado = valorReservado;
            decisao.DataHojeNaCaucao = false;
            decisao.ValorRetido = 0m;
            decisao.ValorLiberado = valorReservado;
            decisao.MensagemAviso = aviso;
        }

        /// <summary>Nao confirmada: a caucao fica PENDENTE para tratar a mao; nada e retido nem liberado na resposta.</summary>
        private static void MarcarNaoConfirmada(DecisaoCaucaoEncerramento decisao, string aviso)
        {
            decisao.Acao = AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR;
            decisao.NovoStatusCaucao = null;
            decisao.ValorCaucaoGravado = null;
            decisao.DataHojeNaCaucao = false;
            decisao.QuitarAlvo = false;
            decisao.TratarAMao = true;
            decisao.ValorRetido = 0m;
            decisao.ValorLiberado = 0m;
            decisao.MensagemAviso = aviso;
        }
    }
}
