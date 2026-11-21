using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras de negocio da cobranca (RF-36 e RF-31): situacao calculada, dias para vencer, inicio do periodo e os predicados
    /// de cada aba, de cada total e da listagem. Os predicados sao expressoes que o EF traduz para SQL (no repositorio) e
    /// que os testes executam em memoria, assim a mesma regra e provada sem banco. Sem acesso a dados.
    /// </summary>
    public static class RegrasPagamento
    {
        /// <summary>
        /// Situacao mostrada ao cliente: PAGO, CANCELADO e ATRASADO ficam como estao; PENDENTE com vencimento anterior a hoje
        /// vira ATRASADO; PENDENTE com vencimento hoje ou depois continua PENDENTE.
        /// </summary>
        public static StatusPagamento Situacao(StatusPagamento status, DateOnly vencimento, DateOnly hoje)
        {
            if (status == StatusPagamento.PENDENTE && vencimento < hoje)
            {
                return StatusPagamento.ATRASADO;
            }

            return status;
        }

        /// <summary>
        /// Situacao mostrada ao cliente, com o tipo: CAUCAO nunca vira ATRASADO pela data (e bloqueio no cartao, nao divida),
        /// entao devolve o status gravado. Os demais tipos seguem a regra da situacao sem tipo.
        /// </summary>
        public static StatusPagamento Situacao(TipoPagamento tipo, StatusPagamento status, DateOnly vencimento, DateOnly hoje)
        {
            if (tipo == TipoPagamento.CAUCAO)
            {
                return status;
            }

            return Situacao(status, vencimento, hoje);
        }

        /// <summary>Dias do vencimento ate hoje: zero vence hoje, positivo ainda falta, negativo ja venceu.</summary>
        public static int DiasParaVencer(DateOnly vencimento, DateOnly hoje)
        {
            return vencimento.DayNumber - hoje.DayNumber;
        }

        /// <summary>
        /// Inicio do periodo pelo vencimento: DOZE_MESES e SEIS_MESES voltam a partir de hoje; ANO_ATUAL comeca em primeiro de
        /// janeiro do ano corrente; TUDO devolve nulo (sem limite). O periodo ja foi validado pelo validador.
        /// </summary>
        public static DateOnly? InicioDoPeriodo(string periodo, DateOnly hoje)
        {
            if (periodo == ConstantsValues.PeriodoDozeMeses)
            {
                return hoje.AddMonths(-ConstantsValues.MesesPeriodoDozeMeses);
            }

            if (periodo == ConstantsValues.PeriodoSeisMeses)
            {
                return hoje.AddMonths(-ConstantsValues.MesesPeriodoSeisMeses);
            }

            if (periodo == ConstantsValues.PeriodoAnoAtual)
            {
                return new DateOnly(hoje.Year, 1, 1);
            }

            return null;
        }

        /// <summary>Pendente: PENDENTE ou ATRASADO, sem caucao. Vale para a aba, a contagem e o total pendente, sem periodo.</summary>
        public static Expression<Func<Pagamento, bool>> PredicadoPendente()
        {
            return x => x.Tipo != TipoPagamento.CAUCAO
                && (x.Status == StatusPagamento.PENDENTE || x.Status == StatusPagamento.ATRASADO);
        }

        /// <summary>Pago no periodo: PAGO, sem caucao, com vencimento a partir do inicio do periodo (nulo = sem limite).</summary>
        public static Expression<Func<Pagamento, bool>> PredicadoPagoNoPeriodo(DateOnly? inicio)
        {
            bool semLimite = !inicio.HasValue;
            DateOnly limite = inicio ?? DateOnly.MinValue;

            return x => x.Status == StatusPagamento.PAGO
                && x.Tipo != TipoPagamento.CAUCAO
                && (semLimite || x.DataVencimento >= limite);
        }

        /// <summary>
        /// Predicado da aba. PENDENTES: so pendentes (o periodo nao corta). PAGOS: pagos no periodo. TODOS: pendentes sempre
        /// mais o resto dentro do periodo (pagos, cancelados e caucoes); como todo pendente ja entra, vale pendente OU
        /// vencimento a partir do inicio.
        /// </summary>
        public static Expression<Func<Pagamento, bool>> PredicadoDaAba(string aba, DateOnly? inicio)
        {
            if (aba == ConstantsValues.AbaPendentes)
            {
                return PredicadoPendente();
            }

            if (aba == ConstantsValues.AbaPagos)
            {
                return PredicadoPagoNoPeriodo(inicio);
            }

            bool semLimite = !inicio.HasValue;
            DateOnly limite = inicio ?? DateOnly.MinValue;

            return x => (x.Tipo != TipoPagamento.CAUCAO && (x.Status == StatusPagamento.PENDENTE || x.Status == StatusPagamento.ATRASADO))
                || semLimite
                || x.DataVencimento >= limite;
        }

        /// <summary>Quitado no ano: PAGO, sem caucao, com data de pagamento em [inicioAno, inicioProximoAno).</summary>
        public static Expression<Func<Pagamento, bool>> PredicadoQuitadoNoAno(DateOnly inicioAno, DateOnly inicioProximoAno)
        {
            return x => x.Status == StatusPagamento.PAGO
                && x.Tipo != TipoPagamento.CAUCAO
                && x.DataPagamento.HasValue
                && x.DataPagamento.Value >= inicioAno
                && x.DataPagamento.Value < inicioProximoAno;
        }

        /// <summary>
        /// Recusas da baixa, nesta ordem: CAUCAO (409, nao se baixa por esta rota), PAGO (409) e CANCELADO (409). Sem recusa,
        /// devolve nulo e o pagamento pode virar PAGO.
        /// </summary>
        public static string? ErroDaBaixa(TipoPagamento tipo, StatusPagamento status)
        {
            if (tipo == TipoPagamento.CAUCAO)
            {
                return ConstantsMessage.ErroPagamentoBaixaCaucao;
            }

            if (status == StatusPagamento.PAGO)
            {
                return ConstantsMessage.ErroPagamentoJaPago;
            }

            if (status == StatusPagamento.CANCELADO)
            {
                return ConstantsMessage.ErroPagamentoCancelado;
            }

            return null;
        }

        /// <summary>Listagem da locadora: status exato (se houver) e intervalo de vencimento (cada limite opcional, inclusive).</summary>
        public static Expression<Func<Pagamento, bool>> PredicadoDaListagem(StatusPagamento? status, DateOnly? de, DateOnly? ate)
        {
            bool semStatus = !status.HasValue;
            StatusPagamento valorStatus = status ?? StatusPagamento.PENDENTE;
            bool semDe = !de.HasValue;
            DateOnly valorDe = de ?? DateOnly.MinValue;
            bool semAte = !ate.HasValue;
            DateOnly valorAte = ate ?? DateOnly.MaxValue;

            return x => (semStatus || x.Status == valorStatus)
                && (semDe || x.DataVencimento >= valorDe)
                && (semAte || x.DataVencimento <= valorAte);
        }

        /// <summary>
        /// Recusa da geracao de PIX, nesta ordem: CAUCAO (tipo que nao se paga por PIX), PAGO e CANCELADO (409). Sem recusa,
        /// devolve nulo e a cobranca pode gerar (ou reabrir) o PIX.
        /// </summary>
        public static string? ErroDaGeracaoPix(TipoPagamento tipo, StatusPagamento status)
        {
            if (tipo == TipoPagamento.CAUCAO)
            {
                return ConstantsMessage.ErroPixTipoNaoPagavel;
            }

            if (status == StatusPagamento.PAGO)
            {
                return ConstantsMessage.ErroPixPagamentoJaPago;
            }

            if (status == StatusPagamento.CANCELADO)
            {
                return ConstantsMessage.ErroPixPagamentoCancelado;
            }

            return null;
        }

        /// <summary>Verdadeiro quando a cobranca pode gerar ou reabrir PIX (ver ErroDaGeracaoPix).</summary>
        public static bool PodeGerarPix(TipoPagamento tipo, StatusPagamento status)
        {
            return ErroDaGeracaoPix(tipo, status) == null;
        }

        /// <summary>
        /// Referencia enviada ao provedor (chave de idempotencia). Sem PIX gravado: PAG-{id}. Com PIX expirado: PAG-{id}-{validade
        /// anterior}. Mesma entrada, mesma saida: duas chamadas sobre o mesmo PIX expirado pedem a mesma cobranca.
        /// </summary>
        public static string ReferenciaDaCobranca(int idPagamento, DateTime? pixExpiraEmAnterior)
        {
            string referencia = ConstantsValues.PixPrefixoReferencia + idPagamento.ToString(CultureInfo.InvariantCulture);

            if (!pixExpiraEmAnterior.HasValue)
            {
                return referencia;
            }

            return referencia + "-" + pixExpiraEmAnterior.Value.ToString(ConstantsValues.PixFormatoSufixoReferencia, CultureInfo.InvariantCulture);
        }

        /// <summary>Segundos inteiros que faltam ate a validade, para baixo; zero quando ja passou.</summary>
        public static int SegundosParaExpirar(DateTime expiraEm, DateTime agora)
        {
            double restantes = Math.Floor((expiraEm - agora).TotalSeconds);

            if (restantes <= 0)
            {
                return 0;
            }

            return (int)restantes;
        }

        /// <summary>PIX gravado ainda valido: tem identificador no provedor e a validade e posterior a agora.</summary>
        public static bool PixAindaValido(string? idTransacao, DateTime? pixExpiraEm, DateTime agora)
        {
            bool temIdentificador = !string.IsNullOrWhiteSpace(idTransacao);

            return temIdentificador && pixExpiraEm.HasValue && pixExpiraEm.Value > agora;
        }

        /// <summary>
        /// Baixa manual com PIX gerado (P6): com id da transacao gravado, a forma diferente de PIX exige confirmacao explicita.
        /// A forma e comparada sem espacos e sem diferenca de maiusculas; PIX segue a baixa normal.
        /// </summary>
        public static bool ExigeConfirmacaoBaixaComPix(string? idTransacao, string forma, bool confirmar)
        {
            bool temPixGerado = !string.IsNullOrWhiteSpace(idTransacao);
            bool formaPix = string.Equals(forma.Trim(), ConstantsValues.FormaPix, StringComparison.OrdinalIgnoreCase);

            return temPixGerado && !formaPix && !confirmar;
        }

        /// <summary>
        /// Baixa manual que cancela o PIX no provedor antes da baixa (decisao 2 de 21/11/2026): forma diferente de PIX,
        /// confirmacao explicita, baixa permitida pelo tipo e status (ErroDaBaixa nulo) e PIX gravado ainda valido.
        /// Sem isso, o servico nao chama o provedor.
        /// </summary>
        public static bool PrecisaCancelarPixAntesDaBaixa(TipoPagamento tipo, StatusPagamento status, string? idTransacao, DateTime? pixExpiraEm, string forma, bool confirmar, DateTime agora)
        {
            return confirmar
                && !EhFormaPix(forma)
                && ErroDaBaixa(tipo, status) == null
                && PixAindaValido(idTransacao, pixExpiraEm, agora);
        }

        /// <summary>
        /// Reconferencia dentro da trava (decisao 2 de 21/11/2026): forma diferente de PIX e PIX gravado ainda valido com
        /// identificador diferente do cancelado (nulo quando nada foi cancelado). Verdadeiro recusa a baixa (409).
        /// </summary>
        public static bool PixValidoSemCancelamento(string? idTransacao, DateTime? pixExpiraEm, string forma, string? idTransacaoCancelado, DateTime agora)
        {
            return !EhFormaPix(forma)
                && PixAindaValido(idTransacao, pixExpiraEm, agora)
                && !string.Equals(idTransacao, idTransacaoCancelado, StringComparison.Ordinal);
        }

        private static bool EhFormaPix(string forma)
        {
            return string.Equals(forma.Trim(), ConstantsValues.FormaPix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Conferencia do retorno do provedor com o pedido: valor igual (decimal) e referencia igual (ordinal). Devolve o motivo
        /// (VALOR ou REFERENCIA) ou nulo quando bate. Divergencia nao grava nada.
        /// </summary>
        public static string? MotivoDivergenciaRetornoPix(decimal valorPedido, decimal valorRetornado, string referenciaPedida, string? referenciaRetornada)
        {
            if (valorPedido != valorRetornado)
            {
                return ConstantsValues.MotivoDivergenciaValor;
            }

            if (!string.Equals(referenciaPedida, referenciaRetornada, StringComparison.Ordinal))
            {
                return ConstantsValues.MotivoDivergenciaReferencia;
            }

            return null;
        }

        /// <summary>Retorno utilizavel para gravar: copia e cola (ate 512 caracteres), QR preenchido e validade futura.</summary>
        public static bool RetornoPixUtilizavel(string? copiaECola, string? qrCodeBase64, DateTime? expiraEm, DateTime agora)
        {
            bool temCopia = !string.IsNullOrWhiteSpace(copiaECola) && copiaECola.Length <= ConstantsValues.TamanhoMaximoPixCopiaECola;
            bool temQr = !string.IsNullOrWhiteSpace(qrCodeBase64);
            bool validadeFutura = expiraEm.HasValue && expiraEm.Value > agora;

            return temCopia && temQr && validadeFutura;
        }
    }
}
