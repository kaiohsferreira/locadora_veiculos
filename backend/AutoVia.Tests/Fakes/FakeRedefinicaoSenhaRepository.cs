using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// IRedefinicaoSenhaRepository em memoria, com as regras do RedefinicaoSenhaRepository real: pendentes
    /// filtram DisabledAt nulo; o titular vem do FakeAutenticacaoRepository (so ativos, como o Include
    /// filtrado do real) e a troca de senha altera o mesmo objeto que o login le.
    /// ExcecaoAoGravar simula falha do banco na gravacao.
    /// </summary>
    public class FakeRedefinicaoSenhaRepository : IRedefinicaoSenhaRepository
    {
        private readonly FakeAutenticacaoRepository _titulares;
        private readonly List<RedefinicaoSenha> _redefinicoes = new List<RedefinicaoSenha>();
        private int _proximoId = 1;

        public FakeRedefinicaoSenhaRepository(FakeAutenticacaoRepository titulares)
        {
            _titulares = titulares;
        }

        /// <summary>Todos os links gravados (inclusive os desativados), para os testes conferirem hash, validade e uso.</summary>
        public IReadOnlyList<RedefinicaoSenha> Redefinicoes => _redefinicoes;

        /// <summary>Quantas vezes ConcluirRedefinicaoAsync foi concluida com sucesso.</summary>
        public int Concluidas { get; private set; }

        /// <summary>Quando nao for null, a gravacao lanca esta excecao (simula falha do banco).</summary>
        public Exception? ExcecaoAoGravar { get; set; }

        /// <summary>Quantas vezes qualquer metodo do repositorio foi chamado. Prova que a requisicao nao consulta os links.</summary>
        public int TotalChamadas { get; private set; }

        /// <summary>Conta os links do titular criados desde a data, inclusive os desativados.</summary>
        public Task<int> CountSolicitacoesDesdeWithDisabledsAsync(int? idCliente, int? idFuncionario, DateTime desde)
        {
            TotalChamadas++;

            int total = _redefinicoes.Count(x => x.CreatedAt >= desde && CombinaTitular(x, idCliente, idFuncionario));

            return Task.FromResult(total);
        }

        /// <summary>Desativa os pendentes do titular e grava a nova solicitacao com id, CreatedAt e UpdatedAt.</summary>
        public Task<RedefinicaoSenha> InsertInvalidandoAnterioresAsync(RedefinicaoSenha nova)
        {
            TotalChamadas++;
            LancarSeConfigurado();

            DateTime agora = DateTime.Now;

            foreach (RedefinicaoSenha pendente in _redefinicoes.Where(x => x.DisabledAt == null && CombinaTitular(x, nova.IdCliente, nova.IdFuncionario)))
            {
                pendente.Disable();
            }

            nova.IdRedefinicaoSenha = _proximoId;
            _proximoId++;
            nova.CreatedAt = agora;
            nova.UpdatedAt = agora;
            nova.DisabledAt = null;

            _redefinicoes.Add(nova);

            return Task.FromResult(nova);
        }

        /// <summary>Busca o link ativo pelo hash; so devolve se o titular estiver ativo, e carrega o titular.</summary>
        public async Task<RedefinicaoSenha?> FindPendenteByTokenHashAsync(string tokenHash)
        {
            TotalChamadas++;
            LancarSeConfigurado();

            RedefinicaoSenha? redefinicao = _redefinicoes.FirstOrDefault(x => x.TokenHash == tokenHash && x.DisabledAt == null);

            if (redefinicao == null)
            {
                return null;
            }

            if (redefinicao.IdCliente.HasValue)
            {
                Cliente? cliente = await _titulares.FindClienteByIdAsync(redefinicao.IdCliente.Value);

                if (cliente == null)
                {
                    return null;
                }

                redefinicao.Cliente = cliente;
            }

            if (redefinicao.IdFuncionario.HasValue)
            {
                Funcionario? funcionario = await _titulares.FindFuncionarioByIdAsync(redefinicao.IdFuncionario.Value);

                if (funcionario == null)
                {
                    return null;
                }

                redefinicao.Funcionario = funcionario;
            }

            return redefinicao;
        }

        /// <summary>Troca a senha do titular carregado e desativa este link e os pendentes da conta.</summary>
        public Task ConcluirRedefinicaoAsync(RedefinicaoSenha redefinicao, string novoSenhaHash)
        {
            TotalChamadas++;
            LancarSeConfigurado();

            DateTime agora = DateTime.Now;

            if (redefinicao.Cliente != null)
            {
                redefinicao.Cliente.SenhaHash = novoSenhaHash;
                // Espelha o repositorio real (decisao C2): a troca de senha apaga o vinculo com o Google.
                redefinicao.Cliente.GoogleSub = null;
                redefinicao.Cliente.UpdatedAt = agora;
            }
            else if (redefinicao.Funcionario != null)
            {
                redefinicao.Funcionario.SenhaHash = novoSenhaHash;
                redefinicao.Funcionario.UpdatedAt = agora;
            }

            redefinicao.Disable();

            foreach (RedefinicaoSenha pendente in _redefinicoes.Where(x => x.DisabledAt == null && CombinaTitular(x, redefinicao.IdCliente, redefinicao.IdFuncionario)))
            {
                pendente.Disable();
            }

            Concluidas++;

            return Task.CompletedTask;
        }

        private static bool CombinaTitular(RedefinicaoSenha redefinicao, int? idCliente, int? idFuncionario)
        {
            if (idCliente.HasValue)
            {
                return redefinicao.IdCliente == idCliente.Value;
            }

            if (idFuncionario.HasValue)
            {
                return redefinicao.IdFuncionario == idFuncionario.Value;
            }

            return false;
        }

        private void LancarSeConfigurado()
        {
            if (ExcecaoAoGravar != null)
            {
                throw ExcecaoAoGravar;
            }
        }
    }
}
