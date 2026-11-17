using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoVia.Tests.Frota
{
    /// <summary>Funcoes puras das fotos do veiculo: teto, proxima ordem, chave do objeto, capa e dimensoes da imagem gravada.</summary>
    public sealed class RegrasVeiculoFotoTests
    {
        private static readonly DateTime DataDeTeste = new DateTime(2026, 11, 16, 10, 0, 0);

        /// <summary>Foto de teste: ativa quando DisabledAt e nulo; desativada quando traz uma data de exclusao logica.</summary>
        private static VeiculoFoto Foto(int id, int ordem, bool ativa)
        {
            return new VeiculoFoto { IdVeiculoFoto = id, Ordem = ordem, DisabledAt = ativa ? null : DataDeTeste };
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(14, true)]
        [InlineData(15, false)]
        [InlineData(16, false)]
        public void PodeReceberMaisFotos_RespeitaOTetoDeQuinze(int fotosAtivas, bool esperado)
        {
            Assert.Equal(esperado, RegrasVeiculoFoto.PodeReceberMaisFotos(fotosAtivas));
        }

        [Fact]
        public void ProximaOrdem_ListaVazia_DevolveUm()
        {
            List<int> ordens = new List<int>();

            Assert.Equal(1, RegrasVeiculoFoto.ProximaOrdem(ordens));
        }

        [Fact]
        public void ProximaOrdem_OrdensSeguidas_DevolveAMaiorMaisUm()
        {
            List<int> ordens = new List<int> { 1, 2, 3 };

            Assert.Equal(4, RegrasVeiculoFoto.ProximaOrdem(ordens));
        }

        [Fact]
        public void ProximaOrdem_ComBuraco_UsaAMaiorMaisUm()
        {
            List<int> ordens = new List<int> { 1, 5 };

            Assert.Equal(6, RegrasVeiculoFoto.ProximaOrdem(ordens));
        }

        [Fact]
        public void ProximaOrdem_OrdemMaiorNoMeio_NaoDependeDaPosicao()
        {
            List<int> ordens = new List<int> { 5, 1, 3 };

            Assert.Equal(6, RegrasVeiculoFoto.ProximaOrdem(ordens));
        }

        [Fact]
        public void MontarChaveObjeto_FormatoDoContrato_SemIdDaLocadora()
        {
            Guid identificador = new Guid("0123456789abcdef0123456789abcdef");

            string chave = RegrasVeiculoFoto.MontarChaveObjeto(7, identificador);

            Assert.Equal("veiculos/7/fotos/0123456789abcdef0123456789abcdef.jpg", chave);
        }

        [Fact]
        public void MontarChaveObjeto_GuidNovo_TemTrintaEDoisHexadecimais()
        {
            string chave = RegrasVeiculoFoto.MontarChaveObjeto(7, Guid.NewGuid());

            Assert.StartsWith("veiculos/7/fotos/", chave, StringComparison.Ordinal);
            Assert.EndsWith(".jpg", chave, StringComparison.Ordinal);
            string nome = chave.Substring("veiculos/7/fotos/".Length, 32);
            Assert.True(nome.All(char.IsAsciiHexDigit));
            Assert.Equal("veiculos/7/fotos/".Length + 32 + ".jpg".Length, chave.Length);
        }

        [Fact]
        public void MontarChaveObjeto_MaiorCaminhoPossivel_CabeNaColuna()
        {
            string chave = RegrasVeiculoFoto.MontarChaveObjeto(int.MaxValue, Guid.NewGuid());

            Assert.True(chave.Length <= 62);
        }

        [Fact]
        public void EscolherCapa_EscolheAMenorOrdemAtiva()
        {
            List<VeiculoFoto> fotos = new List<VeiculoFoto>
            {
                Foto(10, 3, true),
                Foto(11, 1, true),
                Foto(12, 2, true),
            };

            VeiculoFoto? capa = RegrasVeiculoFoto.EscolherCapa(fotos);

            Assert.NotNull(capa);
            Assert.Equal(11, capa.IdVeiculoFoto);
        }

        [Fact]
        public void EscolherCapa_EmpateDeOrdem_DesempataPeloMenorId()
        {
            List<VeiculoFoto> fotos = new List<VeiculoFoto>
            {
                Foto(20, 1, true),
                Foto(18, 1, true),
                Foto(19, 1, true),
            };

            VeiculoFoto? capa = RegrasVeiculoFoto.EscolherCapa(fotos);

            Assert.NotNull(capa);
            Assert.Equal(18, capa.IdVeiculoFoto);
        }

        [Fact]
        public void EscolherCapa_IgnoraFotoDesativada()
        {
            List<VeiculoFoto> fotos = new List<VeiculoFoto>
            {
                Foto(30, 1, false),
                Foto(31, 2, true),
            };

            VeiculoFoto? capa = RegrasVeiculoFoto.EscolherCapa(fotos);

            Assert.NotNull(capa);
            Assert.Equal(31, capa.IdVeiculoFoto);
        }

        [Fact]
        public void EscolherCapa_SemFotoAtiva_DevolveNulo()
        {
            List<VeiculoFoto> semAtivas = new List<VeiculoFoto> { Foto(40, 1, false) };
            List<VeiculoFoto> vazia = new List<VeiculoFoto>();

            Assert.Null(RegrasVeiculoFoto.EscolherCapa(semAtivas));
            Assert.Null(RegrasVeiculoFoto.EscolherCapa(vazia));
            Assert.Null(RegrasVeiculoFoto.EscolherCapa(null));
        }

        [Fact]
        public void CalcularDimensoesDeSaida_4000x3000_ReduzPara1600x1200()
        {
            (int Largura, int Altura) saida = RegrasVeiculoFoto.CalcularDimensoesDeSaida(4000, 3000, 1600);

            Assert.Equal(1600, saida.Largura);
            Assert.Equal(1200, saida.Altura);
        }

        [Fact]
        public void CalcularDimensoesDeSaida_3000x4000_ReduzPara1200x1600()
        {
            (int Largura, int Altura) saida = RegrasVeiculoFoto.CalcularDimensoesDeSaida(3000, 4000, 1600);

            Assert.Equal(1200, saida.Largura);
            Assert.Equal(1600, saida.Altura);
        }

        [Fact]
        public void CalcularDimensoesDeSaida_ImagemPequena_NaoAmplia()
        {
            (int Largura, int Altura) saida = RegrasVeiculoFoto.CalcularDimensoesDeSaida(800, 600, 1600);

            Assert.Equal(800, saida.Largura);
            Assert.Equal(600, saida.Altura);
        }

        [Fact]
        public void CalcularDimensoesDeSaida_FaixaExtrema_NenhumLadoSaiMenorQueUm()
        {
            (int Largura, int Altura) saida = RegrasVeiculoFoto.CalcularDimensoesDeSaida(8000, 1, 1600);

            Assert.Equal(1600, saida.Largura);
            Assert.Equal(1, saida.Altura);
        }

        [Fact]
        public void CalcularDimensoesDeSaida_LadoZero_LancaArgumento()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RegrasVeiculoFoto.CalcularDimensoesDeSaida(0, 10, 1600));
        }
    }
}
