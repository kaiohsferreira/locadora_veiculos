using System;
using System.Collections.Generic;

namespace AutoVia.Communication.ViewObjects.PainelOperacional
{
    /// <summary>Resumo dos movimentos de retirada e devolucao do dia local atual.</summary>
    public class ReturnPainelDiarioVO
    {
        public DateOnly Data { get; set; }
        public List<ItemPainelOperacionalVO> Retiradas { get; set; } = new List<ItemPainelOperacionalVO>();
        public List<ItemPainelOperacionalVO> Devolucoes { get; set; } = new List<ItemPainelOperacionalVO>();
    }
}
