namespace AutoVia.Integrations.Helpers.Pix
{
    /// <summary>
    /// Constantes da cobranca PIX (RF-32, parte PIX): chaves da integracao PAGAMENTO,
    /// nomes dos provedores, limites de validacao e caminhos/status do Mercado Pago.
    /// Mensagens ficam em ConstantsMessage (AutoVia.Helpers, arquivo ConstantsMessage.Pix.cs).
    /// </summary>
    public static class ConstantsPix
    {
        // --- Chaves de integration_setting (PAGAMENTO) ---
        public const string ChaveProvedor = "provedor";
        public const string ChaveAccessToken = "access_token";
        public const string ChavePixExpiracaoSegundos = "pix_expiracao_segundos";

        // --- Nomes dos provedores (valor da chave 'provedor') ---
        public const string ProvedorMercadoPago = "MERCADOPAGO";
        public const string ProvedorFake = "FAKE";

        // --- Expiracao da cobranca, em segundos ---
        public const int ExpiracaoPadraoSegundos = 3600;
        public const int ExpiracaoMaximaSegundos = 2592000;

        // --- Limites de validacao da solicitacao ---
        public const int ReferenciaTamanhoMaximo = 64;
        public const int DescricaoTamanhoMaximo = 140;
        public const int PagadorNomeTamanhoMaximo = 120;
        public const int EmailTamanhoMaximo = 254;
        public const int TamanhoMaximoMotivoRecusa = 200;
        public const int TamanhoMaximoStatusLog = 50;

        // --- Marcador de valor ausente no log tecnico ---
        public const string MarcadorAusente = "-";

        // --- Padroes (regex com \z para nao aceitar quebra de linha no fim) ---
        public const string PadraoReferenciaExterna = @"^[A-Za-z0-9_-]+\z";
        public const string PadraoEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+\z";
        public const string PadraoCpf = @"^[0-9]{11}\z";

        // --- Mercado Pago ---
        public const string MercadoPagoRecursoPagamentos = "/v1/payments";
        public const string MercadoPagoMetodoPix = "pix";
        public const string MercadoPagoTipoDocumentoCpf = "CPF";
        public const string MercadoPagoStatusPendente = "pending";
        public const string MercadoPagoStatusAprovado = "approved";
        public const string MercadoPagoStatusCancelado = "cancelled";
        public const string MercadoPagoStatusRejeitado = "rejected";
        public const string MercadoPagoStatusDetalheExpirado = "expired";
        public const string MercadoPagoFormatoDataExpiracao = "yyyy-MM-dd'T'HH:mm:ss.fffzzz";
        public const string CabecalhoAutorizacao = "Authorization";
        public const string PrefixoBearer = "Bearer ";
        public const string CabecalhoIdempotenciaMercadoPago = "X-Idempotency-Key";

        // --- Codigos HTTP tratados pelo provedor ---
        public const int HttpStatusRequisicaoRecusada = 400;
        public const int HttpStatusNaoAutorizado = 401;
        public const int HttpStatusProibido = 403;
        public const int HttpStatusNaoEncontrado = 404;
        public const int HttpStatusNaoProcessavel = 422;
        public const int HttpStatusLimiteRequisicoes = 429;
        public const int HttpStatusErroServidorMinimo = 500;

        // --- Provedor FAKE (somente SANDBOX) ---
        public const string PrefixoIdentificadorFake = "FAKE-";
        public const int TamanhoHashIdentificadorFake = 12;
        public const string PrefixoCopiaEColaFake = "FAKE-COPIA-";
        public const string PrefixoQrCodeFake = "FAKE-QR-";
    }
}
