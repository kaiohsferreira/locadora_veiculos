using AutoVia.Communication.ViewObjects.Pagamento;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Geracao ou reabertura do PIX de uma cobranca (RF-32, gaveta da tela 30 e painel). Quem chama so passa o id: valor,
    /// pagador, locadora e identificador do provedor saem do servidor. Um PIX valido gravado e reaberto sem chamar o provedor.
    /// </summary>
    public interface ICobrancaPixService
    {
        /// <summary>Gera ou reabre o PIX de uma cobranca do cliente (posse pelo cliente do token). Nao devolve o identificador do provedor.</summary>
        Task<ReturnCobrancaPixVO> GerarOuReabrirDoClienteAsync(int idCliente, int idPagamento);

        /// <summary>Gera ou reabre o PIX de uma cobranca da locadora do contexto (funcionario). Devolve o identificador do provedor.</summary>
        Task<ReturnCobrancaPixFuncionarioVO> GerarOuReabrirDaLocadoraAsync(int idPagamento);
    }
}
