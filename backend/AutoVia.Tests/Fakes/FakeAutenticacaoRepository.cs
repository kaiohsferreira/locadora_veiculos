using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// IAutenticacaoRepository em memoria, com as mesmas regras do AutenticacaoRepository real:
    /// ignora registros desativados (DisabledAt) e compara o CPF exatamente (o servico ja
    /// envia so os digitos). Adicionar* semeia dados, inclusive desativados.
    /// Funcionario sem empresa informada entra na primeira empresa cadastrada (ou numa empresa
    /// operadora padrao, criada na primeira vez). A busca por CPF e por id devolve o funcionario com
    /// a empresa associada (como o Include do repositorio real).
    /// ExcecaoAoBuscar simula falha do banco em qualquer consulta.
    /// </summary>
    public class FakeAutenticacaoRepository : IAutenticacaoRepository
    {
        private readonly List<Cliente> _clientes = new List<Cliente>();
        private readonly List<Funcionario> _funcionarios = new List<Funcionario>();
        private readonly List<Empresa> _empresas = new List<Empresa>();
        private int _proximoIdCliente = 1;
        private int _proximoIdFuncionario = 1;
        private int _proximoIdEmpresa = 1;

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

        /// <summary>Empresas semeadas (ativas, desativadas, operadoras ou nao).</summary>
        public IReadOnlyList<Empresa> Empresas => _empresas;

        /// <summary>Semeia uma empresa. Se o id for 0, gera um.</summary>
        public void AdicionarEmpresa(Empresa empresa)
        {
            if (empresa.IdEmpresa == 0)
            {
                empresa.IdEmpresa = _proximoIdEmpresa;
            }

            if (empresa.IdEmpresa >= _proximoIdEmpresa)
            {
                _proximoIdEmpresa = empresa.IdEmpresa + 1;
            }

            lock (_trava)
            {
                _empresas.Add(empresa);
            }
        }

        /// <summary>Semeia um funcionario (ativo ou desativado). Se o id for 0, gera um. Sem empresa informada, usa a empresa padrao.</summary>
        public void AdicionarFuncionario(Funcionario funcionario)
        {
            if (funcionario.IdEmpresa == 0)
            {
                funcionario.IdEmpresa = EmpresaPadrao().IdEmpresa;
            }

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

        /// <summary>Busca o funcionario ativo pelo id (DisabledAt nulo), com a empresa; a regra de Ativo fica no servico.</summary>
        public Task<Funcionario?> FindFuncionarioByIdAsync(int idFuncionario)
        {
            LancarSeConfigurado();

            Funcionario? funcionario = _funcionarios
                .Where(x => x.IdFuncionario == idFuncionario && x.DisabledAt == null)
                .FirstOrDefault();

            if (funcionario != null)
            {
                ComEmpresa(funcionario);
            }

            return Task.FromResult(funcionario);
        }

        /// <summary>Busca a empresa operadora ativa (DisabledAt nulo); nulo se nao houver.</summary>
        public Task<Empresa?> FindEmpresaOperadoraAsync()
        {
            LancarSeConfigurado();

            Empresa? operadora = _empresas
                .Where(x => x.Operadora && x.DisabledAt == null)
                .OrderBy(x => x.IdEmpresa)
                .FirstOrDefault();

            return Task.FromResult(operadora);
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

            if (funcionario != null)
            {
                ComEmpresa(funcionario);
            }

            return funcionario;
        }

        /// <summary>Associa a empresa do funcionario (equivale ao Include(Empresa) do repositorio real).</summary>
        private void ComEmpresa(Funcionario funcionario)
        {
            funcionario.Empresa = _empresas.FirstOrDefault(x => x.IdEmpresa == funcionario.IdEmpresa)!;
        }

        /// <summary>Empresa padrao do fake: a primeira cadastrada; se nao houver, cria uma empresa ativa e operadora.</summary>
        private Empresa EmpresaPadrao()
        {
            Empresa? padrao = _empresas.FirstOrDefault();

            if (padrao == null)
            {
                padrao = new Empresa
                {
                    RazaoSocial = "Locadora de teste Ltda",
                    NomeFantasia = "Locadora Teste",
                    Cnpj = "00000000000000",
                    Ativo = true,
                    Operadora = true
                };

                AdicionarEmpresa(padrao);
            }

            return padrao;
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
