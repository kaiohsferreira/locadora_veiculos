using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Equipe;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace AutoVia.Tests.Equipe
{
    /// <summary>
    /// Profile da equipe (P10-BE-03): a configuracao do AutoMapper e valida; o CPF sai mascarado (341.***.***-12) e a
    /// data em DD/MM/AAAA; o VO de saida nao tem CPF completo, senha nem hash.
    /// </summary>
    public class FuncionarioProfileTests
    {
        private static readonly MapperConfiguration Configuracao = new MapperConfiguration(cfg => cfg.AddProfile<FuncionarioProfile>(), NullLoggerFactory.Instance);

        [Fact]
        public void Profile_ConfiguracaoDoAutoMapperEhValida()
        {
            Configuracao.AssertConfigurationIsValid();
        }

        [Fact]
        public void Profile_MascaraOCpfFormataADataEIgnoraEhVoce()
        {
            IMapper mapper = Configuracao.CreateMapper();
            Funcionario funcionario = new Funcionario
            {
                IdFuncionario = 7,
                Nome = "Ana Souza",
                Cpf = "12345678909",
                Matricula = "LOC-07",
                Email = "ana@locadora.com",
                Cargo = CargoFuncionario.GESTOR_FROTA,
                Ativo = false,
                DataAdmissao = new DateOnly(2024, 3, 5),
                SenhaHash = "hash-secreto"
            };

            ReturnFuncionarioVO retorno = mapper.Map<ReturnFuncionarioVO>(funcionario);

            Assert.Equal(7, retorno.IdFuncionario);
            Assert.Equal("123.***.***-09", retorno.CpfMascarado);
            Assert.Equal("05/03/2024", retorno.DataAdmissaoStr);
            Assert.Equal(new DateOnly(2024, 3, 5), retorno.DataAdmissao);
            Assert.Equal(CargoFuncionario.GESTOR_FROTA, retorno.Cargo);
            Assert.False(retorno.Ativo);
            Assert.False(retorno.EhVoce);
        }

        [Fact]
        public void VoDeSaida_NaoTemCpfCompletoNemSenhaNemHash()
        {
            Assert.Null(typeof(ReturnFuncionarioVO).GetProperty("Cpf", BindingFlags.Public | BindingFlags.Instance));
            Assert.Null(typeof(ReturnFuncionarioVO).GetProperty("SenhaHash", BindingFlags.Public | BindingFlags.Instance));
            Assert.Null(typeof(ReturnFuncionarioVO).GetProperty("Senha", BindingFlags.Public | BindingFlags.Instance));
        }

        [Fact]
        public void MascararCpf_MantemTresPrimeirosEDoisUltimosDigitos()
        {
            Assert.Equal("123.***.***-09", StaticMethods.MascararCpf("123.456.789-09"));
            Assert.Equal("123.***.***-09", StaticMethods.MascararCpf("12345678909"));
        }

        [Fact]
        public void MascararCpf_ValorSemOnzeDigitosNaoExpoeNada()
        {
            Assert.Equal(string.Empty, StaticMethods.MascararCpf("123"));
            Assert.Equal(string.Empty, StaticMethods.MascararCpf(null));
        }
    }
}
