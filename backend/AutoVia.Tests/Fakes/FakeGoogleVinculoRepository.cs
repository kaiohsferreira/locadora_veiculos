using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// IGoogleVinculoRepository em memoria, com as regras do GoogleVinculoRepository real: so clientes ativos
    /// (DisabledAt nulo), e-mail comparado em minusculas e limitado pela quantidade. Os clientes vem do
    /// FakeAutenticacaoRepository, e a gravacao do sub altera o mesmo objeto que o login le.
    /// Contadores de gravacao e excecoes simuladas (ExcecaoAoBuscar, ExcecaoAoGravar) para os testes.
    /// </summary>
    public class FakeGoogleVinculoRepository : IGoogleVinculoRepository
    {
        private readonly FakeAutenticacaoRepository _titulares;

        public FakeGoogleVinculoRepository(FakeAutenticacaoRepository titulares)
        {
            _titulares = titulares;
        }

        /// <summary>Quando nao for null, as buscas lancam esta excecao (simula falha do banco).</summary>
        public Exception? ExcecaoAoBuscar { get; set; }

        /// <summary>Quando nao for null, a gravacao do sub lanca esta excecao (simula falha do banco).</summary>
        public Exception? ExcecaoAoGravar { get; set; }

        /// <summary>Quantas gravacoes de sub foram feitas com sucesso (para provar que o vinculo nao foi gravado).</summary>
        public int Gravacoes { get; private set; }

        /// <summary>Busca o cliente ativo com esta conta Google (sub); o mais antigo se houver mais de um.</summary>
        public Task<Cliente?> FindClienteByGoogleSubAsync(string googleSub)
        {
            LancarSeConfigurado(ExcecaoAoBuscar);

            Cliente? cliente = _titulares.Clientes
                .Where(x => x.GoogleSub == googleSub && x.DisabledAt == null)
                .OrderBy(x => x.IdCliente)
                .FirstOrDefault();

            return Task.FromResult(cliente);
        }

        /// <summary>Busca os clientes ativos com este e-mail (minusculas), no maximo a quantidade informada.</summary>
        public Task<List<Cliente>> FindClientesByEmailAsync(string email, int quantidade)
        {
            LancarSeConfigurado(ExcecaoAoBuscar);

            string emailMinusculo = email.Trim().ToLowerInvariant();

            List<Cliente> clientes = _titulares.Clientes
                .Where(x => x.DisabledAt == null && x.Email.Trim().ToLowerInvariant() == emailMinusculo)
                .OrderBy(x => x.IdCliente)
                .Take(quantidade)
                .ToList();

            return Task.FromResult(clientes);
        }

        /// <summary>Indica se outro cliente ativo ja usa esta conta Google.</summary>
        public Task<bool> ExistsClienteByGoogleSubAsync(string googleSub, int idClienteIgnorado)
        {
            LancarSeConfigurado(ExcecaoAoBuscar);

            bool existe = _titulares.Clientes.Any(x => x.GoogleSub == googleSub && x.IdCliente != idClienteIgnorado && x.DisabledAt == null);

            return Task.FromResult(existe);
        }

        /// <summary>Grava o sub no cliente ativo e atualiza UpdatedAt; false se o cliente nao existe ou esta desativado.</summary>
        public Task<bool> UpdateGoogleSubAsync(int idCliente, string googleSub)
        {
            LancarSeConfigurado(ExcecaoAoGravar);

            Cliente? cliente = _titulares.Clientes.FirstOrDefault(x => x.IdCliente == idCliente && x.DisabledAt == null);

            if (cliente == null)
            {
                return Task.FromResult(false);
            }

            cliente.GoogleSub = googleSub;
            cliente.UpdatedAt = DateTime.Now;
            Gravacoes++;

            return Task.FromResult(true);
        }

        private static void LancarSeConfigurado(Exception? excecao)
        {
            if (excecao != null)
            {
                throw excecao;
            }
        }
    }
}
