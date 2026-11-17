using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using System.Globalization;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Servico de capa (FotoCapaVeiculoService) com o gateway falso e o log em memoria: uma assinatura em lote por pagina,
    /// tres estados por item (sem foto, sucesso, falha com erroFotoCapa), cancelamento sobe sem log e excecao nao prevista
    /// sobe com log. Sem banco.
    /// </summary>
    public class FotoCapaVeiculoServiceTests
    {
        private const int IdEmpresaDoTeste = 4242;
        private const string PrefixoUrlDoFake = "https://storage.example/foto/";

        private static string ChaveDaFoto(int idVeiculoFoto)
        {
            return "veiculos/" + idVeiculoFoto.ToString(CultureInfo.InvariantCulture) + "/fotos/" + idVeiculoFoto.ToString(CultureInfo.InvariantCulture) + ".jpg";
        }

        private static VeiculoFoto CriarFoto(int idVeiculoFoto, int idVeiculo, int ordem, bool desativada = false)
        {
            VeiculoFoto foto = new VeiculoFoto
            {
                IdVeiculoFoto = idVeiculoFoto,
                IdVeiculo = idVeiculo,
                Ordem = ordem,
                ChaveObjeto = ChaveDaFoto(idVeiculoFoto),
                IdFuncionarioEnvio = 1
            };

            if (desativada)
            {
                foto.DisabledAt = DateTime.Now;
            }

            return foto;
        }

        private static Veiculo CriarVeiculo(int idVeiculo, params VeiculoFoto[] fotos)
        {
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = idVeiculo,
                IdEmpresa = IdEmpresaDoTeste,
                Placa = "PLC" + idVeiculo.ToString(CultureInfo.InvariantCulture),
                Fotos = fotos.ToList()
            };

            return veiculo;
        }

        private static ReturnVeiculoVO CriarItem(int idVeiculo)
        {
            ReturnVeiculoVO item = new ReturnVeiculoVO { IdVeiculo = idVeiculo };

            return item;
        }

        [Fact]
        public async Task ComFoto_UmaAssinaturaEmLote_PreencheACapaSemErro()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            FakeLogService log = new FakeLogService();
            FotoCapaVeiculoService servico = new FotoCapaVeiculoService(gateway, log);
            List<Veiculo?> veiculos = new List<Veiculo?> { CriarVeiculo(1, CriarFoto(10, 1, 1)), CriarVeiculo(2) };
            List<ReturnVeiculoVO> itens = new List<ReturnVeiculoVO> { CriarItem(1), CriarItem(2) };

            await servico.PreencherAsync(veiculos, itens);

            Assert.Equal(1, gateway.AssinaturaLoteCount);
            Assert.Equal(new List<string> { ChaveDaFoto(10) }, gateway.ChavesAssinadas);
            Assert.NotNull(itens[0].FotoCapa);
            Assert.StartsWith(PrefixoUrlDoFake, itens[0].FotoCapa!.Url, StringComparison.Ordinal);
            Assert.Null(itens[0].FotoCapa!.UrlMiniatura);
            Assert.Null(itens[0].ErroFotoCapa);
            DateTimeOffset expiraEm = DateTimeOffset.ParseExact(itens[0].FotoCapa!.ExpiraEm, ConstantsValues.FormatoExpiracaoUrlFoto, CultureInfo.InvariantCulture);
            Assert.Equal(TimeSpan.Zero, expiraEm.Offset);
            Assert.Null(itens[1].FotoCapa);
            Assert.Null(itens[1].ErroFotoCapa);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task SemFotoCadastradaNenhuma_NaoChamaOGatewayNemRegistra()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            FakeLogService log = new FakeLogService();
            FotoCapaVeiculoService servico = new FotoCapaVeiculoService(gateway, log);
            List<Veiculo?> veiculos = new List<Veiculo?> { null, CriarVeiculo(1), CriarVeiculo(2, CriarFoto(20, 2, 1, true)) };
            List<ReturnVeiculoVO> itens = new List<ReturnVeiculoVO> { CriarItem(1), CriarItem(2) };

            await servico.PreencherAsync(veiculos, itens);

            Assert.Equal(0, gateway.AssinaturaLoteCount);
            Assert.Empty(log.Entries);
            Assert.All(itens, item =>
            {
                Assert.Null(item.FotoCapa);
                Assert.Null(item.ErroFotoCapa);
            });
        }

        [Fact]
        public async Task VeiculoDesativadoOuDeLocadoraDesativada_NaoAssinaNemMostraErro()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            FakeLogService log = new FakeLogService();
            FotoCapaVeiculoService servico = new FotoCapaVeiculoService(gateway, log);
            Veiculo desativado = CriarVeiculo(1, CriarFoto(10, 1, 1));
            desativado.DisabledAt = DateTime.Now;
            Veiculo deLocadoraDesativada = CriarVeiculo(2, CriarFoto(20, 2, 1));
            deLocadoraDesativada.Empresa = new Empresa { Ativo = false };
            List<Veiculo?> veiculos = new List<Veiculo?> { desativado, deLocadoraDesativada };
            List<ReturnVeiculoVO> itens = new List<ReturnVeiculoVO> { CriarItem(1), CriarItem(2) };

            await servico.PreencherAsync(veiculos, itens);

            Assert.Equal(0, gateway.AssinaturaLoteCount);
            Assert.Empty(log.Entries);
            Assert.All(itens, item =>
            {
                Assert.Null(item.FotoCapa);
                Assert.Null(item.ErroFotoCapa);
            });
        }

        [Fact]
        public async Task FalhaDoArmazenamento_ItemComFotoLeErroSemExcecao_E_UmRegistro()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway { FalharAssinatura = true };
            FakeLogService log = new FakeLogService();
            FotoCapaVeiculoService servico = new FotoCapaVeiculoService(gateway, log);
            List<Veiculo?> veiculos = new List<Veiculo?> { CriarVeiculo(1, CriarFoto(10, 1, 1)), CriarVeiculo(2) };
            List<ReturnVeiculoVO> itens = new List<ReturnVeiculoVO> { CriarItem(1), CriarItem(2) };

            await servico.PreencherAsync(veiculos, itens);

            Assert.Null(itens[0].FotoCapa);
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, itens[0].ErroFotoCapa);
            Assert.Null(itens[1].FotoCapa);
            Assert.Null(itens[1].ErroFotoCapa);
            Assert.Single(log.Entries);
            Assert.Contains(ConstantsMessage.ErroMontarCapaVeiculo, log.Entries[0], StringComparison.Ordinal);
            Assert.DoesNotContain(PrefixoUrlDoFake, log.Entries[0], StringComparison.Ordinal);
            Assert.DoesNotContain("veiculos/", log.Entries[0], StringComparison.Ordinal);
        }

        [Fact]
        public async Task CancelamentoDoChamador_SobeEFicaSemRegistro()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway { LancarCancelamentoAssinatura = true };
            FakeLogService log = new FakeLogService();
            FotoCapaVeiculoService servico = new FotoCapaVeiculoService(gateway, log);
            List<Veiculo?> veiculos = new List<Veiculo?> { CriarVeiculo(1, CriarFoto(10, 1, 1)) };
            List<ReturnVeiculoVO> itens = new List<ReturnVeiculoVO> { CriarItem(1) };
            using CancellationTokenSource fonte = new CancellationTokenSource();
            fonte.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => servico.PreencherAsync(veiculos, itens, fonte.Token));

            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task ExcecaoNaoPrevista_SobeComUmRegistro()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway { LancarExcecaoGenericaAssinatura = true };
            FakeLogService log = new FakeLogService();
            FotoCapaVeiculoService servico = new FotoCapaVeiculoService(gateway, log);
            List<Veiculo?> veiculos = new List<Veiculo?> { CriarVeiculo(1, CriarFoto(10, 1, 1)) };
            List<ReturnVeiculoVO> itens = new List<ReturnVeiculoVO> { CriarItem(1) };

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.PreencherAsync(veiculos, itens));

            Assert.Single(log.Entries);
            Assert.Contains(ConstantsMessage.ErroMontarCapaVeiculo, log.Entries[0], StringComparison.Ordinal);
        }

        [Fact]
        public async Task ChaveAusenteNoLote_SoAquelItemLeErro_E_UmRegistro()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            gateway.ChavesOmitidasNaAssinatura.Add(ChaveDaFoto(20));
            FakeLogService log = new FakeLogService();
            FotoCapaVeiculoService servico = new FotoCapaVeiculoService(gateway, log);
            List<Veiculo?> veiculos = new List<Veiculo?> { CriarVeiculo(1, CriarFoto(10, 1, 1)), CriarVeiculo(2, CriarFoto(20, 2, 1)) };
            List<ReturnVeiculoVO> itens = new List<ReturnVeiculoVO> { CriarItem(1), CriarItem(2) };

            await servico.PreencherAsync(veiculos, itens);

            Assert.NotNull(itens[0].FotoCapa);
            Assert.Null(itens[0].ErroFotoCapa);
            Assert.Null(itens[1].FotoCapa);
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, itens[1].ErroFotoCapa);
            Assert.Single(log.Entries);
            Assert.Contains(ConstantsMessage.ErroCapaVeiculoSemUrlAssinada, log.Entries[0], StringComparison.Ordinal);
            Assert.DoesNotContain(PrefixoUrlDoFake, log.Entries[0], StringComparison.Ordinal);
        }

        [Fact]
        public async Task VeiculoRepetidoNaPagina_UmaChaveEUmaCapaParaTodosOsItens()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            FakeLogService log = new FakeLogService();
            FotoCapaVeiculoService servico = new FotoCapaVeiculoService(gateway, log);
            Veiculo veiculo = CriarVeiculo(1, CriarFoto(10, 1, 1));
            List<Veiculo?> veiculos = new List<Veiculo?> { veiculo, veiculo };
            List<ReturnVeiculoVO> itens = new List<ReturnVeiculoVO> { CriarItem(1), CriarItem(1) };

            await servico.PreencherAsync(veiculos, itens);

            Assert.Equal(1, gateway.AssinaturaLoteCount);
            Assert.Single(gateway.ChavesAssinadas);
            Assert.NotNull(itens[0].FotoCapa);
            Assert.NotNull(itens[1].FotoCapa);
            Assert.Equal(itens[0].FotoCapa!.Url, itens[1].FotoCapa!.Url);
        }

        [Fact]
        public async Task ItemDeVeiculoFora_DaPaginaFicaNulo()
        {
            FakeArmazenamentoGateway gateway = new FakeArmazenamentoGateway();
            FakeLogService log = new FakeLogService();
            FotoCapaVeiculoService servico = new FotoCapaVeiculoService(gateway, log);
            List<Veiculo?> veiculos = new List<Veiculo?> { CriarVeiculo(1, CriarFoto(10, 1, 1)) };
            List<ReturnVeiculoVO> itens = new List<ReturnVeiculoVO> { CriarItem(9) };

            await servico.PreencherAsync(veiculos, itens);

            Assert.Null(itens[0].FotoCapa);
            Assert.Null(itens[0].ErroFotoCapa);
            Assert.Equal(1, gateway.AssinaturaLoteCount);
        }
    }
}
