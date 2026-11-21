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

        /// <summary>Tamanho maximo da coluna pagamento.pix_copia_e_cola (VARCHAR(512)): codigo copia e cola do PIX.</summary>
        public const int TamanhoMaximoPixCopiaECola = 512;

        /// <summary>Prefixo da referencia da cobranca no provedor (chave de idempotencia): PAG-{id}.</summary>
        public const string PixPrefixoReferencia = "PAG-";

        /// <summary>Sufixo da referencia de recuperacao: PAG-{id}-{validade anterior no formato abaixo}.</summary>
        public const string PixFormatoSufixoReferencia = "yyyyMMddHHmmss";

        /// <summary>Validade do PIX mostrada na resposta (dd/MM/yyyy HH:mm:ss).</summary>
        public const string PixFormatoExpiraEm = "dd/MM/yyyy HH:mm:ss";

        /// <summary>Descricao enviada ao provedor (aparece no app do banco): {0} = id da cobranca.</summary>
        public const string PixDescricaoCobranca = "Cobrança {0} AutoVia";

        /// <summary>Politica de limite das geracoes de PIX (por IP, janela de um minuto).</summary>
        public const string PoliticaLimiteGeracaoPix = "AutoViaGeracaoPix";

        /// <summary>Chave de integration_setting ou de configuracao do limite de geracoes de PIX por minuto.</summary>
        public const string ChaveLimiteGeracaoPix = "Pagamentos:LimiteGeracaoPixPorMinuto";

        /// <summary>Padrao do limite de geracoes de PIX por minuto e por IP.</summary>
        public const int LimiteGeracaoPixPorMinutoPadrao = 10;

        /// <summary>Chave de configuracao do limite de geracoes de PIX por minuto e por cliente (identidade do token, nao IP).</summary>
        public const string ChaveLimiteGeracaoPixPorCliente = "Pagamentos:LimiteGeracaoPixPorClienteMinuto";

        /// <summary>Padrao do limite de geracoes de PIX por minuto e por cliente (aprovado pelo Kaio em 20/11/2026).</summary>
        public const int LimiteGeracaoPixPorClienteMinutoPadrao = 5;

        /// <summary>Janela, em minutos, do limite de geracoes de PIX por cliente.</summary>
        public const int JanelaEmMinutosLimiteGeracaoPixPorCliente = 1;

        /// <summary>Prefixo da chave de particao do limite por cliente: separa o cliente do funcionario que tenha o mesmo id.</summary>
        public const string PrefixoChaveLimiteGeracaoPixCliente = "cliente:";

        /// <summary>Prefixo da chave de particao do limite por cliente quando a rota e a da locadora (funcionario).</summary>
        public const string PrefixoChaveLimiteGeracaoPixFuncionario = "funcionario:";

        /// <summary>Motivo da divergencia do retorno do provedor: valor diferente do pedido (log).</summary>
        public const string MotivoDivergenciaValor = "VALOR";

        /// <summary>Motivo da divergencia do retorno do provedor: referencia diferente da enviada (log).</summary>
        public const string MotivoDivergenciaReferencia = "REFERENCIA";

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
