using AutoVia.Helpers;
using System;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras da proposta de compra (sem banco, sem servico). RF-27: a parcela e o valor de venda menos a entrada,
    /// dividido pelo numero de parcelas, sem juros. Regra de arredondamento escolhida pela Maria Eduarda em 21/11/2026: a
    /// parcela e arredondada para baixo no centavo e a ultima parcela absorve a diferenca, para a soma fechar no valor
    /// financiado. Exemplo: R$ 10.000,00 em 3x da 3.333,33 + 3.333,33 + 3.333,34. Testada direto em RegrasPropostaCompraTests.
    /// </summary>
    public static class RegrasPropostaCompra
    {
        /// <summary>
        /// Calcula o parcelamento. Pre-condicoes (conferidas por quem chama): numeroParcelas maior que zero e entrada menor
        /// que o valor de venda. A diferenca entre a ultima parcela e as demais e sempre de 0 a (numeroParcelas - 1) centavos.
        /// </summary>
        public static ParcelamentoCalculado CalcularParcelamento(decimal valorVenda, decimal valorEntrada, int numeroParcelas)
        {
            decimal valorFinanciado = valorVenda - valorEntrada;
            decimal valorParcela = Math.Floor(valorFinanciado * ConstantsValues.CentavosPorReal / numeroParcelas) / ConstantsValues.CentavosPorReal;
            decimal valorUltimaParcela = valorFinanciado - (valorParcela * (numeroParcelas - 1));

            ParcelamentoCalculado parcelamento = new ParcelamentoCalculado
            {
                ValorVenda = valorVenda,
                ValorEntrada = valorEntrada,
                ValorFinanciado = valorFinanciado,
                NumeroParcelas = numeroParcelas,
                ValorParcela = valorParcela,
                ValorUltimaParcela = valorUltimaParcela
            };

            return parcelamento;
        }

        /// <summary>A entrada precisa ser menor que o valor de venda (CHECK ck_proposta_compra_entrada).</summary>
        public static bool EntradaMenorQueValor(decimal valorVenda, decimal valorEntrada)
        {
            return valorEntrada < valorVenda;
        }

        /// <summary>Cada parcela precisa ser de pelo menos um centavo (CHECK ck_proposta_compra_parcela).</summary>
        public static bool ParcelaAtingeOMinimo(ParcelamentoCalculado parcelamento)
        {
            return parcelamento.ValorParcela >= ConstantsValues.ValorParcelaMinimo;
        }
    }
}
