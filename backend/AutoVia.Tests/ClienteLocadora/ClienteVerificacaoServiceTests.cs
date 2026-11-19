using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Profiles;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.DocumentosCliente;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using VinculoClienteLocadora = AutoVia.Core.Entities.PgSql.ClienteLocadora;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Servico da fila, do detalhe e dos documentos da equipe (P2-BE-05) sobre os fakes do vinculo e dos documentos. Prova:
    /// so entra quem tem vinculo ativo com a locadora do contexto; o status filtra o vinculo; busca por nome, e-mail e CPF
    /// (com e sem pontuacao, sem digito nao casa CPF, so espacos nao filtra); uma linha por cliente (a que vale do par);
    /// ordem e paginacao; detalhe sem senha; 404 igual ao inexistente sem ler o armazenamento; documentos com um por tipo,
    /// item sem link e linha de acesso sem dado pessoal. NAO prova o SQL nem o PostgreSQL (isso esta em ClienteVerificacaoConsultasSqlTests).
    /// </summary>
    public class ClienteVerificacaoServiceTests
    {
        private const int IdEmpresaDoContexto = 10;
        private const int IdEmpresaDeOutraLocadora = 20;
        private const int IdFuncionarioDoContexto = 77;
        private const string CpfMaria = "12345678909";
        private const string CpfJoao = "98765432100";
        private const string CpfAna = "11122233344";
        private const string NomeMaria = "Maria Silva";
        private const string NomeJoao = "Joao Pereira";
        private const string NomeAna = "Ana Souza";
        private const string EmailMaria = "maria.silva@exemplo.com";
        private const string EmailJoao = "joao@exemplo.com";
        private const string EmailAna = "ana@exemplo.com";
        private const string CpfFormatadoMaria = "123.456.789-09";
        private const string CnhMaria = "01234567890";
        private const string ChaveDocumento = "clientes/1/documentos/doc-cnh.jpg";
        private const string ChaveDocumentoComprovante = "clientes/1/documentos/doc-comp.pdf";
        private const string ChaveDocumentoSemLink = "clientes/1/documentos/sem-link.png";
        private const string SenhaHashSecreta = "hash-nao-pode-sair";
        private const string GoogleSubSecreto = "google-sub-nao-pode-sair";

        private static readonly DateTime AgoraFixo = new DateTime(2026, 11, 19, 10, 0, 0);

        private sealed class Montagem
        {
            public Montagem()
            {
                Log = new FakeLogService();
                Armazenamento = new ArmazenamentoClienteLocadora();
                Vinculos = new FakeClienteLocadoraRepository(Contexto(), Armazenamento);
                DocumentosRepositorio = new FakeClienteDocumentoRepository();
                ArmazenamentoDocumentos = new FakeArmazenamentoGateway();

                _mapper = new MapperConfiguration(cfg => cfg.AddProfile<ClienteVerificacaoProfile>(), NullLoggerFactory.Instance).CreateMapper();
                _documentos = new ClienteDocumentoService(DocumentosRepositorio, ArmazenamentoDocumentos, new FakeRelogio(AgoraFixo), new ClientesOptions(), Log);

                Servico = ServicoPara(Contexto());
            }

            private readonly IMapper _mapper;

            private readonly ClienteDocumentoService _documentos;

            /// <summary>Servico com o contexto informado sobre o mesmo armazenamento de vinculos (outra locadora, outro funcionario).</summary>
            public ClienteVerificacaoService ServicoPara(ContextoEmpresa contexto)
            {
                FakeClienteLocadoraRepository repositorio = new FakeClienteLocadoraRepository(contexto, Armazenamento);

                return new ClienteVerificacaoService(repositorio, _documentos, contexto, _mapper, Log);
            }

            public FakeLogService Log { get; }

            public ArmazenamentoClienteLocadora Armazenamento { get; }

            public FakeClienteLocadoraRepository Vinculos { get; }

            public FakeClienteDocumentoRepository DocumentosRepositorio { get; }

            public FakeArmazenamentoGateway ArmazenamentoDocumentos { get; }

            public ClienteVerificacaoService Servico { get; }

            private ContextoEmpresa? _contexto;

            /// <summary>Contexto da locadora 10 com o funcionario 77 (ATENDENTE), o mesmo para todas as chamadas da montagem.</summary>
            public ContextoEmpresa Contexto()
            {
                if (_contexto == null)
                {
                    _contexto = new ContextoEmpresa(Log);
                    _contexto.Definir(IdEmpresaDoContexto, IdFuncionarioDoContexto, CargoFuncionario.ATENDENTE, false);
                }

                return _contexto;
            }

            public Cliente SemearCliente(int id, string nome, string cpf, string email)
            {
                Cliente cliente = new Cliente
                {
                    IdCliente = id,
                    Nome = nome,
                    Cpf = cpf,
                    Email = email,
                    Telefone = "11999990000",
                    DataNascimento = new DateOnly(1990, 3, 15),
                    CnhNumero = CnhMaria,
                    CnhCategoria = CategoriaCnh.B,
                    CnhValidade = new DateOnly(2030, 5, 20),
                    Logradouro = "Rua das Flores",
                    Numero = "100",
                    Complemento = null,
                    Bairro = "Centro",
                    Cidade = "Sao Paulo",
                    Uf = "SP",
                    Cep = "01001000",
                    DataCadastro = new DateTime(2026, 11, 1, 9, 30, 0),
                    SenhaHash = SenhaHashSecreta,
                    GoogleSub = GoogleSubSecreto
                };

                Armazenamento.Clientes.Add(cliente);

                return cliente;
            }

            public static FiltroClienteVerificacaoVO Filtro(string? status = null, string? busca = null, int pagina = 1, int tamanho = 20)
            {
                return new FiltroClienteVerificacaoVO
                {
                    Status = status,
                    Busca = busca,
                    Pagina = pagina,
                    TamanhoPagina = tamanho
                };
            }
        }

        private static ReturnClienteFilaVO Unico(PaginaVO<ReturnClienteFilaVO> pagina)
        {
            Assert.Equal(1, pagina.Total);
            return Assert.Single(pagina.Itens);
        }

        [Fact]
        public async Task Fila_SoEntraQuemTemVinculoAtivoComAEmpresaDoContexto()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.SemearCliente(2, NomeJoao, CpfJoao, EmailJoao);
            m.SemearCliente(3, NomeAna, CpfAna, EmailAna);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);
            m.Armazenamento.Semear(2, IdEmpresaDeOutraLocadora, StatusCadastroCliente.PENDENTE);

            PaginaVO<ReturnClienteFilaVO> pagina = await m.Servico.ListarFilaAsync(Montagem.Filtro());

            ReturnClienteFilaVO item = Unico(pagina);
            Assert.Equal(1, item.IdCliente);
        }

        [Fact]
        public async Task Fila_FiltroDeStatusUsaOStatusDoVinculo()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.APROVADO);
            m.Armazenamento.Semear(1, IdEmpresaDeOutraLocadora, StatusCadastroCliente.PENDENTE);

            PaginaVO<ReturnClienteFilaVO> aprovados = await m.Servico.ListarFilaAsync(Montagem.Filtro(status: "APROVADO"));
            PaginaVO<ReturnClienteFilaVO> pendentes = await m.Servico.ListarFilaAsync(Montagem.Filtro(status: "PENDENTE"));

            Assert.Equal(StatusCadastroCliente.APROVADO, Unico(aprovados).Status);
            Assert.Empty(pendentes.Itens);
            Assert.Equal(0, pendentes.Total);
        }

        [Fact]
        public async Task Fila_BuscaCasaNomeEmailECpfSemPontuacao()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.SemearCliente(2, NomeJoao, CpfJoao, EmailJoao);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);
            m.Armazenamento.Semear(2, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);

            PaginaVO<ReturnClienteFilaVO> porNome = await m.Servico.ListarFilaAsync(Montagem.Filtro(busca: "MARIA silva"));
            PaginaVO<ReturnClienteFilaVO> porEmail = await m.Servico.ListarFilaAsync(Montagem.Filtro(busca: "  JOAO@EXEMPLO  "));
            PaginaVO<ReturnClienteFilaVO> porCpfComPontuacao = await m.Servico.ListarFilaAsync(Montagem.Filtro(busca: "123.456.789-09"));
            PaginaVO<ReturnClienteFilaVO> porCpfSemPontuacao = await m.Servico.ListarFilaAsync(Montagem.Filtro(busca: "98765432100"));

            Assert.Equal(1, Unico(porNome).IdCliente);
            Assert.Equal(2, Unico(porEmail).IdCliente);
            Assert.Equal(1, Unico(porCpfComPontuacao).IdCliente);
            Assert.Equal(2, Unico(porCpfSemPontuacao).IdCliente);
        }

        [Fact]
        public async Task Fila_MesmoClienteAprovadoNaUmaEPendenteNaOutra_AparecePeloStatusDeCadaLocadora()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.APROVADO);
            m.Armazenamento.Semear(1, IdEmpresaDeOutraLocadora, StatusCadastroCliente.PENDENTE);
            ContextoEmpresa contextoOutra = new ContextoEmpresa(m.Log);
            contextoOutra.Definir(IdEmpresaDeOutraLocadora, IdFuncionarioDoContexto, CargoFuncionario.ATENDENTE, false);
            ClienteVerificacaoService servicoOutra = m.ServicoPara(contextoOutra);

            ReturnClienteFilaVO naPrimeira = Unico(await m.Servico.ListarFilaAsync(Montagem.Filtro()));
            ReturnClienteFilaVO naSegunda = Unico(await servicoOutra.ListarFilaAsync(Montagem.Filtro()));
            PaginaVO<ReturnClienteFilaVO> aprovadoNaSegunda = await servicoOutra.ListarFilaAsync(Montagem.Filtro(status: "APROVADO"));

            Assert.Equal(StatusCadastroCliente.APROVADO, naPrimeira.Status);
            Assert.Equal(StatusCadastroCliente.PENDENTE, naSegunda.Status);
            Assert.Empty(aprovadoNaSegunda.Itens);
        }

        [Fact]
        public async Task Fila_BuscaSoComEspacosNaoFiltra()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.SemearCliente(2, NomeJoao, CpfJoao, EmailJoao);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);
            m.Armazenamento.Semear(2, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);

            PaginaVO<ReturnClienteFilaVO> pagina = await m.Servico.ListarFilaAsync(Montagem.Filtro(busca: "     "));

            Assert.Equal(2, pagina.Total);
            Assert.Equal(2, pagina.Itens.Count);
        }

        [Fact]
        public async Task Fila_BuscaSemDigitoNaoCasaCpfNemViraCasaTudo()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.SemearCliente(2, NomeJoao, CpfJoao, EmailJoao);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);
            m.Armazenamento.Semear(2, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);

            PaginaVO<ReturnClienteFilaVO> pagina = await m.Servico.ListarFilaAsync(Montagem.Filtro(busca: "-.-"));

            Assert.Equal(0, pagina.Total);
            Assert.Empty(pagina.Itens);
        }

        [Fact]
        public async Task Fila_UmaLinhaPorClienteUsaAQueValeDoPar()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.RECUSADO, justificativa: "Documento ilegivel");
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);

            PaginaVO<ReturnClienteFilaVO> pagina = await m.Servico.ListarFilaAsync(Montagem.Filtro());

            ReturnClienteFilaVO item = Unico(pagina);
            Assert.Equal(StatusCadastroCliente.RECUSADO, item.Status);
            Assert.Equal("Documento ilegivel", item.JustificativaRecusa);
        }

        [Fact]
        public async Task Fila_OrdenaPelaDataDoPedidoEPagina()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.SemearCliente(2, NomeJoao, CpfJoao, EmailJoao);
            m.SemearCliente(3, NomeAna, CpfAna, EmailAna);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE, dataSolicitacao: new DateTime(2026, 11, 10, 8, 0, 0));
            m.Armazenamento.Semear(2, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE, dataSolicitacao: new DateTime(2026, 11, 5, 8, 0, 0));
            m.Armazenamento.Semear(3, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE, dataSolicitacao: new DateTime(2026, 11, 12, 8, 0, 0));

            PaginaVO<ReturnClienteFilaVO> primeira = await m.Servico.ListarFilaAsync(Montagem.Filtro(pagina: 1, tamanho: 2));
            PaginaVO<ReturnClienteFilaVO> segunda = await m.Servico.ListarFilaAsync(Montagem.Filtro(pagina: 2, tamanho: 2));

            Assert.Equal(3, primeira.Total);
            Assert.Equal(new List<int> { 2, 1 }, primeira.Itens.Select(x => x.IdCliente).ToList());
            Assert.Equal(new List<int> { 3 }, segunda.Itens.Select(x => x.IdCliente).ToList());
            Assert.Equal("05/11/2026 08:00", primeira.Itens[0].DataSolicitacao);
        }

        [Fact]
        public async Task Fila_ClienteDesativadoNaoAparece()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);
            m.Armazenamento.ClientesDesativados.Add(1);

            PaginaVO<ReturnClienteFilaVO> pagina = await m.Servico.ListarFilaAsync(Montagem.Filtro());

            Assert.Equal(0, pagina.Total);
        }

        [Fact]
        public async Task Fila_FormatosDoItemSaoOsDoProjeto()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE, dataSolicitacao: new DateTime(2026, 11, 10, 14, 45, 0));

            ReturnClienteFilaVO item = Unico(await m.Servico.ListarFilaAsync(Montagem.Filtro()));

            Assert.Equal(NomeMaria, item.Nome);
            Assert.Equal(CpfFormatadoMaria, item.Cpf);
            Assert.Equal("01/11/2026 09:30", item.DataCadastro);
            Assert.Equal("10/11/2026 14:45", item.DataSolicitacao);
            Assert.Null(item.DataVerificacao);
            Assert.Null(item.JustificativaRecusa);
        }

        [Fact]
        public async Task Detalhe_ComVinculo_DevolveOsDadosSemSenhaNemGoogle()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);

            ReturnClienteVerificacaoVO detalhe = await m.Servico.ObterVerificacaoAsync(1);

            Assert.Equal(NomeMaria, detalhe.Nome);
            Assert.Equal(EmailMaria, detalhe.Email);
            Assert.Equal("11999990000", detalhe.Telefone);
            Assert.Equal("15/03/1990", detalhe.DataNascimento);
            Assert.Equal(CnhMaria, detalhe.CnhNumero);
            Assert.Equal(CategoriaCnh.B, detalhe.CnhCategoria);
            Assert.Equal("20/05/2030", detalhe.CnhValidade);
            Assert.Equal("Rua das Flores", detalhe.Logradouro);
            Assert.Equal("SP", detalhe.Uf);
            Assert.Equal("01001000", detalhe.Cep);
            Assert.Equal(CpfFormatadoMaria, detalhe.Cpf);
            Assert.Equal(StatusCadastroCliente.PENDENTE, detalhe.Status);
            Assert.Null(typeof(ReturnClienteVerificacaoVO).GetProperty("SenhaHash"));
            Assert.Null(typeof(ReturnClienteVerificacaoVO).GetProperty("GoogleSub"));
        }

        [Fact]
        public async Task Detalhe_SemVinculoNaLocadora_DaNotFoundIgualAoInexistente()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.Armazenamento.Semear(1, IdEmpresaDeOutraLocadora, StatusCadastroCliente.PENDENTE);

            NotFoundException doOutraLocadora = await Assert.ThrowsAsync<NotFoundException>(() => m.Servico.ObterVerificacaoAsync(1));
            NotFoundException doInexistente = await Assert.ThrowsAsync<NotFoundException>(() => m.Servico.ObterVerificacaoAsync(999));

            Assert.Equal(ConstantsMessage.ErroClienteNaoEncontrada, doOutraLocadora.Message);
            Assert.Equal(doInexistente.Message, doOutraLocadora.Message);
        }

        [Fact]
        public async Task Documentos_SemVinculo_Da404SemLerOArmazenamento()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.Armazenamento.Semear(1, IdEmpresaDeOutraLocadora, StatusCadastroCliente.PENDENTE);
            m.DocumentosRepositorio.Semear(new ClienteDocumento { IdCliente = 1, Tipo = TipoDocumentoCliente.CNH, ChaveObjeto = ChaveDocumento, NomeArquivo = "cnh.jpg", TipoConteudo = "image/jpeg", TamanhoBytes = 10 });

            await Assert.ThrowsAsync<NotFoundException>(() => m.Servico.ListarDocumentosAsync(1));

            Assert.Equal(0, m.ArmazenamentoDocumentos.AssinaturaLoteCount);
        }

        [Fact]
        public async Task Documentos_SemDocumento_DevolveListaVaziaSemChamarOArmazenamento()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);

            List<ReturnClienteDocumentoVO> documentos = await m.Servico.ListarDocumentosAsync(1);

            Assert.Empty(documentos);
            Assert.Equal(0, m.ArmazenamentoDocumentos.AssinaturaLoteCount);
        }

        [Fact]
        public async Task Documentos_UmPorTipoValeOMaiorIdECnhAntesDoComprovante()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);
            m.DocumentosRepositorio.Semear(new ClienteDocumento { IdCliente = 1, Tipo = TipoDocumentoCliente.COMPROVANTE_RESIDENCIA, ChaveObjeto = ChaveDocumentoComprovante, NomeArquivo = "comp.pdf", TipoConteudo = "application/pdf", TamanhoBytes = 20 });
            m.DocumentosRepositorio.Semear(new ClienteDocumento { IdCliente = 1, Tipo = TipoDocumentoCliente.CNH, ChaveObjeto = "clientes/1/documentos/antiga.jpg", NomeArquivo = "antiga.jpg", TipoConteudo = "image/jpeg", TamanhoBytes = 5 });
            m.DocumentosRepositorio.Semear(new ClienteDocumento { IdCliente = 1, Tipo = TipoDocumentoCliente.CNH, ChaveObjeto = ChaveDocumento, NomeArquivo = "cnh.jpg", TipoConteudo = "image/jpeg", TamanhoBytes = 10 });

            List<ReturnClienteDocumentoVO> documentos = await m.Servico.ListarDocumentosAsync(1);

            Assert.Equal(2, documentos.Count);
            Assert.Equal("CNH", documentos[0].Tipo);
            Assert.Equal("cnh.jpg", documentos[0].NomeArquivo);
            Assert.Equal("COMPROVANTE_RESIDENCIA", documentos[1].Tipo);
            Assert.NotNull(documentos[0].Url);
            Assert.NotNull(documentos[0].ExpiraEm);
        }

        [Fact]
        public async Task Documentos_ItemSemLinkNaoDerrubaALista()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);
            m.DocumentosRepositorio.Semear(new ClienteDocumento { IdCliente = 1, Tipo = TipoDocumentoCliente.CNH, ChaveObjeto = ChaveDocumentoSemLink, NomeArquivo = "sem-link.png", TipoConteudo = "image/png", TamanhoBytes = 7 });
            m.ArmazenamentoDocumentos.ChavesOmitidasNaAssinatura.Add(ChaveDocumentoSemLink);

            List<ReturnClienteDocumentoVO> documentos = await m.Servico.ListarDocumentosAsync(1);

            ReturnClienteDocumentoVO item = Assert.Single(documentos);
            Assert.Null(item.Url);
            Assert.Null(item.ExpiraEm);
            Assert.Equal("sem-link.png", item.NomeArquivo);
        }

        [Fact]
        public async Task Documentos_RegistraOAcessoSemDadoPessoal()
        {
            Montagem m = new Montagem();
            m.SemearCliente(1, NomeMaria, CpfMaria, EmailMaria);
            m.Armazenamento.Semear(1, IdEmpresaDoContexto, StatusCadastroCliente.PENDENTE);
            m.DocumentosRepositorio.Semear(new ClienteDocumento { IdCliente = 1, Tipo = TipoDocumentoCliente.CNH, ChaveObjeto = ChaveDocumento, NomeArquivo = "cnh.jpg", TipoConteudo = "image/jpeg", TamanhoBytes = 10 });
            m.DocumentosRepositorio.Semear(new ClienteDocumento { IdCliente = 1, Tipo = TipoDocumentoCliente.COMPROVANTE_RESIDENCIA, ChaveObjeto = ChaveDocumentoComprovante, NomeArquivo = "comp.pdf", TipoConteudo = "application/pdf", TamanhoBytes = 20 });

            await m.Servico.ListarDocumentosAsync(1);

            string linha = Assert.Single(m.Log.Entries, entrada => entrada.Contains("listou", StringComparison.Ordinal));
            Assert.Contains("77", linha, StringComparison.Ordinal);
            Assert.Contains("2", linha, StringComparison.Ordinal);
            Assert.DoesNotContain(CpfMaria, linha, StringComparison.Ordinal);
            Assert.DoesNotContain(NomeMaria, linha, StringComparison.Ordinal);
            Assert.DoesNotContain(EmailMaria, linha, StringComparison.Ordinal);
            Assert.DoesNotContain("cnh.jpg", linha, StringComparison.Ordinal);
            Assert.DoesNotContain("clientes/", linha, StringComparison.Ordinal);
            Assert.DoesNotContain("https://", linha, StringComparison.Ordinal);
        }
    }
}
