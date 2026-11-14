using AutoVia.Core.ServicesInterface;
using System;

namespace AutoVia.Tests.Fakes
{
    /// <summary>IRelogio com data fixa, para os testes do painel fecharem os periodos sem depender do dia da execucao.</summary>
    public class FakeRelogio : IRelogio
    {
        public FakeRelogio(DateTime agora)
        {
            Agora = agora;
        }

        /// <summary>Data e hora devolvidas como "agora".</summary>
        public DateTime Agora { get; set; }
    }
}
