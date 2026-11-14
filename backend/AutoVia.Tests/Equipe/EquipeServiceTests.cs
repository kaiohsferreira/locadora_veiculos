using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Equipe;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Equipe
{
    /// <summary>
    /// Testes do EquipeService (P10-BE-03): a locadora vem do contexto e o id de outra locadora responde 404; listagem
    /// paginada com busca, cargo, situacao e totais da locadora inteira; cadastro com CPF na plataforma (409), matricula
    /// na locadora (409) e senha so como hash; edicao sem CPF; desativar e reativar sem exclusao; travas do proprio
    /// usuario e do ultimo administrador (com 1 e com 2 administradores); invalidacao da sessao; senha fora do log.
    /// Usa o repositorio em memoria com o mesmo escopo do real, o hash real e o log em memoria.
    /// </summary>
    public class EquipeServiceTests
    {
        private const int EmpresaA = 1;
        private const int EmpresaB = 2;
        private const string CpfAdminLogado = "12345678909";
        private const string CpfAtendente = "86288366757";
        private const string CpfVendedorB = "52998224725";
        private const string CpfComTrechoBuscado = "34189200123";
        private const string CpfAdminB = "39053344705";
        private const string CpfNovo = "71428793860";
        private const string CpfMascaradoNovo = "714.287.938-60";
        private const string CpfInvalido = "11111111111";
        private const string SenhaValida = "SenhaNova1";
        private const string SenhaTeste = "SenhaSemente1";
        private const string EmailPadrao = "pessoa@locadora.exemplo.com";

        private static readonly DateOnly DataAdmissaoValida = new DateOnly(2024, 3, 5);

        [Fact]
        public async Task GetAll_DevolvePaginaETotaisSoDaLocadoraDoContexto()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA);
            cenario.Semear("Carlos Lima", CpfVendedorB, "A-003", CargoFuncionario.VENDEDOR, EmpresaA, ativo: false);
            cenario.Semear("Bia Outra", CpfAdminB, "B-001", CargoFuncionario.ADMINISTRADOR, EmpresaB);

            ReturnEquipeVO retorno = await cenario.Servico.GetAllAsync(Filtro());

            Assert.Equal(3, retorno.Total);
            Assert.Equal(3, retorno.Itens.Count);
            Assert.DoesNotContain(retorno.Itens, item => item.Nome == "Bia Outra");
            Assert.Equal(2, retorno.TotalAtivos);
            Assert.Equal(1, retorno.TotalInativos);
            Assert.Equal(1, retorno.TotalAdministradores);
            Assert.Equal(3, retorno.TotalCadastrados);
        }

        [Fact]
        public async Task GetAll_TotaisIgnoramFiltrosMasPaginaRespeitaOsFiltros()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA);
            cenario.Semear("Carlos Lima", CpfVendedorB, "A-003", CargoFuncionario.VENDEDOR, EmpresaA, ativo: false);

            ReturnEquipeVO retorno = await cenario.Servico.GetAllAsync(new FiltroEquipeVO { Cargo = CargoFuncionario.VENDEDOR, Pagina = 1, TamanhoPagina = 20 });

            Assert.Equal(1, retorno.Total);
            Assert.Single(retorno.Itens);
            Assert.Equal(2, retorno.TotalAtivos);
            Assert.Equal(1, retorno.TotalInativos);
            Assert.Equal(3, retorno.TotalCadastrados);
        }

        [Fact]
        public async Task GetAll_BuscaPorNomeCpfSoDigitosEMatriculaSemDiferencaDeMaiusculas()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Ana Souza", CpfAtendente, "LOC-77", CargoFuncionario.ATENDENTE, EmpresaA);
            cenario.Semear("Carla Dias", CpfVendedorB, "LOC-88", CargoFuncionario.VENDEDOR, EmpresaA);

            ReturnEquipeVO porNome = await cenario.Servico.GetAllAsync(Filtro("  ana  "));
            ReturnEquipeVO porCpf = await cenario.Servico.GetAllAsync(Filtro("862.883"));
            ReturnEquipeVO porMatricula = await cenario.Servico.GetAllAsync(Filtro("loc-77"));
            ReturnEquipeVO porPrefixo = await cenario.Servico.GetAllAsync(Filtro("LOC-"));

            Assert.Equal(1, porNome.Total);
            Assert.Equal("Ana Souza", porNome.Itens[0].Nome);
            Assert.Equal(1, porCpf.Total);
            Assert.Equal("Ana Souza", porCpf.Itens[0].Nome);
            Assert.Equal(1, porMatricula.Total);
            Assert.Equal("LOC-77", porMatricula.Itens[0].Matricula);
            Assert.Equal(2, porPrefixo.Total);
        }

        [Fact]
        public async Task GetAll_BuscaComLetraEDigitoNaoProcuraNoCpf()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA);
            cenario.Semear("Carla Dias", CpfVendedorB, "A-003", CargoFuncionario.VENDEDOR, EmpresaA);

            ReturnEquipeVO retorno = await cenario.Servico.GetAllAsync(Filtro("Ana 2"));

            Assert.Equal(0, retorno.Total);
        }

        [Theory]
        [InlineData("341.892")]
        [InlineData("341892")]
        public async Task GetAll_BuscaSoDeDigitosComOuSemMascaraProcuraNoCpf(string busca)
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Ana Souza", CpfComTrechoBuscado, "A-002", CargoFuncionario.ATENDENTE, EmpresaA);
            cenario.Semear("Carla Dias", CpfVendedorB, "A-003", CargoFuncionario.VENDEDOR, EmpresaA);

            ReturnEquipeVO retorno = await cenario.Servico.GetAllAsync(Filtro(busca));

            Assert.Equal(1, retorno.Total);
            Assert.Equal("Ana Souza", retorno.Itens[0].Nome);
        }

        [Fact]
        public async Task GetAll_BuscaDeMatriculaComLetrasENumerosContinuaFuncionando()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Ana Souza", CpfAtendente, "MAT-0001", CargoFuncionario.ATENDENTE, EmpresaA);
            cenario.Semear("Carla Dias", CpfVendedorB, "MAT-0002", CargoFuncionario.VENDEDOR, EmpresaA);

            ReturnEquipeVO retorno = await cenario.Servico.GetAllAsync(Filtro("mat-0001"));

            Assert.Equal(1, retorno.Total);
            Assert.Equal("MAT-0001", retorno.Itens[0].Matricula);
        }

        [Fact]
        public async Task GetAll_BuscaSoDeEspacosNaoFiltra()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA);

            ReturnEquipeVO retorno = await cenario.Servico.GetAllAsync(Filtro("   "));

            Assert.Equal(2, retorno.Total);
        }

        [Fact]
        public async Task GetAll_FiltraPorSituacaoAtivoFalsoSoMostraInativos()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA, ativo: false);

            ReturnEquipeVO retorno = await cenario.Servico.GetAllAsync(new FiltroEquipeVO { Ativo = false, Pagina = 1, TamanhoPagina = 20 });

            Assert.Equal(1, retorno.Total);
            Assert.False(retorno.Itens[0].Ativo);
            Assert.Equal("Ana Souza", retorno.Itens[0].Nome);
        }

        [Fact]
        public async Task GetAll_OrdenaPorNomeEPaginaUsaDeslocamento()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Zeca Pires", "11144477735", "A-010", CargoFuncionario.ATENDENTE, EmpresaA);
            cenario.Semear("Mario Gomes", CpfAtendente, "A-011", CargoFuncionario.ATENDENTE, EmpresaA);

            ReturnEquipeVO primeiraPagina = await cenario.Servico.GetAllAsync(new FiltroEquipeVO { Pagina = 1, TamanhoPagina = 2 });
            ReturnEquipeVO segundaPagina = await cenario.Servico.GetAllAsync(new FiltroEquipeVO { Pagina = 2, TamanhoPagina = 2 });

            Assert.Equal(3, primeiraPagina.Total);
            Assert.Equal(new List<string> { "Admin Logado", "Mario Gomes" }, primeiraPagina.Itens.Select(item => item.Nome).ToList());
            Assert.Equal(new List<string> { "Zeca Pires" }, segundaPagina.Itens.Select(item => item.Nome).ToList());
        }

        [Fact]
        public async Task GetAll_MarcaEhVoceSoParaOProprioUsuario()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA);

            ReturnEquipeVO retorno = await cenario.Servico.GetAllAsync(Filtro());

            ReturnFuncionarioVO marcado = Assert.Single(retorno.Itens, item => item.EhVoce);
            Assert.Equal(1, marcado.IdFuncionario);
        }

        [Fact]
        public async Task GetAll_FalhaDoRepositorioEhRegistradaNoLogERelancada()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Funcionarios.ExcecaoAoConsultar = new InvalidOperationException("falha simulada da consulta");

            await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.GetAllAsync(Filtro()));

            Assert.Contains(cenario.Log.Snapshot(), entrada => entrada.Contains(ConstantsMessage.ErroListarEquipe, StringComparison.Ordinal));
        }

        [Fact]
        public async Task GetById_DeOutraLocadora_DevolveNotFoundComAMesmaMensagem()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario deOutra = cenario.Semear("Bia Outra", CpfAdminB, "B-001", CargoFuncionario.ADMINISTRADOR, EmpresaB);

            NotFoundException ex = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.GetByIdAsync(deOutra.IdFuncionario));

            Assert.Equal(ConstantsMessage.ErroEquipeFuncionarioNaoEncontrado, ex.Message);
        }

        [Fact]
        public async Task GetById_InexistenteOuDesativadoDevolveNotFound()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario excluido = cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA, desativadoEm: DateTime.Now);

            await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.GetByIdAsync(excluido.IdFuncionario));
            await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.GetByIdAsync(999));
        }

        [Fact]
        public async Task GetById_InativoAindaConsultavelEMostraCpfMascarado()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario inativo = cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA, ativo: false);

            ReturnFuncionarioVO retorno = await cenario.Servico.GetByIdAsync(inativo.IdFuncionario);

            Assert.False(retorno.Ativo);
            Assert.Equal("862.***.***-57", retorno.CpfMascarado);
            Assert.Equal("05/03/2024", retorno.DataAdmissaoStr);
        }

        [Fact]
        public async Task Insert_GravaNaLocadoraDoContextoComAtivoEHashDaSenha()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            FuncionarioVO vo = VoCadastro(cpf: CpfMascaradoNovo, matricula: "  LOC-01  ", email: "  Ana.Souza@Locadora.COM  ", cargo: CargoFuncionario.VENDEDOR);

            ReturnFuncionarioVO retorno = await cenario.Servico.InsertAsync(vo);

            Funcionario gravado = Assert.Single(cenario.Armazenamento.Funcionarios, x => x.IdFuncionario == retorno.IdFuncionario);
            Assert.Equal(EmpresaA, gravado.IdEmpresa);
            Assert.True(gravado.Ativo);
            Assert.Null(gravado.DisabledAt);
            Assert.Equal(CpfNovo, gravado.Cpf);
            Assert.Equal("LOC-01", gravado.Matricula);
            Assert.Equal("ana.souza@locadora.com", gravado.Email);
            Assert.Equal(CargoFuncionario.VENDEDOR, gravado.Cargo);
            Assert.NotNull(gravado.SenhaHash);
            Assert.NotEqual(SenhaValida, gravado.SenhaHash);
            Assert.True(HasherDeTeste.Verify(SenhaValida, gravado.SenhaHash!));
            Assert.Equal("714.***.***-60", retorno.CpfMascarado);
            Assert.Equal("05/03/2024", retorno.DataAdmissaoStr);
            Assert.False(retorno.EhVoce);
        }

        [Fact]
        public async Task Insert_CargoAdministradorEAtribuivel()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();

            ReturnFuncionarioVO retorno = await cenario.Servico.InsertAsync(VoCadastro(cargo: CargoFuncionario.ADMINISTRADOR));

            Assert.Equal(CargoFuncionario.ADMINISTRADOR, retorno.Cargo);
        }

        [Fact]
        public async Task Insert_CpfUsadoPorFuncionarioAtivoDeOutraLocadora_Conflito409()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Vendedor B", CpfNovo, "B-009", CargoFuncionario.VENDEDOR, EmpresaB);

            BusinessException ex = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.InsertAsync(VoCadastro()));

            Assert.Equal(ConstantsMessage.ErroEquipeCpfDuplicado, ex.Message);
        }

        [Fact]
        public async Task Insert_CpfDeFuncionarioInativoContinuaReservado()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Inativo Antigo", CpfNovo, "A-050", CargoFuncionario.ATENDENTE, EmpresaA, ativo: false);

            BusinessException ex = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.InsertAsync(VoCadastro()));

            Assert.Equal(ConstantsMessage.ErroEquipeCpfDuplicado, ex.Message);
        }

        [Fact]
        public async Task Insert_CpfSoDeFuncionarioDesativadoNaoConflita()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Excluido", CpfNovo, "A-050", CargoFuncionario.ATENDENTE, EmpresaA, desativadoEm: DateTime.Now);

            ReturnFuncionarioVO retorno = await cenario.Servico.InsertAsync(VoCadastro(matricula: "A-051"));

            Assert.True(retorno.IdFuncionario > 0);
        }

        [Fact]
        public async Task Insert_MatriculaRepetidaNaMesmaLocadora_Conflito409SemDiferencaDeMaiusculasOuBordas()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Ana Souza", CpfAtendente, "LOC-01", CargoFuncionario.ATENDENTE, EmpresaA);

            BusinessException ex = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.InsertAsync(VoCadastro(matricula: "  loc-01 ")));

            Assert.Equal(ConstantsMessage.ErroEquipeMatriculaDuplicada, ex.Message);
        }

        [Fact]
        public async Task Insert_MatriculaDeOutraLocadora_EPermitida()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Bia Outra", CpfAdminB, "LOC-01", CargoFuncionario.ATENDENTE, EmpresaB);

            ReturnFuncionarioVO retorno = await cenario.Servico.InsertAsync(VoCadastro(matricula: "LOC-01"));

            Assert.True(retorno.IdFuncionario > 0);
        }

        [Fact]
        public async Task Insert_CpfEhConferidoAntesDaMatricula()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Ana Souza", CpfAtendente, "LOC-01", CargoFuncionario.ATENDENTE, EmpresaA);
            cenario.Semear("Vendedor B", CpfNovo, "B-009", CargoFuncionario.VENDEDOR, EmpresaB);

            BusinessException ex = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.InsertAsync(VoCadastro(matricula: "LOC-01")));

            Assert.Equal(ConstantsMessage.ErroEquipeCpfDuplicado, ex.Message);
        }

        [Fact]
        public async Task Insert_FalhaDeGravacaoRegistraNoLogSemSenha()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Funcionarios.ExcecaoAoConsultar = new InvalidOperationException("falha simulada");

            await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.InsertAsync(VoCadastro()));

            List<string> entradas = cenario.Log.Snapshot();
            Assert.Contains(entradas, entrada => entrada.Contains(ConstantsMessage.ErroSalvarFuncionario, StringComparison.Ordinal));
            Assert.DoesNotContain(entradas, entrada => entrada.Contains(SenhaValida, StringComparison.Ordinal));
        }

        [Fact]
        public async Task Update_CopiaCamposEditaveisSemAlterarCpfNemSenha()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario atendente = cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA);
            string hashAntes = atendente.SenhaHash!;

            await cenario.Servico.UpdateAsync(atendente.IdFuncionario, new FuncionarioEdicaoVO
            {
                Nome = "  Ana Souza Lima  ",
                Matricula = "A-222",
                Email = EmailPadrao,
                DataAdmissao = new DateOnly(2023, 1, 2),
                Cargo = CargoFuncionario.FINANCEIRO
            });

            Funcionario depois = cenario.Armazenamento.Funcionarios.Single(x => x.IdFuncionario == atendente.IdFuncionario);
            Assert.Equal("Ana Souza Lima", depois.Nome);
            Assert.Equal("A-222", depois.Matricula);
            Assert.Equal(EmailPadrao, depois.Email);
            Assert.Equal(new DateOnly(2023, 1, 2), depois.DataAdmissao);
            Assert.Equal(CargoFuncionario.FINANCEIRO, depois.Cargo);
            Assert.Equal(CpfAtendente, depois.Cpf);
            Assert.Equal(hashAntes, depois.SenhaHash);
            Assert.Equal(EmpresaA, depois.IdEmpresa);
        }

        [Fact]
        public async Task Update_MudancaDeCargoInvalidaASessaoDoFuncionario()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario atendente = cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA);

            await cenario.Servico.UpdateAsync(atendente.IdFuncionario, EdicaoComCargo(CargoFuncionario.VENDEDOR));

            Assert.Equal(new List<int> { atendente.IdFuncionario }, cenario.Sessao.IdsInvalidados);
        }

        [Fact]
        public async Task Update_SemMudancaDeCargoNaoInvalidaASessao()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario atendente = cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA);

            await cenario.Servico.UpdateAsync(atendente.IdFuncionario, EdicaoComCargo(CargoFuncionario.ATENDENTE));

            Assert.Empty(cenario.Sessao.IdsInvalidados);
        }

        [Fact]
        public async Task Update_DeOutraLocadora_DevolveNotFoundESemAlterar()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario deOutra = cenario.Semear("Bia Outra", CpfAdminB, "B-001", CargoFuncionario.ATENDENTE, EmpresaB);

            NotFoundException ex = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.UpdateAsync(deOutra.IdFuncionario, EdicaoComCargo(CargoFuncionario.VENDEDOR)));

            Assert.Equal(ConstantsMessage.ErroEquipeFuncionarioNaoEncontrado, ex.Message);
            Assert.Equal(CargoFuncionario.ATENDENTE, deOutra.Cargo);
            Assert.Empty(cenario.Sessao.IdsInvalidados);
        }

        [Fact]
        public async Task Update_DesativadoDevolveNotFound()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario excluido = cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA, desativadoEm: DateTime.Now);

            await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.UpdateAsync(excluido.IdFuncionario, EdicaoComCargo(CargoFuncionario.ATENDENTE)));
        }

        [Fact]
        public async Task Update_MatriculaDeOutroNaMesmaLocadora_Conflito409()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA);
            Funcionario vendedor = cenario.Semear("Carla Dias", CpfVendedorB, "A-003", CargoFuncionario.VENDEDOR, EmpresaA);

            BusinessException ex = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.UpdateAsync(vendedor.IdFuncionario, EdicaoComMatricula("a-002")));

            Assert.Equal(ConstantsMessage.ErroEquipeMatriculaDuplicada, ex.Message);
        }

        [Fact]
        public async Task Update_ManterAPropriaMatriculaComOutraCaixaEPermitido()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario atendente = cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA);

            await cenario.Servico.UpdateAsync(atendente.IdFuncionario, EdicaoComMatricula("  a-002 "));

            Assert.Equal("a-002", cenario.Armazenamento.Funcionarios.Single(x => x.IdFuncionario == atendente.IdFuncionario).Matricula);
        }

        [Fact]
        public async Task Update_ProprioCargo_Conflito409DaPropriaConta()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario admin = AdminLogado(cenario);

            BusinessException ex = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.UpdateAsync(admin.IdFuncionario, EdicaoComCargo(CargoFuncionario.ATENDENTE)));

            Assert.Equal(ConstantsMessage.ErroEquipePropriaConta, ex.Message);
            Assert.Equal(CargoFuncionario.ADMINISTRADOR, admin.Cargo);
        }

        [Fact]
        public async Task Update_ProprioNomeComMesmoCargoEPermitido()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario admin = AdminLogado(cenario);
            cenario.Semear("Outro Admin", CpfAdminB, "A-009", CargoFuncionario.ADMINISTRADOR, EmpresaA);

            await cenario.Servico.UpdateAsync(admin.IdFuncionario, EdicaoComCargo(CargoFuncionario.ADMINISTRADOR));

            Assert.Empty(cenario.Sessao.IdsInvalidados);
        }

        [Fact]
        public async Task Update_UltimoAdministradorAtivoComUmSoAdmin_Conflito409()
        {
            CenarioEquipe cenario = new CenarioEquipe(EmpresaA, idFuncionarioLogado: 99);
            Funcionario unico = cenario.Semear("Unico Admin", CpfAdminLogado, "A-001", CargoFuncionario.ADMINISTRADOR, EmpresaA);

            BusinessException ex = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.UpdateAsync(unico.IdFuncionario, EdicaoComCargo(CargoFuncionario.ATENDENTE)));

            Assert.Equal(ConstantsMessage.ErroEquipeUltimoAdministrador, ex.Message);
            Assert.Equal(CargoFuncionario.ADMINISTRADOR, unico.Cargo);
            Assert.Empty(cenario.Sessao.IdsInvalidados);
        }

        [Fact]
        public async Task Update_UltimoAdministradorComDoisAdmins_EPermitidoERevalidaASessao()
        {
            CenarioEquipe cenario = new CenarioEquipe(EmpresaA, idFuncionarioLogado: 99);
            Funcionario primeiro = cenario.Semear("Admin Um", CpfAdminLogado, "A-001", CargoFuncionario.ADMINISTRADOR, EmpresaA);
            cenario.Semear("Admin Dois", CpfAdminB, "A-002", CargoFuncionario.ADMINISTRADOR, EmpresaA);

            await cenario.Servico.UpdateAsync(primeiro.IdFuncionario, EdicaoComCargo(CargoFuncionario.ATENDENTE));

            Assert.Equal(CargoFuncionario.ATENDENTE, primeiro.Cargo);
            Assert.Equal(new List<int> { primeiro.IdFuncionario }, cenario.Sessao.IdsInvalidados);
        }

        [Fact]
        public async Task Update_AdministradorInativoPodeSerRebaixadoMesmoSendoOUnicoAdmin()
        {
            CenarioEquipe cenario = new CenarioEquipe(EmpresaA, idFuncionarioLogado: 99);
            Funcionario inativo = cenario.Semear("Admin Parado", CpfAdminLogado, "A-001", CargoFuncionario.ADMINISTRADOR, EmpresaA, ativo: false);

            await cenario.Servico.UpdateAsync(inativo.IdFuncionario, EdicaoComCargo(CargoFuncionario.ATENDENTE));

            Assert.Equal(CargoFuncionario.ATENDENTE, inativo.Cargo);
        }

        [Fact]
        public async Task AlterarSituacao_DesativarGravaAtivoFalsoSemExclusaoEInvalidaASessao()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario atendente = cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA);

            await cenario.Servico.AlterarSituacaoAsync(atendente.IdFuncionario, new SituacaoFuncionarioVO { Ativo = false });

            Funcionario depois = cenario.Armazenamento.Funcionarios.Single(x => x.IdFuncionario == atendente.IdFuncionario);
            Assert.False(depois.Ativo);
            Assert.Null(depois.DisabledAt);
            Assert.Equal(new List<int> { atendente.IdFuncionario }, cenario.Sessao.IdsInvalidados);
        }

        [Fact]
        public async Task AlterarSituacao_ReativarVoltaAtivoEInvalidaASessao()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario inativo = cenario.Semear("Ana Souza", CpfAtendente, "A-002", CargoFuncionario.ATENDENTE, EmpresaA, ativo: false);

            await cenario.Servico.AlterarSituacaoAsync(inativo.IdFuncionario, new SituacaoFuncionarioVO { Ativo = true });

            Assert.True(cenario.Armazenamento.Funcionarios.Single(x => x.IdFuncionario == inativo.IdFuncionario).Ativo);
            Assert.Single(cenario.Sessao.IdsInvalidados);
        }

        [Fact]
        public async Task AlterarSituacao_DesativarAPropriaConta_Conflito409DaPropriaConta()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario admin = AdminLogado(cenario);

            BusinessException ex = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.AlterarSituacaoAsync(admin.IdFuncionario, new SituacaoFuncionarioVO { Ativo = false }));

            Assert.Equal(ConstantsMessage.ErroEquipePropriaConta, ex.Message);
            Assert.True(admin.Ativo);
            Assert.Empty(cenario.Sessao.IdsInvalidados);
        }

        [Fact]
        public async Task AlterarSituacao_UltimoAdministradorAtivoComUmSoAdmin_Conflito409()
        {
            CenarioEquipe cenario = new CenarioEquipe(EmpresaA, idFuncionarioLogado: 99);
            Funcionario unico = cenario.Semear("Unico Admin", CpfAdminLogado, "A-001", CargoFuncionario.ADMINISTRADOR, EmpresaA);

            BusinessException ex = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.AlterarSituacaoAsync(unico.IdFuncionario, new SituacaoFuncionarioVO { Ativo = false }));

            Assert.Equal(ConstantsMessage.ErroEquipeUltimoAdministrador, ex.Message);
            Assert.True(unico.Ativo);
        }

        [Fact]
        public async Task AlterarSituacao_UltimoAdministradorComDoisAdmins_EPermitido()
        {
            CenarioEquipe cenario = new CenarioEquipe(EmpresaA, idFuncionarioLogado: 99);
            Funcionario primeiro = cenario.Semear("Admin Um", CpfAdminLogado, "A-001", CargoFuncionario.ADMINISTRADOR, EmpresaA);
            cenario.Semear("Admin Dois", CpfAdminB, "A-002", CargoFuncionario.ADMINISTRADOR, EmpresaA);

            await cenario.Servico.AlterarSituacaoAsync(primeiro.IdFuncionario, new SituacaoFuncionarioVO { Ativo = false });

            Assert.False(primeiro.Ativo);
        }

        [Fact]
        public async Task AlterarSituacao_DeOutraLocadoraOuDesativado_DevolveNotFound()
        {
            CenarioEquipe cenario = CenarioComAdministradorLogado();
            Funcionario deOutra = cenario.Semear("Bia Outra", CpfAdminB, "B-001", CargoFuncionario.ADMINISTRADOR, EmpresaB);
            Funcionario excluido = cenario.Semear("Excluido", CpfVendedorB, "A-009", CargoFuncionario.ATENDENTE, EmpresaA, desativadoEm: DateTime.Now);

            NotFoundException ex = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.AlterarSituacaoAsync(deOutra.IdFuncionario, new SituacaoFuncionarioVO { Ativo = false }));
            await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.AlterarSituacaoAsync(excluido.IdFuncionario, new SituacaoFuncionarioVO { Ativo = false }));

            Assert.Equal(ConstantsMessage.ErroEquipeFuncionarioNaoEncontrado, ex.Message);
            Assert.True(deOutra.Ativo);
            Assert.Empty(cenario.Sessao.IdsInvalidados);
        }

        /// <summary>Hasher real usado pelo servico e pela conferencia.</summary>
        private static readonly SenhaHasher HasherDeTeste = new SenhaHasher(new FakeLogService());

        /// <summary>Cenario em que o ADMINISTRADOR logado e o primeiro funcionario semeado (id 1), na locadora A.</summary>
        private static CenarioEquipe CenarioComAdministradorLogado()
        {
            CenarioEquipe cenario = new CenarioEquipe(EmpresaA, idFuncionarioLogado: 1);
            cenario.Semear("Admin Logado", CpfAdminLogado, "A-001", CargoFuncionario.ADMINISTRADOR, EmpresaA);

            return cenario;
        }

        /// <summary>Funcionario logado do cenario com administrador (id 1), lido do armazenamento.</summary>
        private static Funcionario AdminLogado(CenarioEquipe cenario)
        {
            return cenario.Armazenamento.Funcionarios.Single(x => x.IdFuncionario == 1);
        }

        private static FiltroEquipeVO Filtro(string? busca = null)
        {
            return new FiltroEquipeVO { Busca = busca, Pagina = 1, TamanhoPagina = 20 };
        }

        private static FuncionarioVO VoCadastro(string cpf = CpfNovo, string matricula = "LOC-01", string email = "ana@locadora.com", CargoFuncionario cargo = CargoFuncionario.ATENDENTE)
        {
            return new FuncionarioVO
            {
                Nome = "Ana Souza",
                Cpf = cpf,
                Matricula = matricula,
                Email = email,
                DataAdmissao = DataAdmissaoValida,
                Cargo = cargo,
                SenhaInicial = SenhaValida
            };
        }

        private static FuncionarioEdicaoVO EdicaoComCargo(CargoFuncionario cargo)
        {
            return new FuncionarioEdicaoVO
            {
                Nome = "Nome Editado",
                Matricula = "EDT-01",
                Email = EmailPadrao,
                DataAdmissao = DataAdmissaoValida,
                Cargo = cargo
            };
        }

        private static FuncionarioEdicaoVO EdicaoComMatricula(string matricula)
        {
            return new FuncionarioEdicaoVO
            {
                Nome = "Nome Editado",
                Matricula = matricula,
                Email = EmailPadrao,
                DataAdmissao = DataAdmissaoValida,
                Cargo = CargoFuncionario.ATENDENTE
            };
        }

        /// <summary>
        /// Montagem de um servico de equipe com repositorio, sessao e log em memoria. O funcionario logado e definido no
        /// contexto real (ADMINISTRADOR, locadora informada). Semear grava no armazenamento do login.
        /// </summary>
        private sealed class CenarioEquipe
        {
            private static readonly MapperConfiguration ConfiguracaoMapper = new MapperConfiguration(cfg => cfg.AddProfile<FuncionarioProfile>(), NullLoggerFactory.Instance);

            public CenarioEquipe(int idEmpresaLogada, int idFuncionarioLogado)
            {
                Armazenamento = new FakeAutenticacaoRepository();
                Log = new FakeLogService();
                Contexto = new ContextoEmpresa(Log);
                Contexto.Definir(idEmpresaLogada, idFuncionarioLogado, CargoFuncionario.ADMINISTRADOR, false);
                Funcionarios = new FakeFuncionarioRepository(Armazenamento, Contexto);
                Empresas = new FakeEmpresaRepository(Armazenamento);
                Sessao = new FakeSessaoService();
                Servico = new EquipeService(Funcionarios, Empresas, HasherDeTeste, Sessao, Contexto, ConfiguracaoMapper.CreateMapper(), Log);
            }

            public FakeAutenticacaoRepository Armazenamento { get; }

            public FakeFuncionarioRepository Funcionarios { get; }

            public FakeEmpresaRepository Empresas { get; }

            public FakeSessaoService Sessao { get; }

            public FakeLogService Log { get; }

            public ContextoEmpresa Contexto { get; }

            public EquipeService Servico { get; }

            /// <summary>Grava um funcionario no armazenamento, com hash de teste e e-mail padrao.</summary>
            public Funcionario Semear(string nome, string cpf, string matricula, CargoFuncionario cargo, int idEmpresa, bool ativo = true, DateTime? desativadoEm = null)
            {
                Funcionario funcionario = new Funcionario
                {
                    Nome = nome,
                    Cpf = cpf,
                    Matricula = matricula,
                    Cargo = cargo,
                    Ativo = ativo,
                    IdEmpresa = idEmpresa,
                    DataAdmissao = DataAdmissaoValida,
                    Email = EmailPadrao,
                    SenhaHash = HasherDeTeste.Hash(SenhaTeste),
                    DisabledAt = desativadoEm
                };

                Armazenamento.AdicionarFuncionario(funcionario);

                return funcionario;
            }
        }
    }
}
