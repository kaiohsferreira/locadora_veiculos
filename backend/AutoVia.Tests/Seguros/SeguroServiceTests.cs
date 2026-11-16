using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Seguros;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Seguros
{
    /// <summary>
    /// Testes do SeguroService (P9-BE-01): veiculo inexistente (404), numero da apolice unico
    /// entre as ativas (409), normalizacao, destaque de vencimento em 60 dias (RF-41),
    /// filtro apenasAVencer e paginacao. Usa repositorios e log em memoria e o mapper real.
    /// As datas sao relativas a hoje, para o teste valer em qualquer dia.
    /// </summary>
    public class SeguroServiceTests
    {
        private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Now);

        [Fact]
        public async Task InsertValido_DevolveIdPlacaEValoresFormatados()
        {
            Cenario cenario = new Cenario();
            SeguroVO vo = CriarVO(cenario.Veiculo.IdVeiculo, "AP-001", Hoje.AddDays(200));

            ReturnSeguroVO retorno = await cenario.Servico.InsertAsync(vo);

            Assert.True(retorno.IdSeguro > 0);
            Assert.Equal("ABC1D23", retorno.PlacaVeiculo);
            Assert.Equal(StaticMethods.FormateToMonetary(1200m), retorno.ValorPremioStr);
            Assert.Equal(StaticMethods.FormateDateToPTBR(vo.DataFim), retorno.DataFimStr);
            Assert.False(retorno.VenceEm60Dias);
        }

        [Fact]
        public async Task InsertComTextosComEspacos_GravaComTrimECoberturaVaziaNula()
        {
            Cenario cenario = new Cenario();
            SeguroVO vo = CriarVO(cenario.Veiculo.IdVeiculo, "  AP-002  ", Hoje.AddDays(200));
            vo.Seguradora = "  Porto Seguro ";
            vo.Cobertura = "   ";

            await cenario.Servico.InsertAsync(vo);

            Seguro gravado = Assert.IsType<Seguro>(cenario.Seguros.UltimoGravado);
            Assert.Equal("AP-002", gravado.NumeroApolice);
            Assert.Equal("Porto Seguro", gravado.Seguradora);
            Assert.Null(gravado.Cobertura);
        }

        [Fact]
        public async Task InsertComVeiculoInexistente_LancaVeiculoNaoEncontrado()
        {
            Cenario cenario = new Cenario();
            SeguroVO vo = CriarVO(999, "AP-003", Hoje.AddDays(200));

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task InsertComVeiculoDesativado_LancaVeiculoNaoEncontrado()
        {
            Cenario cenario = new Cenario();
            Veiculo desativado = cenario.SemearVeiculo("XYZ9A88", true);
            SeguroVO vo = CriarVO(desativado.IdVeiculo, "AP-004", Hoje.AddDays(200));

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task InsertComNumeroDeAtivaDiferenteSoEmMaiusculasEEspacos_LancaNumeroDuplicado()
        {
            Cenario cenario = new Cenario();
            cenario.SemearSeguro("ap-100", Hoje.AddDays(100), false);
            SeguroVO vo = CriarVO(cenario.Veiculo.IdVeiculo, "  AP-100 ", Hoje.AddDays(200));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroSeguroNumeroApoliceDuplicado, excecao.Message);
        }

        [Fact]
        public async Task InsertComNumeroDeApoliceDesativada_EPermitido()
        {
            Cenario cenario = new Cenario();
            cenario.SemearSeguro("AP-101", Hoje.AddDays(100), true);
            SeguroVO vo = CriarVO(cenario.Veiculo.IdVeiculo, "AP-101", Hoje.AddDays(200));

            ReturnSeguroVO retorno = await cenario.Servico.InsertAsync(vo);

            Assert.True(retorno.IdSeguro > 0);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(30, true)]
        [InlineData(60, true)]
        [InlineData(61, false)]
        [InlineData(-1, false)]
        public async Task GetById_DestaqueDeVencimentoSegueAJanelaDe60Dias(int diasAteOFim, bool destaqueEsperado)
        {
            Cenario cenario = new Cenario();
            Seguro seguro = cenario.SemearSeguro("AP-200", Hoje.AddDays(diasAteOFim), false);

            ReturnSeguroVO retorno = await cenario.Servico.GetByIdAsync(seguro.IdSeguro);

            Assert.Equal(diasAteOFim, retorno.DiasParaVencer);
            Assert.Equal(destaqueEsperado, retorno.VenceEm60Dias);
        }

        [Fact]
        public async Task GetById_ApoliceDesativada_LancaNaoEncontrada()
        {
            Cenario cenario = new Cenario();
            Seguro seguro = cenario.SemearSeguro("AP-201", Hoje.AddDays(100), true);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.GetByIdAsync(seguro.IdSeguro));

            Assert.Equal(ConstantsMessage.ErroSeguroNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task GetById_FalhaInesperada_LogaERelanca()
        {
            Cenario cenario = new Cenario();
            InvalidOperationException excecaoSimulada = new InvalidOperationException("banco fora");
            cenario.Seguros.ExcecaoAoBuscarPorId = excecaoSimulada;

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.GetByIdAsync(1));

            Assert.Same(excecaoSimulada, excecao);
            Assert.Contains(cenario.Log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroBuscarSeguro));
        }

        [Fact]
        public async Task GetAll_OrdenaPeloFimDaVigenciaEPreencheDestaque()
        {
            Cenario cenario = new Cenario();
            cenario.SemearSeguro("AP-LONGE", Hoje.AddDays(300), false);
            cenario.SemearSeguro("AP-PERTO", Hoje.AddDays(30), false);
            cenario.SemearSeguro("AP-INATIVA", Hoje.AddDays(10), true);

            PaginaVO<ReturnSeguroVO> pagina = await cenario.Servico.GetAllAsync(new FiltroSeguroVO());

            Assert.Equal(2, pagina.Total);
            Assert.Equal("AP-PERTO", pagina.Itens[0].NumeroApolice);
            Assert.True(pagina.Itens[0].VenceEm60Dias);
            Assert.Equal("AP-LONGE", pagina.Itens[1].NumeroApolice);
            Assert.False(pagina.Itens[1].VenceEm60Dias);
        }

        [Fact]
        public async Task GetAll_ApenasAVencer_TrazSoAsDaJanelaDe0A60Dias()
        {
            Cenario cenario = new Cenario();
            cenario.SemearSeguro("AP-VENCIDA", Hoje.AddDays(-1), false);
            cenario.SemearSeguro("AP-HOJE", Hoje, false);
            cenario.SemearSeguro("AP-60", Hoje.AddDays(60), false);
            cenario.SemearSeguro("AP-61", Hoje.AddDays(61), false);
            FiltroSeguroVO filtro = new FiltroSeguroVO { ApenasAVencer = true };

            PaginaVO<ReturnSeguroVO> pagina = await cenario.Servico.GetAllAsync(filtro);

            List<string> numeros = pagina.Itens.Select(item => item.NumeroApolice).ToList();
            Assert.Equal(2, pagina.Total);
            Assert.Equal(new List<string> { "AP-HOJE", "AP-60" }, numeros);
        }

        [Fact]
        public async Task GetAll_FiltroPorVeiculoEPaginacao_TotalBateComOFiltro()
        {
            Cenario cenario = new Cenario();
            Veiculo outro = cenario.SemearVeiculo("DEF2B34", false);
            cenario.SemearSeguro("AP-A", Hoje.AddDays(100), false);
            cenario.SemearSeguro("AP-B", Hoje.AddDays(200), false);
            cenario.SemearSeguro("AP-C", Hoje.AddDays(300), false, outro);
            FiltroSeguroVO filtro = new FiltroSeguroVO { IdVeiculo = cenario.Veiculo.IdVeiculo, Pagina = 2, TamanhoPagina = 1 };

            PaginaVO<ReturnSeguroVO> pagina = await cenario.Servico.GetAllAsync(filtro);

            Assert.Equal(2, pagina.Total);
            ReturnSeguroVO unico = Assert.Single(pagina.Itens);
            Assert.Equal("AP-B", unico.NumeroApolice);
        }

        [Fact]
        public async Task Update_ProprioNumeroNaoContaComoDuplicado()
        {
            Cenario cenario = new Cenario();
            Seguro seguro = cenario.SemearSeguro("AP-300", Hoje.AddDays(100), false);
            SeguroVO vo = CriarVO(cenario.Veiculo.IdVeiculo, "ap-300", Hoje.AddDays(400));
            vo.Seguradora = "Allianz";

            await cenario.Servico.UpdateAsync(seguro.IdSeguro, vo);

            ReturnSeguroVO atualizado = await cenario.Servico.GetByIdAsync(seguro.IdSeguro);
            Assert.Equal("Allianz", atualizado.Seguradora);
            Assert.Equal(Hoje.AddDays(400), atualizado.DataFim);
        }

        [Fact]
        public async Task Update_NumeroDeOutraAtiva_LancaNumeroDuplicado()
        {
            Cenario cenario = new Cenario();
            cenario.SemearSeguro("AP-301", Hoje.AddDays(100), false);
            Seguro seguro = cenario.SemearSeguro("AP-302", Hoje.AddDays(100), false);
            SeguroVO vo = CriarVO(cenario.Veiculo.IdVeiculo, "AP-301", Hoje.AddDays(200));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.UpdateAsync(seguro.IdSeguro, vo));

            Assert.Equal(ConstantsMessage.ErroSeguroNumeroApoliceDuplicado, excecao.Message);
        }

        [Fact]
        public async Task Update_ApoliceInexistente_LancaNaoEncontrada()
        {
            Cenario cenario = new Cenario();
            SeguroVO vo = CriarVO(cenario.Veiculo.IdVeiculo, "AP-303", Hoje.AddDays(200));

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.UpdateAsync(999, vo));

            Assert.Equal(ConstantsMessage.ErroSeguroNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task Update_VeiculoInexistente_LancaVeiculoNaoEncontrado()
        {
            Cenario cenario = new Cenario();
            Seguro seguro = cenario.SemearSeguro("AP-304", Hoje.AddDays(100), false);
            SeguroVO vo = CriarVO(999, "AP-304", Hoje.AddDays(200));

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.UpdateAsync(seguro.IdSeguro, vo));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task Update_RepositorioNaoAchaNaGravacao_LancaNaoEncontrada()
        {
            Cenario cenario = new Cenario();
            Seguro seguro = cenario.SemearSeguro("AP-305", Hoje.AddDays(100), false);
            cenario.Seguros.ForcarUpdateFalse = true;
            SeguroVO vo = CriarVO(cenario.Veiculo.IdVeiculo, "AP-305", Hoje.AddDays(200));

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.UpdateAsync(seguro.IdSeguro, vo));

            Assert.Equal(ConstantsMessage.ErroSeguroNaoEncontrado, excecao.Message);
        }

        private static SeguroVO CriarVO(int idVeiculo, string numeroApolice, DateOnly dataFim)
        {
            SeguroVO vo = new SeguroVO
            {
                IdVeiculo = idVeiculo,
                Seguradora = "Porto Seguro",
                NumeroApolice = numeroApolice,
                DataInicio = dataFim.AddYears(-1),
                DataFim = dataFim,
                ValorPremio = 1200m,
                Cobertura = "Compreensiva"
            };

            return vo;
        }

        /// <summary>Repositorios em memoria, log, servico real e um veiculo ativo (placa ABC1D23) ja semeado.</summary>
        private sealed class Cenario
        {
            public Cenario()
            {
                Seguros = new FakeSeguroRepository();
                Veiculos = new FakeVeiculoRepository();
                Log = new FakeLogService();
                Veiculo = SemearVeiculo("ABC1D23", false);

                MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<SeguroProfile>(), NullLoggerFactory.Instance);
                IMapper mapper = configuracao.CreateMapper();
                Servico = new SeguroService(Seguros, Veiculos, mapper, Log);
            }

            public FakeSeguroRepository Seguros { get; }

            public FakeVeiculoRepository Veiculos { get; }

            public FakeLogService Log { get; }

            public Veiculo Veiculo { get; }

            public SeguroService Servico { get; }

            public Veiculo SemearVeiculo(string placa, bool desativado)
            {
                Veiculo veiculo = new Veiculo
                {
                    IdCategoria = 1,
                    Placa = placa,
                    Chassi = "9BWZZZ377VT004251",
                    Renavam = "12345678901",
                    Marca = "Fiat",
                    Modelo = "Toro",
                    AnoFabricacao = 2022,
                    AnoModelo = 2023,
                    ValorDiaria = 150m
                };

                if (desativado)
                {
                    veiculo.Disable();
                }

                Veiculos.Adicionar(veiculo);

                return veiculo;
            }

            public Seguro SemearSeguro(string numeroApolice, DateOnly dataFim, bool desativado, Veiculo? veiculo = null)
            {
                Veiculo dono = veiculo ?? Veiculo;
                Seguro seguro = new Seguro
                {
                    IdVeiculo = dono.IdVeiculo,
                    Veiculo = dono,
                    Seguradora = "Porto Seguro",
                    NumeroApolice = numeroApolice,
                    DataInicio = dataFim.AddYears(-1),
                    DataFim = dataFim,
                    ValorPremio = 900m
                };

                if (desativado)
                {
                    seguro.Disable();
                }

                Seguros.Adicionar(seguro);

                return seguro;
            }
        }
    }
}
