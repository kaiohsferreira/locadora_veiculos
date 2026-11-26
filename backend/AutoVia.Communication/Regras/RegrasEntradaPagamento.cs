using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using System;
using System.Linq;

namespace AutoVia.Communication.Regras
{
    /// <summary>
    /// Regras puras da entrada do pagamento (RF-31 e RF-36): origem unica (locacao ou venda), tipo x origem, valor com
    /// precisao NUMERIC(12,2), forma da baixa, recusas da baixa, e as listas fechadas dos filtros. Usadas pelos validadores
    /// (rotas HTTP) e pelo servico (chamadas diretas, como a da caucao). Sem acesso a dados e sem excecao.
    /// </summary>
    public static class RegrasEntradaPagamento
    {
        /// <summary>
        /// N4 (26/11/2026): a rota manual recusa DIARIA e COBRANCA_EXTRA quando vem com locacao (idLocacao maior que zero). A
        /// apuracao e o encerramento lancam por outro caminho e nao passam por aqui.
        /// </summary>
        public static bool TipoRecusadoNaRotaManual(string? tipo, int? idLocacao)
        {
            bool temLocacao = idLocacao.HasValue && idLocacao.Value > 0;
            string? nome = tipo?.Trim();
            bool recusado = temLocacao && (nome == nameof(TipoPagamento.DIARIA) || nome == nameof(TipoPagamento.COBRANCA_EXTRA));

            return recusado;
        }

        /// <summary>Verdadeiro quando o texto, sem as pontas, e CAUCAO (a caucao nao entra pela rota de lancamento).</summary>
        public static bool EhCaucao(string? texto)
        {
            return texto != null && texto.Trim() == nameof(TipoPagamento.CAUCAO);
        }

        /// <summary>Le o tipo pelo nome exato (depois de Trim). Aceita os quatro membros, inclusive CAUCAO. Numero e minuscula recusados.</summary>
        public static bool TentarLerTipo(string? texto, out TipoPagamento tipo)
        {
            tipo = default;

            if (texto == null)
            {
                return false;
            }

            string limpo = texto.Trim();

            if (!Enum.GetNames<TipoPagamento>().Contains(limpo, StringComparer.Ordinal))
            {
                return false;
            }

            tipo = Enum.Parse<TipoPagamento>(limpo, false);

            return true;
        }

        /// <summary>Le o status pelo nome exato (PENDENTE, PAGO, ATRASADO ou CANCELADO), sem Trim, como o filtro da listagem.</summary>
        public static bool TentarLerStatus(string? texto, out StatusPagamento status)
        {
            status = default;

            if (texto == null || !Enum.GetNames<StatusPagamento>().Contains(texto, StringComparer.Ordinal))
            {
                return false;
            }

            status = Enum.Parse<StatusPagamento>(texto, false);

            return true;
        }

        /// <summary>
        /// Origem unica: exatamente um entre locacao e venda, com id positivo. Os dois, nenhum ou um id nao positivo devolvem
        /// a mensagem da recusa; sem recusa, devolve nulo.
        /// </summary>
        public static string? ErroDeOrigem(int? idLocacao, int? idVenda)
        {
            if ((idLocacao.HasValue && idLocacao.Value <= 0) || (idVenda.HasValue && idVenda.Value <= 0))
            {
                return ConstantsMessage.ErroPagamentoOrigemObrigatoria;
            }

            if (idLocacao.HasValue && idVenda.HasValue)
            {
                return ConstantsMessage.ErroPagamentoOrigemDupla;
            }

            if (!idLocacao.HasValue && !idVenda.HasValue)
            {
                return ConstantsMessage.ErroPagamentoOrigemObrigatoria;
            }

            return null;
        }

        /// <summary>
        /// Tipo x origem: DIARIA e COBRANCA_EXTRA so com locacao; VENDA so com venda; CAUCAO so com locacao. Fora disso
        /// devolve a mensagem da recusa; sem recusa, devolve nulo.
        /// </summary>
        public static string? ErroDeTipoPorOrigem(TipoPagamento tipo, bool temLocacao, bool temVenda)
        {
            if (tipo == TipoPagamento.VENDA)
            {
                return temVenda && !temLocacao ? null : ConstantsMessage.ErroPagamentoTipoExigeVenda;
            }

            if (tipo == TipoPagamento.CAUCAO)
            {
                return temLocacao && !temVenda ? null : ConstantsMessage.ErroPagamentoCaucaoExigeLocacao;
            }

            return temLocacao && !temVenda ? null : ConstantsMessage.ErroPagamentoTipoExigeLocacao;
        }

        /// <summary>
        /// Valor: maior que zero, com no maximo DigitosInteirosValorPagamento digitos inteiros e EscalaValorPagamento casas
        /// decimais (NUMERIC(12,2)). Devolve a mensagem da recusa ou nulo.
        /// </summary>
        public static string? ErroDeValor(decimal valor)
        {
            if (valor <= 0m)
            {
                return ConstantsMessage.ErroPagamentoValorInvalido;
            }

            bool escalaInvalida = valor != decimal.Round(valor, ConstantsValues.EscalaValorPagamento);
            bool parteInteiraGrande = decimal.Truncate(valor) >= ConstantsValues.LimiteParteInteiraValorPagamento;

            if (escalaInvalida || parteInteiraGrande)
            {
                return string.Format(ConstantsMessage.ErroPagamentoValorPrecisao, ConstantsValues.DigitosInteirosValorPagamento, ConstantsValues.EscalaValorPagamento);
            }

            return null;
        }

        /// <summary>Forma da baixa: nome exato da lista fechada, depois de Trim.</summary>
        public static bool FormaValida(string? texto)
        {
            return texto != null && ConstantsValues.FormasPagamento.Contains(texto.Trim(), StringComparer.Ordinal);
        }

        /// <summary>Aba da tela do cliente: TODOS, PENDENTES ou PAGOS, pelo nome exato.</summary>
        public static bool EhAbaValida(string? texto)
        {
            string[] abas = { ConstantsValues.AbaTodos, ConstantsValues.AbaPendentes, ConstantsValues.AbaPagos };

            return texto != null && abas.Contains(texto, StringComparer.Ordinal);
        }

        /// <summary>Periodo da tela do cliente: DOZE_MESES, ANO_ATUAL, SEIS_MESES ou TUDO, pelo nome exato.</summary>
        public static bool EhPeriodoValido(string? texto)
        {
            string[] periodos = { ConstantsValues.PeriodoDozeMeses, ConstantsValues.PeriodoAnoAtual, ConstantsValues.PeriodoSeisMeses, ConstantsValues.PeriodoTudo };

            return texto != null && periodos.Contains(texto, StringComparer.Ordinal);
        }
    }
}
