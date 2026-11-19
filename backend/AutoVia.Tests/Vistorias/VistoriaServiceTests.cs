using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Vistorias;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Vistorias
{
    public class VistoriaServiceTests
    {
        internal static readonly DateTime Agora = new DateTime(2026, 11, 17, 10, 30, 0);

        internal static VistoriaService CriarServico(FakeVistoriaRepository vistorias,
            bool locacaoExiste = true, bool funcionarioAtivo = true, FakeLogService? log = null, bool locacaoDesativada = false)
        {
            FakeLocacaoRepository locacoes = new FakeLocacaoRepository();
            if (locacaoExiste)
            {
                locacoes.Adicionar(new Locacao { IdLocacao = 10, DisabledAt = locacaoDesativada ? Agora : null });
            }
            locacoes.Adicionar(new Funcionario { IdFuncionario = 20, Ativo = funcionarioAtivo });
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<VistoriaProfile>(), NullLoggerFactory.Instance);
            return new VistoriaService(vistorias, locacoes, new FakeRelogio(Agora), configuracao.CreateMapper(), log ?? new FakeLogService());
        }

        internal static VistoriaEntradaVO Entrada(string? observacoes = null)
        {
            return new VistoriaEntradaVO { KmRegistrado = 100, NivelCombustivel = 0.75m, Observacoes = observacoes };
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("   ", null)]
        [InlineData("  Bom estado  ", "Bom estado")]
        public async Task CriaEntradaComDadosServidorENormalizaObservacoes(string? observacoes, string? esperado)
        {
            FakeVistoriaRepository repositorio = new FakeVistoriaRepository();
            VistoriaService servico = CriarServico(repositorio);
            ReturnVistoriaVO retorno = await servico.RegistrarEntradaAsync(10, Entrada(observacoes), 20);
            Vistoria salva = Assert.Single(repositorio.Vistorias);
            Assert.Equal(10, retorno.IdLocacao);
            Assert.Equal(20, retorno.IdFuncionario);
            Assert.Equal(TipoVistoria.ENTRADA, retorno.Tipo);
            Assert.Equal("17/11/2026 10:30", retorno.DataHoraStr);
            Assert.Equal(100, retorno.KmRegistrado);
            Assert.Equal(0.75m, retorno.NivelCombustivel);
            Assert.Equal(esperado, retorno.Observacoes);
            Assert.Equal(Agora, salva.DataHora);
            Assert.Equal(Agora, salva.CreatedAt);
            Assert.Equal(Agora, salva.UpdatedAt);
            Assert.Null(salva.DisabledAt);
            Assert.False(repositorio.LeituraSemTrava);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public async Task LocacaoAusenteOutraEmpresaOuDesativadaLanca404SemGravar(bool existe, bool desativada)
        {
            FakeVistoriaRepository repositorio = new FakeVistoriaRepository();
            FakeLogService log = new FakeLogService();
            VistoriaService servico = CriarServico(repositorio, locacaoExiste: existe, locacaoDesativada: desativada, log: log);
            await Assert.ThrowsAsync<NotFoundException>(() => servico.RegistrarEntradaAsync(10, Entrada(), 20));
            await Assert.ThrowsAsync<NotFoundException>(() => servico.GetEntradaAsync(10));
            Assert.Empty(repositorio.Vistorias);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task FuncionarioInativoLanca409SemGravar()
        {
            FakeVistoriaRepository repositorio = new FakeVistoriaRepository();
            VistoriaService servico = CriarServico(repositorio, funcionarioAtivo: false);
            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => servico.RegistrarEntradaAsync(10, Entrada(), 20));
            Assert.Equal(ConstantsMessage.ErroVistoriaFuncionarioInativo, erro.Message);
            Assert.Empty(repositorio.Vistorias);
        }

        [Fact]
        public async Task DuplicidadeRejeitadaDentroDaTrava()
        {
            FakeVistoriaRepository repositorio = new FakeVistoriaRepository();
            VistoriaService servico = CriarServico(repositorio);
            await servico.RegistrarEntradaAsync(10, Entrada(), 20);
            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => servico.RegistrarEntradaAsync(10, Entrada(), 20));
            Assert.Equal(ConstantsMessage.ErroVistoriaEntradaDuplicada, erro.Message);
            Assert.Single(repositorio.Vistorias);
            Assert.False(repositorio.LeituraSemTrava);
        }

        [Fact]
        public async Task ChamadasConcorrentesSerializadasRevalidamDuplicidade()
        {
            FakeVistoriaRepository repositorio = new FakeVistoriaRepository();
            VistoriaService servico = CriarServico(repositorio);
            Task<ReturnVistoriaVO> primeira = servico.RegistrarEntradaAsync(10, Entrada(), 20);
            Task<ReturnVistoriaVO> segunda = servico.RegistrarEntradaAsync(10, Entrada(), 20);
            await Assert.ThrowsAsync<BusinessException>(() => Task.WhenAll(primeira, segunda));
            Assert.Equal(1, new[] { primeira, segunda }.Count(tarefa => tarefa.IsCompletedSuccessfully));
            Assert.Single(repositorio.Vistorias);
        }

        [Fact]
        public async Task DesativadaOuSaidaNaoImpedeEntradaEConsultaDevolveSomenteEntradaAtiva()
        {
            FakeVistoriaRepository repositorio = new FakeVistoriaRepository();
            repositorio.Vistorias.Add(new Vistoria { IdLocacao = 10, Tipo = TipoVistoria.ENTRADA, DisabledAt = Agora });
            repositorio.Vistorias.Add(new Vistoria { IdLocacao = 10, Tipo = TipoVistoria.SAIDA });
            VistoriaService servico = CriarServico(repositorio);
            await Assert.ThrowsAsync<NotFoundException>(() => servico.GetEntradaAsync(10));
            ReturnVistoriaVO criada = await servico.RegistrarEntradaAsync(10, Entrada(), 20);
            ReturnVistoriaVO lida = await servico.GetEntradaAsync(10);
            Assert.Equal(criada.IdVistoria, lida.IdVistoria);
            Assert.Equal(TipoVistoria.ENTRADA, lida.Tipo);
        }
    }
}
