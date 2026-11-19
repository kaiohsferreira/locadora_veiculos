namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores fixos do modulo de pagamento (cobranca, RF-31 e RF-36). Arquivo parcial: politica de acesso financeiro,
    /// tamanhos e precisao das colunas da tabela pagamento, formas aceitas na baixa, abas e periodos da tela do cliente.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Tamanho maximo da coluna pagamento.id_transacao (VARCHAR(40)): identificador da transacao no provedor.</summary>
        public const int TamanhoMaximoIdTransacaoPagamento = 40;

        /// <summary>Politica do financeiro: FUNCIONARIO da locadora do contexto com cargo FINANCEIRO ou ADMINISTRADOR (IContextoEmpresa.PodeVerValores).</summary>
        public const string PoliticaFinanceiro = "Financeiro";

        /// <summary>Precisao da coluna pagamento.valor (NUMERIC(12,2)).</summary>
        public const int PrecisaoValorPagamento = 12;

        /// <summary>Escala da coluna pagamento.valor (NUMERIC(12,2)): casas decimais.</summary>
        public const int EscalaValorPagamento = 2;

        /// <summary>Digitos inteiros do valor: precisao menos escala (10).</summary>
        public const int DigitosInteirosValorPagamento = PrecisaoValorPagamento - EscalaValorPagamento;

        /// <summary>Limite exclusivo da parte inteira do valor (10 elevado a 10): acima disso nao cabe em NUMERIC(12,2).</summary>
        public const long LimiteParteInteiraValorPagamento = 10000000000L;

        /// <summary>Tamanho maximo da coluna pagamento.forma (VARCHAR(20)).</summary>
        public const int TamanhoMaximoFormaPagamento = 20;

        /// <summary>Forma PIX (baixa).</summary>
        public const string FormaPix = "PIX";

        /// <summary>Forma cartao (baixa).</summary>
        public const string FormaCartao = "CARTAO";

        /// <summary>Forma boleto (baixa).</summary>
        public const string FormaBoleto = "BOLETO";

        /// <summary>Forma dinheiro (baixa).</summary>
        public const string FormaDinheiro = "DINHEIRO";

        /// <summary>Forma transferencia (baixa).</summary>
        public const string FormaTransferencia = "TRANSFERENCIA";

        /// <summary>Lista fechada e obrigatoria das formas aceitas na baixa.</summary>
        public static readonly string[] FormasPagamento = { FormaPix, FormaCartao, FormaBoleto, FormaDinheiro, FormaTransferencia };

        /// <summary>Aba TODOS da tela do cliente: pendentes sempre, o resto dentro do periodo.</summary>
        public const string AbaTodos = "TODOS";

        /// <summary>Aba PENDENTES da tela do cliente: pendentes e atrasadas, sem caucao.</summary>
        public const string AbaPendentes = "PENDENTES";

        /// <summary>Aba PAGOS da tela do cliente: pagos dentro do periodo, sem caucao.</summary>
        public const string AbaPagos = "PAGOS";

        /// <summary>Periodo DOZE_MESES (padrao da tela): vencimento a partir de doze meses atras.</summary>
        public const string PeriodoDozeMeses = "DOZE_MESES";

        /// <summary>Periodo ANO_ATUAL: vencimento a partir de primeiro de janeiro do ano corrente.</summary>
        public const string PeriodoAnoAtual = "ANO_ATUAL";

        /// <summary>Periodo SEIS_MESES: vencimento a partir de seis meses atras.</summary>
        public const string PeriodoSeisMeses = "SEIS_MESES";

        /// <summary>Periodo TUDO (Historico Completo): sem limite de inicio.</summary>
        public const string PeriodoTudo = "TUDO";

        /// <summary>Aba padrao quando o filtro nao vem.</summary>
        public const string AbaPadrao = AbaTodos;

        /// <summary>Periodo padrao quando o filtro nao vem (a primeira opcao do desenho).</summary>
        public const string PeriodoPadrao = PeriodoDozeMeses;

        /// <summary>Meses do periodo DOZE_MESES.</summary>
        public const int MesesPeriodoDozeMeses = 12;

        /// <summary>Meses do periodo SEIS_MESES.</summary>
        public const int MesesPeriodoSeisMeses = 6;
    }
}
