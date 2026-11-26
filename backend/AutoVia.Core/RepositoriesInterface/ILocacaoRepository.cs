using AutoVia.Core.Entities.PgSql;
using System;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>Persistencia e transicoes atomicas do fluxo de locacao.</summary>
    public interface ILocacaoRepository
    {
        Task<Reserva?> FindReservaParaRetiradaAsync(int idReserva);

        /// <summary>Reserva ativa da locadora do contexto, com o veiculo e o cliente (pagador da caucao), sem rastreamento.</summary>
        Task<Reserva?> FindReservaByIdAsync(int idReserva);

        Task<Funcionario?> FindActiveFuncionarioByIdAsync(int idFuncionario);
        Task<bool> ExistsActiveByReservaAsync(int idReserva);

        /// <summary>
        /// Abre a locacao na transacao com as travas (veiculo, depois reserva) e as reconferencias. Com caucao, grava no MESMO
        /// SaveChanges a locacao com valor_caucao e a linha CAUCAO (pela navegacao Locacao.Pagamentos). Caucao nula = RF-17.
        /// </summary>
        Task<Locacao> StartFromReservaAsync(int idReserva, int idFuncionario, DateTime dataRetirada, CaucaoAberturaRegistro? caucao);

        Task<Locacao?> FindByIdAsync(int idLocacao);

        /// <summary>
        /// Verdadeiro quando existe locacao ativa da reserva com pagamento CAUCAO nao desativado de mesmo id_transacao. Leitura
        /// SEM trava e com o filtro da locadora do contexto; usada pela compensacao para nunca cancelar a caucao gravada.
        /// </summary>
        Task<bool> ExisteCaucaoGravadaAsync(int idReserva, string idTransacao);

        /// <summary>
        /// Locacao ativa da locadora do contexto com o veiculo, sem rastreamento e sem trava (leitura da devolucao). Nulo = 404.
        /// </summary>
        Task<Locacao?> FindParaEncerramentoAsync(int idLocacao);

        /// <summary>Pagamentos ativos da locacao (locacao ativa), sem rastreamento e sem trava.</summary>
        Task<List<Pagamento>> ListarPagamentosAtivosDaLocacaoAsync(int idLocacao);

        /// <summary>Cobrancas extras ativas da locacao, de qualquer apuracao, sem rastreamento e sem trava.</summary>
        Task<List<CobrancaExtra>> ListarCobrancasExtrasAtivasDaLocacaoAsync(int idLocacao);

        /// <summary>
        /// Transacao final do encerramento (devolucao-encerramento v4, passo 10): trava veiculo, reserva, locacao e pagamentos na
        /// ordem global, reconfere sob trava e grava tudo num SaveChanges. Repeticao (locacao ja FINALIZADA) devolve Criada = falso.
        /// </summary>
        Task<ResultadoEncerramento> EncerrarAtomicamenteAsync(EncerramentoRegistro registro);

        /// <summary>Total de locacoes em andamento (ou so as atrasadas) da locadora do contexto, de qualquer data; sem rastreamento.</summary>
        Task<int> ContarDevolucoesPendentesAsync(bool somenteAtrasadas, DateTime agora);

        /// <summary>Pagina das locacoes em andamento (ou so as atrasadas), ordenada pela devolucao prevista efetiva e depois pelo id.</summary>
        Task<List<Locacao>> ListarDevolucoesPendentesAsync(bool somenteAtrasadas, DateTime agora, int deslocamento, int quantidade);
    }
}
