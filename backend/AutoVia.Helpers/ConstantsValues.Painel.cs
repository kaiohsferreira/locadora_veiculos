namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores da visao geral da locadora (multi-locadora, P11-BE-01 e P11-BE-02): tamanho dos periodos curtos e medios
    /// (em dias, incluindo hoje) e casas decimais dos percentuais. O periodo padrao (TRINTA_DIAS) e o membro do enum
    /// PeriodoVisaoGeral, definido no FiltroVisaoGeralVO, porque Helpers nao referencia o projeto Communication.
    /// PrimeiroDiaDoMes e MesesDaJanelaMensal definem a janela de veiculos novos no mes (ObterVisaoGeralAsync) e o dia
    /// inicial do periodo MES_ATUAL (CalcularJanela).
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Dias do periodo SETE_DIAS, incluindo hoje.</summary>
        public const int DiasPeriodoCurto = 7;

        /// <summary>Dias do periodo TRINTA_DIAS, incluindo hoje.</summary>
        public const int DiasPeriodoMedio = 30;

        /// <summary>Casas decimais de taxa de ocupacao e variacao de receita.</summary>
        public const int CasasDecimaisPercentualVisaoGeral = 1;

        /// <summary>Dia do mes em que comeca a janela de veiculos novos no mes (primeiro dia, a partir de 00:00).</summary>
        public const int PrimeiroDiaDoMes = 1;

        /// <summary>Meses de duracao da janela de veiculos novos no mes (um mes, ate o dia 1 do mes seguinte, excluido).</summary>
        public const int MesesDaJanelaMensal = 1;

        /// <summary>Dias maximos do periodo do painel financeiro: diferenca em dias entre a data final e a inicial (366 vale, 367 nao).</summary>
        public const int DiasLimitePeriodoFinanceiro = 366;

        /// <summary>Dias da aba PAGOS_RECENTES, incluindo hoje (hoje menos 29 ate hoje).</summary>
        public const int DiasPagosRecentes = 30;

        /// <summary>Casas decimais do ticket medio do painel financeiro (arredondamento AwayFromZero, em C#).</summary>
        public const int CasasDecimaisTicketMedio = 2;

        /// <summary>Digitos de um CPF sem pontuacao.</summary>
        public const int TamanhoCpfDigitos = 11;

        /// <summary>Digitos iniciais do CPF mascarado (desenho da tela 09: CPF: 194.***.***-12).</summary>
        public const int DigitosCpfInicioMascara = 3;

        /// <summary>Digitos finais do CPF mascarado.</summary>
        public const int DigitosCpfFimMascara = 2;

        /// <summary>Mascara do CPF: {0} = primeiros digitos, {1} = ultimos digitos.</summary>
        public const string FormatoCpfMascarado = "{0}.***.***-{1}";

        /// <summary>Aba TODOS de GET /api/painel/financeiro/titulos (padrao).</summary>
        public const string AbaTitulosTodos = "TODOS";

        /// <summary>Aba EM_ATRASO: a mesma populacao do indicador em atraso.</summary>
        public const string AbaTitulosEmAtraso = "EM_ATRASO";

        /// <summary>Aba VENCE_HOJE: pendentes com vencimento hoje (so se hoje estiver no periodo).</summary>
        public const string AbaTitulosVenceHoje = "VENCE_HOJE";

        /// <summary>Aba PAGOS_RECENTES: pagos nos ultimos DiasPagosRecentes dias; ignora o periodo.</summary>
        public const string AbaTitulosPagosRecentes = "PAGOS_RECENTES";

        /// <summary>Abas aceitas pela lista de titulos; outro valor vira 400.</summary>
        public static readonly string[] AbasTitulosFinanceiros = { AbaTitulosTodos, AbaTitulosEmAtraso, AbaTitulosVenceHoje, AbaTitulosPagosRecentes };

        /// <summary>Origem do titulo quando a cobranca vem de uma locacao (texto da resposta).</summary>
        public const string OrigemTituloLocacao = "LOCACAO";

        /// <summary>Origem do titulo quando a cobranca vem de uma venda (texto da resposta).</summary>
        public const string OrigemTituloVenda = "VENDA";
    }
}
