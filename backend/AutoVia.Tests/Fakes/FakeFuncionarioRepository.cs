using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// IFuncionarioRepository em memoria, com o mesmo escopo do repositorio real: so enxerga os funcionarios da locadora
    /// do contexto informado (filtro "Empresa") e ignora os desativados (DisabledAt). Compartilha o armazenamento do login
    /// (FakeAutenticacaoRepository) para a sessao enxergar o cargo e a situacao gravados pela equipe. Pode simular falha.
    /// </summary>
    public class FakeFuncionarioRepository : IFuncionarioRepository
    {
        private readonly FakeAutenticacaoRepository _armazenamento;
        private readonly IContextoEmpresa _contextoEmpresa;

        public FakeFuncionarioRepository(FakeAutenticacaoRepository armazenamento, IContextoEmpresa contextoEmpresa)
        {
            _armazenamento = armazenamento;
            _contextoEmpresa = contextoEmpresa;
        }

        /// <summary>Quando nao for null, toda consulta e gravacao lanca esta excecao (simula falha do banco).</summary>
        public Exception? ExcecaoAoConsultar { get; set; }

        public Task<bool> ExistsByMatriculaAsync(string matricula, int? idFuncionarioIgnorado)
        {
            LancarSeConfigurado();

            string alvo = matricula.Trim();
            bool existe = DaLocadora().Any(x => string.Equals(x.Matricula.Trim(), alvo, StringComparison.OrdinalIgnoreCase)
                && (idFuncionarioIgnorado == null || x.IdFuncionario != idFuncionarioIgnorado.Value));

            return Task.FromResult(existe);
        }

        public Task<Funcionario?> FindByIdAsync(int idFuncionario)
        {
            LancarSeConfigurado();

            Funcionario? funcionario = DaLocadora().FirstOrDefault(x => x.IdFuncionario == idFuncionario);

            return Task.FromResult(funcionario);
        }

        public Task<List<Funcionario>> FindAllAsync(string? busca, string? digitosCpf, CargoFuncionario? cargo, bool? ativo, int deslocamento, int quantidade)
        {
            LancarSeConfigurado();

            List<Funcionario> pagina = Filtrar(busca, digitosCpf, cargo, ativo)
                .OrderBy(x => x.Nome, StringComparer.Ordinal)
                .ThenBy(x => x.IdFuncionario)
                .Skip(deslocamento)
                .Take(quantidade)
                .ToList();

            return Task.FromResult(pagina);
        }

        public Task<int> CountAsync(string? busca, string? digitosCpf, CargoFuncionario? cargo, bool? ativo)
        {
            LancarSeConfigurado();

            int total = Filtrar(busca, digitosCpf, cargo, ativo).Count();

            return Task.FromResult(total);
        }

        public Task<int> CountAdministradoresAtivosAsync(int? idFuncionarioIgnorado)
        {
            LancarSeConfigurado();

            int total = DaLocadora().Count(x => x.Ativo
                && x.Cargo == CargoFuncionario.ADMINISTRADOR
                && (idFuncionarioIgnorado == null || x.IdFuncionario != idFuncionarioIgnorado.Value));

            return Task.FromResult(total);
        }

        public Task<Funcionario> InsertAsync(Funcionario funcionario)
        {
            LancarSeConfigurado();

            funcionario.IdFuncionario = 0;
            funcionario.CreatedAt = DateTime.Now;
            funcionario.UpdatedAt = DateTime.Now;
            funcionario.DisabledAt = null;
            _armazenamento.AdicionarFuncionario(funcionario);

            return Task.FromResult(funcionario);
        }

        public Task<bool> UpdateAsync(Funcionario funcionario)
        {
            LancarSeConfigurado();

            Funcionario? salvo = DaLocadora().FirstOrDefault(x => x.IdFuncionario == funcionario.IdFuncionario);

            if (salvo == null)
            {
                return Task.FromResult(false);
            }

            salvo.Nome = funcionario.Nome;
            salvo.Matricula = funcionario.Matricula;
            salvo.Email = funcionario.Email;
            salvo.DataAdmissao = funcionario.DataAdmissao;
            salvo.Cargo = funcionario.Cargo;
            salvo.UpdatedAt = DateTime.Now;

            return Task.FromResult(true);
        }

        public Task<bool> SetSituacaoAsync(int idFuncionario, bool ativo)
        {
            LancarSeConfigurado();

            Funcionario? salvo = DaLocadora().FirstOrDefault(x => x.IdFuncionario == idFuncionario);

            if (salvo == null)
            {
                return Task.FromResult(false);
            }

            salvo.Ativo = ativo;
            salvo.UpdatedAt = DateTime.Now;

            return Task.FromResult(true);
        }

        /// <summary>Funcionarios ativos (DisabledAt nulo) da locadora do contexto: o filtro "Empresa" do repositorio real.</summary>
        private IEnumerable<Funcionario> DaLocadora()
        {
            int idEmpresa = _contextoEmpresa.IdEmpresa;

            if (idEmpresa <= 0)
            {
                return Enumerable.Empty<Funcionario>();
            }

            return _armazenamento.Funcionarios.Where(x => x.IdEmpresa == idEmpresa && x.DisabledAt == null);
        }

        /// <summary>Aplica busca (nome ou matricula por texto; CPF por digitos), cargo e situacao, como a consulta real.</summary>
        private IEnumerable<Funcionario> Filtrar(string? busca, string? digitosCpf, CargoFuncionario? cargo, bool? ativo)
        {
            IEnumerable<Funcionario> consulta = DaLocadora();

            if (busca != null || digitosCpf != null)
            {
                consulta = consulta.Where(x => (busca != null && (x.Nome.ToLowerInvariant().Contains(busca) || x.Matricula.ToLowerInvariant().Contains(busca)))
                    || (digitosCpf != null && x.Cpf.Contains(digitosCpf)));
            }

            if (cargo.HasValue)
            {
                consulta = consulta.Where(x => x.Cargo == cargo.Value);
            }

            if (ativo.HasValue)
            {
                consulta = consulta.Where(x => x.Ativo == ativo.Value);
            }

            return consulta;
        }

        private void LancarSeConfigurado()
        {
            if (ExcecaoAoConsultar != null)
            {
                throw ExcecaoAoConsultar;
            }
        }
    }
}
