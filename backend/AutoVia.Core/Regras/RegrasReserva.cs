using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using System;
using System.Globalization;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras da reserva (sem banco, sem servico). Usadas pelo ReservaService e pelo ReservaRepository (dentro
    /// da transacao) e testadas direto em RegrasReservaTests.
    /// </summary>
    public static class RegrasReserva
    {
        private const char SinalCardinal = '#';
        private const char DigitoMinimo = '0';
        private const char DigitoMaximo = '9';

        /// <summary>Veiculo aceita reserva quando esta DISPONIVEL ou RESERVADO.</summary>
        public static bool VeiculoAceitaReserva(StatusVeiculo status)
        {
            return status == StatusVeiculo.DISPONIVEL || status == StatusVeiculo.RESERVADO;
        }

        /// <summary>
        /// Status do veiculo depois de um cancelamento: DISPONIVEL ou RESERVADO viram RESERVADO (ha outra reserva ativa)
        /// ou DISPONIVEL (nao ha). Os demais status (LOCADO, MANUTENCAO, VENDIDO, BAIXADO) voltam iguais.
        /// </summary>
        public static StatusVeiculo StatusDoVeiculoAposCancelamento(StatusVeiculo atual, bool haOutraReservaAtiva)
        {
            if (!VeiculoAceitaReserva(atual))
            {
                return atual;
            }

            return haOutraReservaAtiva ? StatusVeiculo.RESERVADO : StatusVeiculo.DISPONIVEL;
        }

        /// <summary>Diarias pela diferenca de DayNumber (DateOnly nao tem subtracao).</summary>
        public static int CalcularDiarias(DateOnly dataRetirada, DateOnly dataDevolucao)
        {
            return dataDevolucao.DayNumber - dataRetirada.DayNumber;
        }

        /// <summary>Valor estimado: diarias vezes a diaria do veiculo.</summary>
        public static decimal CalcularValorEstimado(int diarias, decimal valorDiaria)
        {
            return diarias * valorDiaria;
        }

        /// <summary>
        /// Codigo de exibicao: prefixo AV- e o id com no minimo cinco digitos (42 -> AV-00042; 123456 -> AV-123456).
        /// Gerado so para exibicao; nao e gravado e nao substitui o id nas rotas.
        /// </summary>
        public static string FormateCodigoReserva(int idReserva)
        {
            return ConstantsValues.PrefixoCodigoReserva + idReserva.ToString(CultureInfo.InvariantCulture).PadLeft(ConstantsValues.DigitosCodigoReserva, DigitoMinimo);
        }

        /// <summary>
        /// Id pedido pelo termo de busca: "AV-" (sem diferenciar maiusculas) seguido so de digitos, ou so digitos, com numero
        /// que cabe em int. Termo sem codigo valido devolve null.
        /// </summary>
        public static int? LerCodigoDaBusca(string? busca)
        {
            string? termo = NormalizarBusca(busca);

            if (termo == null)
            {
                return null;
            }

            string digitos = termo.StartsWith(ConstantsValues.PrefixoCodigoReserva, StringComparison.OrdinalIgnoreCase)
                ? termo.Substring(ConstantsValues.PrefixoCodigoReserva.Length)
                : termo;

            if (!SoDigitos(digitos))
            {
                return null;
            }

            bool cabeEmInt = int.TryParse(digitos, NumberStyles.None, CultureInfo.InvariantCulture, out int codigo);

            return cabeEmInt ? codigo : null;
        }

        /// <summary>
        /// Texto livre da busca, em minusculas, comparado so com o modelo do veiculo. Quando o termo e um codigo (AV-
        /// seguido de digitos, ou so digitos que cabem em int), o termo tambem vale como texto; quando e so "AV-" com
        /// digitos (sem int), fica como texto. Termo vazio devolve null.
        /// </summary>
        public static string? LerTextoDaBusca(string? busca)
        {
            string? termo = NormalizarBusca(busca);

            if (termo == null)
            {
                return null;
            }

            bool comPrefixo = termo.StartsWith(ConstantsValues.PrefixoCodigoReserva, StringComparison.OrdinalIgnoreCase);

            if (comPrefixo && LerCodigoDaBusca(termo).HasValue)
            {
                return null;
            }

            return termo.ToLowerInvariant();
        }

        /// <summary>Descarta espacos das pontas e um '#' inicial; termo vazio vira null.</summary>
        private static string? NormalizarBusca(string? busca)
        {
            string termo = (busca ?? string.Empty).Trim();

            if (termo.Length > 0 && termo[0] == SinalCardinal)
            {
                termo = termo.Substring(1).Trim();
            }

            return termo.Length == 0 ? null : termo;
        }

        /// <summary>Verdadeiro quando o texto tem so digitos ASCII 0 a 9 e pelo menos um.</summary>
        private static bool SoDigitos(string texto)
        {
            if (texto.Length == 0)
            {
                return false;
            }

            foreach (char caractere in texto)
            {
                if (caractere < DigitoMinimo || caractere > DigitoMaximo)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
