using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Caucao na abertura da locacao (P4-BE-04, RF-20; contrato caucao-retirada v1.1). Pre-autoriza o cartao no gateway FORA de
    /// transacao e de trava, confere o retorno, decide pelo estado (so RESERVADA abre, devolvida e confirmada) e compensa so o id
    /// novo que nao foi gravado. Nao grava nada: a linha CAUCAO nasce junto com a locacao, no repositorio. Na leitura da
    /// retirada, preenche os dados da caucao para a tela 16.
    /// </summary>
    public interface ICaucaoRetiradaService
    {
        /// <summary>Verdadeiro quando a retirada exige caucao (Pagamentos:ValorCaucao maior que 0). Com falso, a abertura e a do RF-17.</summary>
        bool CaucaoExigida { get; }

        /// <summary>
        /// Recusas antes do provedor (cartao ausente 400, cliente da reserva ausente 404, regras do lancamento), pre-autorizacao
        /// com CancellationToken.None, conferencia do retorno (divergencia 502 sem cancelar), decisao sobre o estado devolvido e
        /// confirmacao do estado atual por ConsultarAsync. So devolve com RESERVADA devolvida E confirmada. EM_ANALISE e falha da
        /// confirmacao sao compensadas por dentro (so id novo).
        /// </summary>
        Task<CaucaoAberturaRegistro> PreAutorizarAsync(Reserva reserva, LocacaoVO locacaoVO);

        /// <summary>
        /// Compensacao depois de uma RESERVADA que nao foi gravada. Rele sem trava se o mesmo id ja esta gravado numa CAUCAO de
        /// locacao ativa da reserva: se estiver, nao cancela e devolve verdadeiro (quem chama responde 409 de locacao duplicada).
        /// Senao cancela no provedor em melhor esforco, rele de novo para registrar o que precisa de tratamento manual e devolve
        /// falso (quem chama relanca o erro original). Nunca lanca.
        /// </summary>
        Task<bool> CompensarAsync(int idReserva, CaucaoAberturaRegistro caucao);

        /// <summary>
        /// Leitura da retirada (GET): preenche os cinco campos da caucao no retorno ja montado. Sem caucao exigida, campos neutros
        /// e nenhuma consulta ao gateway. Com a locacao ja aberta, os campos da configuracao e os dois indicadores falsos, sem
        /// consulta e sem log. Senao consulta a configuracao do cartao (CancellationToken.None): o modo de simulacao vem da mesma
        /// regra da trava do FAKE; falha vira caucao exigida e indisponivel, com log, sem derrubar a leitura. Nunca devolve
        /// id_transacao, chave publica, nome do provedor nem dado de cartao.
        /// </summary>
        Task PreencherRetiradaAsync(ReturnRetiradaVO retorno);
    }
}
