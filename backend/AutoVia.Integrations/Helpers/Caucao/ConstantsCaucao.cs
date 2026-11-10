namespace AutoVia.Integrations.Helpers.Caucao
{
    /// <summary>
    /// Constantes da caucao no cartao (P7-INT-01, RF-20): chaves da integracao PAGAMENTO, nomes
    /// dos provedores, limites de validacao, caminhos/status do Mercado Pago e do FAKE.
    /// Mensagens ficam em ConstantsMessage (AutoVia.Helpers, arquivo ConstantsMessage.Caucao.cs).
    /// </summary>
    public static class ConstantsCaucao
    {
        // --- Chaves de integration_setting (PAGAMENTO) ---
        public const string ChaveProvedor = "provedor";
        public const string ChaveAccessToken = "access_token";
        public const string ChavePublica = "public_key";
        public const string ChaveValidadeDias = "caucao_validade_dias";

        // --- Nomes dos provedores (valor da chave 'provedor') ---
        public const string ProvedorMercadoPago = "MERCADOPAGO";
        public const string ProvedorFake = "FAKE";

        // --- Validade da reserva (D1=A: so calcula ReservaValidaAte, nunca renova) ---
        public const int ValidadePadraoDias = 5;
        public const int ValidadeMinimaDias = 1;
        public const int ValidadeMaximaDias = 30;

        // --- Limites de validacao da solicitacao ---
        public const int ReferenciaTamanhoMaximo = 64;
        public const int DescricaoTamanhoMaximo = 140;
        public const int TokenTamanhoMinimo = 8;
        public const int TokenTamanhoMaximo = 128;
        public const int MetodoPagamentoTamanhoMinimo = 2;
        public const int MetodoPagamentoTamanhoMaximo = 30;
        public const int EmailTamanhoMaximo = 254;

        // --- Tamanhos de texto logado (status, detalhe, codigos de causa) ---
        public const int TamanhoMaximoDetalheProvedor = 50;
        public const int MaximoCodigosCausa = 5;

        // --- Marcador de valor ausente no log tecnico ---
        public const string MarcadorAusente = "-";

        // --- Padroes (regex com \z para nao aceitar quebra de linha no fim) ---
        public const string PadraoReferenciaExterna = @"^[A-Za-z0-9_-]+\z";
        public const string PadraoToken = @"^[A-Za-z0-9_-]+\z";
        public const string PadraoMetodoPagamento = @"^[a-z0-9_]+\z";
        public const string PadraoEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+\z";
        public const string PadraoCpf = @"^[0-9]{11}\z";
        public const string PadraoDispositivo = @"^[A-Za-z0-9_.:\-]{8,128}\z";
        public const string PadraoIdentificadorMercadoPago = @"^[0-9]{1,20}\z";
        public const string PadraoIdentificadorFake = @"^FAKE-CAU-[0-9a-f]{12}\z";
        public const string PadraoCartaoFinal = @"^[0-9]{4}\z";
        public const string PadraoCodigoCausa = @"^[0-9]{1,8}\z";

        // --- Chave de idempotencia (derivada da tentativa: referencia + token + valor) ---
        public const string PrefixoChaveIdempotencia = "caucao-";
        public const int TamanhoHashChaveIdempotencia = 40;
        public const string FormatoValorIdempotencia = "F2";
        public const string SeparadorHashIdempotencia = "|";

        // --- Mercado Pago (Payments API, /v1/payments) ---
        public const string MercadoPagoRecursoPagamentos = "/v1/payments";
        public const string MercadoPagoTipoDocumentoCpf = "CPF";
        public const int MercadoPagoParcelasUnicas = 1;
        public const string MercadoPagoStatusAutorizado = "authorized";
        public const string MercadoPagoDetalhePendenteCaptura = "pending_capture";
        public const string MercadoPagoStatusAprovado = "approved";
        public const string MercadoPagoStatusPendente = "pending";
        public const string MercadoPagoStatusEmAnalise = "in_process";
        public const string MercadoPagoStatusRejeitado = "rejected";
        public const string MercadoPagoStatusCancelado = "cancelled";
        public const string MercadoPagoStatusCanceladoAlternativo = "canceled";
        public const string MercadoPagoDetalheExpirado = "expired";
        public const string MercadoPagoStatusEstornado = "refunded";
        public const string MercadoPagoStatusContestado = "charged_back";
        public const string MercadoPagoDetalheSaldoInsuficiente = "cc_rejected_insufficient_amount";
        public const string MercadoPagoDetalheDadosIncorretosPrefixo = "cc_rejected_bad_filled_";
        public const string MercadoPagoDetalheAutorizacaoEmissor = "cc_rejected_call_for_authorize";
        public const string MercadoPagoDetalheCartaoDesabilitado = "cc_rejected_card_disabled";
        public const string MercadoPagoDetalheRiscoAlto = "cc_rejected_high_risk";
        public const string CabecalhoAutorizacao = "Authorization";
        public const string PrefixoBearer = "Bearer ";
        public const string CabecalhoIdempotenciaMercadoPago = "X-Idempotency-Key";
        public const string CabecalhoSessaoMercadoPago = "X-meli-session-id";

        // --- Codigos HTTP tratados pelo provedor ---
        public const int HttpStatusRequisicaoRecusada = 400;
        public const int HttpStatusNaoAutorizado = 401;
        public const int HttpStatusProibido = 403;
        public const int HttpStatusNaoEncontrado = 404;
        public const int HttpStatusNaoProcessavel = 422;
        public const int HttpStatusLimiteRequisicoes = 429;
        public const int HttpStatusErroServidorMinimo = 500;

        // --- Provedor FAKE (somente SANDBOX) ---
        public const string PrefixoIdentificadorFake = "FAKE-CAU-";
        public const int TamanhoHashIdentificadorFake = 12;
        public const string ChavePublicaFake = "FAKE-PUBLIC-KEY";
        public const string CartaoFinalFake = "0000";
        public const string TokenFakeRecusado = "FAKE-RECUSADO";
        public const string TokenFakeAnalise = "FAKE-ANALISE";
    }
}
