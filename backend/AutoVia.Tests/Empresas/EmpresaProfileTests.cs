using AutoMapper;
using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Empresas
{
    /// <summary>
    /// Testes do EmpresaProfile (P10-BE-01): a configuracao do AutoMapper e valida; o VO de entrada nao grava flags,
    /// id, cargo nem hash; a empresa sai com CNPJ e data formatados e com os dados do responsavel achatados, sem senha.
    /// </summary>
    public class EmpresaProfileTests
    {
        private const string CnpjDigitos = "11222333000181";

        private static IMapper CriarMapper()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<EmpresaProfile>(), NullLoggerFactory.Instance);

            return configuracao.CreateMapper();
        }

        [Fact]
        public void ConfiguracaoDoProfile_EValida()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<EmpresaProfile>(), NullLoggerFactory.Instance);

            configuracao.AssertConfigurationIsValid();
        }

        [Fact]
        public void EmpresaParaRetorno_FormataCnpjEDataEAchataOResponsavel()
        {
            DateTime cadastro = new DateTime(2026, 11, 14, 9, 30, 0);
            Empresa empresa = new Empresa
            {
                IdEmpresa = 7,
                RazaoSocial = "Rota Sul Locadora Ltda",
                NomeFantasia = "Rota Sul",
                Cnpj = CnpjDigitos,
                Telefone = "1134567890",
                Email = "contato@rotasul.exemplo.com",
                Cep = "01234567",
                Logradouro = "Rua das Flores",
                Numero = "100",
                Complemento = null,
                Bairro = "Centro",
                Cidade = "São Paulo",
                Uf = "SP",
                Ativo = true,
                CreatedAt = cadastro
            };
            empresa.Funcionarios.Add(new Funcionario
            {
                IdFuncionario = 12,
                Nome = "Ana Responsável",
                Cpf = "52998224725",
                Matricula = "MAT-0001",
                Email = "ana@rotasul.exemplo.com",
                SenhaHash = "PBKDF2-NAO-PODE-SAIR"
            });

            ReturnEmpresaVO retorno = CriarMapper().Map<ReturnEmpresaVO>(empresa);

            Assert.Equal(7, retorno.IdEmpresa);
            Assert.Equal(CnpjDigitos, retorno.Cnpj);
            Assert.Equal("11.222.333/0001-81", retorno.CnpjStr);
            Assert.Equal(StaticMethods.FormateDateTimeToPTBR(cadastro), retorno.DataCadastroStr);
            Assert.True(retorno.Ativo);
            Assert.Equal(12, retorno.IdFuncionarioResponsavel);
            Assert.Equal("Ana Responsável", retorno.ResponsavelNome);
            Assert.Equal("ana@rotasul.exemplo.com", retorno.ResponsavelEmail);
            Assert.Equal("MAT-0001", retorno.MatriculaResponsavel);
            Assert.DoesNotContain("PBKDF2", retorno.ResponsavelNome);
        }

        [Fact]
        public void EmpresaSemResponsavel_NaoEstouraEDevolveCamposVazios()
        {
            Empresa empresa = new Empresa { IdEmpresa = 3, Cnpj = CnpjDigitos, RazaoSocial = "Sem equipe", NomeFantasia = "Sem equipe" };

            ReturnEmpresaVO retorno = CriarMapper().Map<ReturnEmpresaVO>(empresa);

            Assert.Equal(0, retorno.IdFuncionarioResponsavel);
            Assert.Equal(string.Empty, retorno.ResponsavelNome);
            Assert.Equal(string.Empty, retorno.ResponsavelEmail);
            Assert.Equal(string.Empty, retorno.MatriculaResponsavel);
        }

        [Fact]
        public void VODeEntrada_NaoGravaIdFlagsNemAuditoriaNaEmpresa()
        {
            CadastroLocadoraVO vo = new CadastroLocadoraVO
            {
                RazaoSocial = "Rota Sul Locadora Ltda",
                NomeFantasia = "Rota Sul",
                Cnpj = CnpjDigitos,
                Email = "contato@rotasul.exemplo.com",
                ResponsavelNome = "Ana",
                ResponsavelCpf = "52998224725",
                ResponsavelEmail = "ana@rotasul.exemplo.com",
                Senha = "SenhaForte1",
                AceiteTermos = true
            };

            Empresa empresa = CriarMapper().Map<Empresa>(vo);

            Assert.Equal(0, empresa.IdEmpresa);
            Assert.Equal(CnpjDigitos, empresa.Cnpj);
            Assert.Equal(default(DateTime), empresa.CreatedAt);
            Assert.Null(empresa.DisabledAt);
            Assert.Empty(empresa.Funcionarios);
        }

        [Fact]
        public void VODeEntrada_LevaODadosDoResponsavelSemSenhaNemCargo()
        {
            CadastroLocadoraVO vo = new CadastroLocadoraVO
            {
                RazaoSocial = "Rota Sul Locadora Ltda",
                NomeFantasia = "Rota Sul",
                Cnpj = CnpjDigitos,
                ResponsavelNome = "Ana Responsável",
                ResponsavelCpf = "52998224725",
                ResponsavelEmail = "ana@rotasul.exemplo.com",
                Senha = "SenhaForte1",
                AceiteTermos = true
            };

            Funcionario responsavel = CriarMapper().Map<Funcionario>(vo);

            Assert.Equal("Ana Responsável", responsavel.Nome);
            Assert.Equal("52998224725", responsavel.Cpf);
            Assert.Equal("ana@rotasul.exemplo.com", responsavel.Email);
            Assert.Null(responsavel.SenhaHash);
            Assert.Equal(0, responsavel.IdFuncionario);
            Assert.Equal(0, responsavel.IdEmpresa);
            Assert.Equal(default(DateOnly), responsavel.DataAdmissao);
        }
    }
}
