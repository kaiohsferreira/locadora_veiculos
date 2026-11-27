namespace AutoVia.Helpers
{
    /// <summary>Valores da proposta de compra de seminovo (P6-BE-02, RF-27): limites do parcelamento e do arredondamento.</summary>
    public static partial class ConstantsValues
    {
        /// <summary>Menor numero de parcelas aceito (o mesmo do CHECK ck_proposta_compra_parcelas).</summary>
        public const int NumeroParcelasMinimo = 1;

        /// <summary>Maior numero de parcelas aceito (o mesmo do CHECK ck_proposta_compra_parcelas).</summary>
        public const int NumeroParcelasMaximo = 60;

        /// <summary>Menor valor de parcela aceito: um centavo (o CHECK ck_proposta_compra_parcela exige parcela maior que zero).</summary>
        public const decimal ValorParcelaMinimo = 0.01m;

        /// <summary>Centavos em um real: usado para arredondar a parcela para baixo no centavo.</summary>
        public const decimal CentavosPorReal = 100m;

        /// <summary>Precisao da coluna NUMERIC(12,2) dos valores da proposta: total de digitos.</summary>
        public const int PrecisaoValorProposta = 12;

        /// <summary>Precisao da coluna NUMERIC(12,2) dos valores da proposta: casas decimais.</summary>
        public const int EscalaValorProposta = 2;
    }
}
