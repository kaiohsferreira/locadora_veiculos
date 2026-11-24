using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Profiles;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.DocumentosCliente;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;

namespace AutoVia.Tests.Locacoes
{
    public class LocacaoServiceTests
    {
        private static readonly DateTime AgoraDoRelogio = new DateTime(2026, 11, 19, 9, 30, 0);

        [Fact]
        public async Task IniciarReservaValida_CriaLocacaoEAtualizaEstados()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioValido();
            LocacaoService servico = CriarServico(repositorio);

            ReturnLocacaoVO retorno = await servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20);

            Reserva reserva = repositorio.ObterReserva(10)!;
            Veiculo veiculo = repositorio.ObterVeiculo(10)!;
            Assert.True(retorno.IdLocacao > 0);
            Assert.Equal(10, retorno.IdReserva);
            Assert.Equal(30, retorno.IdCliente);
            Assert.Equal(40, retorno.IdVeiculo);
            Assert.Equal(20, retorno.IdFuncionario);
            Assert.Equal(1200, retorno.KmSaida);
            Assert.Equal(175m, retorno.ValorDiaria);
            Assert.Equal(0m, retorno.ValorCaucao);
            Assert.Equal(StatusLocacao.EM_ANDAMENTO, retorno.Status);
            Assert.Equal(new DateTime(2026, 11, 20, 10, 0, 0), retorno.DataDevolucaoPrevista);
            Assert.Equal(StatusReserva.CONCLUIDA, reserva.Status);
            Assert.Equal(StatusVeiculo.LOCADO, veiculo.Status);
        }

        [Fact]
        public async Task IniciarReserva_DataDeRetiradaVemDoRelogioEIrParaORepositorio()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioValido();
            LocacaoService servico = CriarServico(repositorio);

            ReturnLocacaoVO retorno = await servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20);

            Assert.Equal(AgoraDoRelogio, retorno.DataRetirada);
            Assert.Equal(AgoraDoRelogio, repositorio.UltimaDataRetirada);
            Assert.Equal("19/11/2026 09:30", retorno.DataRetiradaStr);
            Assert.Equal("20/11/2026", retorno.DataDevolucaoPrevistaStr);
            Assert.Equal(StaticMethods.FormateToMonetary(175m), retorno.ValorDiariaStr);
            Assert.Equal(StaticMethods.FormateToMonetary(0m), retorno.ValorCaucaoStr);
        }

        [Fact]
        public async Task ReservaInexistente_LancaNotFound()
        {
            LocacaoService servico = CriarServico(new FakeLocacaoRepository());

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 99 }, 20));

            Assert.Equal(ConstantsMessage.ErroReservaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task ReservaDeOutraLocadora_DevolveNuloNoRepositorio_LancaNotFoundSemAbrirLocacao()
        {
            // O filtro "Empresa" faz a reserva de outra locadora sumir da leitura: o repositorio devolve nulo.
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            LocacaoService servico = CriarServico(repositorio);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20));

            Assert.Equal(ConstantsMessage.ErroReservaNaoEncontrada, excecao.Message);
            Assert.Equal(0, repositorio.ChamadasStartFromReserva);
        }

        [Theory]
        [InlineData(StatusReserva.CANCELADA)]
        [InlineData(StatusReserva.CONCLUIDA)]
        public async Task ReservaEmEstadoNaoElegivel_LancaConflito(StatusReserva status)
        {
            FakeLocacaoRepository repositorio = CriarRepositorioValido(statusReserva: status);
            LocacaoService servico = CriarServico(repositorio);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20));

            Assert.Equal(ConstantsMessage.ErroLocacaoReservaEstadoInvalido, excecao.Message);
        }

        [Fact]
        public async Task VeiculoQueNaoEstaReservado_LancaConflito()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioValido(statusVeiculo: StatusVeiculo.DISPONIVEL);
            LocacaoService servico = CriarServico(repositorio);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20));

            Assert.Equal(ConstantsMessage.ErroLocacaoVeiculoIndisponivel, excecao.Message);
        }

        [Fact]
        public async Task ReservaComLocacaoAtiva_LancaConflito()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioValido();
            repositorio.Adicionar(new Locacao { IdReserva = 10, DisabledAt = null });
            LocacaoService servico = CriarServico(repositorio);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20));

            Assert.Equal(ConstantsMessage.ErroLocacaoReservaDuplicada, excecao.Message);
        }

        [Fact]
        public async Task FuncionarioInativo_LancaConflito()
        {
            FakeLocacaoRepository repositorio = CriarRepositorioValido(funcionarioAtivo: false);
            LocacaoService servico = CriarServico(repositorio);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20));

            Assert.Equal(ConstantsMessage.ErroLocacaoFuncionarioNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task ReservaInexistente_RegistraAoMenosUmLogDeErro()
        {
            // Log do Kaio (16/11): o 404 do servico deixa registro. Sem exigir log vazio em 4xx.
            FakeLogService log = new FakeLogService();
            LocacaoService servico = CriarServico(new FakeLocacaoRepository(), log);

            await Assert.ThrowsAsync<NotFoundException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 99 }, 20));

            Assert.NotEmpty(log.Entries);
        }

        [Fact]
        public async Task ReservaConcluida_RegistraAoMenosUmLogDeErro()
        {
            // Log do Kaio (16/11): o 409 do servico deixa registro. Sem exigir log vazio em 4xx.
            FakeLogService log = new FakeLogService();
            FakeLocacaoRepository repositorio = CriarRepositorioValido(statusReserva: StatusReserva.CONCLUIDA);
            LocacaoService servico = CriarServico(repositorio, log);

            await Assert.ThrowsAsync<BusinessException>(
                () => servico.StartFromReservaAsync(new LocacaoVO { IdReserva = 10 }, 20));

            Assert.NotEmpty(log.Entries);
        }

        private static FakeLocacaoRepository CriarRepositorioValido(
            StatusReserva statusReserva = StatusReserva.ATIVA,
            StatusVeiculo statusVeiculo = StatusVeiculo.RESERVADO,
            bool funcionarioAtivo = true)
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 40,
                KmAtual = 1200,
                ValorDiaria = 175m,
                Status = statusVeiculo
            };
            Reserva reserva = new Reserva
            {
                IdReserva = 10,
                IdCliente = 30,
                IdVeiculo = 40,
                DataRetiradaPrevista = new DateTime(2026, 11, 19, 10, 0, 0),
                DataDevolucaoPrevista = new DateTime(2026, 11, 20, 10, 0, 0),
                Status = statusReserva,
                Veiculo = veiculo
            };
            Funcionario funcionario = new Funcionario
            {
                IdFuncionario = 20,
                Ativo = funcionarioAtivo
            };

            repositorio.Adicionar(reserva);
            repositorio.Adicionar(funcionario);

            return repositorio;
        }

        [Theory]
        [InlineData(-1, true)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        public async Task RetiradaCnhVencida_SegueORelogioDoServico(int diasAteVencer, bool esperado)
        {
            DateTime agora = new DateTime(2026, 11, 24, 10, 0, 0);
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            repositorio.Adicionar(CriarReservaParaRetirada(cnhValidade: DateOnly.FromDateTime(agora).AddDays(diasAteVencer)));
            LocacaoService servico = CriarServicoRetirada(repositorio, new FakeRelogio(agora), new FakeClienteDocumentoRepository(), new FakeArmazenamentoGateway(), new FakeLogService());

            ReturnRetiradaVO retorno = await servico.GetRetiradaAsync(10);

            Assert.Equal(esperado, retorno.CnhVencida);
        }

        [Fact]
        public async Task RetiradaSemDocumentoEnviado_DatasDeEnvioNulasEStrVazios()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            repositorio.Adicionar(CriarReservaParaRetirada());
            LocacaoService servico = CriarServicoRetirada(repositorio, new FakeRelogio(AgoraDoRelogio), new FakeClienteDocumentoRepository(), new FakeArmazenamentoGateway(), new FakeLogService());

            ReturnRetiradaVO retorno = await servico.GetRetiradaAsync(10);

            Assert.Null(retorno.CnhEnviadaEm);
            Assert.Equal(string.Empty, retorno.CnhEnviadaEmStr);
            Assert.Null(retorno.ComprovanteResidenciaEnviadoEm);
            Assert.Equal(string.Empty, retorno.ComprovanteResidenciaEnviadoEmStr);
        }

        [Fact]
        public async Task RetiradaSoComCnhEnviada_DataDaCnhEComprovanteVazio()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            repositorio.Adicionar(CriarReservaParaRetirada());
            FakeClienteDocumentoRepository documentos = new FakeClienteDocumentoRepository();
            documentos.Semear(CriarDocumento(1, TipoDocumentoCliente.CNH, new DateTime(2026, 11, 10, 14, 20, 0)));
            LocacaoService servico = CriarServicoRetirada(repositorio, new FakeRelogio(AgoraDoRelogio), documentos, new FakeArmazenamentoGateway(), new FakeLogService());

            ReturnRetiradaVO retorno = await servico.GetRetiradaAsync(10);

            Assert.Equal(new DateTime(2026, 11, 10, 14, 20, 0), retorno.CnhEnviadaEm);
            Assert.Equal("10/11/2026 14:20", retorno.CnhEnviadaEmStr);
            Assert.Null(retorno.ComprovanteResidenciaEnviadoEm);
            Assert.Equal(string.Empty, retorno.ComprovanteResidenciaEnviadoEmStr);
        }

        [Fact]
        public async Task RetiradaComCnhEComprovante_DatasDosDois()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            repositorio.Adicionar(CriarReservaParaRetirada());
            FakeClienteDocumentoRepository documentos = new FakeClienteDocumentoRepository();
            documentos.Semear(CriarDocumento(1, TipoDocumentoCliente.CNH, new DateTime(2026, 11, 10, 14, 20, 0)));
            documentos.Semear(CriarDocumento(2, TipoDocumentoCliente.COMPROVANTE_RESIDENCIA, new DateTime(2026, 11, 12, 8, 5, 0)));
            LocacaoService servico = CriarServicoRetirada(repositorio, new FakeRelogio(AgoraDoRelogio), documentos, new FakeArmazenamentoGateway(), new FakeLogService());

            ReturnRetiradaVO retorno = await servico.GetRetiradaAsync(10);

            Assert.Equal("10/11/2026 14:20", retorno.CnhEnviadaEmStr);
            Assert.Equal(new DateTime(2026, 11, 12, 8, 5, 0), retorno.ComprovanteResidenciaEnviadoEm);
            Assert.Equal("12/11/2026 08:05", retorno.ComprovanteResidenciaEnviadoEmStr);
        }

        [Fact]
        public async Task RetiradaDocumentoDesativado_EIgnorado()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            repositorio.Adicionar(CriarReservaParaRetirada());
            FakeClienteDocumentoRepository documentos = new FakeClienteDocumentoRepository();
            ClienteDocumento desativado = CriarDocumento(1, TipoDocumentoCliente.CNH, new DateTime(2026, 11, 10, 14, 20, 0));
            desativado.Disable();
            documentos.Semear(desativado);
            LocacaoService servico = CriarServicoRetirada(repositorio, new FakeRelogio(AgoraDoRelogio), documentos, new FakeArmazenamentoGateway(), new FakeLogService());

            ReturnRetiradaVO retorno = await servico.GetRetiradaAsync(10);

            Assert.Null(retorno.CnhEnviadaEm);
            Assert.Equal(string.Empty, retorno.CnhEnviadaEmStr);
        }

        [Fact]
        public async Task RetiradaDoisAtivosDoMesmoTipo_ValeOMaiorId()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            repositorio.Adicionar(CriarReservaParaRetirada());
            FakeClienteDocumentoRepository documentos = new FakeClienteDocumentoRepository();
            documentos.Semear(CriarDocumento(1, TipoDocumentoCliente.CNH, new DateTime(2026, 11, 1, 9, 0, 0)));
            documentos.Semear(CriarDocumento(2, TipoDocumentoCliente.CNH, new DateTime(2026, 11, 15, 9, 0, 0)));
            LocacaoService servico = CriarServicoRetirada(repositorio, new FakeRelogio(AgoraDoRelogio), documentos, new FakeArmazenamentoGateway(), new FakeLogService());

            ReturnRetiradaVO retorno = await servico.GetRetiradaAsync(10);

            Assert.Equal("15/11/2026 09:00", retorno.CnhEnviadaEmStr);
        }

        [Fact]
        public async Task RetiradaVeiculoSemFoto_CapaNulaESemChamadaAoArmazenamento()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            repositorio.Adicionar(CriarReservaParaRetirada());
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway();
            LocacaoService servico = CriarServicoRetirada(repositorio, new FakeRelogio(AgoraDoRelogio), new FakeClienteDocumentoRepository(), armazenamento, new FakeLogService());

            ReturnRetiradaVO retorno = await servico.GetRetiradaAsync(10);

            Assert.Null(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
            Assert.Equal(0, armazenamento.AssinaturaLoteCount);
        }

        [Fact]
        public async Task RetiradaVeiculoComFoto_CapaPreenchida()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            repositorio.Adicionar(CriarReservaParaRetirada(comFoto: true));
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway();
            LocacaoService servico = CriarServicoRetirada(repositorio, new FakeRelogio(AgoraDoRelogio), new FakeClienteDocumentoRepository(), armazenamento, new FakeLogService());

            ReturnRetiradaVO retorno = await servico.GetRetiradaAsync(10);

            Assert.NotNull(retorno.FotoCapa);
            Assert.Null(retorno.ErroFotoCapa);
            Assert.Equal(1, armazenamento.AssinaturaLoteCount);
        }

        [Fact]
        public async Task RetiradaArmazenamentoFalhando_RespondeComErroFotoCapaSemDerrubar()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            repositorio.Adicionar(CriarReservaParaRetirada(comFoto: true));
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway { FalharAssinatura = true };
            LocacaoService servico = CriarServicoRetirada(repositorio, new FakeRelogio(AgoraDoRelogio), new FakeClienteDocumentoRepository(), armazenamento, new FakeLogService());

            ReturnRetiradaVO retorno = await servico.GetRetiradaAsync(10);

            Assert.Null(retorno.FotoCapa);
            Assert.Equal(ConstantsMessage.ErroFotoCapaIndisponivel, retorno.ErroFotoCapa);
        }

        [Fact]
        public async Task RetiradaDeOutraLocadora_LancaNotFound_SemLerDocumentosNemArmazenamento()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            repositorio.Adicionar(CriarReservaParaRetirada(idEmpresaVeiculo: 2, comFoto: true));
            FakeClienteDocumentoRepository documentos = new FakeClienteDocumentoRepository();
            documentos.Semear(CriarDocumento(1, TipoDocumentoCliente.CNH, new DateTime(2026, 11, 10, 14, 20, 0)));
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway();
            LocacaoService servico = CriarServicoRetirada(repositorio, new FakeRelogio(AgoraDoRelogio), documentos, armazenamento, new FakeLogService());

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.GetRetiradaAsync(10));

            Assert.Equal(ConstantsMessage.ErroReservaNaoEncontrada, excecao.Message);
            Assert.Equal(0, documentos.ConsultasAtivosDoCliente);
            Assert.Equal(0, armazenamento.AssinaturaLoteCount);
        }

        private static Reserva CriarReservaParaRetirada(int idEmpresaVeiculo = 1, DateOnly? cnhValidade = null, bool comFoto = false)
        {
            Cliente cliente = new Cliente
            {
                IdCliente = 30, Nome = "Maria da Silva", Cpf = "12345678909", CnhNumero = "12345678901",
                CnhCategoria = CategoriaCnh.B, CnhValidade = cnhValidade ?? new DateOnly(2030, 1, 1)
            };
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 40, IdEmpresa = idEmpresaVeiculo, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Toro", AnoModelo = 2024,
                KmAtual = 1500, ValorDiaria = 200m, Status = StatusVeiculo.RESERVADO, IdCategoria = 3,
                Categoria = new Categoria { IdCategoria = 3, Nome = "SUV" },
                Empresa = new Empresa { IdEmpresa = 1, Ativo = true, NomeFantasia = "Locadora Teste", Logradouro = "Rua das Flores", Numero = "100", Bairro = "Centro", Cidade = "Campinas", Uf = "SP" }
            };
            if (comFoto)
            {
                veiculo.Fotos.Add(new VeiculoFoto { IdVeiculoFoto = 1, IdVeiculo = 40, Ordem = 1, ChaveObjeto = "veiculos/40/capa.jpg", IdFuncionarioEnvio = 20 });
            }

            return new Reserva
            {
                IdReserva = 10, IdCliente = 30, Cliente = cliente, IdVeiculo = 40, Veiculo = veiculo,
                DataRetiradaPrevista = new DateTime(2026, 11, 18, 9, 30, 0), DataDevolucaoPrevista = new DateTime(2026, 11, 19, 9, 30, 0),
                ValorEstimado = 200m, Status = StatusReserva.CONFIRMADA
            };
        }

        private static ClienteDocumento CriarDocumento(int id, TipoDocumentoCliente tipo, DateTime criadoEm)
        {
            return new ClienteDocumento
            {
                IdClienteDocumento = id, IdCliente = 30, Tipo = tipo, ChaveObjeto = "clientes/30/" + id,
                NomeArquivo = "documento.pdf", TipoConteudo = "application/pdf", TamanhoBytes = 10, CreatedAt = criadoEm, UpdatedAt = criadoEm
            };
        }

        private static LocacaoService CriarServicoRetirada(FakeLocacaoRepository repositorio, IRelogio relogio, FakeClienteDocumentoRepository documentos, FakeArmazenamentoGateway armazenamento, FakeLogService log)
        {
            MapperConfiguration configuracao = new MapperConfiguration(
                cfg => cfg.AddProfile<LocacaoProfile>(),
                NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();

            return new LocacaoService(repositorio, relogio, mapper, log, ContextoEmpresaDeTeste.Criar(), new FotoCapaVeiculoService(armazenamento, log), documentos, CriarCaucaoSemValor(repositorio, log));
        }

        /// <summary>Caucao com Pagamentos:ValorCaucao 0 (sem caucao): a abertura e a do RF-17 e o gateway nunca e chamado.</summary>
        private static CaucaoRetiradaService CriarCaucaoSemValor(FakeLocacaoRepository repositorio, FakeLogService log)
        {
            return new CaucaoRetiradaService(new FakeCaucaoGateway(), repositorio, new CaucaoRetiradaOptions(0m), log);
        }

        private static LocacaoService CriarServico(FakeLocacaoRepository repositorio, FakeLogService? logService = null)
        {
            MapperConfiguration configuracao = new MapperConfiguration(
                cfg => cfg.AddProfile<LocacaoProfile>(),
                NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            FakeLogService log = logService ?? new FakeLogService();
            FakeRelogio relogio = new FakeRelogio(AgoraDoRelogio);
            LocacaoService servico = new LocacaoService(repositorio, relogio, mapper, log, ContextoEmpresaDeTeste.Criar(), new FotoCapaVeiculoService(new FakeArmazenamentoGateway(), log), new FakeClienteDocumentoRepository(), CriarCaucaoSemValor(repositorio, log));

            return servico;
        }
    }
}
