using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Clientes
{
    /// <summary>
    /// Testes do ClienteService: cadastro PENDENTE com senha so como hash, normalizacao da
    /// entrada, CPF e CNH unicos entre os ativos, listagem paginada por status, consulta
    /// que trata desativado como inexistente e log sem senha. Usa o repositorio em memoria,
    /// o hash de teste e o log em memoria. CPFs sinteticos com digito verificador valido.
    /// </summary>
    public class ClienteServiceTests
    {
        private const string CpfA = "123.456.789-09";
        private const string CpfASemMascara = "12345678909";
        private const string CpfBSemMascara = "98765432100";
        private const string CnhA = "12345678901";
        private const string CnhB = "98765432100";
        private const string SenhaTeste = "senha1234";

        [Fact]
        public async Task InsertValido_GravaPendenteEDevolveRetornoSemSenha()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            ReturnClienteVO retorno = await servico.InsertAsync(CriarVOValido());

            Assert.True(retorno.IdCliente > 0);
            Assert.Equal(StatusCadastroCliente.PENDENTE, retorno.StatusCadastro);
            Cliente gravado = Assert.Single(repositorio.Armazenados);
            Assert.Equal(StatusCadastroCliente.PENDENTE, gravado.StatusCadastro);
        }

        [Fact]
        public async Task Insert_NormalizaCpfTelefoneCnhCepEmailNomeEUf()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            ClienteService servico = CriarServico(repositorio, new FakeLogService());
            ClienteVO vo = CriarVOValido();
            vo.Nome = "  Maria da Silva  ";
            vo.Cpf = CpfA;
            vo.Telefone = "(11) 98765-4321";
            vo.CnhNumero = "123-456-789-01";
            vo.Email = "  Maria.Silva@EXEMPLO.com ";
            vo.Cep = "01234-567";
            vo.Uf = " sp ";

            await servico.InsertAsync(vo);

            Cliente gravado = Assert.Single(repositorio.Armazenados);
            Assert.Equal("Maria da Silva", gravado.Nome);
            Assert.Equal(CpfASemMascara, gravado.Cpf);
            Assert.Equal("11987654321", gravado.Telefone);
            Assert.Equal(CnhA, gravado.CnhNumero);
            Assert.Equal("maria.silva@exemplo.com", gravado.Email);
            Assert.Equal("01234567", gravado.Cep);
            Assert.Equal("SP", gravado.Uf);
        }

        [Fact]
        public async Task Insert_OpcionaisVaziosOuSoEspacosViramNulo()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            ClienteService servico = CriarServico(repositorio, new FakeLogService());
            ClienteVO vo = CriarVOValido();
            vo.Logradouro = "   ";
            vo.Numero = string.Empty;
            vo.Complemento = null;
            vo.Bairro = " ";
            vo.Cidade = string.Empty;
            vo.Uf = "   ";
            vo.Cep = "   ";

            await servico.InsertAsync(vo);

            Cliente gravado = Assert.Single(repositorio.Armazenados);
            Assert.Null(gravado.Logradouro);
            Assert.Null(gravado.Numero);
            Assert.Null(gravado.Complemento);
            Assert.Null(gravado.Bairro);
            Assert.Null(gravado.Cidade);
            Assert.Null(gravado.Uf);
            Assert.Null(gravado.Cep);
        }

        [Fact]
        public async Task Insert_SenhaGravadaSoComoHashEConferidaPeloHasher()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            FakeSenhaHasher hasher = new FakeSenhaHasher();
            ClienteService servico = CriarServico(repositorio, new FakeLogService(), hasher);

            await servico.InsertAsync(CriarVOValido());

            Cliente gravado = Assert.Single(repositorio.Armazenados);
            Assert.NotNull(gravado.SenhaHash);
            Assert.NotEqual(SenhaTeste, gravado.SenhaHash);
            Assert.True(hasher.Verify(SenhaTeste, gravado.SenhaHash));
        }

        [Fact]
        public async Task Insert_DesativadoNuloEDataCadastroPreenchidaComUmMesmoInstante()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            await servico.InsertAsync(CriarVOValido());

            Cliente gravado = Assert.Single(repositorio.Armazenados);
            Assert.Null(gravado.DisabledAt);
            Assert.NotEqual(default(DateTime), gravado.DataCadastro);
            Assert.Equal(gravado.DataCadastro, gravado.CreatedAt);
            Assert.Equal(gravado.DataCadastro, gravado.UpdatedAt);
        }

        [Fact]
        public async Task InsertComCpfDeClienteAtivo_LancaCpfDuplicado()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            repositorio.Adicionar(CriarClienteAtivo(CpfASemMascara, CnhB, StatusCadastroCliente.APROVADO));
            ClienteService servico = CriarServico(repositorio, new FakeLogService());
            ClienteVO vo = CriarVOValido();
            vo.Cpf = CpfA;

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroClienteCpfDuplicado, excecao.Message);
        }

        [Fact]
        public async Task InsertComCnhDeClienteAtivo_LancaCnhDuplicada()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            repositorio.Adicionar(CriarClienteAtivo(CpfBSemMascara, CnhA, StatusCadastroCliente.PENDENTE));
            ClienteService servico = CriarServico(repositorio, new FakeLogService());
            ClienteVO vo = CriarVOValido();
            vo.CnhNumero = "123-456-789-01";

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroClienteCnhDuplicada, excecao.Message);
        }

        [Fact]
        public async Task InsertComCpfECnhDuplicados_InformaCpfPrimeiro()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            repositorio.Adicionar(CriarClienteAtivo(CpfASemMascara, CnhA, StatusCadastroCliente.PENDENTE));
            ClienteService servico = CriarServico(repositorio, new FakeLogService());
            ClienteVO vo = CriarVOValido();

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroClienteCpfDuplicado, excecao.Message);
        }

        [Fact]
        public async Task InsertComCpfDeClienteDesativado_NaoBloqueia()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente desativado = CriarClienteAtivo(CpfASemMascara, CnhB, StatusCadastroCliente.PENDENTE);
            desativado.Disable();
            repositorio.Adicionar(desativado);
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            ReturnClienteVO retorno = await servico.InsertAsync(CriarVOValido());

            Assert.True(retorno.IdCliente > 0);
            Assert.NotEqual(desativado.IdCliente, retorno.IdCliente);
        }

        [Fact]
        public async Task GetAll_FiltraPorStatusContaEOrdenaPorDataCadastro()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            repositorio.Adicionar(CriarClienteComDataCadastro("Ana", "11111111111", "CNH001", StatusCadastroCliente.APROVADO, new DateTime(2026, 11, 3, 9, 0, 0)));
            repositorio.Adicionar(CriarClienteComDataCadastro("Bruno", "22222222222", "CNH002", StatusCadastroCliente.PENDENTE, new DateTime(2026, 11, 2, 9, 0, 0)));
            repositorio.Adicionar(CriarClienteComDataCadastro("Carla", "33333333333", "CNH003", StatusCadastroCliente.PENDENTE, new DateTime(2026, 11, 1, 9, 0, 0)));
            Cliente desativado = CriarClienteComDataCadastro("Diego", "44444444444", "CNH004", StatusCadastroCliente.PENDENTE, new DateTime(2026, 10, 1, 9, 0, 0));
            desativado.Disable();
            repositorio.Adicionar(desativado);
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnClienteVO> todos = await servico.GetAllAsync(new ClienteFiltroVO { Pagina = 1, TamanhoPagina = 20 });
            PaginaVO<ReturnClienteVO> pendentes = await servico.GetAllAsync(new ClienteFiltroVO { Pagina = 1, TamanhoPagina = 20, Status = StatusCadastroCliente.PENDENTE });

            Assert.Equal(3, todos.Total);
            Assert.Equal(new List<string> { "Carla", "Bruno", "Ana" }, todos.Itens.Select(x => x.Nome).ToList());
            Assert.Equal(2, pendentes.Total);
            Assert.Equal(new List<string> { "Carla", "Bruno" }, pendentes.Itens.Select(x => x.Nome).ToList());
        }

        [Fact]
        public async Task GetAll_PaginaEOrdemDeDesempateMantemTotalCorreto()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            DateTime mesmaData = new DateTime(2026, 11, 4, 8, 0, 0);
            repositorio.Adicionar(CriarClienteComDataCadastro("Eva", "55555555555", "CNH005", StatusCadastroCliente.PENDENTE, mesmaData));
            repositorio.Adicionar(CriarClienteComDataCadastro("Fabio", "66666666666", "CNH006", StatusCadastroCliente.PENDENTE, mesmaData));
            repositorio.Adicionar(CriarClienteComDataCadastro("Gisele", "77777777777", "CNH007", StatusCadastroCliente.PENDENTE, mesmaData));
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            PaginaVO<ReturnClienteVO> pagina1 = await servico.GetAllAsync(new ClienteFiltroVO { Pagina = 1, TamanhoPagina = 2 });
            PaginaVO<ReturnClienteVO> pagina2 = await servico.GetAllAsync(new ClienteFiltroVO { Pagina = 2, TamanhoPagina = 2 });

            Assert.Equal(3, pagina1.Total);
            Assert.Equal(2, pagina1.TamanhoPagina);
            Assert.Equal(new List<string> { "Eva", "Fabio" }, pagina1.Itens.Select(x => x.Nome).ToList());
            Assert.Equal(2, pagina2.Pagina);
            Assert.Equal(new List<string> { "Gisele" }, pagina2.Itens.Select(x => x.Nome).ToList());
        }

        [Fact]
        public async Task GetById_IdInexistente_LancaNaoEncontrada()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.GetByIdAsync(99));

            Assert.Equal(ConstantsMessage.ErroClienteNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task GetById_ClienteDesativado_LancaNaoEncontrada()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente desativado = CriarClienteAtivo(CpfASemMascara, CnhA, StatusCadastroCliente.PENDENTE);
            desativado.Disable();
            repositorio.Adicionar(desativado);
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.GetByIdAsync(desativado.IdCliente));

            Assert.Equal(ConstantsMessage.ErroClienteNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task GetById_ClienteAtivo_DevolveRetornoFormatado()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente ativo = CriarClienteAtivo(CpfASemMascara, CnhA, StatusCadastroCliente.PENDENTE);
            repositorio.Adicionar(ativo);
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            ReturnClienteVO retorno = await servico.GetByIdAsync(ativo.IdCliente);

            Assert.Equal(ativo.IdCliente, retorno.IdCliente);
            Assert.Equal("123.456.789-09", retorno.CpfStr);
        }

        [Fact]
        public async Task InsertFalhaInesperadaDoRepositorio_LogaSemSenhaERelanca()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            InvalidOperationException excecaoSimulada = new InvalidOperationException("falha simulada");
            repositorio.ExcecaoAoInserir = excecaoSimulada;
            FakeLogService log = new FakeLogService();
            ClienteService servico = CriarServico(repositorio, log);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.InsertAsync(CriarVOValido()));

            Assert.Same(excecaoSimulada, excecao);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroSalvarCliente));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(SenhaTeste));
        }

        [Fact]
        public async Task GetByIdFalhaInesperadaDoRepositorio_LogaERelanca()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            InvalidOperationException excecaoSimulada = new InvalidOperationException("falha simulada");
            repositorio.ExcecaoAoBuscarPorId = excecaoSimulada;
            FakeLogService log = new FakeLogService();
            ClienteService servico = CriarServico(repositorio, log);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GetByIdAsync(1));

            Assert.Same(excecaoSimulada, excecao);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroBuscarCliente));
        }

        [Fact]
        public async Task InsertComEmailDeClienteAtivo_LancaEmailDuplicado()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente existente = CriarClienteAtivo(CpfBSemMascara, CnhB, StatusCadastroCliente.APROVADO);
            existente.Email = "maria.silva@exemplo.com";
            repositorio.Adicionar(existente);
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(CriarVOValido()));

            Assert.Equal(ConstantsMessage.ErroClienteEmailDuplicado, excecao.Message);
            Assert.Single(repositorio.Armazenados);
        }

        [Fact]
        public async Task InsertComEmailLegadoEmMaiusculas_LancaEmailDuplicadoSemDiferenciarMaiusculas()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente legado = CriarClienteAtivo(CpfBSemMascara, CnhB, StatusCadastroCliente.APROVADO);
            legado.Email = "MARIA.Silva@EXEMPLO.com";
            repositorio.Adicionar(legado);
            ClienteService servico = CriarServico(repositorio, new FakeLogService());
            ClienteVO vo = CriarVOValido();
            vo.Email = "  Maria.SILVA@exemplo.COM ";

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(vo));

            Assert.Equal(ConstantsMessage.ErroClienteEmailDuplicado, excecao.Message);
        }

        [Fact]
        public async Task InsertComEmailDeClienteDesativado_NaoBloqueia()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente desativado = CriarClienteAtivo(CpfBSemMascara, CnhB, StatusCadastroCliente.APROVADO);
            desativado.Email = "maria.silva@exemplo.com";
            desativado.Disable();
            repositorio.Adicionar(desativado);
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            ReturnClienteVO retorno = await servico.InsertAsync(CriarVOValido());

            Assert.True(retorno.IdCliente > 0);
        }

        [Theory]
        [InlineData(StatusCadastroCliente.PENDENTE)]
        [InlineData(StatusCadastroCliente.RECUSADO)]
        public async Task InsertComEmailDePendenteOuRecusado_Bloqueia(StatusCadastroCliente status)
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente existente = CriarClienteAtivo(CpfBSemMascara, CnhB, status);
            existente.Email = "maria.silva@exemplo.com";
            repositorio.Adicionar(existente);
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(CriarVOValido()));

            Assert.Equal(ConstantsMessage.ErroClienteEmailDuplicado, excecao.Message);
        }

        [Fact]
        public async Task InsertComEmailECnhDuplicados_InformaEmailAntesDaCnh()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente existente = CriarClienteAtivo(CpfBSemMascara, CnhA, StatusCadastroCliente.PENDENTE);
            existente.Email = "maria.silva@exemplo.com";
            repositorio.Adicionar(existente);
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(CriarVOValido()));

            Assert.Equal(ConstantsMessage.ErroClienteEmailDuplicado, excecao.Message);
        }

        [Fact]
        public async Task InsertComCpfEEmailDuplicados_InformaCpfAntesDoEmail()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente existente = CriarClienteAtivo(CpfASemMascara, CnhB, StatusCadastroCliente.PENDENTE);
            existente.Email = "maria.silva@exemplo.com";
            repositorio.Adicionar(existente);
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(CriarVOValido()));

            Assert.Equal(ConstantsMessage.ErroClienteCpfDuplicado, excecao.Message);
        }

        [Fact]
        public async Task VerificarCadastro_CpfComMascaraDeClienteExistente_DevolveExiste()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            repositorio.Adicionar(CriarClienteAtivo(CpfASemMascara, CnhB, StatusCadastroCliente.PENDENTE));
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            ReturnVerificacaoCadastroVO retorno = await servico.VerificarCadastroAsync(CriarVerificacao(CampoVerificacaoCadastro.CPF, CpfA));

            Assert.True(retorno.Existe);
        }

        [Fact]
        public async Task VerificarCadastro_CpfLivre_DevolveNaoExiste()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            repositorio.Adicionar(CriarClienteAtivo(CpfASemMascara, CnhB, StatusCadastroCliente.PENDENTE));
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            ReturnVerificacaoCadastroVO retorno = await servico.VerificarCadastroAsync(CriarVerificacao(CampoVerificacaoCadastro.CPF, "987.654.321-00"));

            Assert.False(retorno.Existe);
        }

        [Fact]
        public async Task VerificarCadastro_EmailComEspacosEMaiusculas_DevolveExisteSemDiferenciarMaiusculas()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente existente = CriarClienteAtivo(CpfASemMascara, CnhA, StatusCadastroCliente.APROVADO);
            existente.Email = "maria.silva@exemplo.com";
            repositorio.Adicionar(existente);
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            ReturnVerificacaoCadastroVO retorno = await servico.VerificarCadastroAsync(CriarVerificacao(CampoVerificacaoCadastro.EMAIL, "  MARIA.Silva@Exemplo.com "));

            Assert.True(retorno.Existe);
        }

        [Fact]
        public async Task VerificarCadastro_EmailLivre_DevolveNaoExiste()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            repositorio.Adicionar(CriarClienteAtivo(CpfASemMascara, CnhA, StatusCadastroCliente.APROVADO));
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            ReturnVerificacaoCadastroVO retorno = await servico.VerificarCadastroAsync(CriarVerificacao(CampoVerificacaoCadastro.EMAIL, "outra.pessoa@exemplo.com"));

            Assert.False(retorno.Existe);
        }

        [Fact]
        public async Task VerificarCadastro_CnhComTracos_DevolveExiste()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            repositorio.Adicionar(CriarClienteAtivo(CpfBSemMascara, CnhA, StatusCadastroCliente.PENDENTE));
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            ReturnVerificacaoCadastroVO retorno = await servico.VerificarCadastroAsync(CriarVerificacao(CampoVerificacaoCadastro.CNH, "123-456-789-01"));

            Assert.True(retorno.Existe);
        }

        [Fact]
        public async Task VerificarCadastro_CnhLivre_DevolveNaoExiste()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            repositorio.Adicionar(CriarClienteAtivo(CpfBSemMascara, CnhA, StatusCadastroCliente.PENDENTE));
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            ReturnVerificacaoCadastroVO retorno = await servico.VerificarCadastroAsync(CriarVerificacao(CampoVerificacaoCadastro.CNH, CnhB));

            Assert.False(retorno.Existe);
        }

        [Fact]
        public async Task VerificarCadastro_ClienteDesativado_NaoConta()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente desativado = CriarClienteAtivo(CpfASemMascara, CnhA, StatusCadastroCliente.APROVADO);
            desativado.Disable();
            repositorio.Adicionar(desativado);
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            ReturnVerificacaoCadastroVO retorno = await servico.VerificarCadastroAsync(CriarVerificacao(CampoVerificacaoCadastro.CPF, CpfA));

            Assert.False(retorno.Existe);
        }

        [Fact]
        public async Task VerificarCadastro_ClienteRecusado_ContaComoExistente()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            repositorio.Adicionar(CriarClienteAtivo(CpfASemMascara, CnhA, StatusCadastroCliente.RECUSADO));
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            ReturnVerificacaoCadastroVO retorno = await servico.VerificarCadastroAsync(CriarVerificacao(CampoVerificacaoCadastro.CPF, CpfA));

            Assert.True(retorno.Existe);
        }

        [Fact]
        public async Task VerificarCadastroFalhaInesperadaDoRepositorio_LogaSoAMensagemPadraoSemOValor()
        {
            // CPF distinto e reconhecivel: se qualquer argumento logado trouxesse o valor, o teste acusa.
            const string cpfConsultado = "529.982.247-25";
            const string digitosConsultados = "52998224725";
            FakeClienteRepository repositorio = new FakeClienteRepository();
            InvalidOperationException excecaoSimulada = new InvalidOperationException("falha simulada");
            repositorio.ExcecaoAoConsultarExistencia = excecaoSimulada;
            LogCapturado log = new LogCapturado();
            ClienteService servico = CriarServico(repositorio, log);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.VerificarCadastroAsync(CriarVerificacao(CampoVerificacaoCadastro.CPF, cpfConsultado)));

            Assert.Same(excecaoSimulada, excecao);
            Assert.Single(log.MensagensAmigaveis);
            Assert.Equal(ConstantsMessage.ErroVerificarCadastroCliente, log.MensagensAmigaveis[0]);
            Assert.DoesNotContain(log.Textos, texto => texto.Contains(cpfConsultado) || texto.Contains(digitosConsultados));
        }

        [Fact]
        public async Task VerificarCadastro_EmailEntregueAoRepositorioJaNormalizado()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            ClienteService servico = CriarServico(repositorio, new FakeLogService());

            await servico.VerificarCadastroAsync(CriarVerificacao(CampoVerificacaoCadastro.EMAIL, "  MARIA.Silva@EXEMPLO.com "));

            Assert.Equal("maria.silva@exemplo.com", repositorio.UltimoEmailConsultado);
        }

        [Fact]
        public async Task VerificarCadastro_CampoForaDoEnum_LancaInvalidOperationSemConsultarERegistra()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            LogCapturado log = new LogCapturado();
            ClienteService servico = CriarServico(repositorio, log);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.VerificarCadastroAsync(CriarVerificacao((CampoVerificacaoCadastro)99, CpfA)));

            Assert.Equal(ConstantsMessage.ErroVerificarCadastroCliente, excecao.Message);
            Assert.Equal(0, repositorio.ConsultasDeExistencia);
            Assert.Equal(ConstantsMessage.ErroVerificarCadastroCliente, Assert.Single(log.MensagensAmigaveis));
        }

        [Fact]
        public async Task VerificarCadastro_CampoNulo_LancaInvalidOperationSemConsultar()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            ClienteService servico = CriarServico(repositorio, new LogCapturado());
            VerificarCadastroVO vo = new VerificarCadastroVO { Campo = null, Valor = CpfA };

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => servico.VerificarCadastroAsync(vo));

            Assert.Equal(ConstantsMessage.ErroVerificarCadastroCliente, excecao.Message);
            Assert.Equal(0, repositorio.ConsultasDeExistencia);
        }

        /// <summary>Log que guarda cada argumento de texto recebido (mensagem, friendly e origem), para o teste conferir o que sairia no log.</summary>
        private sealed class LogCapturado : ILogService
        {
            public List<string> Textos { get; } = new List<string>();

            public List<string> MensagensAmigaveis { get; } = new List<string>();

            public void Write(string message, string friendlyMessage, string source)
            {
                Textos.Add(message);
                Textos.Add(friendlyMessage);
                Textos.Add(source);
                MensagensAmigaveis.Add(friendlyMessage);
            }

            public void Write(Exception exception, string friendlyMessage, string source)
            {
                Textos.Add(exception.Message);
                Textos.Add(friendlyMessage);
                Textos.Add(source);
                MensagensAmigaveis.Add(friendlyMessage);
            }

            public void WriteInfo(string message, string source)
            {
                Textos.Add(message);
                Textos.Add(source);
            }
        }

        private static VerificarCadastroVO CriarVerificacao(CampoVerificacaoCadastro campo, string valor)
        {
            VerificarCadastroVO vo = new VerificarCadastroVO
            {
                Campo = campo,
                Valor = valor
            };

            return vo;
        }

        /// <summary>VO valido com CPF, CNH e senha de teste; cada teste altera o que precisa.</summary>
        private static ClienteVO CriarVOValido()
        {
            ClienteVO vo = new ClienteVO
            {
                Nome = "Maria da Silva",
                Cpf = CpfASemMascara,
                DataNascimento = new DateOnly(1990, 5, 20),
                Email = "maria.silva@exemplo.com",
                Telefone = "11987654321",
                CnhNumero = CnhA,
                CnhCategoria = CategoriaCnh.B,
                CnhValidade = DateOnly.FromDateTime(DateTime.Now).AddYears(3),
                Logradouro = "Rua das Flores",
                Numero = "100",
                Complemento = "Apto 2",
                Bairro = "Centro",
                Cidade = "Sao Paulo",
                Uf = "SP",
                Cep = "01234567",
                Senha = SenhaTeste
            };

            return vo;
        }

        private static Cliente CriarClienteAtivo(string cpf, string cnh, StatusCadastroCliente status)
        {
            Cliente cliente = new Cliente
            {
                Nome = "Cliente existente",
                Cpf = cpf,
                DataNascimento = new DateOnly(1980, 1, 1),
                Email = "existente@exemplo.com",
                Telefone = "11911112222",
                CnhNumero = cnh,
                CnhCategoria = CategoriaCnh.B,
                CnhValidade = new DateOnly(2031, 1, 1),
                StatusCadastro = status
            };

            return cliente;
        }

        private static Cliente CriarClienteComDataCadastro(string nome, string cpf, string cnh, StatusCadastroCliente status, DateTime dataCadastro)
        {
            Cliente cliente = CriarClienteAtivo(cpf, cnh, status);
            cliente.Nome = nome;
            cliente.DataCadastro = dataCadastro;

            return cliente;
        }

        private static ClienteService CriarServico(FakeClienteRepository repositorio, ILogService log)
        {
            return CriarServico(repositorio, log, new FakeSenhaHasher());
        }

        private static ClienteService CriarServico(FakeClienteRepository repositorio, ILogService log, FakeSenhaHasher hasher)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<ClienteProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            ClienteService servico = new ClienteService(repositorio, hasher, mapper, log);

            return servico;
        }
    }
}
