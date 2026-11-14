using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Equipe;
using AutoVia.Communication.ViewObjects.Equipe;
using AutoVia.Helpers;
using FluentValidation.Results;
using System.Reflection;

namespace AutoVia.Tests.Equipe
{
    /// <summary>
    /// Validadores da equipe (P10-BE-03): cada campo do cadastro, da edicao, da situacao e do filtro, com os limites
    /// (nome 120, matricula 20, e-mail 120, senha 8 a 64 com maiuscula e numero, busca 60, pagina 1..100) e a data de
    /// admissao que nao pode ser futura. Reflexao: nenhum VO de entrada tem id de empresa, e a edicao nao tem CPF nem senha.
    /// </summary>
    public class EquipeValidadoresTests
    {
        private const string CpfValido = "71428793860";
        private const string CpfValidoComMascara = "714.287.938-60";
        private const string SenhaValida = "Senha123";
        private const string EmailValido = "ana@locadora.exemplo.com";

        private static readonly FuncionarioVOValidator ValidadorCadastro = new FuncionarioVOValidator();
        private static readonly FuncionarioEdicaoVOValidator ValidadorEdicao = new FuncionarioEdicaoVOValidator();
        private static readonly SituacaoFuncionarioVOValidator ValidadorSituacao = new SituacaoFuncionarioVOValidator();
        private static readonly FiltroEquipeVOValidator ValidadorFiltro = new FiltroEquipeVOValidator();

        [Fact]
        public void Cadastro_DadosValidos_NaoTemErro()
        {
            ValidationResult resultado = ValidadorCadastro.Validate(CadastroValido());

            Assert.True(resultado.IsValid, string.Join(" | ", Mensagens(resultado)));
        }

        [Fact]
        public void Cadastro_CpfComMascaraEhAceito()
        {
            FuncionarioVO vo = CadastroValido();
            vo.Cpf = CpfValidoComMascara;

            Assert.True(ValidadorCadastro.Validate(vo).IsValid);
        }

        [Fact]
        public void Cadastro_NomeVazioOuAcimaDoLimite()
        {
            FuncionarioVO vazio = CadastroValido();
            vazio.Nome = "   ";
            FuncionarioVO longo = CadastroValido();
            longo.Nome = new string('a', 121);

            Assert.Contains(ConstantsMessage.ErroEquipeNomeObrigatorio, Mensagens(ValidadorCadastro.Validate(vazio)));
            Assert.Contains(string.Format(ConstantsMessage.ErroEquipeCampoTamanho, "nome", 120), Mensagens(ValidadorCadastro.Validate(longo)));
        }

        [Fact]
        public void Cadastro_NomeNoLimiteDe120EhAceito()
        {
            FuncionarioVO vo = CadastroValido();
            vo.Nome = new string('a', 120);

            Assert.True(ValidadorCadastro.Validate(vo).IsValid);
        }

        [Fact]
        public void Cadastro_CpfVazioOuInvalido()
        {
            FuncionarioVO vazio = CadastroValido();
            vazio.Cpf = string.Empty;
            FuncionarioVO invalido = CadastroValido();
            invalido.Cpf = "11111111111";

            Assert.Contains(ConstantsMessage.ErroEquipeCpfObrigatorio, Mensagens(ValidadorCadastro.Validate(vazio)));
            Assert.Contains(ConstantsMessage.ErroEquipeCpfInvalido, Mensagens(ValidadorCadastro.Validate(invalido)));
        }

        [Fact]
        public void Cadastro_MatriculaVaziaOuAcimaDoLimite()
        {
            FuncionarioVO vazia = CadastroValido();
            vazia.Matricula = " ";
            FuncionarioVO longa = CadastroValido();
            longa.Matricula = new string('1', 21);
            FuncionarioVO noLimite = CadastroValido();
            noLimite.Matricula = new string('1', 20);

            Assert.Contains(ConstantsMessage.ErroEquipeMatriculaObrigatoria, Mensagens(ValidadorCadastro.Validate(vazia)));
            Assert.Contains(string.Format(ConstantsMessage.ErroEquipeCampoTamanho, "matrícula", 20), Mensagens(ValidadorCadastro.Validate(longa)));
            Assert.True(ValidadorCadastro.Validate(noLimite).IsValid);
        }

        [Fact]
        public void Cadastro_EmailObrigatorioValidoELimitado()
        {
            FuncionarioVO vazio = CadastroValido();
            vazio.Email = string.Empty;
            FuncionarioVO invalido = CadastroValido();
            invalido.Email = "ana-sem-arroba";
            FuncionarioVO longo = CadastroValido();
            longo.Email = new string('a', 115) + "@x.com";

            Assert.Contains(ConstantsMessage.ErroEquipeEmailObrigatorio, Mensagens(ValidadorCadastro.Validate(vazio)));
            Assert.Contains(ConstantsMessage.ErroEquipeEmailInvalido, Mensagens(ValidadorCadastro.Validate(invalido)));
            Assert.Contains(string.Format(ConstantsMessage.ErroEquipeCampoTamanho, "e-mail", 120), Mensagens(ValidadorCadastro.Validate(longo)));
        }

        [Fact]
        public void Cadastro_DataAdmissaoObrigatoriaENaoFutura()
        {
            FuncionarioVO padrao = CadastroValido();
            padrao.DataAdmissao = default;
            FuncionarioVO futura = CadastroValido();
            futura.DataAdmissao = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
            FuncionarioVO hoje = CadastroValido();
            hoje.DataAdmissao = DateOnly.FromDateTime(DateTime.Now);

            Assert.Contains(ConstantsMessage.ErroEquipeDataAdmissaoObrigatoria, Mensagens(ValidadorCadastro.Validate(padrao)));
            Assert.Contains(ConstantsMessage.ErroEquipeDataAdmissaoFutura, Mensagens(ValidadorCadastro.Validate(futura)));
            Assert.True(ValidadorCadastro.Validate(hoje).IsValid);
        }

        [Fact]
        public void Cadastro_CargoAusenteEhRecusadoECadaUmDosSeisEAtribuivel()
        {
            FuncionarioVO ausente = CadastroValido();
            ausente.Cargo = null;

            Assert.Contains(ConstantsMessage.ErroEquipeCargoObrigatorio, Mensagens(ValidadorCadastro.Validate(ausente)));

            foreach (CargoFuncionario cargo in Enum.GetValues<CargoFuncionario>())
            {
                FuncionarioVO vo = CadastroValido();
                vo.Cargo = cargo;

                Assert.True(ValidadorCadastro.Validate(vo).IsValid, cargo.ToString());
            }
        }

        [Fact]
        public void Cadastro_SenhaSemMaiusculaOuSemNumeroEFraca()
        {
            FuncionarioVO semMaiuscula = CadastroValido();
            semMaiuscula.SenhaInicial = "senha1234";
            FuncionarioVO semNumero = CadastroValido();
            semNumero.SenhaInicial = "SENHAAAA";

            Assert.Contains(ConstantsMessage.ErroEquipeSenhaInicialFraca, Mensagens(ValidadorCadastro.Validate(semMaiuscula)));
            Assert.Contains(ConstantsMessage.ErroEquipeSenhaInicialFraca, Mensagens(ValidadorCadastro.Validate(semNumero)));
        }

        [Fact]
        public void Cadastro_SenhaVaziaCurtaOuLongaRecusadas()
        {
            FuncionarioVO vazia = CadastroValido();
            vazia.SenhaInicial = string.Empty;
            FuncionarioVO curta = CadastroValido();
            curta.SenhaInicial = "Abc1";
            FuncionarioVO longa = CadastroValido();
            longa.SenhaInicial = new string('A', 64) + "1";

            Assert.Contains(ConstantsMessage.ErroEquipeSenhaInicialObrigatoria, Mensagens(ValidadorCadastro.Validate(vazia)));
            Assert.Contains(string.Format(ConstantsMessage.ErroEquipeSenhaInicialTamanho, 8, 64), Mensagens(ValidadorCadastro.Validate(curta)));
            Assert.Contains(string.Format(ConstantsMessage.ErroEquipeSenhaInicialTamanho, 8, 64), Mensagens(ValidadorCadastro.Validate(longa)));
        }

        [Fact]
        public void Cadastro_SenhaNoLimiteDe8E64EhAceita()
        {
            FuncionarioVO minima = CadastroValido();
            minima.SenhaInicial = "Senha123";
            FuncionarioVO maxima = CadastroValido();
            maxima.SenhaInicial = "Aa1" + new string('x', 61);

            Assert.True(ValidadorCadastro.Validate(minima).IsValid);
            Assert.True(ValidadorCadastro.Validate(maxima).IsValid);
        }

        [Fact]
        public void Edicao_DadosValidosEAusenciaDeCargoOuDataFutura()
        {
            FuncionarioEdicaoVO valida = EdicaoValida();
            FuncionarioEdicaoVO semCargo = EdicaoValida();
            semCargo.Cargo = null;
            FuncionarioEdicaoVO futura = EdicaoValida();
            futura.DataAdmissao = DateOnly.FromDateTime(DateTime.Now).AddDays(3);

            Assert.True(ValidadorEdicao.Validate(valida).IsValid);
            Assert.Contains(ConstantsMessage.ErroEquipeCargoObrigatorio, Mensagens(ValidadorEdicao.Validate(semCargo)));
            Assert.Contains(ConstantsMessage.ErroEquipeDataAdmissaoFutura, Mensagens(ValidadorEdicao.Validate(futura)));
        }

        [Fact]
        public void Situacao_AtivoEObrigatorio()
        {
            Assert.Contains(ConstantsMessage.ErroEquipeSituacaoObrigatoria, Mensagens(ValidadorSituacao.Validate(new SituacaoFuncionarioVO { Ativo = null })));
            Assert.True(ValidadorSituacao.Validate(new SituacaoFuncionarioVO { Ativo = true }).IsValid);
            Assert.True(ValidadorSituacao.Validate(new SituacaoFuncionarioVO { Ativo = false }).IsValid);
        }

        [Fact]
        public void Filtro_PaginacaoEBuscaELimitesDoContrato()
        {
            Assert.True(ValidadorFiltro.Validate(new FiltroEquipeVO { Pagina = 1, TamanhoPagina = 20 }).IsValid);
            Assert.Contains(ConstantsMessage.ErroPaginaInvalida, Mensagens(ValidadorFiltro.Validate(new FiltroEquipeVO { Pagina = 0, TamanhoPagina = 20 })));
            Assert.Contains(string.Format(ConstantsMessage.ErroTamanhoPaginaInvalido, 100), Mensagens(ValidadorFiltro.Validate(new FiltroEquipeVO { Pagina = 1, TamanhoPagina = 101 })));
            Assert.Contains(string.Format(ConstantsMessage.ErroEquipeBuscaTamanho, 60), Mensagens(ValidadorFiltro.Validate(new FiltroEquipeVO { Pagina = 1, TamanhoPagina = 20, Busca = new string('b', 61) })));
        }

        [Fact]
        public void Filtro_BuscaDe60EAceitaEBuscaAparadaNaoConta()
        {
            FiltroEquipeVO noLimite = new FiltroEquipeVO { Pagina = 1, TamanhoPagina = 20, Busca = new string('b', 60) };
            FiltroEquipeVO comBordas = new FiltroEquipeVO { Pagina = 1, TamanhoPagina = 20, Busca = "  " + new string('b', 60) + "  " };

            Assert.True(ValidadorFiltro.Validate(noLimite).IsValid);
            Assert.True(ValidadorFiltro.Validate(comBordas).IsValid);
        }

        [Fact]
        public void VOsDeEntradaDaEquipe_NaoTemIdEmpresaNemCamposProibidos()
        {
            Type[] voDeEntrada = new Type[] { typeof(FuncionarioVO), typeof(FuncionarioEdicaoVO), typeof(SituacaoFuncionarioVO), typeof(FiltroEquipeVO) };

            foreach (Type tipo in voDeEntrada)
            {
                Assert.Null(tipo.GetProperty("IdEmpresa", BindingFlags.Public | BindingFlags.Instance));
            }

            Assert.Null(typeof(FuncionarioEdicaoVO).GetProperty("Cpf", BindingFlags.Public | BindingFlags.Instance));
            Assert.Null(typeof(FuncionarioEdicaoVO).GetProperty("SenhaInicial", BindingFlags.Public | BindingFlags.Instance));
        }

        /// <summary>Lista as mensagens de erro do resultado, para asserts por texto da constante.</summary>
        private static List<string> Mensagens(ValidationResult resultado)
        {
            return resultado.Errors.Select(erro => erro.ErrorMessage).ToList();
        }

        private static FuncionarioVO CadastroValido()
        {
            return new FuncionarioVO
            {
                Nome = "Ana Souza",
                Cpf = CpfValido,
                Matricula = "LOC-01",
                Email = EmailValido,
                DataAdmissao = new DateOnly(2024, 3, 5),
                Cargo = CargoFuncionario.ATENDENTE,
                SenhaInicial = SenhaValida
            };
        }

        private static FuncionarioEdicaoVO EdicaoValida()
        {
            return new FuncionarioEdicaoVO
            {
                Nome = "Ana Souza",
                Matricula = "LOC-01",
                Email = EmailValido,
                DataAdmissao = new DateOnly(2024, 3, 5),
                Cargo = CargoFuncionario.VENDEDOR
            };
        }
    }
}
