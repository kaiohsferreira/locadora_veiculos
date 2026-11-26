using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace AutoVia.Tests.Empresas
{
    /// <summary>
    /// Testes do EmpresaService (P10-BE-01): gravacao unica de empresa e responsavel, ordem dos 409 (CNPJ, e-mail,
    /// CPF), CNPJ da operadora (400), normalizacao, hash da senha (PBKDF2 real), matricula, cargo e flags fixos,
    /// verificacao publica so com a existencia e log sem senha, CNPJ, CPF ou e-mail.
    /// </summary>
    public class EmpresaServiceTests
    {
        private const string CnpjValido = "11.222.333/0001-81";
        private const string CnpjDigitos = "11222333000181";
        private const string CnpjOutro = "11444777000161";
        private const string CnpjOperadora = "11222333000181";
        private const string CpfValido = "529.982.247-25";
        private const string CpfDigitos = "52998224725";
        private const string SenhaValida = "SenhaForte1";
        private const string EmailEmpresa = "contato@rotasul.exemplo.com";
        private const string EmailResponsavel = "ana@rotasul.exemplo.com";

        [Fact]
        public async Task InsertValido_GravaEmpresaEResponsavelNumaUnicaGravacao()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            FakeLogService log = new FakeLogService();
            EmpresaService servico = CriarServico(repositorio, log);

            ReturnEmpresaVO retorno = await servico.InsertAsync(CriarVOValido());

            Assert.Equal(1, repositorio.Insercoes);
            Empresa empresa = Assert.Single(repositorio.Empresas);
            Funcionario responsavel = Assert.Single(repositorio.Funcionarios);
            Assert.Same(responsavel, Assert.Single(empresa.Funcionarios));
            Assert.Equal(CnpjDigitos, empresa.Cnpj);
            Assert.True(empresa.Ativo);
            Assert.False(empresa.Operadora);
            Assert.Equal(CargoFuncionario.ADMINISTRADOR, responsavel.Cargo);
            Assert.Equal("MAT-0001", responsavel.Matricula);
            Assert.True(responsavel.Ativo);
            Assert.Equal(CpfDigitos, responsavel.Cpf);
            Assert.Equal(EmailResponsavel, responsavel.Email);
            Assert.Equal(DateOnly.FromDateTime(DateTime.Now), responsavel.DataAdmissao);
            Assert.Equal(empresa.CreatedAt, responsavel.CreatedAt);
            Assert.Equal(empresa.UpdatedAt, responsavel.UpdatedAt);
            Assert.Null(empresa.DisabledAt);
            Assert.Null(responsavel.DisabledAt);

            Assert.True(retorno.IdEmpresa > 0);
            Assert.Equal(CnpjValido, retorno.CnpjStr);
            Assert.Equal(EmailResponsavel, retorno.ResponsavelEmail);
            Assert.Equal("MAT-0001", retorno.MatriculaResponsavel);
            Assert.True(retorno.Ativo);
            Assert.Equal(responsavel.IdFuncionario, retorno.IdFuncionarioResponsavel);
        }

        [Fact]
        public async Task InsertValido_HashDaSenhaEhPBKDF2_NuncaTextoPuro()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            FakeLogService log = new FakeLogService();
            EmpresaService servico = CriarServico(repositorio, log);

            await servico.InsertAsync(CriarVOValido());

            Funcionario responsavel = Assert.Single(repositorio.Funcionarios);
            Assert.NotNull(responsavel.SenhaHash);
            Assert.NotEqual(SenhaValida, responsavel.SenhaHash);
            Assert.True(new SenhaHasher(log).Verify(SenhaValida, responsavel.SenhaHash!));
        }

        [Fact]
        public async Task InsertValido_NaoEscreveSenhaCnpjCpfNemEmailNoLog()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            FakeLogService log = new FakeLogService();
            EmpresaService servico = CriarServico(repositorio, log);

            await servico.InsertAsync(CriarVOValido());

            List<string> entradas = log.Snapshot();
            foreach (string entrada in entradas)
            {
                Assert.DoesNotContain(SenhaValida, entrada);
                Assert.DoesNotContain(CnpjDigitos, entrada);
                Assert.DoesNotContain(CpfDigitos, entrada);
                Assert.DoesNotContain(EmailResponsavel, entrada);
            }
        }

        [Fact]
        public async Task CnpjDuplicadoEEmailEDuplicadoEOCpfTambem_DevolveOCnpjPrimeiro()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            repositorio.AdicionarEmpresa(new Empresa { Cnpj = CnpjDigitos, Email = EmailEmpresa, RazaoSocial = "Outra", NomeFantasia = "Outra" });
            repositorio.AdicionarFuncionario(new Funcionario { Cpf = CpfDigitos, Nome = "Outro", Matricula = "M-1", Cargo = CargoFuncionario.ATENDENTE, DataAdmissao = DateOnly.FromDateTime(DateTime.Now) });
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(CriarVOValido()));

            Assert.Equal(ConstantsMessage.ErroLocadoraCnpjDuplicado, excecao.Message);
            Assert.Equal(0, repositorio.Insercoes);
        }

        [Fact]
        public async Task EmailDuplicadoEOCpfDuplicado_DevolveOEmailAntesDoCpf()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            repositorio.AdicionarEmpresa(new Empresa { Cnpj = CnpjOutro, Email = EmailEmpresa, RazaoSocial = "Outra", NomeFantasia = "Outra" });
            repositorio.AdicionarFuncionario(new Funcionario { Cpf = CpfDigitos, Nome = "Outro", Matricula = "M-1", Cargo = CargoFuncionario.ATENDENTE, DataAdmissao = DateOnly.FromDateTime(DateTime.Now) });
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(CriarVOValido()));

            Assert.Equal(ConstantsMessage.ErroLocadoraEmailDuplicado, excecao.Message);
            Assert.Equal(0, repositorio.Insercoes);
        }

        [Fact]
        public async Task CpfDoResponsavelDuplicado_DevolveOCpfPorUltimo()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            repositorio.AdicionarFuncionario(new Funcionario { Cpf = CpfDigitos, Nome = "Outro", Matricula = "M-1", Cargo = CargoFuncionario.ATENDENTE, Ativo = false, DataAdmissao = DateOnly.FromDateTime(DateTime.Now) });
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(CriarVOValido()));

            Assert.Equal(ConstantsMessage.ErroLocadoraResponsavelCpfDuplicado, excecao.Message);
            Assert.Equal(0, repositorio.Insercoes);
        }

        [Fact]
        public async Task EmpresaDesativada_NaoBloqueiaCnpjNemEmail()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            Empresa antiga = new Empresa { Cnpj = CnpjDigitos, Email = EmailEmpresa, RazaoSocial = "Antiga", NomeFantasia = "Antiga" };
            repositorio.AdicionarEmpresa(antiga);
            antiga.DisabledAt = DateTime.Now;
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            ReturnEmpresaVO retorno = await servico.InsertAsync(CriarVOValido());

            Assert.True(retorno.IdEmpresa > 0);
            Assert.Equal(1, repositorio.Insercoes);
        }

        [Fact]
        public async Task EmpresaInativaMasNaoDesativada_AindaBloqueiaOCnpj()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            repositorio.AdicionarEmpresa(new Empresa { Cnpj = CnpjDigitos, Email = EmailEmpresa, RazaoSocial = "Suspensa", NomeFantasia = "Suspensa", Ativo = false });
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.InsertAsync(CriarVOValido()));

            Assert.Equal(ConstantsMessage.ErroLocadoraCnpjDuplicado, excecao.Message);
        }

        [Fact]
        public async Task CpfDeFuncionarioDesativadoDoTodo_NaoBloqueia()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            Funcionario desligado = new Funcionario { Cpf = CpfDigitos, Nome = "Desligado", Matricula = "M-1", Cargo = CargoFuncionario.ATENDENTE, DataAdmissao = DateOnly.FromDateTime(DateTime.Now) };
            repositorio.AdicionarFuncionario(desligado);
            desligado.DisabledAt = DateTime.Now;
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            ReturnEmpresaVO retorno = await servico.InsertAsync(CriarVOValido());

            Assert.True(retorno.IdEmpresa > 0);
        }

        [Fact]
        public async Task CnpjDaOperadora_E_RecusadoComoInvalido_SemGravar()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            repositorio.AdicionarEmpresa(new Empresa { Cnpj = CnpjOperadora, Email = "operadora@autovia.exemplo.com", RazaoSocial = "Operadora", NomeFantasia = "Operadora", Operadora = true });
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => servico.InsertAsync(CriarVOValido()));

            Assert.Equal(ConstantsMessage.ErroLocadoraCnpjInvalido, Assert.Single(excecao.Errors).ErrorMessage);
            Assert.Equal(0, repositorio.Insercoes);
        }

        [Fact]
        public async Task Insert_NormalizaCnpjTelefoneCepEmailUfERazao()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());
            CadastroLocadoraVO vo = CriarVOValido();
            vo.RazaoSocial = "  Rota Sul Locadora Ltda  ";
            vo.Cnpj = CnpjValido;
            vo.Telefone = "(11) 3456-7890";
            vo.Cep = "01234-567";
            vo.Email = "  Contato@ROTASUL.exemplo.com ";
            vo.Uf = " sp ";
            vo.Complemento = "   ";
            vo.ResponsavelCpf = CpfValido;
            vo.ResponsavelEmail = "  Ana@RotaSul.Exemplo.COM ";

            await servico.InsertAsync(vo);

            Empresa empresa = Assert.Single(repositorio.Empresas);
            Assert.Equal("Rota Sul Locadora Ltda", empresa.RazaoSocial);
            Assert.Equal(CnpjDigitos, empresa.Cnpj);
            Assert.Equal("1134567890", empresa.Telefone);
            Assert.Equal("01234567", empresa.Cep);
            Assert.Equal("contato@rotasul.exemplo.com", empresa.Email);
            Assert.Equal("SP", empresa.Uf);
            Assert.Null(empresa.Complemento);
            Funcionario responsavel = Assert.Single(repositorio.Funcionarios);
            Assert.Equal(CpfDigitos, responsavel.Cpf);
            Assert.Equal("ana@rotasul.exemplo.com", responsavel.Email);
        }

        [Fact]
        public async Task Insert_FalhaDoBanco_RelancaELogaSemSenhaNemDocumento()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository { ExcecaoAoConsultar = new InvalidOperationException("falha simulada do banco") };
            FakeLogService log = new FakeLogService();
            EmpresaService servico = CriarServico(repositorio, log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.InsertAsync(CriarVOValido()));

            List<string> entradas = log.Snapshot();
            Assert.Contains(entradas, entrada => entrada.StartsWith(ConstantsMessage.ErroSalvarLocadora, StringComparison.Ordinal));
            foreach (string entrada in entradas)
            {
                Assert.DoesNotContain(SenhaValida, entrada);
                Assert.DoesNotContain(CnpjDigitos, entrada);
                Assert.DoesNotContain(CpfDigitos, entrada);
            }
        }

        [Fact]
        public async Task GetCidades_MesmaCidadeComGrafiasDiferentes_DevolveAMistaMesmoQueChegueDepois()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository
            {
                CidadesNaOrdemDevolvida = new List<ReturnCidadeLocadoraVO>
                {
                    new ReturnCidadeLocadoraVO { Cidade = "SÃO PAULO", Uf = "SP" },
                    new ReturnCidadeLocadoraVO { Cidade = "são paulo", Uf = "SP" },
                    new ReturnCidadeLocadoraVO { Cidade = "São Paulo", Uf = "SP" }
                }
            };
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            List<ReturnCidadeLocadoraVO> cidades = await servico.GetCidadesAsync();

            ReturnCidadeLocadoraVO opcao = Assert.Single(cidades);
            Assert.Equal("São Paulo", opcao.Cidade);
            Assert.Equal("SP", opcao.Uf);
        }

        [Fact]
        public async Task GetCidades_SemGraciaMista_DevolveAMenorEmOrdemOrdinal()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository
            {
                CidadesNaOrdemDevolvida = new List<ReturnCidadeLocadoraVO>
                {
                    new ReturnCidadeLocadoraVO { Cidade = "são paulo", Uf = "SP" },
                    new ReturnCidadeLocadoraVO { Cidade = "SÃO PAULO", Uf = "SP" }
                }
            };
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            List<ReturnCidadeLocadoraVO> cidades = await servico.GetCidadesAsync();

            ReturnCidadeLocadoraVO opcao = Assert.Single(cidades);
            Assert.Equal("SÃO PAULO", opcao.Cidade);
        }

        [Fact]
        public async Task GetCidades_VariasGraciasMistas_DevolveAMenorEntreAsMistas()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository
            {
                CidadesNaOrdemDevolvida = new List<ReturnCidadeLocadoraVO>
                {
                    new ReturnCidadeLocadoraVO { Cidade = "SAO PAULO", Uf = "SP" },
                    new ReturnCidadeLocadoraVO { Cidade = "sao Paulo", Uf = "SP" },
                    new ReturnCidadeLocadoraVO { Cidade = "Sao Paulo", Uf = "SP" }
                }
            };
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            List<ReturnCidadeLocadoraVO> cidades = await servico.GetCidadesAsync();

            ReturnCidadeLocadoraVO opcao = Assert.Single(cidades);
            Assert.Equal("Sao Paulo", opcao.Cidade);
        }

        [Fact]
        public async Task GetCidades_OrdenaPorCidadeSemCaixaEAcentoEDepoisPorUf()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository
            {
                CidadesNaOrdemDevolvida = new List<ReturnCidadeLocadoraVO>
                {
                    new ReturnCidadeLocadoraVO { Cidade = "curitiba", Uf = "SC" },
                    new ReturnCidadeLocadoraVO { Cidade = "Curitiba", Uf = "PR" },
                    new ReturnCidadeLocadoraVO { Cidade = "Águas Claras", Uf = "DF" },
                    new ReturnCidadeLocadoraVO { Cidade = "Campinas", Uf = "SP" },
                    new ReturnCidadeLocadoraVO { Cidade = "Bauru", Uf = "SP" }
                }
            };
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            List<ReturnCidadeLocadoraVO> cidades = await servico.GetCidadesAsync();

            List<string> pares = cidades.Select(cidade => cidade.Cidade + "/" + cidade.Uf).ToList();
            Assert.Equal(new List<string> { "Águas Claras/DF", "Bauru/SP", "Campinas/SP", "Curitiba/PR", "curitiba/SC" }, pares);
        }

        [Fact]
        public async Task GetCidades_CidadeSemAcentoOuOutraUf_FicaEmOpcaoSeparadaNaOrdemCerta()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            repositorio.AdicionarEmpresa(new Empresa { Cidade = "São Paulo", Uf = "SP" });
            repositorio.AdicionarEmpresa(new Empresa { Cidade = "Sao Paulo", Uf = "SP" });
            repositorio.AdicionarEmpresa(new Empresa { Cidade = "São Paulo", Uf = "RJ" });
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            List<ReturnCidadeLocadoraVO> cidades = await servico.GetCidadesAsync();

            List<string> pares = cidades.Select(cidade => cidade.Cidade + "/" + cidade.Uf).ToList();
            Assert.Equal(new List<string> { "São Paulo/RJ", "Sao Paulo/SP", "São Paulo/SP" }, pares);
        }

        [Fact]
        public async Task GetCidades_CidadeVaziaDepoisDeAparada_NaoEntraNaLista()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository
            {
                CidadesNaOrdemDevolvida = new List<ReturnCidadeLocadoraVO>
                {
                    new ReturnCidadeLocadoraVO { Cidade = "   ", Uf = "SP" },
                    new ReturnCidadeLocadoraVO { Cidade = "Campinas", Uf = "SP" }
                }
            };
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            List<ReturnCidadeLocadoraVO> cidades = await servico.GetCidadesAsync();

            List<string> pares = cidades.Select(cidade => cidade.Cidade + "/" + cidade.Uf).ToList();
            Assert.Equal(new List<string> { "Campinas/SP" }, pares);
        }

        [Fact]
        public void TabelaDeAcentosDaOrdemTemOMesmoTamanhoNosDoisLados()
        {
            Assert.Equal(ConstantsValues.LetrasComAcentoCidade.Length, ConstantsValues.LetrasSemAcentoCidade.Length);
            Assert.Equal('A', ConstantsValues.LetrasSemAcentoCidade[ConstantsValues.LetrasComAcentoCidade.IndexOf('Ã')]);
            Assert.Equal('A', ConstantsValues.LetrasSemAcentoCidade[ConstantsValues.LetrasComAcentoCidade.IndexOf('Á')]);
            Assert.Equal('C', ConstantsValues.LetrasSemAcentoCidade[ConstantsValues.LetrasComAcentoCidade.IndexOf('Ç')]);
        }

        /// <summary>Confere as 53 posicoes: cada letra acentuada tem de trocar pela sua base sem acento (decomposicao FormD) em maiuscula.</summary>
        [Fact]
        public void TabelaDeAcentosDaOrdemTrocaCadaLetraPelaBaseSemAcento()
        {
            List<string> divergencias = new List<string>();

            for (int posicao = 0; posicao < ConstantsValues.LetrasComAcentoCidade.Length; posicao++)
            {
                char letraComAcento = ConstantsValues.LetrasComAcentoCidade[posicao];
                string decomposta = letraComAcento.ToString().Normalize(NormalizationForm.FormD);
                char baseSemAcento = char.ToUpperInvariant(decomposta[0]);
                char esperada = ConstantsValues.LetrasSemAcentoCidade[posicao];

                if (baseSemAcento != esperada)
                {
                    divergencias.Add(letraComAcento + " deveria virar " + esperada + " e virou " + baseSemAcento);
                }
            }

            Assert.Empty(divergencias);
        }

        [Fact]
        public async Task GetCidades_FalhaDoBanco_RelancaELogaComAMensagemDaConsulta()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository { ExcecaoAoConsultar = new InvalidOperationException("falha simulada do banco") };
            FakeLogService log = new FakeLogService();
            EmpresaService servico = CriarServico(repositorio, log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.GetCidadesAsync());

            List<string> entradas = log.Snapshot();
            Assert.Contains(entradas, entrada => entrada.StartsWith(ConstantsMessage.ErroConsultarLocadora, StringComparison.Ordinal));
        }

        [Fact]
        public async Task VerificarCnpj_ExistenteComMascara_DevolveExiste()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            repositorio.AdicionarEmpresa(new Empresa { Cnpj = CnpjDigitos, Email = EmailEmpresa, RazaoSocial = "Rota", NomeFantasia = "Rota" });
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            ReturnVerificacaoCadastroEmpresaVO retorno = await servico.VerificarCadastroAsync(new VerificarCadastroEmpresaVO { Campo = CampoVerificacaoEmpresa.CNPJ, Valor = CnpjValido });

            Assert.True(retorno.Existe);
        }

        [Fact]
        public async Task VerificarCnpj_Inexistente_DevolveNaoExiste()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            ReturnVerificacaoCadastroEmpresaVO retorno = await servico.VerificarCadastroAsync(new VerificarCadastroEmpresaVO { Campo = CampoVerificacaoEmpresa.CNPJ, Valor = CnpjOutro });

            Assert.False(retorno.Existe);
        }

        [Fact]
        public async Task VerificarEmail_ComMaiusculasEBordas_NormalizaEDevolveExiste()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            repositorio.AdicionarEmpresa(new Empresa { Cnpj = CnpjDigitos, Email = EmailEmpresa, RazaoSocial = "Rota", NomeFantasia = "Rota" });
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            ReturnVerificacaoCadastroEmpresaVO retorno = await servico.VerificarCadastroAsync(new VerificarCadastroEmpresaVO { Campo = CampoVerificacaoEmpresa.EMAIL, Valor = "  CONTATO@RotaSul.exemplo.com " });

            Assert.True(retorno.Existe);
        }

        [Fact]
        public async Task VerificarCnpj_EmpresaDesativada_NaoConta()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            Empresa desativada = new Empresa { Cnpj = CnpjDigitos, Email = EmailEmpresa, RazaoSocial = "Rota", NomeFantasia = "Rota" };
            repositorio.AdicionarEmpresa(desativada);
            desativada.DisabledAt = DateTime.Now;
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            ReturnVerificacaoCadastroEmpresaVO retorno = await servico.VerificarCadastroAsync(new VerificarCadastroEmpresaVO { Campo = CampoVerificacaoEmpresa.CNPJ, Valor = CnpjValido });

            Assert.False(retorno.Existe);
        }

        [Fact]
        public async Task VerificarCadastro_NaoGravaNada()
        {
            FakeEmpresaRepository repositorio = new FakeEmpresaRepository();
            EmpresaService servico = CriarServico(repositorio, new FakeLogService());

            await servico.VerificarCadastroAsync(new VerificarCadastroEmpresaVO { Campo = CampoVerificacaoEmpresa.CNPJ, Valor = CnpjValido });

            Assert.Equal(0, repositorio.Insercoes);
            Assert.Empty(repositorio.Empresas);
        }

        /// <summary>Monta o servico real com o repositorio em memoria, o hash PBKDF2 real, o mapeamento real e o log em memoria.</summary>
        private static EmpresaService CriarServico(FakeEmpresaRepository repositorio, FakeLogService log)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<EmpresaProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            SenhaHasher hasher = new SenhaHasher(log);

            return new EmpresaService(repositorio, hasher, mapper, log);
        }

        private static CadastroLocadoraVO CriarVOValido()
        {
            CadastroLocadoraVO vo = new CadastroLocadoraVO
            {
                RazaoSocial = "Rota Sul Locadora Ltda",
                NomeFantasia = "Rota Sul",
                Cnpj = CnpjValido,
                Telefone = "(11) 3456-7890",
                Email = EmailEmpresa,
                Cep = "01234-567",
                Logradouro = "Rua das Flores",
                Numero = "100",
                Complemento = null,
                Bairro = "Centro",
                Cidade = "São Paulo",
                Uf = "SP",
                ResponsavelNome = "Ana Responsável",
                ResponsavelCpf = CpfValido,
                ResponsavelEmail = EmailResponsavel,
                Senha = SenhaValida,
                AceiteTermos = true
            };

            return vo;
        }
    }
}
