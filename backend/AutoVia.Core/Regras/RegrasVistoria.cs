using AutoVia.Communication.Enums;
using AutoVia.Helpers;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras da vistoria (ajustes 2 e 3 do plano de ajustes da semana 2, 20/11/2026). Sem banco, sem log e sem relogio:
    /// o servico chama estas funcoes dentro da trava da locacao, e os testes as conferem direto.
    /// </summary>
    public static class RegrasVistoria
    {
        /// <summary>
        /// Verdadeiro quando a locacao esta em aberto, pela fonte unica RegrasStatusVeiculo.StatusLocacoesEmAberto (EM_ANDAMENTO ou
        /// ATRASADA). Usado pela vistoria de entrada, pela vistoria de saida e pela apuracao das cobrancas extras.
        /// </summary>
        public static bool LocacaoAceitaVistoria(StatusLocacao status)
        {
            StatusLocacao[] statusAbertos = RegrasStatusVeiculo.StatusLocacoesEmAberto();

            bool aceita = statusAbertos.Contains(status);

            return aceita;
        }

        /// <summary>Verdadeiro quando o km da entrada nao fica abaixo do km de saida registrado na locacao. Nao ha teto superior.</summary>
        public static bool KmEntradaValido(int kmEntrada, int kmSaidaLocacao)
        {
            bool valido = kmEntrada >= kmSaidaLocacao;

            return valido;
        }

        /// <summary>
        /// Motivo da recusa de avaria numa vistoria, ou nulo quando a avaria e aceita. Ordem: 1) vistoria de SAIDA;
        /// 2) vistoria de ENTRADA com saida ativa na locacao; 3) locacao fora de EM_ANDAMENTO e ATRASADA.
        /// </summary>
        public static string? MotivoRecusaAvaria(TipoVistoria tipo, StatusLocacao statusLocacao, bool existeSaidaAtiva)
        {
            string? motivo = null;
            if (tipo == TipoVistoria.SAIDA)
            {
                motivo = ConstantsMessage.ErroAvariaVistoriaSaida;
            }
            else if (existeSaidaAtiva)
            {
                motivo = ConstantsMessage.ErroAvariaEntradaAposSaida;
            }
            else if (!LocacaoAceitaVistoria(statusLocacao))
            {
                motivo = ConstantsMessage.ErroAvariaLocacaoFechada;
            }

            return motivo;
        }
    }
}
