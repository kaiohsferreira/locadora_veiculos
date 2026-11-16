namespace AutoVia.Integrations.Helpers.Multas
{
    /// <summary>Chaves, provedor e dados sinteticos da integracao MULTAS.</summary>
    public static class ConstantsMultas
    {
        public const string ChaveProvedor = "provedor";
        public const string ProvedorFake = "FAKE";
        public const char FinalPlacaSemResultados = '0';
        public const char FinalPlacaUmResultado = '1';
        public const string CodigoInfracaoFakeUm = "FAKE-001";
        public const string CodigoInfracaoFakeDois = "FAKE-002";
        public const string DescricaoInfracaoFakeUm = "Excesso de velocidade em via urbana";
        public const string DescricaoInfracaoFakeDois = "Estacionamento em local proibido";
        public const decimal ValorInfracaoFakeUm = 195.23m;
        public const decimal ValorInfracaoFakeDois = 130.16m;
        public const string PrefixoIdentificadorFake = "FAKE-MUL-";
        public const int TamanhoHashIdentificadorFake = 12;
        public const int CasasDecimaisValor = 2;
        public const int IdentificadorProvedorTamanhoMaximo = 100;
        public const int CodigoInfracaoTamanhoMaximo = 50;
        public const int DescricaoTamanhoMaximo = 200;
        public const decimal ValorMaximo = 99999999.99m;
    }
}