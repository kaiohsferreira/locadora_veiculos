using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Manutencao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Testes do ManutencaoService com o repositorio em memoria (que aplica as mesmas regras do real). Confere: abertura
    /// (veiculo em MANUTENCAO, reserva, locacao, ordem aberta, veiculo inexistente, baixado, km registrado, data de entrada
    /// padrao e futura, tipo pelo nome), encerramento (certo, ja encerrada, data anterior, data futura, km menor, veiculo
    /// fora de MANUTENCAO, km do veiculo atualizado), alertas (lista, total, ordem e base) e o log informativo so do certo.
    /// </summary>
    public class ManutencaoServiceTests
    {
        private const int IdEmpresaDoContexto = 7;
        private const int IdEmpresaAlheia = 9;
        private const int IdFuncionarioDoContexto = 1;
        private const string PlacaDoTeste = "ABC1D23";
        private const string ChassiDoTeste = "9BWZZZ377VT004251";
        private const string RenavamDoTeste = "12345678901";
        private const string TipoPreventiva = "PREVENTIVA";
        private const string TipoCorretiva = "CORRETIVA";
        private const string OficinaDoTeste = "  Oficina Centro  ";
        private const int KmAtualDoVeiculo = 4500;
        private const int IdVeiculoAusente = 77;

        private static readonly DateTime MomentoDeCriacao = new DateTime(2020, 1, 1, 10, 0, 0);
        private static readonly DateOnly HojeDoTeste = new DateOnly(2020, 1, 1);

        private static ManutencaoService CriarServico(FakeManutencaoRepository repositorio, FakeLogService log, DateOnly hoje)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<ManutencaoProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            FakeRelogio relogio = new FakeRelogio(hoje.ToDateTime(new TimeOnly(10, 0)));

            return new ManutencaoService(repositorio, mapper, relogio, ContextoEmpresaDeTeste.Criar(IdEmpresaDoContexto), log);
        }

        private static Veiculo SemearVeiculo(FakeManutencaoRepository repositorio, StatusVeiculo status, int idEmpresa = IdEmpresaDoContexto, int km = KmAtualDoVeiculo)
        {
            Veiculo veiculo = new Veiculo
            {
                IdCategoria = 1,
                IdEmpresa = idEmpresa,
                Placa = PlacaDoTeste,
                Chassi = ChassiDoTeste,
                Renavam = RenavamDoTeste,
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                KmAtual = km,
                ValorDiaria = 150m,
                Status = status,
                CreatedAt = MomentoDeCriacao,
                UpdatedAt = MomentoDeCriacao
            };

            return repositorio.SemearVeiculo(veiculo);
        }

        private static Manutencao SemearOrdem(FakeManutencaoRepository repositorio, Veiculo veiculo, DateOnly dataEntrada, DateOnly? dataSaida, TipoManutencao tipo = TipoManutencao.CORRETIVA, int km = KmAtualDoVeiculo)
        {
            Manutencao ordem = new Manutencao
            {
                IdVeiculo = veiculo.IdVeiculo,
                Veiculo = veiculo,
                Tipo = tipo,
                DataEntrada = dataEntrada,
                DataSaida = dataSaida,
                KmRegistrado = km,
                CreatedAt = MomentoDeCriacao,
                UpdatedAt = MomentoDeCriacao
            };

            return repositorio.SemearManutencao(ordem);
        }

        private static AbrirManutencaoVO Abertura(int idVeiculo, string? tipo = TipoCorretiva, DateOnly? dataEntrada = null)
        {
            return new AbrirManutencaoVO { IdVeiculo = idVeiculo, Tipo = tipo, Oficina = OficinaDoTeste, Descricao = "  Troca de oleo  ", DataEntrada = dataEntrada };
        }

        [Fact]
        public async Task Abrir_VeiculoDisponivel_GravaOrdemComKmDoVeiculoEColocaEmManutencao()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.DISPONIVEL);
            FakeLogService log = new FakeLogService();
            ManutencaoService servico = CriarServico(repositorio, log, HojeDoTeste);

            ReturnManutencaoVO retorno = await servico.AbrirAsync(Abertura(veiculo.IdVeiculo, TipoPreventiva));

            Assert.Equal(StatusVeiculo.MANUTENCAO, Assert.Single(repositorio.Veiculos).Status);
            Manutencao ordem = Assert.Single(repositorio.Manutencoes);
            Assert.Equal(KmAtualDoVeiculo, ordem.KmRegistrado);
            Assert.Equal(TipoManutencao.PREVENTIVA, ordem.Tipo);
            Assert.Equal(HojeDoTeste, ordem.DataEntrada);
            Assert.Equal("Oficina Centro", ordem.Oficina);
            Assert.Equal("Troca de oleo", ordem.Descricao);
            Assert.True(retorno.Aberta);
            Assert.Equal(PlacaDoTeste, retorno.Placa);
            Assert.Equal(KmAtualDoVeiculo, retorno.KmRegistrado);
            Assert.Equal(TipoManutencao.PREVENTIVA, retorno.Tipo);
        }

        [Fact]
        public async Task Abrir_GravaUmaLinhaInformativaSemPlacaChassiNemRenavam()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.DISPONIVEL);
            FakeLogService log = new FakeLogService();
            ManutencaoService servico = CriarServico(repositorio, log, HojeDoTeste);

            ReturnManutencaoVO retorno = await servico.AbrirAsync(Abertura(veiculo.IdVeiculo));

            string esperado = string.Format(ConstantsMessage.InfoManutencaoAberta, retorno.IdManutencao, veiculo.IdVeiculo, IdFuncionarioDoContexto);
            Assert.Equal(new List<string> { esperado }, log.Snapshot());
            Assert.DoesNotContain(PlacaDoTeste, esperado);
            Assert.DoesNotContain(ChassiDoTeste, esperado);
            Assert.DoesNotContain(RenavamDoTeste, esperado);
        }

        [Fact]
        public async Task Abrir_VeiculoJaEmManutencao_SegueSemTrocarOStatus()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.MANUTENCAO);
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            ReturnManutencaoVO retorno = await servico.AbrirAsync(Abertura(veiculo.IdVeiculo));

            Assert.Equal(StatusVeiculo.MANUTENCAO, Assert.Single(repositorio.Veiculos).Status);
            Assert.Equal(MomentoDeCriacao, Assert.Single(repositorio.Veiculos).UpdatedAt);
            Assert.True(retorno.Aberta);
        }

        [Fact]
        public async Task Abrir_ComReservaEmAberto_Recusa409ESemOrdem()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.DISPONIVEL);
            repositorio.ReservasDoVeiculo.Add(new Reserva { IdVeiculo = veiculo.IdVeiculo, Status = StatusReserva.CONFIRMADA });
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.AbrirAsync(Abertura(veiculo.IdVeiculo)));

            Assert.Equal(ConstantsMessage.ErroVeiculoStatusComReservaEmAberto, excecao.Message);
            Assert.Empty(repositorio.Manutencoes);
            Assert.Equal(StatusVeiculo.DISPONIVEL, Assert.Single(repositorio.Veiculos).Status);
        }

        [Fact]
        public async Task Abrir_ComLocacaoEmAberto_Recusa409()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.LOCADO);
            repositorio.LocacoesDoVeiculo.Add(new Locacao { IdVeiculo = veiculo.IdVeiculo, Status = StatusLocacao.ATRASADA });
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.AbrirAsync(Abertura(veiculo.IdVeiculo)));

            Assert.Equal(ConstantsMessage.ErroVeiculoStatusComLocacaoEmAberto, excecao.Message);
            Assert.Empty(repositorio.Manutencoes);
        }

        [Fact]
        public async Task Abrir_ComOrdemJaAberta_Recusa409ComMensagemPropria()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.MANUTENCAO);
            SemearOrdem(repositorio, veiculo, HojeDoTeste, null);
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.AbrirAsync(Abertura(veiculo.IdVeiculo)));

            Assert.Equal(ConstantsMessage.ErroManutencaoJaAberta, excecao.Message);
            Assert.Single(repositorio.Manutencoes);
        }

        [Fact]
        public async Task Abrir_VeiculoInexistenteOuDeOutraLocadora_Relanca404SemLog()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo alheio = SemearVeiculo(repositorio, StatusVeiculo.DISPONIVEL, IdEmpresaAlheia);
            FakeLogService log = new FakeLogService();
            ManutencaoService servico = CriarServico(repositorio, log, HojeDoTeste);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.AbrirAsync(Abertura(alheio.IdVeiculo)));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async Task Abrir_VeiculoBaixado_Recusa409VendidoOuBaixado()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.BAIXADO);
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.AbrirAsync(Abertura(veiculo.IdVeiculo)));

            Assert.Equal(ConstantsMessage.ErroVeiculoStatusVendidoOuBaixado, excecao.Message);
        }

        [Fact]
        public async Task Abrir_DataDeEntradaFutura_Recusa400()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.DISPONIVEL);
            FakeLogService log = new FakeLogService();
            ManutencaoService servico = CriarServico(repositorio, log, HojeDoTeste);

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => servico.AbrirAsync(Abertura(veiculo.IdVeiculo, TipoCorretiva, HojeDoTeste.AddDays(1))));

            Assert.Equal(ConstantsMessage.ErroManutencaoDataEntradaFutura, excecao.Errors.First().ErrorMessage);
            Assert.Empty(repositorio.Manutencoes);
            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async Task Abrir_TipoQueNaoEUmNome_Recusa400()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.DISPONIVEL);
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => servico.AbrirAsync(Abertura(veiculo.IdVeiculo, "1")));

            Assert.Equal(ConstantsMessage.ErroManutencaoTipoInvalido, excecao.Errors.First().ErrorMessage);
        }

        [Fact]
        public async Task Abrir_SemDataDeEntrada_UsaHojeDoRelogio()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.DISPONIVEL);
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            ReturnManutencaoVO retorno = await servico.AbrirAsync(Abertura(veiculo.IdVeiculo, TipoCorretiva, null));

            Assert.Equal(HojeDoTeste, retorno.DataEntrada);
        }

        [Fact]
        public async Task Encerrar_OrdemAbertaComVeiculoEmManutencao_GravaSaidaCustoKmEDevolveADisponivel()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.MANUTENCAO);
            Manutencao ordem = SemearOrdem(repositorio, veiculo, HojeDoTeste.AddDays(-5), null);
            FakeLogService log = new FakeLogService();
            ManutencaoService servico = CriarServico(repositorio, log, HojeDoTeste);

            ReturnManutencaoVO retorno = await servico.EncerrarAsync(ordem.IdManutencao, new EncerrarManutencaoVO { KmAtual = 5000, DataSaida = HojeDoTeste, Custo = 350.75m });

            Assert.False(retorno.Aberta);
            Assert.Equal(HojeDoTeste, retorno.DataSaida);
            Assert.Equal(350.75m, retorno.Custo);
            Assert.Equal(KmAtualDoVeiculo, retorno.KmRegistrado);
            Assert.Equal(StatusVeiculo.DISPONIVEL, veiculo.Status);
            Assert.Equal(5000, veiculo.KmAtual);
            Assert.Equal(KmAtualDoVeiculo, ordem.KmRegistrado);
            string esperado = string.Format(ConstantsMessage.InfoManutencaoEncerrada, ordem.IdManutencao, veiculo.IdVeiculo, IdFuncionarioDoContexto);
            Assert.Equal(new List<string> { esperado }, log.Snapshot());
        }

        [Fact]
        public async Task Encerrar_SemDataNemCusto_UsaHojeEDeixaOCustoNulo()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.MANUTENCAO);
            Manutencao ordem = SemearOrdem(repositorio, veiculo, HojeDoTeste.AddDays(-2), null);
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            ReturnManutencaoVO retorno = await servico.EncerrarAsync(ordem.IdManutencao, new EncerrarManutencaoVO { KmAtual = 4600 });

            Assert.Equal(HojeDoTeste, retorno.DataSaida);
            Assert.Null(retorno.Custo);
            Assert.Equal(string.Empty, retorno.CustoStr);
        }

        [Fact]
        public async Task Encerrar_JaEncerrada_Recusa409()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.DISPONIVEL);
            Manutencao ordem = SemearOrdem(repositorio, veiculo, HojeDoTeste.AddDays(-5), HojeDoTeste.AddDays(-1));
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.EncerrarAsync(ordem.IdManutencao, new EncerrarManutencaoVO { KmAtual = 5000 }));

            Assert.Equal(ConstantsMessage.ErroManutencaoJaEncerrada, excecao.Message);
        }

        [Fact]
        public async Task Encerrar_DataDeSaidaAnteriorAEntrada_Recusa400()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.MANUTENCAO);
            Manutencao ordem = SemearOrdem(repositorio, veiculo, HojeDoTeste.AddDays(-5), null);
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => servico.EncerrarAsync(ordem.IdManutencao, new EncerrarManutencaoVO { KmAtual = 5000, DataSaida = HojeDoTeste.AddDays(-6) }));

            Assert.Equal(ConstantsMessage.ErroManutencaoDataSaidaAnterior, excecao.Errors.First().ErrorMessage);
            Assert.Null(ordem.DataSaida);
            Assert.Equal(StatusVeiculo.MANUTENCAO, veiculo.Status);
        }

        [Fact]
        public async Task Encerrar_DataDeSaidaFutura_Recusa400()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.MANUTENCAO);
            Manutencao ordem = SemearOrdem(repositorio, veiculo, HojeDoTeste.AddDays(-5), null);
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => servico.EncerrarAsync(ordem.IdManutencao, new EncerrarManutencaoVO { KmAtual = 5000, DataSaida = HojeDoTeste.AddDays(1) }));

            Assert.Equal(ConstantsMessage.ErroManutencaoDataSaidaFutura, excecao.Errors.First().ErrorMessage);
        }

        [Fact]
        public async Task Encerrar_KmMenorQueOAtualDoVeiculo_Recusa400ComOKmAtual()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.MANUTENCAO, km: 6000);
            Manutencao ordem = SemearOrdem(repositorio, veiculo, HojeDoTeste.AddDays(-5), null);
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => servico.EncerrarAsync(ordem.IdManutencao, new EncerrarManutencaoVO { KmAtual = 5999 }));

            Assert.Equal(string.Format(ConstantsMessage.ErroManutencaoKmMenor, 6000), excecao.Errors.First().ErrorMessage);
            Assert.Equal(6000, veiculo.KmAtual);
        }

        [Fact]
        public async Task Encerrar_VeiculoForaDeManutencao_EncerraAOrdemSemMudarOStatus()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculo = SemearVeiculo(repositorio, StatusVeiculo.BAIXADO);
            Manutencao ordem = SemearOrdem(repositorio, veiculo, HojeDoTeste.AddDays(-5), null);
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            ReturnManutencaoVO retorno = await servico.EncerrarAsync(ordem.IdManutencao, new EncerrarManutencaoVO { KmAtual = 5000 });

            Assert.False(retorno.Aberta);
            Assert.Equal(StatusVeiculo.BAIXADO, veiculo.Status);
            Assert.Equal(5000, veiculo.KmAtual);
        }

        [Fact]
        public async Task Encerrar_OrdemInexistente_Relanca404SemLog()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            FakeLogService log = new FakeLogService();
            ManutencaoService servico = CriarServico(repositorio, log, HojeDoTeste);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.EncerrarAsync(999, new EncerrarManutencaoVO { KmAtual = 5000 }));

            Assert.Equal(ConstantsMessage.ErroManutencaoNaoEncontrada, excecao.Message);
            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async Task ListarAlertas_DevolveOsItensNaOrdemRecebidaComBaseProximaFaltamEPercentual()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            repositorio.ItensAlertas.Add(new VeiculoRevisaoRegistro(1, "AAA1A11", "Fiat", "Toro", "SUV", 14600, 4500));
            repositorio.ItensAlertas.Add(new VeiculoRevisaoRegistro(2, "BBB2B22", "Fiat", "Mobi", "Economico", 3000, null));
            repositorio.TotalAlertas = 2;
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            ReturnAlertasRevisaoVO retorno = await servico.ListarAlertasRevisaoAsync();

            Assert.Equal(2, retorno.Total);
            Assert.Equal(2, retorno.Itens.Count);
            ReturnAlertaRevisaoVO vencido = retorno.Itens[0];
            Assert.Equal("AAA1A11", vencido.Placa);
            Assert.Equal(4500, vencido.KmUltimaPreventiva);
            Assert.Equal(14500, vencido.KmProximaRevisao);
            Assert.Equal(-100, vencido.FaltamKm);
            Assert.Equal(ConstantsValues.SituacaoRevisaoVencida, vencido.Situacao);
            Assert.Equal(101.0m, vencido.PercentualCiclo);
            ReturnAlertaRevisaoVO proximo = retorno.Itens[1];
            Assert.Equal(0, proximo.KmUltimaPreventiva);
            Assert.Equal(10000, proximo.KmProximaRevisao);
            Assert.Equal(7000, proximo.FaltamKm);
            Assert.Equal(ConstantsValues.SituacaoRevisaoEmDia, proximo.Situacao);
        }

        [Fact]
        public async Task ListarAlertas_SemItens_DevolveTotalZeroEListaVazia()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            ReturnAlertasRevisaoVO retorno = await servico.ListarAlertasRevisaoAsync();

            Assert.Equal(0, retorno.Total);
            Assert.Empty(retorno.Itens);
        }

        [Fact]
        public async Task Encerrar_VeiculoDaOrdemAusente_Relanca404DoVeiculoSemLog()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            Veiculo veiculoAusente = new Veiculo { IdVeiculo = IdVeiculoAusente };
            Manutencao ordem = SemearOrdem(repositorio, veiculoAusente, HojeDoTeste.AddDays(-5), null);
            FakeLogService log = new FakeLogService();
            ManutencaoService servico = CriarServico(repositorio, log, HojeDoTeste);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.EncerrarAsync(ordem.IdManutencao, new EncerrarManutencaoVO { KmAtual = 5000 }));

            Assert.Equal(ConstantsMessage.ErroVeiculoNaoEncontrado, excecao.Message);
            Assert.Empty(log.Snapshot());
        }

        /// <summary>
        /// Quantidade de alertas: o servico repassa o total e os itens que o repositorio entrega. O teto da lista e aplicado na
        /// consulta (ConsultaAlertasRevisaoOrdenada), provado em ManutencaoConsultasSqlTests, e nao aqui.
        /// </summary>
        [Fact]
        public async Task ListarAlertas_DevolveOTotalEOsItensQueORepositorioEntrega()
        {
            FakeManutencaoRepository repositorio = new FakeManutencaoRepository { IdEmpresaDoContexto = IdEmpresaDoContexto };
            for (int indice = 1; indice <= 3; indice++)
            {
                repositorio.ItensAlertas.Add(new VeiculoRevisaoRegistro(indice, "AAA1A1" + indice, "Fiat", "Toro", "SUV", 14000 + indice, 4500));
            }
            repositorio.TotalAlertas = ConstantsValues.TetoAlertasRevisao + 50;
            ManutencaoService servico = CriarServico(repositorio, new FakeLogService(), HojeDoTeste);

            ReturnAlertasRevisaoVO retorno = await servico.ListarAlertasRevisaoAsync();

            Assert.Equal(ConstantsValues.TetoAlertasRevisao + 50, retorno.Total);
            Assert.Equal(3, retorno.Itens.Count);
        }
    }
}
