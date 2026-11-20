using System.Collections.Generic;

namespace AutoVia.Communication.ViewObjects.Manutencao
{
    /// <summary>
    /// VO de saida da lista de alertas de revisao (GET /api/manutencoes/alertas, RF-39). Total = quantos veiculos estao
    /// VENCIDA ou PROXIMA; Itens = no maximo o teto da lista, do mais atrasado para o menos atrasado.
    /// </summary>
    public class ReturnAlertasRevisaoVO
    {
        /// <summary>Quantos veiculos estao VENCIDA ou PROXIMA (sem o teto da lista).</summary>
        public int Total { get; set; }

        /// <summary>Veiculos em alerta, do mais atrasado para o menos atrasado.</summary>
        public List<ReturnAlertaRevisaoVO> Itens { get; set; } = new List<ReturnAlertaRevisaoVO>();

        /// <summary>Intervalo padrao entre revisoes, em km (ConstantsValues.IntervaloRevisaoKm). Alimenta o "Intervalo Padrao" da tela 14.</summary>
        public int IntervaloRevisaoKm { get; set; }
    }
}
