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

        private int _totalBuscas;
        private readonly object _trava = new object();

        /// <summary>Quando nao for null, toda consulta lanca esta excecao (simula falha do banco).</summary>
        public Exception? ExcecaoAoBuscar { get; set; }

        /// <summary>
        /// Quando nao for null, a busca por CPF espera este sinal antes de responder (simula banco lento sem tempo fixo).
        /// O teste libera o sinal quando quiser; a espera nao tem tempo limite proprio.
        /// </summary>
        public TaskCompletionSource? Portao { get; set; }

        /// <summary>Quantas buscas por CPF comecaram (inclusive as que esperam o Portao ou lancam). Lido com Volatile.</summary>
        public int TotalBuscas => Volatile.Read(ref _totalBuscas);

        /// <summary>Funcionarios gravados (inclusive os semeados), para os testes conferirem o que foi inserido.</summary>
        public IReadOnlyList<Funcionario> Funcionarios => _funcionarios;

        /// <summary>Clientes semeados (ativos e desativados), na mesma lista que o login le. Usada pelo FakeGoogleVinculoRepository (BE-6) para enxergar os mesmos clientes.</summary>
        public IReadOnlyList<Cliente> Clientes => _clientes;

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

            lock (_trava)
            {
                _clientes.Add(cliente);
            }
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

            lock (_trava)
            {
                _funcionarios.Add(funcionario);
            }
        }

        /// <summary>Busca o cliente ativo pelo CPF; o mais antigo se houver mais de um. Conta a busca e espera o Portao, se houver.</summary>
        public async Task<Cliente?> FindClienteByCpfAsync(string cpf)
        {
            Interlocked.Increment(ref _totalBuscas);

            await AguardarPortaoAsync();

            LancarSeConfigurado();

            Cliente? cliente;

            lock (_trava)
            {
                cliente = _clientes
                    .Where(x => x.Cpf == cpf && x.DisabledAt == null)
                    .OrderBy(x => x.IdCliente)
                    .FirstOrDefault();
            }

            return cliente;
        }

        /// <summary>Busca o cliente ativo pelo id (DisabledAt nulo).</summary>
        public Task<Cliente?> FindClienteByIdAsync(int idCliente)
        {
            LancarSeConfigurado();

            Cliente? cliente = _clientes
                .Where(x => x.IdCliente == idCliente && x.DisabledAt == null)
                .FirstOrDefault();

            return Task.FromResult(cliente);
        }

        /// <summary>Busca o funcionario ativo pelo id (DisabledAt nulo); a regra de Ativo fica no servico.</summary>
        public Task<Funcionario?> FindFuncionarioByIdAsync(int idFuncionario)
        {
            LancarSeConfigurado();

            Funcionario? funcionario = _funcionarios
                .Where(x => x.IdFuncionario == idFuncionario && x.DisabledAt == null)
                .FirstOrDefault();

            return Task.FromResult(funcionario);
        }

        /// <summary>Busca o funcionario ativo (DisabledAt nulo) pelo CPF; a regra de Ativo fica no servico. Conta a busca e espera o Portao, se houver.</summary>
        public async Task<Funcionario?> FindFuncionarioByCpfAsync(string cpf)
        {
            Interlocked.Increment(ref _totalBuscas);

            await AguardarPortaoAsync();

            LancarSeConfigurado();

            Funcionario? funcionario;

            lock (_trava)
            {
                funcionario = _funcionarios
                    .Where(x => x.Cpf == cpf && x.DisabledAt == null)
                    .OrderBy(x => x.IdFuncionario)
                    .FirstOrDefault();
            }

            return funcionario;
        }

        /// <summary>Indica se existe funcionario ativo com o CPF informado.</summary>
        public Task<bool> ExistsFuncionarioByCpfAsync(string cpf)
        {
            LancarSeConfigurado();

            bool existe = _funcionarios.Any(x => x.Cpf == cpf && x.DisabledAt == null);

            return Task.FromResult(existe);
        }

        /// <summary>Indica se existe funcionario com o CPF, ativo ou desativado (sem o filtro de DisabledAt).</summary>
        public Task<bool> ExistsFuncionarioByCpfWithDisabledsAsync(string cpf)
        {
            LancarSeConfigurado();

            bool existe = _funcionarios.Any(x => x.Cpf == cpf);

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

        /// <summary>Espera o Portao quando ele estiver configurado; sem Portao, nao espera nada.</summary>
        private async Task AguardarPortaoAsync()
        {
            TaskCompletionSource? portao = Portao;

            if (portao != null)
            {
                await portao.Task;
            }
        }
    }
}
