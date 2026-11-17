using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras das fotos do veiculo (sem banco, sem servico): teto de fotos, proxima ordem, chave do objeto, capa e
    /// dimensoes da imagem gravada. Testadas direto em RegrasVeiculoFotoTests.
    /// </summary>
    public static class RegrasVeiculoFoto
    {
        /// <summary>Verdadeiro quando o veiculo ainda tem vaga: menos fotos ativas que ConstantsValues.MaximoFotosPorVeiculo.</summary>
        public static bool PodeReceberMaisFotos(int fotosAtivas)
        {
            return fotosAtivas < ConstantsValues.MaximoFotosPorVeiculo;
        }

        /// <summary>Ordem da foto nova: 1 quando nao ha foto ativa; senao a maior ordem ativa mais 1. Remover nao renumera as outras.</summary>
        public static int ProximaOrdem(IEnumerable<int> ordensAtivas)
        {
            bool haOrdem = false;
            int maiorOrdem = 0;

            foreach (int ordem in ordensAtivas)
            {
                if (!haOrdem || ordem > maiorOrdem)
                {
                    maiorOrdem = ordem;
                    haOrdem = true;
                }
            }

            if (!haOrdem)
            {
                return 1;
            }

            return maiorOrdem + 1;
        }

        /// <summary>
        /// Chave do objeto no armazenamento: veiculos/{idVeiculo}/fotos/{guid}.jpg. Sem id da locadora e sem nome de arquivo do cliente.
        /// O identificador unico e recebido por parametro para a funcao continuar pura.
        /// </summary>
        public static string MontarChaveObjeto(int idVeiculo, Guid identificador)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                ConstantsValues.FormatoChaveFotoVeiculo,
                idVeiculo,
                identificador.ToString(ConstantsValues.FormatoGuidChaveFotoVeiculo, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Capa do veiculo: a foto ativa (DisabledAt nulo) de menor Ordem; no empate, a de menor IdVeiculoFoto. Desativadas sao
        /// ignoradas; sem foto ativa devolve nulo. Recebe as fotos da colecao Veiculo.Fotos.
        /// </summary>
        public static VeiculoFoto? EscolherCapa(IEnumerable<VeiculoFoto>? fotos)
        {
            if (fotos == null)
            {
                return null;
            }

            VeiculoFoto? capa = null;

            foreach (VeiculoFoto foto in fotos)
            {
                if (foto.DisabledAt != null)
                {
                    continue;
                }

                if (capa == null)
                {
                    capa = foto;
                    continue;
                }

                if (foto.Ordem < capa.Ordem || (foto.Ordem == capa.Ordem && foto.IdVeiculoFoto < capa.IdVeiculoFoto))
                {
                    capa = foto;
                }
            }

            return capa;
        }

        /// <summary>
        /// Dimensoes da imagem gravada: mantem a proporcao e deixa o lado maior em ate ladoMaximo. Nunca amplia imagem pequena
        /// e nenhum lado sai menor que 1.
        /// </summary>
        public static (int Largura, int Altura) CalcularDimensoesDeSaida(int largura, int altura, int ladoMaximo)
        {
            if (largura < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(largura));
            }

            if (altura < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(altura));
            }

            if (ladoMaximo < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(ladoMaximo));
            }

            int maiorLado = Math.Max(largura, altura);

            if (maiorLado <= ladoMaximo)
            {
                return (largura, altura);
            }

            int novaLargura = Math.Max(1, (int)Math.Round((double)largura * ladoMaximo / maiorLado, MidpointRounding.AwayFromZero));
            int novaAltura = Math.Max(1, (int)Math.Round((double)altura * ladoMaximo / maiorLado, MidpointRounding.AwayFromZero));

            return (novaLargura, novaAltura);
        }
    }
}
