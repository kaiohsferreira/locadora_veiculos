using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// IClienteRepository em memoria, com as mesmas regras do ClienteRepository real:
    /// ignora desativados em todas as buscas, filtra por status quando informado,
    /// ordena por DataCadastro e IdCliente e pagina por deslocamento/quantidade.
    /// Armazenados guarda tambem os desativados, para os testes conferirem o que foi gravado.
    /// </summary>
    public class FakeClienteRepository : IClienteRepository
    {
        private int _proximoId = 1;

        /// <summary>Todos os clientes gravados (ativos e desativados). Os testes leem daqui.</summary>
        public List<Cliente> Armazenados { get; } = new List<Cliente>();

        /// <summary>Quando nao for null, FindByIdAsync lanca esta excecao (simula falha inesperada do banco).</summary>
        public Exception? ExcecaoAoBuscarPorId { get; set; }

        /// <summary>Quando nao for null, InsertAsync lanca esta excecao (simula falha ao gravar).</summary>
        public Exception? ExcecaoAoInserir { get; set; }

        /// <summary>Quando nao for null, as consultas de existencia (CPF, e-mail, CNH) lancam esta excecao (simula falha do banco).</summary>
        public Exception? ExcecaoAoConsultarExistencia { get; set; }

        /// <summary>Quantas consultas de existencia (CPF, e-mail ou CNH) foram feitas. Os testes conferem que a entrada malformada nao chegou a consultar.</summary>
        public int ConsultasDeExistencia { get; private set; }

        /// <summary>Ultimo e-mail recebido por ExistsByEmailAsync, exatamente como o servico o passou (sem normalizacao feita aqui). Os testes conferem a normalizacao do servico.</summary>
        public string? UltimoEmailConsultado { get; private set; }

        /// <summary>Semeia um cliente (ativo ou desativado) sem passar pelo servico. Se o id for 0, gera um.</summary>
        public void Adicionar(Cliente cliente)
        {
            if (cliente.IdCliente == 0)
            {
                cliente.IdCliente = _proximoId;
            }

            if (cliente.IdCliente >= _proximoId)
            {
                _proximoId = cliente.IdCliente + 1;
            }

            Armazenados.Add(Copiar(cliente));
        }

        /// <summary>Lista os clientes ativos (com filtro de status opcional), ordenados por DataCadastro e IdCliente, na pagina pedida.</summary>
        public Task<List<Cliente>> FindAllAsync(StatusCadastroCliente? status, int deslocamento, int quantidade)
        {
            List<Cliente> clientes = FiltrarAtivos(status)
                .OrderBy(x => x.DataCadastro)
                .ThenBy(x => x.IdCliente)
                .Skip(deslocamento)
                .Take(quantidade)
                .Select(x => Copiar(x))
                .ToList();

            return Task.FromResult(clientes);
        }

        /// <summary>Conta os clientes ativos com o mesmo filtro de status da listagem.</summary>
        public Task<int> CountAsync(StatusCadastroCliente? status)
        {
            int total = FiltrarAtivos(status).Count();

            return Task.FromResult(total);
        }

        /// <summary>Busca um cliente ativo pelo id; null se nao existir ou estiver desativado.</summary>
        public Task<Cliente?> FindByIdAsync(int idCliente)
        {
            if (ExcecaoAoBuscarPorId != null)
            {
                throw ExcecaoAoBuscarPorId;
            }

            Cliente? encontrado = Armazenados.FirstOrDefault(x => x.IdCliente == idCliente && x.DisabledAt == null);
            Cliente? copia = null;

            if (encontrado != null)
            {
                copia = Copiar(encontrado);
            }

            return Task.FromResult(copia);
        }

        /// <summary>Verifica se existe cliente ativo com o mesmo CPF.</summary>
        public Task<bool> ExistsByCpfAsync(string cpf)
        {
            RegistrarConsultaDeExistencia();

            bool existe = Armazenados.Any(x => x.DisabledAt == null && x.Cpf == cpf);

            return Task.FromResult(existe);
        }

        /// <summary>Verifica se existe cliente ativo com a mesma CNH.</summary>
        public Task<bool> ExistsByCnhNumeroAsync(string cnhNumero)
        {
            RegistrarConsultaDeExistencia();

            bool existe = Armazenados.Any(x => x.DisabledAt == null && x.CnhNumero == cnhNumero);

            return Task.FromResult(existe);
        }

        /// <summary>Verifica se existe cliente ativo com o mesmo e-mail, sem diferenciar maiusculas nem bordas (como o banco faz com lower(email)).</summary>
        public Task<bool> ExistsByEmailAsync(string email)
        {
            RegistrarConsultaDeExistencia();
            UltimoEmailConsultado = email;

            string emailNormalizado = email.Trim().ToLowerInvariant();
            bool existe = Armazenados.Any(x => x.DisabledAt == null && x.Email.Trim().ToLowerInvariant() == emailNormalizado);

            return Task.FromResult(existe);
        }

        /// <summary>Conta a consulta de existencia e, se configurado, lanca a excecao simulada antes de responder.</summary>
        private void RegistrarConsultaDeExistencia()
        {
            ConsultasDeExistencia++;

            if (ExcecaoAoConsultarExistencia != null)
            {
                throw ExcecaoAoConsultarExistencia;
            }
        }

        /// <summary>Grava um novo cliente com id gerado, DataCadastro, CreatedAt e UpdatedAt iguais a DateTime.Now e DisabledAt nulo.</summary>
        public Task<Cliente> InsertAsync(Cliente cliente)
        {
            if (ExcecaoAoInserir != null)
            {
                throw ExcecaoAoInserir;
            }

            DateTime agora = DateTime.Now;

            cliente.IdCliente = _proximoId;
            _proximoId++;
            cliente.DataCadastro = agora;
            cliente.CreatedAt = agora;
            cliente.UpdatedAt = agora;
            cliente.DisabledAt = null;

            Armazenados.Add(Copiar(cliente));

            return Task.FromResult(cliente);
        }

        private IEnumerable<Cliente> FiltrarAtivos(StatusCadastroCliente? status)
        {
            IEnumerable<Cliente> ativos = Armazenados.Where(x => x.DisabledAt == null);

            if (status.HasValue)
            {
                ativos = ativos.Where(x => x.StatusCadastro == status.Value);
            }

            return ativos;
        }

        /// <summary>Copia os campos do cliente, para que o chamador nao altere o armazenamento sem passar pelo repositorio.</summary>
        private static Cliente Copiar(Cliente origem)
        {
            Cliente copia = new Cliente
            {
                IdCliente = origem.IdCliente,
                Nome = origem.Nome,
                Cpf = origem.Cpf,
                DataNascimento = origem.DataNascimento,
                Email = origem.Email,
                Telefone = origem.Telefone,
                CnhNumero = origem.CnhNumero,
                CnhCategoria = origem.CnhCategoria,
                CnhValidade = origem.CnhValidade,
                Logradouro = origem.Logradouro,
                Numero = origem.Numero,
                Complemento = origem.Complemento,
                Bairro = origem.Bairro,
                Cidade = origem.Cidade,
                Uf = origem.Uf,
                Cep = origem.Cep,
                DataCadastro = origem.DataCadastro,
                StatusCadastro = origem.StatusCadastro,
                SenhaHash = origem.SenhaHash,
                CreatedAt = origem.CreatedAt,
                UpdatedAt = origem.UpdatedAt,
                DisabledAt = origem.DisabledAt
            };

            return copia;
        }
    }
}
