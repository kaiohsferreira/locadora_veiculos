using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// IEmpresaRepository em memoria, com as mesmas regras do EmpresaRepository real: consultas so sobre registros
    /// com DisabledAt nulo, CPF do responsavel na plataforma inteira e uma unica gravacao (InsertComResponsavelAsync)
    /// para empresa e responsavel. Compartilha o armazenamento com o FakeAutenticacaoRepository quando informado, para
    /// o login enxergar o responsavel recem-cadastrado. Conta as gravacoes e pode simular falha do banco.
    /// </summary>
    public class FakeEmpresaRepository : IEmpresaRepository
    {
        private readonly FakeAutenticacaoRepository _armazenamento;
        private readonly object _trava = new object();
        private int _insercoes;

        public FakeEmpresaRepository(FakeAutenticacaoRepository? armazenamento = null)
        {
            _armazenamento = armazenamento ?? new FakeAutenticacaoRepository();
        }

        /// <summary>Quando nao for null, toda consulta de unicidade lanca esta excecao (simula falha do banco).</summary>
        public Exception? ExcecaoAoConsultar { get; set; }

        /// <summary>Quantas vezes InsertComResponsavelAsync foi chamado (a gravacao unica conta uma vez por cadastro).</summary>
        public int Insercoes
        {
            get
            {
                lock (_trava)
                {
                    return _insercoes;
                }
            }
        }

        /// <summary>Empresas gravadas e semeadas (ativas, desativadas, operadoras ou nao).</summary>
        public IReadOnlyList<Empresa> Empresas => _armazenamento.Empresas;

        /// <summary>Funcionarios gravados e semeados, na mesma lista que o login le.</summary>
        public IReadOnlyList<Funcionario> Funcionarios => _armazenamento.Funcionarios;

        /// <summary>Armazenamento compartilhado de login (empresas e funcionarios).</summary>
        public FakeAutenticacaoRepository Armazenamento => _armazenamento;

        /// <summary>Semeia uma empresa (ativa, desativada, operadora ou nao). Se o id for 0, gera um.</summary>
        public void AdicionarEmpresa(Empresa empresa)
        {
            _armazenamento.AdicionarEmpresa(empresa);
        }

        /// <summary>Semeia um funcionario com a empresa informada no IdEmpresa.</summary>
        public void AdicionarFuncionario(Funcionario funcionario)
        {
            _armazenamento.AdicionarFuncionario(funcionario);
        }

        /// <summary>Indica se existe empresa ativa (DisabledAt nulo) com o CNPJ informado.</summary>
        public Task<bool> ExistsByCnpjAsync(string cnpj)
        {
            Lancar();

            bool existe = _armazenamento.Empresas.Any(x => x.Cnpj == cnpj && x.DisabledAt == null);

            return Task.FromResult(existe);
        }

        /// <summary>Indica se existe empresa ativa (DisabledAt nulo) com o e-mail informado (em minusculas).</summary>
        public Task<bool> ExistsByEmailAsync(string email)
        {
            Lancar();

            bool existe = _armazenamento.Empresas.Any(x => x.Email == email && x.DisabledAt == null);

            return Task.FromResult(existe);
        }

        /// <summary>Indica se o CNPJ pertence a empresa operadora (Operadora verdadeiro).</summary>
        public Task<bool> ExistsOperadoraByCnpjAsync(string cnpj)
        {
            Lancar();

            bool existe = _armazenamento.Empresas.Any(x => x.Operadora && x.Cnpj == cnpj);

            return Task.FromResult(existe);
        }

        /// <summary>Indica se existe funcionario com DisabledAt nulo e o CPF informado, de qualquer empresa.</summary>
        public Task<bool> ExistsFuncionarioByCpfAsync(string cpf)
        {
            Lancar();

            bool existe = _armazenamento.Funcionarios.Any(x => x.Cpf == cpf && x.DisabledAt == null);

            return Task.FromResult(existe);
        }

        /// <summary>Grava empresa e responsavel como o repositorio real: mesmo DateTime.Now nos dois, empresa levando o responsavel.</summary>
        public Task<Empresa> InsertComResponsavelAsync(Empresa empresa, Funcionario responsavel)
        {
            lock (_trava)
            {
                _insercoes++;
            }

            DateTime agora = DateTime.Now;
            empresa.CreatedAt = agora;
            empresa.UpdatedAt = agora;
            empresa.DisabledAt = null;

            responsavel.CreatedAt = agora;
            responsavel.UpdatedAt = agora;
            responsavel.DisabledAt = null;

            _armazenamento.AdicionarEmpresa(empresa);
            empresa.Funcionarios.Add(responsavel);
            responsavel.Empresa = empresa;
            responsavel.IdEmpresa = empresa.IdEmpresa;
            _armazenamento.AdicionarFuncionario(responsavel);

            return Task.FromResult(empresa);
        }

        /// <summary>Lanca a excecao configurada, se houver (falha do banco).</summary>
        private void Lancar()
        {
            if (ExcecaoAoConsultar != null)
            {
                throw ExcecaoAoConsultar;
            }
        }
    }
}
