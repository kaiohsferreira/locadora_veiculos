namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores da caucao na retirada (P4-BE-04, RF-20; contrato caucao-retirada v1.1): chave, formato e faixa do valor da
    /// caucao na configuracao do servidor, referencia enviada ao provedor, descricao fixa, formato dos campos do cartao na
    /// entrada (espelho de ConstantsCaucao, que o validador nao enxerga) e o teto de tentativas por funcionario. Arquivo
    /// parcial de ConstantsValues.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>
        /// Chave do valor da caucao na configuracao do servidor (variavel de ambiente Pagamentos__ValorCaucao). Igual para todas
        /// as locadoras. Ausente, vazia ou 0 = sem caucao. Lida uma vez, na subida da API; valor invalido impede a subida.
        /// </summary>
        public const string ChaveValorCaucao = "Pagamentos:ValorCaucao";

        /// <summary>
        /// Formato aceito do valor da caucao (texto aparado): so digitos, de 1 a 8 inteiros, ponto como separador decimal e no
        /// maximo 2 casas. Sem sinal, sem separador de milhar, sem virgula, sem expoente e sem espaco no meio. O \z nao aceita
        /// quebra de linha no fim.
        /// </summary>
        public const string PadraoValorCaucao = @"^[0-9]{1,8}(\.[0-9]{1,2})?\z";

        /// <summary>Maior valor de caucao aceito: 99999999.99, que cabe em locacao.valor_caucao NUMERIC(10,2) e em pagamento.valor NUMERIC(12,2).</summary>
        public const decimal ValorCaucaoMaximo = 99999999.99m;

        /// <summary>Prefixo da referencia da caucao no provedor (external_reference): CAUCAO-RES-{idReserva}.</summary>
        public const string CaucaoPrefixoReferencia = "CAUCAO-RES-";

        /// <summary>Descricao fixa enviada ao provedor na pre-autorizacao (com cartao real aparece para o cliente no app do banco). Ate 140 caracteres.</summary>
        public const string CaucaoDescricaoRetirada = "Caução da locação AutoVia";

        // --- Campos do cartao no LocacaoVO (mesmos valores de ConstantsCaucao; um teste compara um a um) ---

        /// <summary>Menor tamanho do token do cartao (igual a ConstantsCaucao.TokenTamanhoMinimo).</summary>
        public const int CaucaoTokenTamanhoMinimo = 8;

        /// <summary>Maior tamanho do token do cartao (igual a ConstantsCaucao.TokenTamanhoMaximo).</summary>
        public const int CaucaoTokenTamanhoMaximo = 128;

        /// <summary>Formato do token do cartao: letras, numeros, '-' e '_' (igual a ConstantsCaucao.PadraoToken).</summary>
        public const string CaucaoPadraoToken = @"^[A-Za-z0-9_-]+\z";

        /// <summary>Menor tamanho da bandeira (metodoPagamentoId) (igual a ConstantsCaucao.MetodoPagamentoTamanhoMinimo).</summary>
        public const int CaucaoMetodoPagamentoTamanhoMinimo = 2;

        /// <summary>Maior tamanho da bandeira (metodoPagamentoId) (igual a ConstantsCaucao.MetodoPagamentoTamanhoMaximo).</summary>
        public const int CaucaoMetodoPagamentoTamanhoMaximo = 30;

        /// <summary>Formato da bandeira: minusculas, numeros e '_' (igual a ConstantsCaucao.PadraoMetodoPagamento).</summary>
        public const string CaucaoPadraoMetodoPagamento = @"^[a-z0-9_]+\z";

        /// <summary>Maior emissor (issuer_id) aceito na entrada: 2147483647, ate 10 digitos (um numero de cartao inteiro nao cabe).</summary>
        public const long CaucaoEmissorIdMaximo = int.MaxValue;

        /// <summary>Formato do device id do formulario de cartao, quando vier (igual a ConstantsCaucao.PadraoDispositivo).</summary>
        public const string CaucaoPadraoDispositivo = @"^[A-Za-z0-9_.:\-]{8,128}\z";

        /// <summary>Separadores retirados antes de conferir se o texto parece numero de cartao: '-', '_', '.', ':' e espaco.</summary>
        public const string CaucaoSeparadoresNumeroCartao = "-_.: ";

        /// <summary>Menor quantidade de digitos que, sozinhos, parecem numero de cartao.</summary>
        public const int CaucaoDigitosCartaoMinimo = 12;

        /// <summary>Maior quantidade de digitos que, sozinhos, parecem numero de cartao.</summary>
        public const int CaucaoDigitosCartaoMaximo = 19;

        /// <summary>Motivo de log quando o identificador devolvido pelo provedor veio vazio ou maior que a coluna id_transacao.</summary>
        public const string MotivoDivergenciaIdentificador = "IDENTIFICADOR";

        // --- Teto de tentativas por funcionario (so com a caucao exigida) ---

        /// <summary>Chave de configuracao do teto de aberturas com caucao por minuto e por funcionario (identidade do token).</summary>
        public const string ChaveLimiteCaucaoPorFuncionario = "Pagamentos:LimiteCaucaoPorFuncionarioMinuto";

        /// <summary>Padrao do teto de aberturas com caucao por minuto e por funcionario (ausente, nao inteiro, zero ou negativo cai aqui).</summary>
        public const int LimiteCaucaoPorFuncionarioMinutoPadrao = 5;

        /// <summary>Janela, em minutos, do teto de aberturas com caucao por funcionario.</summary>
        public const int JanelaEmMinutosLimiteCaucaoPorFuncionario = 1;

        /// <summary>Prefixo da chave de particao do teto por funcionario (funcionario: + id da claim).</summary>
        public const string PrefixoChaveLimiteCaucaoFuncionario = "funcionario:";
    }
}
