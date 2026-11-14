using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Utils;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Aprovacao do cadastro do cliente por locadora (T-BE-06, RF-74 a RF-83). A locadora vem sempre do contexto
    /// (ou do veiculo lido pelo servidor, na reserva). O cliente nunca cria vinculo por outro caminho que nao a
    /// tentativa propria de reserva.
    /// </summary>
    public interface IClienteLocadoraService
    {
        /// <summary>
        /// Exige o cadastro APROVADO do cliente na locadora antes da reserva. Sem vinculo e chamado pelo proprio cliente,
        /// grava o pedido PENDENTE e lanca 409 de analise. Lanca 404 se a locadora nao existe, esta inativa ou desativada.
        /// CHAMADA ANTES DA TRANSACAO da reserva: o PENDENTE precisa ser confirmado no banco antes do 409. Se for chamada
        /// com transacao aberta, o repositorio lanca InvalidOperationException (ErroVinculoDentroDeTransacao) e nada e gravado.
        /// </summary>
        Task ExigirCadastroAprovadoAsync(int idCliente, int idEmpresa);

        /// <summary>Registra a decisao da locadora do contexto sobre o cadastro do cliente (APROVADO ou RECUSADO).</summary>
        Task DecidirAsync(int idCliente, VerificacaoCadastroVO verificacaoVO);

        /// <summary>Lista paginada das locadoras em que o cliente do contexto tem vinculo (sem a justificativa da recusa).</summary>
        Task<PaginaVO<ReturnClienteLocadoraVO>> GetMinhasAsync(FiltroPaginacaoVO filtro);
    }
}
