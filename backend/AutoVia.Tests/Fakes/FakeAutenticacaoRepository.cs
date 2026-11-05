using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// IAutenticacaoRepository em memoria, com as mesmas regras do AutenticacaoRepository real:
    /// ignora registros desativados (DisabledAt) e compara o CPF exatamente (o servico ja
    /// envia so os digitos). Adicionar* semeia dados, inclusive desativados.
    /// ExcecaoAoBuscar simula falha do banco em qualquer consulta.
    /// </summary>
    public class FakeAutenticacaoRepository : IAutenticacaoRepository
    {
        private readonly List<Cliente> _clientes = new List<Cliente>();
        private readonly List<Funcionario> _funcionarios = new List<Funcionario>();
        private int _proximoIdCliente = 1;
        private int _proximoIdFuncionario = 1;

        /// <summary>Quando nao for null, toda consulta lanca esta excecao (simula falha do banco).</summary>
        public Exception? ExcecaoAoBuscar { get; set; }

        /// <summary>Funcionarios gravados (inclusive os semeados), para os testes conferirem o que foi inserido.</summary>
        public IReadOnlyList<Funcionario> Funcionarios => _funcionarios;

        /// <summary>Semeia um cliente (ativo ou desativado). Se o id for 0, gera um.</summary>
        public void AdicionarCliente(Cliente cliente)
        {
            if (cliente.IdCliente == 0)
            {
                cliente.IdCliente = _proximoIdCliente;
            }

            if (cliente.IdCliente >= _proximoIdCliente)
            {
                _proximoIdCliente = cliente.IdCliente + 1;
            }

            _clientes.Add(cliente);
        }

        /// <summary>Semeia um funcionario (ativo ou desativado). Se o id for 0, gera um.</summary>
        public void AdicionarFuncionario(Funcionario funcionario)
        {
            if (funcionario.IdFuncionario == 0)
            {
                funcionario.IdFuncionario = _proximoIdFuncionario;
            }

            if (funcionario.IdFuncionario >= _proximoIdFuncionario)
            {
                _proximoIdFuncionario = funcionario.IdFuncionario + 1;
            }

            _funcionarios.Add(funcionario);
        }

        /// <summary>Busca o cliente ativo pelo CPF; o mais antigo se houver mais de um.</summary>
        public Task<Cliente?> FindClienteByCpfAsync(string cpf)
        {
            LancarSeConfigurado();

            Cliente? cliente = _clientes
                .Where(x => x.Cpf == cpf && x.DisabledAt == null)
                .OrderBy(x => x.IdCliente)
                .FirstOrDefault();

            return Task.FromResult(cliente);
        }

        /// <summary>Busca o funcionario ativo (DisabledAt nulo) pelo CPF; a regra de Ativo fica no servico.</summary>
        public Task<Funcionario?> FindFuncionarioByCpfAsync(string cpf)
        {
            LancarSeConfigurado();

            Funcionario? funcionario = _funcionarios
                .Where(x => x.Cpf == cpf && x.DisabledAt == null)
                .OrderBy(x => x.IdFuncionario)
                .FirstOrDefault();

            return Task.FromResult(funcionario);
        }

        /// <summary>Indica se existe funcionario ativo com o CPF informado.</summary>
        public Task<bool> ExistsFuncionarioByCpfAsync(string cpf)
        {
            LancarSeConfigurado();

            bool existe = _funcionarios.Any(x => x.Cpf == cpf && x.DisabledAt == null);

            return Task.FromResult(existe);
        }

        /// <summary>Grava o funcionario com id gerado, datas de auditoria e DisabledAt nulo.</summary>
        public Task<Funcionario> InsertFuncionarioAsync(Funcionario funcionario)
        {
            LancarSeConfigurado();

            DateTime agora = DateTime.Now;

            funcionario.IdFuncionario = _proximoIdFuncionario;
            _proximoIdFuncionario++;
            funcionario.CreatedAt = agora;
            funcionario.UpdatedAt = agora;
            funcionario.DisabledAt = null;

            _funcionarios.Add(funcionario);

            return Task.FromResult(funcionario);
        }

        private void LancarSeConfigurado()
        {
            if (ExcecaoAoBuscar != null)
            {
                throw ExcecaoAoBuscar;
            }
        }
    }
}
